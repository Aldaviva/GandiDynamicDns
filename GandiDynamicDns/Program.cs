using GandiDynamicDns;
using GandiDynamicDns.Net.Dns;
using GandiDynamicDns.Util;
using Microsoft.Extensions.Options;
using System.Net.Security;
using System.Security.Authentication;
using Unfucked;
using Unfucked.DNS;
using Unfucked.STUN;

HostApplicationBuilder appConfig = Host.CreateApplicationBuilder(args);

appConfig.Logging.AddUnfuckedConsole();

appConfig.Configuration.AlsoSearchForJsonFilesInExecutableDirectory();

appConfig.Services
    .AddLogging()
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
    }) { Timeout = TimeSpan.FromSeconds(20) })
    .AddHostedService<DynamicDnsServiceImpl>()
    .SetExitCodeOnBackgroundServiceException()
    .AddSingleton<IGandiLiveDns>(provider => {
        string apiKey = provider.GetRequiredService<IOptions<Configuration>>().Value.gandiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "<Generate an API key on https://account.gandi.net/en/users/_/security>") {
            throw new ArgumentException($"Missing configuration option {nameof(Configuration.gandiApiKey)} in appsettings.json");
        }
        return new GandiLiveDns { ApiKey = apiKey };
    })
    .AddSingleton<DnsManager, GandiDnsManager>()
    .AddStunClient(ctx => new StunOptions { serverHostnameBlacklist = ctx.GetRequiredService<IOptions<Configuration>>().Value.stunServerBlacklist });

using IHost app = appConfig.Build();

await app.RunAsync();