using STUN.Proxy;
using System.Net;

// ReSharper disable InconsistentNaming

namespace GandiDynamicDns.Unfucked.Stun;

/// <summary>
/// Like <see cref="STUN.Client.IStunClient5389"/> but it allows consumers to introspect the STUN server that the instance was configured with, without restoring to reflection on private fields.
/// </summary>
public interface IStunClient5389: STUN.Client.IStunClient5389 {

    /// <summary>
    /// The STUN server IP address and port that this instance was configured with.
    /// </summary>
    public IPEndPoint Server { get; }

}

/// <inheritdoc cref="IStunClient5389" />
public class StunClient5389UDP(IPEndPoint server, IPEndPoint local, IUdpProxy? proxy = null): STUN.Client.StunClient5389UDP(server, local, proxy), IStunClient5389 {

    /// <inheritdoc />
    public IPEndPoint Server { get; } = server;

}