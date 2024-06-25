using GandiDynamicDns.Net.Dns;
using GandiDynamicDns.Net.Stun;
using GandiDynamicDns.Unfucked.Tasks;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;

namespace GandiDynamicDns;

public interface DynamicDnsService: IDisposable {

    IPAddress? selfWanAddress { get; }

}

public class DynamicDnsServiceImpl(DnsManager dns, SelfWanAddressClient stun, IOptions<Configuration> configuration, ILogger<DynamicDnsServiceImpl> logger, IHostApplicationLifetime lifetime)
    : BackgroundService, DynamicDnsService {

    private const string DNS_A_RECORD = "A";

    public IPAddress? selfWanAddress { get; private set; }

    private readonly EventLog? eventLog =
#if WINDOWS
        new("Application") { Source = "GandiDynamicDns" };
#else
        null;
#endif

    protected override async Task ExecuteAsync(CancellationToken ct) {
        if ((await dns.fetchDnsRecords(configuration.Value.subdomain, configuration.Value.domain, DnsRecordType.A, ct)).FirstOrDefault() is { } existingIpAddress) {
            try {
                selfWanAddress = IPAddress.Parse(existingIpAddress);
            } catch (FormatException) { }
        }
        logger.LogInformation("On startup, the {fqdn} DNS A record was pointing to {address}", configuration.Value.fqdn, selfWanAddress?.ToString() ?? "(nothing)");

        while (!ct.IsCancellationRequested) {
            await updateDnsRecordIfNecessary(ct);

            if (configuration.Value.updateInterval > TimeSpan.Zero) {
                await Task2.Delay(configuration.Value.updateInterval, ct);
            } else {
                lifetime.StopApplication();
                break;
            }
        }
    }

    private async Task updateDnsRecordIfNecessary(CancellationToken ct = default) {
        SelfWanAddressResponse stunResponse = await stun.getSelfWanAddress(ct);
        if (stunResponse.selfWanAddress != null && !stunResponse.selfWanAddress.Equals(selfWanAddress)) {
            logger.LogInformation("This computer's public IP address changed from {old} to {new} according to {server} ({serverAddr}), updating {fqdn} A record in DNS server", selfWanAddress,
                stunResponse.selfWanAddress, stunResponse.server.Host, stunResponse.serverAddress.ToString(), configuration.Value.fqdn);
#if WINDOWS
            eventLog?.WriteEntry(
                $"This computer's public IP address changed from {selfWanAddress} to {stunResponse.selfWanAddress}, according to {stunResponse.server.Host} ({stunResponse.serverAddress}), updating {configuration.Value.fqdn} A record in DNS server",
                EventLogEntryType.Information, 1);
#endif

            selfWanAddress = stunResponse.selfWanAddress;
            await updateDnsRecord(stunResponse.selfWanAddress, ct);
        } else {
            logger.LogDebug("Not updating DNS {type} record for {fqdn} because it is already set to {value}", DNS_A_RECORD, configuration.Value.fqdn, selfWanAddress);
        }
    }

    private async Task updateDnsRecord(IPAddress currentIPAddress, CancellationToken ct = default) {
        if (!configuration.Value.dryRun) {
            await dns.setDnsRecord(configuration.Value.subdomain, configuration.Value.domain, DnsRecordType.A, configuration.Value.dnsRecordTimeToLive, [currentIPAddress.ToString()], ct);
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