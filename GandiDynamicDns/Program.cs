using G6.GandiLiveDns;
using GandiDynamicDns;
using GandiDynamicDns.Unfucked.Stun;
using Microsoft.Extensions.Options;
using System.Net.Security;
using System.Security.Authentication;

HostApplicationBuilder appConfig = Host.CreateApplicationBuilder(args);

appConfig.Logging.AddConsole(options => options.FormatterName = MyConsoleFormatter.NAME).AddConsoleFormatter<MyConsoleFormatter, MyConsoleFormatter.MyConsoleOptions>(options => {
    options.includeNamespaces = false;
});

appConfig.Configuration.AlsoSearchForJsonFilesInExecutableDirectory();

appConfig.Services
    .AddSystemd()
    .AddWindowsService(options => options.ServiceName = "GandiDynamicDns")
    .Configure<Configuration>(appConfig.Configuration)
    .PostConfigure<Configuration>(configuration => {
        configuration.subdomain = configuration.subdomain.TrimEnd('.');
        if (string.IsNullOrWhiteSpace(configuration.subdomain)) {
            configuration.subdomain = "@";
        }

        if (configuration.dnsRecordTimeToLive < Configuration.MINIMUM_TIME_TO_LIVE) {
            configuration.dnsRecordTimeToLive = Configuration.MINIMUM_TIME_TO_LIVE;
        }
    })
    .AddSingleton(_ => new HttpClient(new SocketsHttpHandler {
        AllowAutoRedirect        = true,
        ConnectTimeout           = TimeSpan.FromSeconds(10),
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        MaxConnectionsPerServer  = 8,
        SslOptions = new SslClientAuthenticationOptions {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }
    }))
    .AddHostedService<GandiDynamicDnsService>()
    .AddSingleton(provider => {
        string apiKey = provider.GetRequiredService<IOptions<Configuration>>().Value.gandiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "<Generate an API key on https://account.gandi.net/en/users/_/security>") {
            throw new ArgumentException($"Missing configuration option {nameof(Configuration.gandiApiKey)} in appsettings.json");
        }
        return new GandiLiveDns { Apikey = apiKey };
    })
    .AddTransient<IStunClient5389, MultiServerStunClient>();

using IHost app = appConfig.Build();
await app.RunAsync();