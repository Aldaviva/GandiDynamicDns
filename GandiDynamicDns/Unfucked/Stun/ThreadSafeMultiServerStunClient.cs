using GandiDynamicDns.Unfucked.DependencyInjection;
using STUN.Enums;
using STUN.StunResult;
using System.Net;

namespace GandiDynamicDns.Unfucked.Stun;

public interface SelfWanAddressClient {

    Task<IPAddress?> getSelfWanAddress(CancellationToken ct = default);

}

public class ThreadSafeMultiServerStunClient(Provider<IStunClient5389> stunProvider): SelfWanAddressClient {

    public async Task<IPAddress?> getSelfWanAddress(CancellationToken ct = default) {
        using IStunClient5389 stun     = stunProvider.get();
        StunResult5389        response = await stun.BindingTestAsync(ct);
        return response.BindingTestResult == BindingTestResult.Success ? response.PublicEndPoint?.Address : null;
    }

}