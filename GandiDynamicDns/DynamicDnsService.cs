using Gandi;
using Gandi.Dns;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using ThrottleDebounce;
using Unfucked;
using Unfucked.STUN;

namespace GandiDynamicDns;

public interface DynamicDnsService: IDisposable {

    IPAddress? selfWanAddress { get; }

}

public class DynamicDnsServiceImpl(ILiveDns liveDns, ISelfWanAddressClient stun, IOptions<Configuration> configuration, ILogger<DynamicDnsServiceImpl> logger, IHostApplicationLifetime lifetime)
    : BackgroundService, DynamicDnsService {

    public IPAddress? selfWanAddress { get; private set; }

    private readonly EventLog? eventLog =
#if WINDOWS
        new("Application") { Source = "GandiDynamicDns" };
#else
        null;
#endif

    protected override async Task ExecuteAsync(CancellationToken ct) {
        await Retrier.Attempt(async _ => {
                if ((await liveDns.Get(RecordType.A, configuration.Value.subdomain, ct))?.Values.First() is { } existingIpAddress) {
                    try {
                        selfWanAddress = IPAddress.Parse(existingIpAddress);
                    } catch (FormatException) { }
                }
            }, maxAttempts: null, delay: _ => TimeSpan.FromSeconds(15), ex => ex is HttpRequestException or TaskCanceledException,
            beforeRetry: (i, e) =>
                logger.LogWarning("Failed to fetch existing DNS record from Gandi HTTP API server, retrying (attempt {attempt}) {eType}: {message}", i + 2, e.GetType().Name, e.Message), ct);

        logger.LogInformation("On startup, the {fqdn} DNS A record was pointing to {address}", configuration.Value.fqdn, selfWanAddress?.ToString() ?? "(nothing)");

        while (!ct.IsCancellationRequested) {
            await updateDnsRecordIfNecessary(ct);

            if (configuration.Value.updateInterval > TimeSpan.Zero) {
                await Tasks.Delay(configuration.Value.updateInterval, ct);
            } else {
                lifetime.StopApplication();
                break;
            }
        }
    }

    private async Task updateDnsRecordIfNecessary(CancellationToken ct = default) {
        SelfWanAddressResponse originalResponse = await stun.GetSelfWanAddress(ct);
        if (originalResponse.SelfWanAddress != null && !originalResponse.SelfWanAddress.Equals(selfWanAddress)) {
            int unanimity = (int) Math.Max(1, configuration.Value.unanimity);
            if (await getUnanimousAgreement(originalResponse, unanimity, ct)) {
                logger.LogInformation(
                    "This computer's public IP address changed from {old} to {new} according to {server} ({serverAddr}) and {extraServerCount:N0} other STUN servers, updating {fqdn} A record in DNS server",
                    selfWanAddress, originalResponse.SelfWanAddress, originalResponse.Server.Host, originalResponse.ServerAddress.ToString(), unanimity - 1, configuration.Value.fqdn);
#if WINDOWS
                eventLog?.WriteEntry(
                    $"This computer's public IP address changed from {selfWanAddress} to {originalResponse.SelfWanAddress}, according to {originalResponse.Server.Host} ({originalResponse.ServerAddress}) and {unanimity - 1:N0} others, updating {configuration.Value.fqdn} A record in DNS server",
                    EventLogEntryType.Information, 1);
#endif

                selfWanAddress = originalResponse.SelfWanAddress;
                await updateDnsRecord(originalResponse.SelfWanAddress, ct);
            } else {
                logger.LogWarning("Not updating DNS A record for {fqdn} because there was a disagreement among {serverCount:N0} STUN servers about our public IP address, leaving it set to {value}",
                    configuration.Value.fqdn, unanimity, selfWanAddress);
            }
        } else {
            logger.LogDebug("Not updating DNS A record for {fqdn} because it is already set to {value}", configuration.Value.fqdn, selfWanAddress);
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

    private async Task updateDnsRecord(IPAddress currentIPAddress, CancellationToken ct = default) {
        if (!configuration.Value.dryRun) {
            try {
                await liveDns.Set(new DnsRecord(RecordType.A, configuration.Value.subdomain, configuration.Value.dnsRecordTimeToLive, currentIPAddress.ToString()), ct);
            } catch (GandiException e) {
                logger.LogError(e, "Failed to update DNS record for {fqdn} to {newAddr} due to DNS API server error", configuration.Value.fqdn, currentIPAddress);
            }
        } else {
            logger.LogInformation("Dry run mode, not updating {fqdn} to {newAddr}. To actually make DNS changes, change {dryRun} from true to false in appsettings.json.", configuration.Value.fqdn,
                currentIPAddress, nameof(Configuration.dryRun));
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