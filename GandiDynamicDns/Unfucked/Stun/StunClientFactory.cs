using STUN.Proxy;
using System.Net;

namespace GandiDynamicDns.Unfucked.Stun;

public interface StunClientFactory {

    IStunClient5389 createStunClient(IPEndPoint server, IPEndPoint? client = null, IUdpProxy? proxy = null);

}

public class StunClient5389Factory: StunClientFactory {

    private static readonly IPEndPoint LOCAL_HOST = new(IPAddress.Any, 0);

    public IStunClient5389 createStunClient(IPEndPoint server, IPEndPoint? client = null, IUdpProxy? proxy = null) => new StunClient5389UDP(server, client ?? LOCAL_HOST, proxy);

}