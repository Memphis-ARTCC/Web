using dotenv.net;
using Memphis.Jobs.Airports;
using Memphis.Shared.Data;
using Microsoft.EntityFrameworkCore;
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
        }, ServiceLifetime.Singleton);
        var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ??
                throw new ArgumentNullException("REDIS_HOST env variable not found");
        services.AddSingleton(ConnectionMultiplexer.Connect(redisHost).GetDatabase());
        services.AddHostedService<Worker>();
    })
    .Build();
await host.RunAsync();