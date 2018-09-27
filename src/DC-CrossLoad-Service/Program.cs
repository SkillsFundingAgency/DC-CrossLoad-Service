using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DC_CrossLoad_Service.Configuration;
using DC_CrossLoad_Service.Interface;
using DC_CrossLoad_Service.Service;
using ESFA.DC.CrossLoad.Dto;
using ESFA.DC.DateTimeProvider;
using ESFA.DC.DateTimeProvider.Interface;
using ESFA.DC.Jobs.Model;
using ESFA.DC.JobStatus.Interface;
using ESFA.DC.Logging;
using ESFA.DC.Logging.Config;
using ESFA.DC.Logging.Config.Interfaces;
using ESFA.DC.Logging.Enums;
using ESFA.DC.Logging.Interfaces;
using ESFA.DC.Queueing;
using ESFA.DC.Queueing.Interface;
using ESFA.DC.Queueing.Interface.Configuration;
using ESFA.DC.Serialization.Interfaces;
using ESFA.DC.Serialization.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ExecutionContext = ESFA.DC.Logging.ExecutionContext;
using QueueConfiguration = DC_CrossLoad_Service.Configuration.QueueConfiguration;

namespace DC_CrossLoad_Service
{
    public static class Program
    {
#if DEBUG
        private const string ConfigFile = "privatesettings.json";
#else
        private const string ConfigFile = "appsettings.json";
#endif

        private static ICrossLoadStatusService crossLoadStatusService;

        private static ICrossLoadActiveJobService crossLoadActiveJobService;

        private static ILogger logger;

        private static Timer timer;

        private static int failJobFrequency;

        private static int jobAgeToFail;

        public static void Main(string[] args)
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(ConfigFile);

            IConfiguration configuration = configBuilder.Build();
            failJobFrequency = GetConfigItemAsInt(configuration, "numberOfMinutesCheckFail", 60);
            jobAgeToFail = GetConfigItemAsInt(configuration, "numberOfMinutesBeforeFail", 240);

            IQueueConfiguration queueConfiguration = new QueueConfiguration(configuration["queueConnectionString"], configuration["queueName"], 1);
            WebApiConfiguration webApiConfiguration = new WebApiConfiguration(configuration["jobSchedulerApiEndPoint"]);
            ISerializationService serializationService = new JsonSerializationService();
            IDateTimeProvider dateTimeProvider = new DateTimeProvider();
            IApplicationLoggerSettings applicationLoggerOutputSettings = new ApplicationLoggerSettings
            {
                ApplicationLoggerOutputSettingsCollection = new List<IApplicationLoggerOutputSettings>
                {
                    new MsSqlServerApplicationLoggerOutputSettings
                    {
                        ConnectionString = configuration["loggerConnectionString"],
                        MinimumLogLevel = LogLevel.Information
                    },
                    new ConsoleApplicationLoggerOutputSettings
                    {
                        MinimumLogLevel = LogLevel.Information
                    }
                },
                TaskKey = "Cross Loader",
                EnableInternalLogs = true,
                JobId = "Cross Loader Service",
                MinimumLogLevel = LogLevel.Information
            };
            IExecutionContext executionContext = new ExecutionContext
            {
                JobId = "Cross Loader Service",
                TaskKey = "Cross Loader"
            };
            logger = new SeriLogger(applicationLoggerOutputSettings, executionContext);

            DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlServer(
                configuration["jobQueueManagerConnectionString"],
                options => options.EnableRetryOnFailure(3, TimeSpan.FromSeconds(3), new List<int>()));

            crossLoadStatusService = new CrossLoadStatusService(webApiConfiguration);
            crossLoadActiveJobService = new CrossLoadActiveJobService(optionsBuilder.Options, dateTimeProvider, jobAgeToFail);

            IQueueSubscriptionService<MessageCrossLoadDcftToDctDto> queueSubscriptionService = new QueueSubscriptionService<MessageCrossLoadDcftToDctDto>(queueConfiguration, serializationService, logger);

            logger.LogInfo("Cross Loader service subscribing to queue");
            queueSubscriptionService.Subscribe(Callback, CancellationToken.None);

            logger.LogInfo("Cross Loader service initialising crashed jobs timer");
            timer = new Timer(FindCrashedJobs, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(Timeout.Infinite));

            logger.LogInfo("Started Cross Loader Service!");

            ManualResetEvent oSignalEvent = new ManualResetEvent(false);
            oSignalEvent.WaitOne();
        }

        /// <summary>
        /// Called by the timer to check if jobs have been stuck for too long, and if so, fail them.
        /// </summary>
        /// <param name="state">Not used.</param>
        private static void FindCrashedJobs(object state)
        {
            try
            {
                IEnumerable<Job> failedJobs = crossLoadActiveJobService.GetStuckJobs();
                foreach (Job failedJob in failedJobs)
                {
                    Callback(
                        new MessageCrossLoadDcftToDctDto
                        {
                            JobId = failedJob.JobId,
                            DcftJobId = "NA",
                            ErrorMessage =
                                $"Cross load job has been marked as failed as it has not changed state within the configured timeout of {jobAgeToFail} minutes"
                        },
                        new Dictionary<string, object>(),
                        CancellationToken.None).Wait();
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Cross loading error in find crashed jobs logic", ex);
            }
            finally
            {
                timer.Change(TimeSpan.FromMinutes(failJobFrequency), TimeSpan.FromMilliseconds(Timeout.Infinite));
            }
        }

        /// <summary>
        /// Queue callback to complete or fail jobs based on the message contents.
        /// </summary>
        /// <param name="message">The incoming message.</param>
        /// <param name="messageProperties">The message properties.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Object representing whether the job status call succeeded.</returns>
        private static async Task<IQueueCallbackResult> Callback(MessageCrossLoadDcftToDctDto message, IDictionary<string, object> messageProperties, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInfo($"Cross loading Job Id {message.JobId} is matched with DC Job Id of {message.DcftJobId}");

                if (string.IsNullOrEmpty(message.ErrorMessage))
                {
                    logger.LogInfo($"Cross loading successful for Job Id {message.JobId}");
                    await crossLoadStatusService.SendAsync(message.JobId, JobStatusType.Completed, cancellationToken);
                }
                else
                {
                    logger.LogWarning($"Cross loading failed for Job Id {message.JobId} because of {message.ErrorMessage}");
                    await crossLoadStatusService.SendAsync(message.JobId, JobStatusType.Failed, cancellationToken);
                }

                return new QueueCallbackResult(true, null);
            }
            catch (Exception ex)
            {
                logger.LogError($"Cross loading failed to post status update for Job Id {message.JobId}", ex);
                return new QueueCallbackResult(false, ex);
            }
        }

        /// <summary>
        /// Returns a config item as an int, returns default value on error condition.
        /// </summary>
        /// <param name="configuration">Configuration object.</param>
        /// <param name="configItem">The config item to read.</param>
        /// <param name="def">The default value to use on error condition.</param>
        /// <returns>The int value to use.</returns>
        private static int GetConfigItemAsInt(IConfiguration configuration, string configItem, int def)
        {
            try
            {
                string val = configuration[configItem];
                if (string.IsNullOrEmpty(val) || !int.TryParse(val, out var intVal))
                {
                    return def;
                }

                return intVal;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to read {configItem} from config and convert to int, returning and using default {def}", ex);
                return def;
            }
        }
    }
}
