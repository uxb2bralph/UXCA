
using ContractHome.Models.Helper;
using Microsoft.Extensions.Options;

namespace ContractHome.Services.Jobs
{
    public class JobNotifyTerminationContract : JobAbstract, IRecurringJob
    {
        private readonly ContractServices? _contractServices;

        public JobNotifyTerminationContract(IServiceProvider serviceProvider, IOptions<List<JobSetting>> jobSettings)
            : base(jobSettings)
        {
            _contractServices = serviceProvider.CreateScope().ServiceProvider.GetService<ContractServices>();
        }

        public async Task Execute()
        {
            if (IsEnable) {
                // 通知合約終止
                await _contractServices.TerminationContractFlow();
            }
        }
    }
}
