using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DC_CrossLoad_Service.Configuration;
using DC_CrossLoad_Service.Interface;
using ESFA.DC.JobStatus.Interface;

namespace DC_CrossLoad_Service.Service
{
    public sealed class CrossLoadStatusService : ICrossLoadStatusService
    {
        private readonly string _endPointUrl;

        public CrossLoadStatusService(WebApiConfiguration webApiConfiguration)
        {
            _endPointUrl = webApiConfiguration.EndPointUrl;
            _endPointUrl = !_endPointUrl.EndsWith("/") ? $"{_endPointUrl}/cross-loading/Status" : $"{_endPointUrl}cross-loading/Status";
        }

        public async Task SendAsync(long jobId, JobStatusType jobStatusType, CancellationToken cancellationToken)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.PostAsync($"{_endPointUrl}/{jobId}/{jobStatusType}", null, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
