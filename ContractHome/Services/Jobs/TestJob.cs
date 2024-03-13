namespace ContractHome.Services.Jobs
{
    public class TestJob : IRecurringJob
    {
        public string CronExpression => "*/1 * * * *";

        public string JobId => "TestJobId";

        public Task Execute()
        {
            return new Task(()=>Console.WriteLine("this is Test."));
        }
    }
}
