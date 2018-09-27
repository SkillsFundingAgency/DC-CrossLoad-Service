using System;
using System.Collections.Generic;
using System.Linq;
using DC_CrossLoad_Service.Interface;
using ESFA.DC.DateTimeProvider.Interface;
using ESFA.DC.JobQueueManager;
using ESFA.DC.JobQueueManager.Data;
using ESFA.DC.JobQueueManager.Data.Entities;
using ESFA.DC.Jobs.Model;
using ESFA.DC.JobStatus.Interface;
using Microsoft.EntityFrameworkCore;

namespace DC_CrossLoad_Service.Service
{
    public sealed class CrossLoadActiveJobService : ICrossLoadActiveJobService
    {
        private readonly DbContextOptions _contextOptions;

        private readonly IDateTimeProvider _dateTimeProvider;

        private readonly int _numberOfMinutesBeforeFail;

        private readonly int _crossLoadingStatus;

        public CrossLoadActiveJobService(DbContextOptions contextOptions, IDateTimeProvider dateTimeProvider, int numberOfMinutesBeforeFail)
        {
            _contextOptions = contextOptions;
            _dateTimeProvider = dateTimeProvider;
            _numberOfMinutesBeforeFail = numberOfMinutesBeforeFail;

            _crossLoadingStatus = (int)JobStatusType.MovedForProcessing;
        }

        public IEnumerable<Job> GetStuckJobs()
        {
            DateTime utcNow = _dateTimeProvider.GetNowUtc();
            List<Job> jobs = new List<Job>();

            using (var context = new JobQueueDataContext(_contextOptions))
            {
                IEnumerable<JobEntity> jobEntities = context.Jobs
                    .Where(x => x.CrossLoadingStatus != null && x.CrossLoadingStatus.Value == _crossLoadingStatus)
                    .AsEnumerable()
                    .Where(x => x.DateTimeUpdatedUtc != null && x.DateTimeUpdatedUtc.Value.AddMinutes(_numberOfMinutesBeforeFail) < utcNow);
                foreach (JobEntity jobEntity in jobEntities)
                {
                    jobs.Add(JobConverter.Convert(jobEntity));
                }
            }

            return jobs;
        }
    }
}
