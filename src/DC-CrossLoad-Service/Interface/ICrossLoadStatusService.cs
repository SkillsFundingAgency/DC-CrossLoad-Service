using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.JobStatus.Interface;

namespace DC_CrossLoad_Service.Interface
{
    public interface ICrossLoadStatusService
    {
        Task SendAsync(long jobId, JobStatusType jobStatusType, CancellationToken cancellationToken);
    }
}
