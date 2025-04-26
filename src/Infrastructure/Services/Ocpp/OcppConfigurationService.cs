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
    /// Background service that sends initial ChangeConfiguration requests
    /// to all known charging stations once they have connected.
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

            // List of station IDs to configure
            var stationIds = new[] { "K0031041", "AE0007H1GN5C00832V" };

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

                    _logger.LogInformation("All ChangeConfiguration requests sent successfully. Exiting configuration loop.");
                    break; // no need to resend once succeeded
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
