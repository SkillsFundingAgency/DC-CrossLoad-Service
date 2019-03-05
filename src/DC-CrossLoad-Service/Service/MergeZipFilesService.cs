using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using DC_CrossLoad_Service.Interface;
using ESFA.DC.IO.Interfaces;
using ESFA.DC.Logging.Interfaces;

namespace DC_CrossLoad_Service.Service
{
    public sealed class MergeZipFilesService : IMergeZipFilesService
    {
        public async Task Merge(long jobId, string zip1, string zip2, IStreamableKeyValuePersistenceService streamableKeyValuePersistenceService, ILogger logger, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(zip1))
            {
                if (string.IsNullOrEmpty(zip2))
                {
                    logger.LogWarning($"Cross loading can't find any reports for Job Id {jobId}");
                    return;
                }

                await CopyFile(zip2, GetNewFilename(zip2), streamableKeyValuePersistenceService, cancellationToken);
                return;
            }

            if (string.IsNullOrEmpty(zip2))
            {
                await CopyFile(zip1, GetNewFilename(zip1), streamableKeyValuePersistenceService, cancellationToken);
                return;
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    await AddZipContents(zip1, archive, streamableKeyValuePersistenceService, cancellationToken);
                    await AddZipContents(zip2, archive, streamableKeyValuePersistenceService, cancellationToken);
                }

                await streamableKeyValuePersistenceService.SaveAsync(GetNewFilename(zip1), memoryStream, cancellationToken);
            }
        }

        public string GetNewFilename(string zip)
        {
            const string filename = "ReportsDC.zip";
            int lastPos = zip.LastIndexOf('/');
            if (lastPos == -1)
            {
                return filename;
            }

            lastPos++;
            return zip.Substring(0, lastPos) + filename;
        }

        private async Task CopyFile(string inFile, string outFile, IStreamableKeyValuePersistenceService streamableKeyValuePersistenceService, CancellationToken cancellationToken)
        {
            using (var memoryStream = new MemoryStream())
            {
                await streamableKeyValuePersistenceService.GetAsync(inFile, memoryStream, cancellationToken);
                memoryStream.Seek(0, SeekOrigin.Begin);
                await streamableKeyValuePersistenceService.SaveAsync(outFile, memoryStream, cancellationToken);
            }
        }

        private async Task AddZipContents(string zip, ZipArchive archive, IStreamableKeyValuePersistenceService streamableKeyValuePersistenceService, CancellationToken cancellationToken)
        {
            using (var memoryStream = new MemoryStream())
            {
                await streamableKeyValuePersistenceService.GetAsync(zip, memoryStream, cancellationToken);
                memoryStream.Seek(0, SeekOrigin.Begin);
                using (var archiveIn = new ZipArchive(memoryStream, ZipArchiveMode.Read, true))
                {
                    foreach (ZipArchiveEntry zipArchiveEntry in archiveIn.Entries)
                    {
                        ZipArchiveEntry newEntry = archive.CreateEntry(zipArchiveEntry.Name);
                        using (Stream streamOut = newEntry.Open())
                        {
                            using (var streamIn = zipArchiveEntry.Open())
                            {
                                await streamIn.CopyToAsync(streamOut, cancellationToken);
                            }
                        }
                    }
                }
            }
        }
    }
}
