using GandiDynamicDns.Util;
using System.Net;

namespace GandiDynamicDns.Unfucked.Stun;

public interface StunClientFactory {

    Task<IStunClient5389?> createStunClient(DnsEndPoint server);

}

public class StunClient5389Factory: StunClientFactory {

    private static readonly IPEndPoint LOCAL_HOST = new(IPAddress.Any, 0);

    public async Task<IStunClient5389?> createStunClient(DnsEndPoint server) => await server.Resolve() is { } addr ? new StunClient5389UDP(addr, server.Host, LOCAL_HOST) : null;

}