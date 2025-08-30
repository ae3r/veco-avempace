using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Ocpp
{
    /// <summary>
    /// Sends initial ChangeConfiguration to known stations after they connect.
    /// </summary>
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
            // Give chargers time to establish their WebSocket connections
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            // Use your real station IDs here
            var stationIds = new[] { "24DS0100788" /*, "K0031041", "AE0007H1GN5C00832V"*/ };

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var ocppService = scope.ServiceProvider.GetRequiredService<IOcppService>();

                    foreach (var stationId in stationIds)
                    {
                        _logger.LogInformation("Configuring station {StationId}: MeterValuesSampledData", stationId);
                        await ocppService.SendChangeConfigurationAsync(
                            stationId,
                            "MeterValuesSampledData",
                            "Power.Active.Import,Current.Import"
                        );

                        _logger.LogInformation("Configuring station {StationId}: MeterValueSampleInterval", stationId);
                        await ocppService.SendChangeConfigurationAsync(
                            stationId,
                            "MeterValueSampleInterval",
                            "10"
                        );
                    }

                    _logger.LogInformation("All ChangeConfiguration requests sent successfully.");
                    break; // send once
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending ChangeConfiguration. Retrying in 30 seconds...");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
