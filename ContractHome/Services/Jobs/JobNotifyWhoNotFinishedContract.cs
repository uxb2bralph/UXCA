using CommonLib.Core.Utility;
using ContractHome.Models.Helper;
using Org.BouncyCastle.Asn1.Ocsp;

namespace ContractHome.Services.Jobs
{
    public class JobNotifyWhoNotFinishedContract : IRecurringJob
    {
        private ContractServices? _contractServices;

        public JobNotifyWhoNotFinishedContract(IServiceProvider serviceProvider)
        {
            _contractServices = serviceProvider.CreateScope().ServiceProvider.GetService<ContractServices>();
        }



        public string CronExpression => "*/1 * * * *";        //every minute
        //public string CronExpression => "10 9 * * *";        //every weekday at 09:10

        public string JobId => "NotifyWhoNotFinishedContract";

        public async Task Execute()
        {
            _contractServices.NotifyWhoNotFinishedContract();
        }
    }
}
