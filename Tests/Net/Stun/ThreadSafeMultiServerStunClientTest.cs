using GandiDynamicDns.Net.Stun;
using GandiDynamicDns.Unfucked.DependencyInjection;
using GandiDynamicDns.Unfucked.Stun;
using STUN.Enums;
using STUN.StunResult;
using System.Net;

namespace Tests.Net.Stun;

public class ThreadSafeMultiServerStunClientTest {

    private readonly IStunClient5389                 threadUnsafeStunClient = A.Fake<IStunClient5389>();
    private readonly ThreadSafeMultiServerStunClient threadSafeStunClient;

    public ThreadSafeMultiServerStunClientTest() {
        var provider = A.Fake<Provider<IStunClient5389>>();
        A.CallTo(() => provider.get()).Returns(threadUnsafeStunClient);
        threadSafeStunClient = new ThreadSafeMultiServerStunClient(provider);
    }

    [Fact]
    public async Task getSelfWanAddress() {
        A.CallTo(() => threadUnsafeStunClient.BindingTestAsync(A<CancellationToken>._)).Returns(new StunResult5389 {
            BindingTestResult = BindingTestResult.Success,
            PublicEndPoint    = new IPEndPoint(IPAddress.Parse("192.0.2.1"), 12345)
        });

        IPAddress? actual = await threadSafeStunClient.getSelfWanAddress();
        actual.Should().Be(IPAddress.Parse("192.0.2.1"));
    }

}