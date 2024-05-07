using Memphis.Jobs.Jobs;
using Quartz;
using Quartz.Impl;

namespace Memphis.Jobs
{
    public class Worker
    {
        private readonly ILogger<Worker> _logger;
        private readonly DatabaseContext _context;

        public Worker(ILogger<Worker> logger, DatabaseContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task Start()
        {
            _logger.LogInformation("Starting jobs...");
            var factory = new StdSchedulerFactory();
            var scheduler = await factory.GetScheduler();

            scheduler.Context["databaseContext"] = _context;
            scheduler.Context["logger"] = _logger;

            var rosterJob = JobBuilder.Create<Roster>()
                .WithIdentity("roster", "jobs")
                .Build();
            var rosterTrigger = TriggerBuilder.Create()
                .WithIdentity("roster", "jobs")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromMinutes(10))
                    .RepeatForever())
                .Build();
            await scheduler.ScheduleJob(rosterJob, rosterTrigger);

            await scheduler.Start();
        }
    }
}
