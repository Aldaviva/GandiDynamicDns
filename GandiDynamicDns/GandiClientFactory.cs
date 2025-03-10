using Gandi;
using Microsoft.Extensions.Options;

namespace GandiDynamicDns;

public static class GandiClientFactory {

    public static IGandiClient createGandiClient(IServiceProvider services) {
        var        logger     = services.GetRequiredService<ILogger<Program>>();
        var        config     = services.GetRequiredService<IOptionsMonitor<Configuration>>();
        HttpClient httpClient = services.GetRequiredService<HttpClient>();

        string      authToken = getValidAuthToken(config.CurrentValue);
        GandiClient gandi     = new(authToken, httpClient);

        config.OnChange((configuration, _) => {
            string newAuthToken = getValidAuthToken(configuration);
            if (newAuthToken != gandi.AuthToken) {
                gandi.AuthToken = newAuthToken;
                logger.LogInformation("{key} changed, reloading configuration", nameof(Configuration.gandiAuthToken));
            }
        });

        return gandi;
    }

    private static string getValidAuthToken(Configuration config) => Configuration.isAuthTokenValid(config.gandiAuthToken)
        ? config.gandiAuthToken : throw new ArgumentException($"Missing or invalid required configuration option {nameof(Configuration.gandiAuthToken)} in appsettings.json.");

}