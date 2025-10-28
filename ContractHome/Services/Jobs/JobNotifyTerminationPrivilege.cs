
using ContractHome.Models.Helper;
using Microsoft.Extensions.Options;

namespace ContractHome.Services.Jobs
{
    /// <summary>
    /// 通知合約建立權限終止通知信
    /// </summary>
    public class JobNotifyTerminationPrivilege : JobAbstract, IRecurringJob
    {
        private readonly ContractServices? _contractServices;

        public JobNotifyTerminationPrivilege(IServiceProvider serviceProvider, IOptions<List<JobSetting>> jobSettings) : base(jobSettings)
        {
            _contractServices = serviceProvider.CreateScope().ServiceProvider.GetService<ContractServices>();
        }

        public async Task Execute()
        {
            await _contractServices.NotifyTerminationPrivilege();
        }
    }
}
