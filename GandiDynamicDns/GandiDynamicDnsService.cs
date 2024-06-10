using G6.GandiLiveDns;
using GandiDynamicDns.Unfucked;
using GandiDynamicDns.Unfucked.Stun;
using Microsoft.Extensions.Options;
using STUN.Enums;
using STUN.StunResult;
using System.Net;

namespace GandiDynamicDns;

public interface DynamicDnsService: IDisposable {

    IPAddress? selfWanAddress { get; }

}

public class GandiDynamicDnsService(GandiLiveDns gandi, IStunClient5389 stun, IOptions<Configuration> configuration, ILogger<GandiDynamicDnsService> logger): BackgroundService, DynamicDnsService {

    private const string DNS_A_RECORD = "A";

    public IPAddress? selfWanAddress { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        IEnumerable<GandiLiveDnsListRecord> existingDnsRecords = await gandi.GetDomainRecords(configuration.Value.domain, stoppingToken);
        if (existingDnsRecords.FirstOrDefault(record => record.rrset_type == DNS_A_RECORD && record.rrset_name == configuration.Value.subdomain) is { rrset_values: [var existingIpAddress, ..] }) {
            try {
                selfWanAddress = IPAddress.Parse(existingIpAddress);
            } catch (FormatException) { }
        }

        while (!stoppingToken.IsCancellationRequested) {
            await updateDnsRecordIfNecessary(stoppingToken);
            await Task2.Delay(configuration.Value.updateInterval, cancellationToken: stoppingToken);
        }
    }

    private async Task updateDnsRecordIfNecessary(CancellationToken stoppingToken = default) {
        IPAddress? newAddress = await getCurrentAddress(stoppingToken);
        if (newAddress != null && !newAddress.Equals(selfWanAddress)) {
            logger.LogInformation("IP address changed from {old} to {new}, updating {subdomain}.{domain} DNS {type} record", selfWanAddress, newAddress, configuration.Value.subdomain,
                configuration.Value.domain, DNS_A_RECORD);

            selfWanAddress = newAddress;
            await updateDnsRecord(newAddress, stoppingToken);
        } else {
            logger.LogDebug("Not updating DNS {type} record for {subdomain}.{domain} because it is already set to {new}", DNS_A_RECORD, configuration.Value.subdomain, configuration.Value.domain,
                selfWanAddress);
        }
    }

    private async Task updateDnsRecord(IPAddress currentIPAddress, CancellationToken stoppingToken = default) {
        await gandi.PostDomainRecord(configuration.Value.domain, configuration.Value.subdomain, DNS_A_RECORD, [currentIPAddress.ToString()], (int) configuration.Value.dnsRecordTimeToLive.TotalSeconds,
            stoppingToken);
    }

    private async Task<IPAddress?> getCurrentAddress(CancellationToken stoppingToken = default) {
        StunResult5389 result = await stun.BindingTestAsync(stoppingToken);
        return result.BindingTestResult == BindingTestResult.Success ? result.PublicEndPoint?.Address : null;
    }

}