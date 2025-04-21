using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Ocpp
{
    public class OcppConfigurationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OcppConfigurationService> _logger;

        public OcppConfigurationService(IServiceProvider serviceProvider, ILogger<OcppConfigurationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // give chargers time to connect
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            var stationIds = new[] { "K0031041", "AE0007H1GN5C00832V" };

            using var scope = _serviceProvider.CreateScope();
            var ocpp = scope.ServiceProvider.GetRequiredService<IOcppService>();

            foreach (var id in stationIds)
            {
                _logger.LogInformation("Configuring MeterValuesSampledData for {StationId}", id);
                await ocpp.SendChangeConfigurationAsync(id,
                    "MeterValuesSampledData",
                    "Power.Active.Import,Current.Import"
                );
                await ocpp.SendChangeConfigurationAsync(id,
                    "MeterValueSampleInterval",
                    "10"
                );
            }
        }
    }
}