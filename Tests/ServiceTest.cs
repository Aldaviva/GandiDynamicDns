using Gandi;
using Gandi.Dns;
using GandiDynamicDns;
using GandiDynamicDns.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.ComponentModel.Design;
using System.Reflection;
using Unfucked.HTTP;
using Unfucked.STUN;

namespace Tests;

public sealed class ServiceTest: IDisposable {

    private readonly IServiceHostInterceptor hostInterceptor = new ServiceHostInterceptor();

    [Fact]
    public async Task start() {
        hostInterceptor.hostBuilding += (_, builder) => {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> {
                { "gandiAuthToken", "0afe09375a3c37b0c5014933c5d2d0529aa203c8" }
            }));

            builder.ConfigureServices(services => {
                services.RemoveAll<IHostedService>();
                services.AddHostedService<MockHostedService>();
            });
        };

        Task mainTask = runMainMethod();
        hostInterceptor.host?.StopAsync();
        await mainTask;
    }

    [Fact]
    public async Task crashOnMissingAuthToken() {
        hostInterceptor.hostBuilding += (_, builder) => {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> {
                { "gandiAuthToken", "<Generate an API key on https://account.gandi.net/en/users/_/security>" }
            }));

            builder.ConfigureServices(services => {
                services.RemoveAll<IHostedService>();
                services.AddHostedService<MockHostedService>();
            });
        };

        Func<Task> thrower = () => runMainMethod();
        await thrower.Should().ThrowWithinAsync<ArgumentException>(TimeSpan.FromSeconds(15));
    }

    private static async Task runMainMethod(params string[] args) =>
        await (Task) typeof(Program).GetMethod("<Main>$", BindingFlags.NonPublic | BindingFlags.Static, [typeof(string[])])!.Invoke(null, [args])!;

    [Fact]
    public void serviceName() {
        WindowsServiceLifetimeOptions serviceLifetimeOptions = new();
        WindowsService.configure(serviceLifetimeOptions);
        serviceLifetimeOptions.ServiceName.Should().Be("GandiDynamicDns");
    }

    [Fact]
    public async Task version() {
        await runMainMethod("--version");
    }

    [Fact]
    public void configUpdate() {
        using ServiceContainer services = new();
        services.AddService(typeof(ILogger<Program>), NullLogger<Program>.Instance);
        services.AddService(typeof(HttpClient), new UnfuckedHttpClient());
        var optionsMonitor = A.Fake<IOptionsMonitor<Configuration>>();
        services.AddService(typeof(IOptionsMonitor<Configuration>), optionsMonitor);

        Configuration initialConfig = new() {
            domain         = "example.com",
            gandiAuthToken = "28930c604ff7df83b4341b0958acdc69141f4d62"
        };

        A.CallTo(() => optionsMonitor.CurrentValue).Returns(initialConfig);
        Captured<Action<Configuration, string?>> captured = A.Captured<Action<Configuration, string?>>();
        A.CallTo(() => optionsMonitor.OnChange(captured._)).Returns(A.Fake<IDisposable>());

        using IGandiClient gandiClient = GandiClientFactory.createGandiClient(services);
        gandiClient.AuthToken.Should().Be("28930c604ff7df83b4341b0958acdc69141f4d62");

        Configuration newConfig = initialConfig with { gandiAuthToken = "d4572c4dd184fe2329acaf49c5617b1b99cb672d" };
        captured.GetLastValue()(newConfig, string.Empty);
        gandiClient.AuthToken.Should().Be("d4572c4dd184fe2329acaf49c5617b1b99cb672d");
    }

    public void Dispose() {
        hostInterceptor.Dispose();
    }

    public class MockHostedService(ILiveDns gandi, StunServerList stun, IOptions<Configuration> config): IHostedService {

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    }

}