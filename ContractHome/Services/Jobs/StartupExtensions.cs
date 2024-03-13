using Hangfire;
using Hangfire.MemoryStorage;
using System.Data.Entity;

namespace ContractHome.Services.Jobs
{
    public static class StartupExtensions
    {
        public static JobConfiguration AddJobManager(this IServiceCollection services)
        {
            services.AddHangfire(config =>
            {
                config.UseMemoryStorage();
                //config.UseSerilogLogProvider();
            });
            services.AddHangfireServer();
            services.AddSingleton<RecurringJobManager>();

            return new JobConfiguration(services);
        }

        public static IApplicationBuilder StartRecurringJobs(this IApplicationBuilder app)
        {
            var manager = app.ApplicationServices.CreateScope().ServiceProvider.GetService<RecurringJobManager>();
            manager.Start();
            return app;
        }
    }

    public class JobConfiguration
    {
        private readonly IServiceCollection services;

        internal JobConfiguration(IServiceCollection services)
        {
            this.services = services;
        }

        public JobConfiguration AddrecurringJob<TJob>() where TJob : IRecurringJob
        {
            services.AddSingleton(typeof(IRecurringJob), typeof(TJob));
            return this;
        }
    }
}
