using STUN.Enums;
using STUN.StunResult;
using System.Net;
using System.Net.Sockets;

namespace GandiDynamicDns.Unfucked.Stun;

public class MultiServerStunClient(HttpClient http, ILogger<MultiServerStunClient> logger): IStunClient5389 {

    private const string STUN_LIST_CACHE_KEY = "always-on-stun";

    private static readonly Random     RANDOM                   = new();
    private static readonly IPEndPoint LOCAL_HOST               = new(IPAddress.Any, 0);
    private static readonly TimeSpan   STUN_LIST_CACHE_DURATION = TimeSpan.FromDays(1);
    private static readonly Uri        STUN_SERVER_LIST_URL     = new("https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_ipv4s.txt");

    private static readonly IList<DnsEndPoint> FALLBACK_STUN_SERVER_HOSTS = [
        new DnsEndPoint("stun.ekiga.net", 3478),
        new DnsEndPoint("stun.freeswitch.org", 3478),
        new DnsEndPoint("stun1.l.google.com", 19302),
        new DnsEndPoint("stun2.l.google.com", 19302),
        new DnsEndPoint("stun3.l.google.com", 19302),
        new DnsEndPoint("stun4.l.google.com", 19302)
    ];

    public StunResult5389 State { get; private set; } = new();

    private readonly MemoryCache<IPEndPoint[]> stunListCache = new($"{nameof(MultiServerStunClient)}.{nameof(stunListCache)}");

    private async Task<IEnumerable<IStunClient5389>> getStunClients(CancellationToken ct = default) {
        IPEndPoint[] servers = await stunListCache.GetOrAdd(STUN_LIST_CACHE_KEY, async () => await fetchStunServers(ct), STUN_LIST_CACHE_DURATION);

        RANDOM.Shuffle(servers);

        return servers.Select(serverHost => new StunClient5389UDP(serverHost, LOCAL_HOST));
    }

    private async Task<IPEndPoint[]> fetchStunServers(CancellationToken ct) {
        try {
            logger.LogDebug("Fetching list of STUN servers from pradt2/always-online-stun");
            IPEndPoint[] servers = (await http.GetStringAsync(STUN_SERVER_LIST_URL, ct)).TrimEnd().Split('\n').Select(line => {
                    string[] columns = line.Split(':', 2);
                    return new IPEndPoint(IPAddress.Parse(columns[0]), ushort.Parse(columns[1]));
                })
                .ToArray();
            logger.LogDebug("Fetched {count:N0} STUN servers", servers.Length);
            return servers;
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return await Task.WhenAll(FALLBACK_STUN_SERVER_HOSTS.Select(async dnsHost =>
                new IPEndPoint((await Dns.GetHostAddressesAsync(dnsHost.Host, AddressFamily.InterNetwork, ct)).First(), dnsHost.Port)));
        }
    }

    public void Dispose() {
        stunListCache.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask QueryAsync(CancellationToken cancellationToken = default) {
        foreach (IStunClient5389 stun in await getStunClients(cancellationToken)) {
            using (stun) {
                Server = stun.Server;
                await stun.QueryAsync(cancellationToken);
                State = stun.State;
                if (isSuccessfulResult(State)) {
                    break;
                } else {
                    logger.LogWarning("STUN request to {host} failed, trying another server", stun.Server.ToString());
                }
            }
        }
    }

    public async ValueTask<StunResult5389> BindingTestAsync(CancellationToken cancellationToken = default) {
        foreach (IStunClient5389 stun in await getStunClients(cancellationToken)) {
            using (stun) {
                Server = stun.Server;
                logger.LogDebug("Sending STUN request to {host}", stun.Server.ToString());
                State = await stun.BindingTestAsync(cancellationToken);
                if (isSuccessfulResult(State)) {
                    return State;
                } else {
                    logger.LogWarning("STUN request to {host} failed, trying another server", stun.Server.ToString());
                }
            }
        }
        return State;
    }

    public async ValueTask MappingBehaviorTestAsync(CancellationToken cancellationToken = default) {
        foreach (IStunClient5389 stun in await getStunClients(cancellationToken)) {
            using (stun) {
                Server = stun.Server;
                await stun.MappingBehaviorTestAsync(cancellationToken);
                State = stun.State;
                if (isSuccessfulResult(State)) {
                    break;
                } else {
                    logger.LogWarning("STUN request to {host} failed, trying another server", stun.Server.ToString());
                }
            }
        }
    }

    public async ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default) {
        foreach (IStunClient5389 stun in await getStunClients(cancellationToken)) {
            using (stun) {
                Server = stun.Server;
                await stun.FilteringBehaviorTestAsync(cancellationToken);
                State = stun.State;
                if (isSuccessfulResult(State)) {
                    break;
                } else {
                    logger.LogWarning("STUN request to {host} failed, trying another server", stun.Server.ToString());
                }
            }
        }
    }

    private static bool isSuccessfulResult(StunResult5389 result) => result.BindingTestResult == BindingTestResult.Success && result.FilteringBehavior != FilteringBehavior.UnsupportedServer
        && result.MappingBehavior != MappingBehavior.Fail && result.MappingBehavior != MappingBehavior.UnsupportedServer;

    public IPEndPoint Server { get; private set; } = null!;

}