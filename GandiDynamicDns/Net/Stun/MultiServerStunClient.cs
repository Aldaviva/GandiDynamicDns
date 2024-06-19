﻿using GandiDynamicDns.Unfucked.Caching;
using GandiDynamicDns.Unfucked.Stun;
using STUN.Enums;
using STUN.StunResult;
using System.Net;

namespace GandiDynamicDns.Net.Stun;

public class MultiServerStunClient(HttpClient http, ILogger<MultiServerStunClient> logger): IStunClient5389 {

    private static readonly Random     RANDOM                   = new();
    private static readonly IPEndPoint LOCAL_HOST               = new(IPAddress.Any, 0);
    private static readonly TimeSpan   STUN_LIST_CACHE_DURATION = TimeSpan.FromDays(1);

    // ExceptionAdjustment: M:System.Uri.#ctor(System.String) -T:System.UriFormatException
    private static readonly Uri STUN_SERVER_LIST_URL = new("https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_ipv4s.txt");

    private static readonly IMemoryCache<IEnumerable<IPEndPoint>> SERVERS_CACHE = new MemoryCache<IEnumerable<IPEndPoint>>($"{nameof(MultiServerStunClient)}.{nameof(SERVERS_CACHE)}");

    private static readonly IList<DnsEndPoint> FALLBACK_SERVERS = [
        new DnsEndPoint("stun.ekiga.net", 3478),
        new DnsEndPoint("stun.freeswitch.org", 3478),
        new DnsEndPoint("stun1.l.google.com", 19302),
        new DnsEndPoint("stun2.l.google.com", 19302),
        new DnsEndPoint("stun3.l.google.com", 19302),
        new DnsEndPoint("stun4.l.google.com", 19302)
    ];

    public StunResult5389 State { get; private set; } = new();

    public IPEndPoint Server { get; private set; } = null!;

    private async Task<IEnumerable<IStunClient5389>> getStunClients(CancellationToken ct = default) {
        const string STUN_LIST_CACHE_KEY = "always-on-stun";

        IPEndPoint[] servers = (await SERVERS_CACHE.GetOrAdd(STUN_LIST_CACHE_KEY, async () => await fetchStunServers(ct), STUN_LIST_CACHE_DURATION)).ToArray();
        RANDOM.Shuffle(servers);
        return servers.Select(serverHost => new StunClient5389UDP(serverHost, LOCAL_HOST));
    }

    private async Task<IEnumerable<IPEndPoint>> fetchStunServers(CancellationToken ct) {
        try {
            logger.LogDebug("Fetching list of STUN servers from pradt2/always-online-stun");
            ICollection<IPEndPoint> servers = (await http.GetStringAsync(STUN_SERVER_LIST_URL, ct)).TrimEnd().Split('\n').Select(line => {
                string[] columns = line.Split(':', 2);
                try {
                    return new IPEndPoint(IPAddress.Parse(columns[0]), ushort.Parse(columns[1]));
                } catch (FormatException) {
                    return null;
                }
            }).Compact().ToList();
            logger.LogDebug("Fetched {count:N0} STUN servers", servers.Count);
            return servers;
        } catch (HttpRequestException) {
            return await getFallbackStunServers();
        } catch (TaskCanceledException) {
            return await getFallbackStunServers();
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return await getFallbackStunServers();
        }

        async Task<IEnumerable<IPEndPoint>> getFallbackStunServers() => (await Task.WhenAll(FALLBACK_SERVERS.Select(async dnsHost =>
            await dnsHost.Resolve(ct)))).Compact();
    }

    public async ValueTask QueryAsync(CancellationToken cancellationToken = default) {
        foreach (IStunClient5389 stun in await getStunClients(cancellationToken)) {
            using (stun) {
                Server = stun.Server;
                await stun.QueryAsync(cancellationToken);
                State = stun.State;
                if (isSuccessfulResponse(State)) {
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
                if (isSuccessfulResponse(State)) {
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
                if (isSuccessfulResponse(State)) {
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
                if (isSuccessfulResponse(State)) {
                    break;
                } else {
                    logger.LogWarning("STUN request to {host} failed, trying another server", stun.Server.ToString());
                }
            }
        }
    }

    private static bool isSuccessfulResponse(StunResult5389 response) =>
        response.BindingTestResult == BindingTestResult.Success &&
        response.FilteringBehavior != FilteringBehavior.UnsupportedServer &&
        response.MappingBehavior != MappingBehavior.Fail &&
        response.MappingBehavior != MappingBehavior.UnsupportedServer;

    public void Dispose() => GC.SuppressFinalize(this);

}