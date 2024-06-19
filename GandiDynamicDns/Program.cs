using GandiDynamicDns;
using GandiDynamicDns.Net.Dns;
using GandiDynamicDns.Net.Stun;
using GandiDynamicDns.Unfucked.Dns;
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
    .AddSingleton(_ => new HttpClient(new SocketsHttpHandler {
        AllowAutoRedirect        = true,
        ConnectTimeout           = TimeSpan.FromSeconds(10),
        PooledConnectionLifetime = TimeSpan.FromMinutes(15),
        MaxConnectionsPerServer  = 8,
        SslOptions = new SslClientAuthenticationOptions {
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }
    }))
    .AddHostedService<DynamicDnsServiceImpl>()
    .AddSingleton<IGandiLiveDns>(provider => {
        string apiKey = provider.GetRequiredService<IOptions<Configuration>>().Value.gandiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "<Generate an API key on https://account.gandi.net/en/users/_/security>") {
            throw new ArgumentException($"Missing configuration option {nameof(Configuration.gandiApiKey)} in appsettings.json");
        }
        return new GandiLiveDns { ApiKey = apiKey };
    })
    .AddSingleton<DnsManager, GandiDnsManager>()
    .AddTransient<IStunClient5389, MultiServerStunClient>()
    .AddSingleton<SelfWanAddressClient, ThreadSafeMultiServerStunClient>()
    .AddSingleton<StunClientFactory, StunClient5389Factory>();

using IHost app = appConfig.Build();
await app.RunAsync();