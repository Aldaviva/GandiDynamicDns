using GandiDynamicDns.Net.Stun;
using GandiDynamicDns.Unfucked.Stun;
using Microsoft.Extensions.Logging.Abstractions;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;
using System.Net;
using Tests.Mocks;

namespace Tests.Net.Stun;

public class MultiServerStunClientTest: IDisposable {

    private readonly MultiServerStunClient  multiServerStunClient;
    private readonly StunClientFactory      stunClientFactory      = A.Fake<StunClientFactory>();
    private readonly FakeHttpMessageHandler httpMessageHandler     = A.Fake<FakeHttpMessageHandler>();
    private readonly IStunClient5389        singleServerStunClient = A.Fake<IStunClient5389>();

    public MultiServerStunClientTest() {
        MultiServerStunClient.SERVERS_CACHE.Clear();
        multiServerStunClient = new MultiServerStunClient(new HttpClient(httpMessageHandler), stunClientFactory, new NullLogger<MultiServerStunClient>());

        A.CallTo(() => stunClientFactory.createStunClient(A<IPEndPoint>._, A<IPEndPoint>._, A<IUdpProxy>._)).Returns(singleServerStunClient);

        A.CallTo(() => httpMessageHandler.fakeSendAsync(A<HttpRequestMessage>._, A<CancellationToken>._))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("192.0.2.1:12345\n192.0.2.2:12345\n192.0.2.3:hargle\n") });
    }

    public void Dispose() {
        multiServerStunClient.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task bindingTestAsync() {
        A.CallTo(() => singleServerStunClient.BindingTestAsync(A<CancellationToken>._))
            .Returns(new StunResult5389 { BindingTestResult = BindingTestResult.Fail }).NumberOfTimes(1)
            .Then.Returns(new StunResult5389
                { BindingTestResult = BindingTestResult.Success, PublicEndPoint = new IPEndPoint(IPAddress.Parse("198.51.100.1"), 12345) });

        multiServerStunClient.Server.Should().BeNull();

        StunResult5389 actual = await multiServerStunClient.BindingTestAsync();

        actual.BindingTestResult.Should().Be(BindingTestResult.Success);
        actual.PublicEndPoint.Should().Be(new IPEndPoint(IPAddress.Parse("198.51.100.1"), 12345));
        multiServerStunClient.Server.Should().NotBeNull();
    }

    [Fact]
    public async Task queryAsync() {
        A.CallTo(() => singleServerStunClient.State)
            .Returns(new StunResult5389 { BindingTestResult = BindingTestResult.Fail }).NumberOfTimes(1)
            .Then.Returns(new StunResult5389
                { BindingTestResult = BindingTestResult.Success, PublicEndPoint = new IPEndPoint(IPAddress.Parse("198.51.100.1"), 12345) });

        await multiServerStunClient.QueryAsync();

        StunResult5389 actual = multiServerStunClient.State;
        actual.BindingTestResult.Should().Be(BindingTestResult.Success);
        actual.PublicEndPoint.Should().Be(new IPEndPoint(IPAddress.Parse("198.51.100.1"), 12345));
    }

    [Fact]
    public async Task mappingBehaviorTestAsync() {
        A.CallTo(() => singleServerStunClient.State)
            .Returns(new StunResult5389 { MappingBehavior = MappingBehavior.Fail }).NumberOfTimes(1)
            .Then.Returns(new StunResult5389
                { MappingBehavior = MappingBehavior.EndpointIndependent });

        await multiServerStunClient.MappingBehaviorTestAsync();

        StunResult5389 actual = multiServerStunClient.State;
        actual.MappingBehavior.Should().Be(MappingBehavior.EndpointIndependent);
    }

    [Fact]
    public async Task filteringBehaviorTestAsync() {
        A.CallTo(() => singleServerStunClient.State)
            .Returns(new StunResult5389 { FilteringBehavior = FilteringBehavior.UnsupportedServer }).NumberOfTimes(1)
            .Then.Returns(new StunResult5389
                { FilteringBehavior = FilteringBehavior.AddressAndPortDependent });

        await multiServerStunClient.FilteringBehaviorTestAsync();

        StunResult5389 actual = multiServerStunClient.State;
        actual.FilteringBehavior.Should().Be(FilteringBehavior.AddressAndPortDependent);
    }

    [Theory]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(TaskCanceledException))]
    [InlineData(typeof(Exception))]
    public async Task fallbackStunServers(Type exceptionType) {
        A.CallTo(() => httpMessageHandler.fakeSendAsync(A<HttpRequestMessage>._, A<CancellationToken>._)).Throws((Exception) exceptionType.GetConstructor(Type.EmptyTypes)!.Invoke([]));

        await multiServerStunClient.QueryAsync();
    }

}