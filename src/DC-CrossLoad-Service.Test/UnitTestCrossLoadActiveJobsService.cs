using System;
using System.Text;
using System.Threading.Tasks;
using DC_CrossLoad_Service.Interface;
using DC_CrossLoad_Service.Service;
using ESFA.DC.DateTimeProvider.Interface;
using ESFA.DC.JobQueueManager.Data;
using ESFA.DC.JobQueueManager.Data.Entities;
using ESFA.DC.Jobs.Model.Enums;
using ESFA.DC.JobStatus.Interface;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace DC_CrossLoad_Service.Test
{
    public sealed class UnitTestCrossLoadActiveJobsService
    {
        [Theory]
        [InlineData(5, -6, JobStatusType.MovedForProcessing, 1)]
        [InlineData(5, -6, JobStatusType.Completed, 0)]
        [InlineData(5, -4, JobStatusType.MovedForProcessing, 0)]
        public async Task TestRemove(int age, int updateTimeBack, JobStatusType status, int expected)
        {
            DateTime utcNow = DateTime.UtcNow;
            Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>();

            dateTimeProvider.Setup(x => x.GetNowUtc()).Returns(utcNow);

            using (var connection = new SqliteConnection("DataSource=:memory:"))
            {
                connection.Open();
                var options = new DbContextOptionsBuilder<JobQueueDataContext>()
                    .UseSqlite(connection)
                    .Options;

                // Create the schema in the database
                using (var context = new JobQueueDataContext(options))
                {
                    context.Database.EnsureCreated();
                    context.Jobs.Add(new JobEntity
                    {
                        JobId = 1,
                        DateTimeUpdatedUtc = utcNow.AddMinutes(updateTimeBack),
                        CrossLoadingStatus = (short)status,
                        DateTimeSubmittedUtc = utcNow.AddMinutes(updateTimeBack + -1),
                        JobType = (short)JobType.IlrSubmission,
                        NotifyEmail = "a@b.com",
                        Priority = 1,
                        Status = (short)JobStatusType.Completed,
                        RowVersion = Encoding.UTF8.GetBytes("RowVersion"),
                        SubmittedBy = "Tester"
                    });
                    await context.SaveChangesAsync();

                    ICrossLoadActiveJobService crossLoadActiveJobService =
                        new CrossLoadActiveJobService(options, dateTimeProvider.Object, age);
                    var results = crossLoadActiveJobService.GetStuckJobs();
                    results.Should().HaveCount(expected);
                }
            }
        }
    }
}
