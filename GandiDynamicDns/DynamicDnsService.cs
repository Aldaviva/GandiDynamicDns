using Gandi;
using Gandi.Dns;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using ThrottleDebounce;
using Unfucked;
using Unfucked.HTTP.Exceptions;
using Unfucked.STUN;

namespace GandiDynamicDns;

public interface DynamicDnsService: IDisposable {

    IPAddress? selfWanAddress { get; }

}

public class DynamicDnsServiceImpl(ILiveDns liveDns, ISelfWanAddressClient stun, IOptions<Configuration> configuration, ILogger<DynamicDnsServiceImpl> logger, IHostApplicationLifetime lifetime)
    : BackgroundService, DynamicDnsService {

    private static readonly TimeSpan ONE_SHOT_MODE = TimeSpan.Zero;

    public IPAddress? selfWanAddress { get; private set; }

    private readonly IDictionary<string, IPAddress?> initialRecordValues = new Dictionary<string, IPAddress?>(configuration.Value.subdomains.Count);

    private readonly EventLog? eventLog =
#if WINDOWS
        new("Application") { Source = "GandiDynamicDns" };
#else
        null;
#endif

    protected override async Task ExecuteAsync(CancellationToken ct) {
        try {
            foreach (string subdomain in configuration.Value.subdomains) {
                // this retry is to handle the case where the service starts before the computer connects to the network on bootup, not where Gandi's API servers are down
                await Retrier.Attempt(async _ => {
                        IPAddress? initialRecordValue = null;
                        if ((await liveDns.Get(RecordType.A, subdomain, ct))?.Values.First() is { } existingIpAddress) {
                            try {
                                initialRecordValue = IPAddress.Parse(existingIpAddress);
                            } catch (FormatException) { }
                        }
                        initialRecordValues[subdomain] = initialRecordValue;
                    }, maxAttempts: null, delay: Retrier.Delays.Constant(TimeSpan.FromSeconds(3)), ex => ex is not (OutOfMemoryException or GandiException { InnerException: ClientErrorException }),
                    beforeRetry: async (i, e) =>
                        logger.LogWarning("Failed to fetch existing DNS record from Gandi HTTP API server, retrying (attempt {attempt}): {message}", i + 2, e.MessageChain()), ct);

                logger.LogInformation("On startup, the {subdomain}.{domain} DNS A record is pointing to {address}", subdomain, configuration.Value.domain,
                    initialRecordValues[subdomain]?.ToString() ?? "(nothing)");
            }

            if (configuration.Value.updateInterval > ONE_SHOT_MODE) {
                logger.LogInformation("Checking for public IP address changes every {period}", configuration.Value.updateInterval);
            }

            while (!ct.IsCancellationRequested) {
                await updateDnsRecordIfNecessary(ct);

                if (configuration.Value.updateInterval > ONE_SHOT_MODE) {
                    await Tasks.Delay(configuration.Value.updateInterval, ct);
                } else {
                    logger.LogInformation(
                        $"Exiting after one public IP address check. To continue running and checking for IP address changes repeatedly, set {nameof(Configuration.updateInterval)} to a {nameof(TimeSpan)} longer than {{zero}} in appsettings.json.",
                        ONE_SHOT_MODE);
                    lifetime.StopApplication();
                    break;
                }
            }
        } catch (GandiException e) when (e is GandiException.AuthException or { InnerException: ForbiddenException or NotAuthorizedException }) {
            logger.LogError(
                $"Auth error from Gandi. Please check the value of {nameof(Configuration.gandiAuthToken)} in appsettings.json and https://admin.gandi.net/organizations/account/pat to see if the Personal Access Token has expired.");
            throw;
        } catch (GandiException e) {
            logger.LogError(e, "Error while communicating with the Gandi LiveDNS API.");
            throw;
        }
    }

    private async Task updateDnsRecordIfNecessary(CancellationToken ct = default) {
        SelfWanAddressResponse originalResponse = await stun.GetSelfWanAddress(ct);
        if (originalResponse.SelfWanAddress != null) {
            bool updateRequired = selfWanAddress == null
                ? initialRecordValues.Any(pair => !originalResponse.SelfWanAddress.Equals(pair.Value))
                : !originalResponse.SelfWanAddress.Equals(selfWanAddress);
            if (updateRequired) {
                int unanimity = (int) Math.Max(1, configuration.Value.unanimity);
                if (await getUnanimousAgreement(originalResponse, unanimity, ct)) {
                    logger.LogInformation(
                        "This computer's public IP address changed from {old} to {new} according to {server} ({serverAddr}) and {extraServerCount:N0} other STUN servers, updating {recordCount} A records in DNS server",
                        selfWanAddress, originalResponse.SelfWanAddress, originalResponse.Server.Host, originalResponse.ServerAddress.ToString(), unanimity - 1, configuration.Value.subdomains.Count);
#if WINDOWS
                    eventLog?.WriteEntry(
                        $"This computer's public IP address changed from {selfWanAddress} to {originalResponse.SelfWanAddress}, according to {originalResponse.Server.Host} ({originalResponse.ServerAddress}) and {unanimity - 1:N0} others, updating {configuration.Value.subdomains.Count} A records in DNS server",
                        EventLogEntryType.Information, 1);
#endif

                    selfWanAddress = originalResponse.SelfWanAddress;
                    await updateDnsRecords(originalResponse.SelfWanAddress!, ct);
                } else {
                    logger.LogWarning("Not updating DNS A record because there was a disagreement among {serverCount:N0} STUN servers about our public IP address, leaving it set to {value}",
                        unanimity, selfWanAddress);
                }
            } else {
                selfWanAddress ??= originalResponse.SelfWanAddress;
                logger.LogDebug("Not updating DNS A records because they are all already set to {value}", selfWanAddress);
            }
        } else {
            logger.LogDebug("STUN request to {server} ({serverAddr}) did not return a public WAN IP address, will try again with a different server next time", originalResponse.Server.Host,
                originalResponse.ServerAddress.ToString());
        }
    }

    private async Task<bool> getUnanimousAgreement(SelfWanAddressResponse originalResponse, int unanimity = 1, CancellationToken ct = default) {
        IList<SelfWanAddressResponse> extraResponses  = (await Task.WhenAll(Enumerable.Range(1, unanimity - 1).Select(_ => stun.GetSelfWanAddress(ct)))).ToList();
        ISet<string>                  serverHostnames = extraResponses.Append(originalResponse).Select(response => response.Server.Host).ToHashSet();

        while (serverHostnames.Count < unanimity) {
            SelfWanAddressResponse distinctResponse = await stun.GetSelfWanAddress(ct);
            extraResponses.Add(distinctResponse);
            serverHostnames.Add(distinctResponse.Server.Host);
        }

        return extraResponses.All(extra => originalResponse.SelfWanAddress!.Equals(extra.SelfWanAddress));
    }

    private async Task updateDnsRecords(IPAddress currentIPAddress, CancellationToken ct = default) {
        foreach (string subdomain in configuration.Value.subdomains) {
            if (!configuration.Value.dryRun) {
                try {
                    await liveDns.Set(new DnsRecord(RecordType.A, subdomain, configuration.Value.dnsRecordTimeToLive, [currentIPAddress.ToString()]), ct);
                } catch (GandiException e) {
                    logger.LogError(e, "Failed to update DNS record for {subdomain}.{domain} to {newAddr} due to DNS API server error", subdomain, configuration.Value.domain, currentIPAddress);
                    if (e is GandiException.AuthException or { InnerException: ForbiddenException or NotAuthorizedException }) {
                        throw;
                    }
                }
            } else {
                logger.LogInformation("Dry run mode, not updating {subdomain}.{domain} to {newAddr}. To actually make DNS changes, change {dryRun} from true to false in appsettings.json.", subdomain,
                    configuration.Value.domain, currentIPAddress, nameof(Configuration.dryRun));
            }
        }
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            eventLog?.Dispose();
        }
    }

    public sealed override void Dispose() {
        Dispose(true);
        base.Dispose();
        GC.SuppressFinalize(this);
    }

}