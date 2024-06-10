using STUN.Proxy;
using System.Net;

// ReSharper disable InconsistentNaming

namespace GandiDynamicDns.Unfucked.Stun;

public interface IStunClient5389: STUN.Client.IStunClient5389 {

    public IPEndPoint Server { get; }

}

public class StunClient5389UDP(IPEndPoint server, IPEndPoint local, IUdpProxy? proxy = null): STUN.Client.StunClient5389UDP(server, local, proxy), IStunClient5389 {

    public IPEndPoint Server { get; } = server;

}