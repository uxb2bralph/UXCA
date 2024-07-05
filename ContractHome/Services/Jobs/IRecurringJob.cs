namespace ContractHome.Services.Jobs
{
    public interface IRecurringJob
    {
        string CronExpression { get; }

        string JobId { get; }

        bool IsEnable { get; }

        Task Execute();
    }
}
