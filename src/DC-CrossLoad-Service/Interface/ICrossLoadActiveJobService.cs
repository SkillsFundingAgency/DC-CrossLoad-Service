using System.Collections.Generic;
using ESFA.DC.Jobs.Model;

namespace DC_CrossLoad_Service.Interface
{
    public interface ICrossLoadActiveJobService
    {
        IEnumerable<Job> GetStuckJobs();
    }
}
