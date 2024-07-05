namespace ContractHome.Services.Jobs
{
    public class JobSetting
    {
        public string JobId { get; set; }
        public string CronExpression { get; set; }

        public bool Enable { get; set; }
    }

}
