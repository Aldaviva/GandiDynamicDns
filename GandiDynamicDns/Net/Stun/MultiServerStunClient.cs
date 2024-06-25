using GandiDynamicDns.Unfucked.Caching;
using GandiDynamicDns.Unfucked.Stun;
using GandiDynamicDns.Util;
using Microsoft.Extensions.Options;
using STUN.Enums;
using STUN.StunResult;
using System.Collections.Frozen;
using System.Net;
using System.Runtime.CompilerServices;

namespace GandiDynamicDns.Net.Stun;

public class MultiServerStunClient(HttpClient http, StunClientFactory stunClientFactory, ILogger<MultiServerStunClient> logger, IOptions<Configuration> config): IStunClient5389 {

    private static readonly Random   RANDOM                   = new();
    private static readonly TimeSpan STUN_LIST_CACHE_DURATION = TimeSpan.FromDays(1);

    // ExceptionAdjustment: M:System.Uri.#ctor(System.String) -T:System.UriFormatException
    private static readonly Uri STUN_SERVER_LIST_URL = new("https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts.txt");

    internal static readonly IMemoryCache<IEnumerable<DnsEndPoint>> SERVERS_CACHE = new MemoryCache<IEnumerable<DnsEndPoint>>($"{nameof(MultiServerStunClient)}.{nameof(SERVERS_CACHE)}");

    private static readonly IList<DnsEndPoint> FALLBACK_SERVERS = [
        new DnsEndPoint("stun.ekiga.net", 3478),
        new DnsEndPoint("stun.freeswitch.org", 3478),
        new DnsEndPoint("stun1.l.google.com", 19302),
        new DnsEndPoint("stun2.l.google.com", 19302),
        new DnsEndPoint("stun3.l.google.com", 19302),
        new DnsEndPoint("stun4.l.google.com", 19302)
    ];

    private readonly IReadOnlySet<string> blacklistedServers = config.Value.stunServerBlacklist.Concat([
        "stun.bergophor.de", // Some German agriculture company. 87.129.12.229 incorrectly told me that my IP address was 192.168.80.67
        "stun.usfamily.net" // Some small-town Minnesota ISP. 64.131.63.216 and 64.131.63.217 told me that my IP address was a wide variety of incorrect values, including 107.12.198.224, 121.5.32.247, 136.249.128.168, 151.70.235.94, 170.87.255.116, 189.172.15.230, 199.225.99.231, 237.120.242.186, 25.32.55.201, 54.65.6.154, 55.204.238.254, 79.244.154.201, 8.144.106.11, 87.46.19.57, and 99.93.120.192
    ]).Select(s => s.ToLowerInvariant()).ToFrozenSet();

    public StunResult5389 State { get; private set; } = new();
    public DnsEndPoint Server { get; private set; } = null!;
    public IPEndPoint ServerAddress { get; private set; } = null!;

    private async IAsyncEnumerable<IStunClient5389> getStunClients([EnumeratorCancellation] CancellationToken ct = default) {
        const string  STUN_LIST_CACHE_KEY = "always-on-stun";
        DnsEndPoint[] servers             = [];
        try {
            servers = (await SERVERS_CACHE.GetOrAdd(STUN_LIST_CACHE_KEY, async () => await fetchStunServers(ct), STUN_LIST_CACHE_DURATION)).ToArray();
        } catch (HttpRequestException) { } catch (TaskCanceledException) { /* timeout */
        } catch (Exception e) when (e is not OutOfMemoryException) { }

        RANDOM.Shuffle(servers);

        foreach (DnsEndPoint host in servers.Concat(FALLBACK_SERVERS)) {
            if (await stunClientFactory.createStunClient(host) is { } stunClient) {
                yield return stunClient;
            }
        }
    }

    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="TaskCanceledException"></exception>
    private async Task<IEnumerable<DnsEndPoint>> fetchStunServers(CancellationToken ct) {
        logger.LogDebug("Fetching list of STUN servers from pradt2/always-online-stun");
        ICollection<DnsEndPoint> servers = (await http.GetStringAsync(STUN_SERVER_LIST_URL, ct))
            .TrimEnd()
            .Split('\n')
            .Select(line => {
                string[] columns = line.Split(':', 2);
                try {
                    return new DnsEndPoint(columns[0], columns.ElementAtOrDefault(1) is { } port ? ushort.Parse(port) : 3478);
                } catch (FormatException) {
                    return null;
                }
            })
            .Compact()
            .ExceptBy(blacklistedServers, host => host.Host)
            .ToList();

        logger.LogDebug("Fetched {count:N0} STUN servers", servers.Count);
        return servers;
    }

    public async ValueTask QueryAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in getStunClients(cancellationToken)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                await stun.QueryAsync(cancellationToken);
                State = stun.State;
                if (isSuccessfulResponse(State)) {
                    break;
                } else {
                    logger.LogWarning("STUN request to {host} ({addr}:{port}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
    }

    public async ValueTask<StunResult5389> BindingTestAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in getStunClients(cancellationToken)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                logger.LogDebug("Sending UDP STUN request to {host} ({addr}:{port})", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                State = await stun.BindingTestAsync(cancellationToken);
                if (isSuccessfulResponse(State)) {
                    break;
                } else {
                    logger.LogWarning("STUN request to {host} ({addr}:{port}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
        return State;
    }

    public async ValueTask MappingBehaviorTestAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in getStunClients(cancellationToken)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                await stun.MappingBehaviorTestAsync(cancellationToken);
                State = stun.State;
                if (isSuccessfulResponse(State)) {
                    break;
                } else {
                    logger.LogWarning("STUN request to {host} ({addr}:{port}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
    }

    public async ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default) {
        await foreach (IStunClient5389 stun in getStunClients(cancellationToken)) {
            using (stun) {
                Server        = stun.Server;
                ServerAddress = stun.ServerAddress;
                await stun.FilteringBehaviorTestAsync(cancellationToken);
                State = stun.State;
                if (isSuccessfulResponse(State)) {
                    break;
                } else {
                    logger.LogWarning("STUN request to {host} ({addr}:{port}) failed, trying another server", stun.Server.Host, stun.ServerAddress.Address, stun.ServerAddress.Port);
                }
            }
        }
    }

    private static bool isSuccessfulResponse(StunResult5389 response) =>
        response.BindingTestResult != BindingTestResult.Fail &&
        response.BindingTestResult != BindingTestResult.UnsupportedServer &&
        response.FilteringBehavior != FilteringBehavior.UnsupportedServer &&
        response.MappingBehavior != MappingBehavior.Fail &&
        response.MappingBehavior != MappingBehavior.UnsupportedServer;

    public void Dispose() => GC.SuppressFinalize(this);

}