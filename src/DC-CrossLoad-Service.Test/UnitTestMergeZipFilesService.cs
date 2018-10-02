using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using DC_CrossLoad_Service.Service;
using ESFA.DC.IO.Interfaces;
using ESFA.DC.Logging.Interfaces;
using Moq;
using Xunit;

namespace DC_CrossLoad_Service.Test
{
    public sealed class UnitTestMergeZipFilesService
    {
        [Fact]
        public async Task Merge()
        {
            const string file1 = "File_A.zip";
            const string file2 = "File_B.zip";

            MemoryStream savedZip = new MemoryStream();

            Mock<IStreamableKeyValuePersistenceService> streamableKeyValuePersistenceService = new Mock<IStreamableKeyValuePersistenceService>();
            streamableKeyValuePersistenceService
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<string, Stream, CancellationToken>((str, stm, can) => File.Open(str, FileMode.Open, FileAccess.Read, FileShare.Read).CopyTo(stm))
                .Returns(Task.CompletedTask);
            streamableKeyValuePersistenceService.Setup(x =>
                    x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<string, Stream, CancellationToken>((str, stm, can) =>
                {
                    stm.Seek(0, SeekOrigin.Begin);
                    stm.CopyTo(savedZip);
                })
                .Returns(Task.CompletedTask);
            Mock<ILogger> logger = new Mock<ILogger>();

            MergeZipFilesService mergeZipFilesService = new MergeZipFilesService();
            await mergeZipFilesService.Merge(1, file1, file2, streamableKeyValuePersistenceService.Object, logger.Object, CancellationToken.None);

            using (var archive = new ZipArchive(savedZip, ZipArchiveMode.Read, false))
            {
                Assert.Equal(2, archive.Entries.Count);
            }
        }

        [Theory]
        [InlineData("File_A.zip", "")]
        [InlineData("", "File_B.zip")]
        public async Task Copy(string file1, string file2)
        {
            MemoryStream savedZip = new MemoryStream();

            Mock<IStreamableKeyValuePersistenceService> streamableKeyValuePersistenceService = new Mock<IStreamableKeyValuePersistenceService>();
            streamableKeyValuePersistenceService
                .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<string, Stream, CancellationToken>((str, stm, can) => File.Open(str, FileMode.Open, FileAccess.Read, FileShare.Read).CopyTo(stm))
                .Returns(Task.CompletedTask);
            streamableKeyValuePersistenceService.Setup(x =>
                    x.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback<string, Stream, CancellationToken>((str, stm, can) =>
                {
                    stm.Seek(0, SeekOrigin.Begin);
                    stm.CopyTo(savedZip);
                })
                .Returns(Task.CompletedTask);
            Mock<ILogger> logger = new Mock<ILogger>();

            MergeZipFilesService mergeZipFilesService = new MergeZipFilesService();
            await mergeZipFilesService.Merge(1, file1, file2, streamableKeyValuePersistenceService.Object, logger.Object, CancellationToken.None);

            using (var archive = new ZipArchive(savedZip, ZipArchiveMode.Read, false))
            {
                Assert.Equal(1, archive.Entries.Count);
            }
        }

        [Theory]
        [InlineData("10000116/70002/ReportsDC1.zip", "10000116/70002/ReportsDC.zip")]
        [InlineData("10000116/70002/ReportsDC2.zip", "10000116/70002/ReportsDC.zip")]
        [InlineData("ReportsDC3.zip", "ReportsDC.zip")]
        public void GetNewFilename(string inFile, string outFile)
        {
            MergeZipFilesService mergeZipFilesService = new MergeZipFilesService();

            string newFile = mergeZipFilesService.GetNewFilename(inFile);

            Assert.Equal(outFile, newFile);
        }
    }
}
