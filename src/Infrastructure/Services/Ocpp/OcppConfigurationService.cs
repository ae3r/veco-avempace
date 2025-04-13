using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Application.Common.Interfaces;
using System;
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
            // Wait 30 seconds for the chargers to connect
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Attempting to send ChangeConfiguration for station AE0007H1GN5C00832V");
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var ocppService = scope.ServiceProvider.GetRequiredService<IOcppService>();

                        await ocppService.SendChangeConfigurationAsync(
                            stationId: "AE0007H1GN5C00832V",
                            key: "MeterValuesSampledData",
                            value: "Power.Active.Import,Current.Import"
                        );
                        await ocppService.SendChangeConfigurationAsync(
                            stationId: "AE0007H1GN5C00832V",
                            key: "MeterValueSampleInterval",
                            value: "10"
                        );
                    }
                    break; // Exit the loop once the configuration is sent successfully.
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending ChangeConfiguration request");
                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
