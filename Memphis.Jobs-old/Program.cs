using dotenv.net;
using Memphis.Jobs;
using Memphis.Jobs.Jobs;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

DotEnv.Load();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(options =>
    {
        options.ClearProviders();
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
        options.AddSerilog(logger, dispose: true);
    })
    .ConfigureServices(services =>
    {
        services.AddDbContext<DatabaseContext>(options =>
        {
            options.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                throw new ArgumentException("CONNECTION_STRING env variable not found"));
        });
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ??
                throw new ArgumentNullException("REDIS_HOST env variable not found");
        services.AddSingleton(ConnectionMultiplexer.Connect(redisHost).GetDatabase());
        services.AddQuartz(options =>
        {
            options.UseInMemoryStore();
            options.ScheduleJob<Roster>(trigger => trigger
                .WithIdentity("roster", "jobs")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromMinutes(10))
                    .RepeatForever())
                );
        });
        services.AddTransient<Roster>();
    })
    .Build()
    .RunAsync();