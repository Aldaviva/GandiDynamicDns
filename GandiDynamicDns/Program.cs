using Gandi;
using Gandi.Dns;
using GandiDynamicDns;
using GandiDynamicDns.Util;
using Microsoft.Extensions.Options;
using RuntimeUpgrade.Notifier;
using RuntimeUpgrade.Notifier.Data;
using System.Net.Http.Headers;
using System.Reflection;
using Unfucked;
using Unfucked.HTTP;
using Unfucked.STUN;

// ReSharper disable RedundantTypeArgumentsOfMethod - make service declarations more readable

AssemblyName assemblyName = typeof(Program).Assembly.GetName();

if (args.Intersect(["--version", "-v"], StringComparer.InvariantCulture).Any()) {
    Console.WriteLine(assemblyName.Version!.ToString(3));
    return;
}

HostApplicationBuilder appConfig = Host.CreateApplicationBuilder(args);

appConfig.Logging.AddUnfuckedConsole();

appConfig.Configuration.AlsoSearchForJsonFilesInExecutableDirectory();

appConfig.Services
    .AddLogging()
    .AddSystemd()
    .AddWindowsService(WindowsService.configure)
    .Configure<Configuration>(appConfig.Configuration)
    .PostConfigure<Configuration>(configuration => configuration.sanitize())
    .AddSingleton<HttpClient>(_ => new UnfuckedHttpClient {
        DefaultRequestHeaders = {
            UserAgent = {
                new ProductInfoHeaderValue("(+https://github.com/Aldaviva/GandiDynamicDns)") // program name and version already added by UnfuckedHttpClient
            }
        }
    })
    .AddHostedService<DynamicDnsServiceImpl>()
    .SetExitCodeOnBackgroundServiceException()
    .AddSingleton<IGandiClient>(GandiClientFactory.createGandiClient)
    .AddSingleton<ILiveDns>(provider => provider.GetRequiredService<IGandiClient>().LiveDns(provider.GetRequiredService<IOptions<Configuration>>().Value.domain))
    .AddStunClient(ctx => new StunOptions { ServerHostnameBlacklist = ctx.GetRequiredService<IOptions<Configuration>>().Value.stunServerBlacklist });

using IHost app = appConfig.Build();

using RuntimeUpgradeNotifier upgradeNotifier = new() {
    LoggerFactory   = app.Services.GetRequiredService<ILoggerFactory>(),
    RestartStrategy = RestartStrategy.AutoRestartService
};

await app.RunAsync();