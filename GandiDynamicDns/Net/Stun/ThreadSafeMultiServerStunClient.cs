using GandiDynamicDns.Unfucked.DependencyInjection;
using GandiDynamicDns.Unfucked.Stun;
using STUN.Enums;
using STUN.StunResult;
using System.Net;

namespace GandiDynamicDns.Net.Stun;

public interface SelfWanAddressClient {

    Task<SelfWanAddressResponse> getSelfWanAddress(CancellationToken ct = default);

}

public record SelfWanAddressResponse(IPAddress? selfWanAddress, IPEndPoint server);

public class ThreadSafeMultiServerStunClient(Provider<IStunClient5389> stunProvider): SelfWanAddressClient {

    public async Task<SelfWanAddressResponse> getSelfWanAddress(CancellationToken ct = default) {
        using IStunClient5389 stun     = stunProvider.get();
        StunResult5389        response = await stun.BindingTestAsync(ct);
        return new SelfWanAddressResponse(response.BindingTestResult == BindingTestResult.Success ? response.PublicEndPoint?.Address : null, stun.Server);
    }

}