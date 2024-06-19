using GandiDynamicDns.Unfucked.DependencyInjection;
using GandiDynamicDns.Util;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Sockets;

namespace Tests.Util;

public class ExtensionsTest {

    [Fact]
    public void addInjectableProviders() {
        IServiceCollection services = new ServiceCollection();

        services.AddInjectableProviders();

        services.Should().Contain(descriptor =>
            descriptor.Lifetime == ServiceLifetime.Singleton &&
            descriptor.ServiceType == typeof(Provider<>) &&
            descriptor.ImplementationType == typeof(MicrosoftDependencyInjectionServiceProvider<>));

        services.Should().Contain(descriptor =>
            descriptor.Lifetime == ServiceLifetime.Singleton &&
            descriptor.ServiceType == typeof(OptionalProvider<>) &&
            descriptor.ImplementationType == typeof(MicrosoftDependencyInjectionServiceProvider<>));
    }

    // TODO this test depends on a working internet connection
    [Fact]
    public async Task resolve() {
        IPEndPoint? actual = await new DnsEndPoint("one.one.one.one", 53, AddressFamily.InterNetwork).Resolve();

        actual.Should().BeOneOf(new IPEndPoint(IPAddress.Parse("1.1.1.1"), 53), new IPEndPoint(IPAddress.Parse("1.0.0.1"), 53));
    }

    // TODO this test depends on a working internet connection
    [Fact]
    public async Task resolveFailure() {
        IPEndPoint? actual = await new DnsEndPoint("asdflkasdjflas.aldaviva.com", 80, AddressFamily.InterNetwork).Resolve();

        actual.Should().BeNull();
    }

}