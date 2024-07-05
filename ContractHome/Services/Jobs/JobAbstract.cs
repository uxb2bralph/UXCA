using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;

namespace ContractHome.Services.Jobs
{
    public abstract class JobAbstract
    {
        private readonly JobSetting _jobSetting;
        protected JobAbstract(IOptions<List<JobSetting>> jobSettings)
        {
            _jobSetting = jobSettings.Value.Where(x => x.JobId.Equals(this.GetType().Name)).FirstOrDefault();
            CronExpression = _jobSetting.CronExpression;
            IsEnable = _jobSetting.Enable;
        }

        public string CronExpression { get; set; }

        public string JobId => $"{this.GetType().Name}";

        public bool IsEnable { get; set; }
    }
}