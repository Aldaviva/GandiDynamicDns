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
    .AddWindowsService()
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
    .AddHostedService<GandiDynamicDnsService>()
    .AddSingleton(provider => new GandiLiveDns { Apikey = provider.GetRequiredService<IOptions<Configuration>>().Value.gandiApiKey })
    .AddTransient<IStunClient5389, MultiServerStunClient>();

using IHost app = appConfig.Build();
await app.RunAsync();