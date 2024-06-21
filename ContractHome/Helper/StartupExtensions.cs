using ContractHome.Services.Jobs;
using Hangfire;
using Hangfire.MemoryStorage;
using System.Data.Entity;

namespace ContractHome.Helper
{
    public static class StartupExtensions
    {
        public static JobConfiguration AddJobManager(this IServiceCollection services)
        {
            services.AddHangfire(config =>
            {
                config.UseMemoryStorage();
                //config.UseSqlServerStorage("Data Source=;Initial Catalog = ;User Id=;Password=;Connect Timeout=30;encrypt=false;");
                //config.UseSerilogLogProvider();
            });
            services.AddHangfireServer();
            services.AddSingleton<Services.Jobs.RecurringJobManager>();

            return new JobConfiguration(services);
        }

        public static IApplicationBuilder StartRecurringJobs(this IApplicationBuilder app)
        {
            var manager = app.ApplicationServices.CreateScope().ServiceProvider.GetService<Services.Jobs.RecurringJobManager>();
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
            //services.AddScoped(typeof(IRecurringJob), typeof(TJob));
            services.AddSingleton(typeof(IRecurringJob), typeof(TJob));
            return this;
        }
    }
}
