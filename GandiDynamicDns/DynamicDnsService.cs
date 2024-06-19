using GandiDynamicDns.Net.Dns;
using GandiDynamicDns.Net.Stun;
using GandiDynamicDns.Unfucked.Tasks;
using Microsoft.Extensions.Options;
using System.Net;

namespace GandiDynamicDns;

public interface DynamicDnsService: IDisposable {

    IPAddress? selfWanAddress { get; }

}

public class DynamicDnsServiceImpl(DnsManager dns, SelfWanAddressClient stun, IOptions<Configuration> configuration, ILogger<DynamicDnsServiceImpl> logger, IHostApplicationLifetime lifetime)
    : BackgroundService, DynamicDnsService {

    private const string DNS_A_RECORD = "A";

    public IPAddress? selfWanAddress { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken ct) {
        if ((await dns.fetchDnsRecords(configuration.Value.subdomain, configuration.Value.domain, DnsRecordType.A, ct)).FirstOrDefault() is { } existingIpAddress) {
            try {
                selfWanAddress = IPAddress.Parse(existingIpAddress);
            } catch (FormatException) { }
        }
        logger.LogInformation("On startup, {subdomain}.{domain} was set to {address}", configuration.Value.subdomain, configuration.Value.domain, selfWanAddress?.ToString() ?? "(nothing)");

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
        IPAddress? newAddress = await stun.getSelfWanAddress(ct);
        if (newAddress != null && !newAddress.Equals(selfWanAddress)) {
            logger.LogInformation("IP address changed from {old} to {new}, updating {subdomain}.{domain} DNS {type} record", selfWanAddress, newAddress, configuration.Value.subdomain,
                configuration.Value.domain, DNS_A_RECORD);

            selfWanAddress = newAddress;
            await updateDnsRecord(newAddress, ct);
        } else {
            logger.LogDebug("Not updating DNS {type} record for {subdomain}.{domain} because it is already set to {value}", DNS_A_RECORD, configuration.Value.subdomain, configuration.Value.domain,
                selfWanAddress);
        }
    }

    private async Task updateDnsRecord(IPAddress currentIPAddress, CancellationToken ct = default) {
        await dns.setDnsRecord(configuration.Value.subdomain, configuration.Value.domain, DnsRecordType.A, configuration.Value.dnsRecordTimeToLive, [currentIPAddress.ToString()], ct);
    }

}