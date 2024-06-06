using CommonLib.Core.Utility;
using ContractHome.Models.Helper;
using Org.BouncyCastle.Asn1.Ocsp;

namespace ContractHome.Services.Jobs
{
    public class TestJob : IRecurringJob
    {
        private ContractServices? _contractServices;

        public TestJob(IServiceProvider serviceProvider)
        {
            _contractServices = serviceProvider.CreateScope().ServiceProvider.GetService<ContractServices>(); ;
        }


        //every minute
        public string CronExpression => "*/1 * * * *";

        public string JobId => "TestJobId";

        public async Task Execute()
        {
            //File.WriteAllText(Path.Combine(FileLogger.Logger.LogDailyPath, $"IRecurringJob-{Guid.NewGuid()}.json"), "ttt");
            _contractServices.JobTest();
        }
    }
}
