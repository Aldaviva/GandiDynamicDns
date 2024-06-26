using GandiDynamicDns;
using GandiDynamicDns.Net.Dns;
using GandiDynamicDns.Net.Stun;
using GandiDynamicDns.Unfucked.Dns;
using GandiDynamicDns.Unfucked.Http;
using GandiDynamicDns.Unfucked.Stun;
using GandiDynamicDns.Util;
using Microsoft.Extensions.Options;
using System.Net.Security;
using System.Security.Authentication;

HostApplicationBuilder appConfig = Host.CreateApplicationBuilder(args);

appConfig.Logging.AddConsole(options => options.FormatterName = MyConsoleFormatter.NAME).AddConsoleFormatter<MyConsoleFormatter, MyConsoleFormatter.MyConsoleOptions>(options => {
    options.includeNamespaces = false;
});

appConfig.Configuration.AlsoSearchForJsonFilesInExecutableDirectory();

appConfig.Services
    .AddLogging()
    .AddInjectableProviders()
    .AddSystemd()
    .AddWindowsService(WindowsService.configure)
    .Configure<Configuration>(appConfig.Configuration)
    .AddSingleton<HttpMessageHandler>(_ => new SocketsHttpHandler {
        AllowAutoRedirect        = true,
        ConnectTimeout           = TimeSpan.FromSeconds(10),
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        MaxConnectionsPerServer  = 8,
        SslOptions = new SslClientAuthenticationOptions {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }
    })
    .AddSingleton(provider => new HttpClient(provider.GetRequiredService<HttpMessageHandler>()))
    .AddHostedService<DynamicDnsServiceImpl>()
    .AddSingleton(provider => {
        IGandiLiveDns gandi               = new GandiLiveDns(new FilteringHttpClientHandler(provider.GetRequiredService<HttpMessageHandler>()));
        Configuration configuration       = provider.GetRequiredService<IOptions<Configuration>>().Value;
        string?       apiKey              = configuration.gandiApiKey;
        string?       personalAccessToken = configuration.gandiPersonalAccessToken;
        if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.StartsWith('<')) {
            gandi.ApiKey = apiKey;
        } else if (!string.IsNullOrWhiteSpace(personalAccessToken) && !personalAccessToken.StartsWith('<')) {
            gandi.PersonalAccessToken = personalAccessToken;
        } else {
            throw new ArgumentException($"One of {nameof(Configuration.gandiPersonalAccessToken)} or {nameof(Configuration.gandiApiKey)} is required in appsettings.json, but both were missing.");
        }

        return gandi;
    })
    .AddSingleton<DnsManager, GandiDnsManager>()
    .AddTransient<IStunClient5389, MultiServerStunClient>()
    .AddSingleton<SelfWanAddressClient, ThreadSafeMultiServerStunClient>()
    .AddSingleton<StunClientFactory, StunClient5389Factory>();

using IHost app = appConfig.Build();
await app.RunAsync();