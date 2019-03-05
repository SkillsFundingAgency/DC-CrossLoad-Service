using System.Threading;
using System.Threading.Tasks;
using ESFA.DC.IO.Interfaces;
using ESFA.DC.Logging.Interfaces;

namespace DC_CrossLoad_Service.Interface
{
    public interface IMergeZipFilesService
    {
        Task Merge(
            long jobId,
            string zip1,
            string zip2,
            IStreamableKeyValuePersistenceService streamableKeyValuePersistenceService,
            ILogger logger,
            CancellationToken cancellationToken);
    }
}
