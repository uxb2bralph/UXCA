using CommonLib.Core.Utility;
using ContractHome.Models.Helper;
using Hangfire;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Asn1.Ocsp;

namespace ContractHome.Services.Jobs
{
    public class JobNotifyWhoNotFinishedDoc : JobAbstract, IRecurringJob
    {
        private readonly ContractServices? _contractServices;

        public JobNotifyWhoNotFinishedDoc(IServiceProvider serviceProvider, IOptions<List<JobSetting>> jobSettings)
            :base(jobSettings)
        {
            _contractServices = serviceProvider.CreateScope().ServiceProvider.GetService<ContractServices>();
        }

        public async Task Execute()
        {
            if (IsEnable) { await _contractServices.NotifyWhoNotFinishedDoc(); }
        }
    }
}
