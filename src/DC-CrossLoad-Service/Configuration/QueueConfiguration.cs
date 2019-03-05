using System;
using ESFA.DC.Queueing;

namespace DC_CrossLoad_Service.Configuration
{
    public sealed class QueueConfiguration : ESFA.DC.Queueing.QueueConfiguration
    {
        public QueueConfiguration(string connectionString, string queueName, int maxConcurrentCalls, int minimumBackoffSeconds = 5, int maximumBackoffSeconds = 50, int maximumRetryCount = 3, int maximumCallbackTimeoutMinutes = 10)
            : base(connectionString, queueName, maxConcurrentCalls, minimumBackoffSeconds, maximumBackoffSeconds, maximumRetryCount, TimeSpan.FromMinutes(maximumCallbackTimeoutMinutes))
        {
        }
    }
}
