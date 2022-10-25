namespace ServiceWorkerCronJobDemo.Services
{
    public class FilesCronJob : CronJobService
    {
        private readonly ILogger<FilesCronJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public FilesCronJob(IScheduleConfig<FilesCronJob> config, ILogger<FilesCronJob> logger, IServiceProvider serviceProvider)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FilesCronJob starts.");
            return base.StartAsync(cancellationToken);
        }

        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation("{now} FilesCronJob is working.", DateTime.Now.ToString("T"));
            using var scope = _serviceProvider.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IScopedService>();
            await svc.DoWork(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FilesCronJob is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
