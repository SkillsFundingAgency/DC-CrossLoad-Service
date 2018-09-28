using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DC_CrossLoad_Service.Configuration;
using DC_CrossLoad_Service.Interface;
using ESFA.DC.JobStatus.Interface;
using ESFA.DC.Logging.Interfaces;

namespace DC_CrossLoad_Service.Service
{
    public sealed class CrossLoadStatusService : ICrossLoadStatusService
    {
        private readonly ILogger _logger;

        private readonly string _endPointUrl;

        public CrossLoadStatusService(WebApiConfiguration webApiConfiguration, ILogger logger)
        {
            _logger = logger;
            _endPointUrl = webApiConfiguration.EndPointUrl;
            _endPointUrl = !_endPointUrl.EndsWith("/") ? $"{_endPointUrl}/job/cross-loading/status" : $"{_endPointUrl}job/cross-loading/status";
        }

        public async Task SendAsync(long jobId, JobStatusType jobStatusType, CancellationToken cancellationToken)
        {
            using (HttpClient client = new HttpClient())
            {
                string endPoint = $"{_endPointUrl}/{jobId}/{(int)jobStatusType}";
                _logger.LogDebug($"Calling status api at endpoint '{endPoint}'");
                HttpResponseMessage response = await client.PostAsync(endPoint, null, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
