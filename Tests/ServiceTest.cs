using GandiDynamicDns;
using GandiDynamicDns.Unfucked.Dns;
using GandiDynamicDns.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Tests;

public sealed class ServiceTest: IDisposable {

    private readonly IServiceHostInterceptor hostInterceptor = new ServiceHostInterceptor();

    [Fact]
    public async Task start() {
        hostInterceptor.hostBuilding += (_, builder) => {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> {
                { "gandiApiKey", "abcdef" }
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
    public async Task crashOnMissingPagerDutyIntegrationKey() {
        hostInterceptor.hostBuilding += (_, builder) => {
            builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?> {
                { "gandiApiKey", "<Generate an API key on https://account.gandi.net/en/users/_/security>" }
            }));

            builder.ConfigureServices(services => {
                services.RemoveAll<IHostedService>();
                services.AddHostedService<MockHostedService>();
            });
        };

        Func<Task> thrower = runMainMethod;
        await thrower.Should().ThrowWithinAsync<ArgumentException>(TimeSpan.FromSeconds(15));
    }

    private static async Task runMainMethod() =>
        await (Task) typeof(Program).GetMethod("<Main>$", BindingFlags.NonPublic | BindingFlags.Static, [typeof(string[])])!.Invoke(null, [Array.Empty<string>()])!;

    [Fact]
    public void serviceName() {
        WindowsServiceLifetimeOptions serviceLifetimeOptions = new();
        WindowsService.configure(serviceLifetimeOptions);
        serviceLifetimeOptions.ServiceName.Should().Be("GandiDynamicDns");
    }

    public void Dispose() {
        hostInterceptor.Dispose();
    }

    public class MockHostedService(HttpClient http, IGandiLiveDns gandi, IOptions<Configuration> config): IHostedService {

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    }

}