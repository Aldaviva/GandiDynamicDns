using GandiDynamicDns.Unfucked.Dns;

namespace GandiDynamicDns.Net.Dns;

public interface DnsManager {

    Task<IEnumerable<string>> fetchDnsRecords(string subdomain, string domain, DnsRecordType type = DnsRecordType.A, CancellationToken ct = default);

    Task setDnsRecord(string subdomain, string domain, DnsRecordType type, TimeSpan timeToLive, IEnumerable<string> values, CancellationToken ct = default);

}

public class GandiDnsManager(IGandiLiveDns gandi): DnsManager {

    public static readonly  TimeSpan MINIMUM_TIME_TO_LIVE = TimeSpan.FromSeconds(300);       // 5 minutes
    private static readonly TimeSpan MAXIMUM_TIME_TO_LIVE = TimeSpan.FromSeconds(2_592_000); // 30 days

    public async Task<IEnumerable<string>> fetchDnsRecords(string subdomain, string domain, DnsRecordType type = DnsRecordType.A, CancellationToken ct = default) =>
        from record in await gandi.GetDomainRecords(domain, ct)
        where record.rrset_type == type.ToString() && record.rrset_name == subdomain && record.rrset_values.LongLength != 0
        select record.rrset_values[0];

    public async Task setDnsRecord(string subdomain, string domain, DnsRecordType type, TimeSpan timeToLive, IEnumerable<string> values, CancellationToken ct = default) =>
        await gandi.PutDomainRecord(domain: domain,
            name: subdomain,
            type: type.ToString(),
            values: values.ToArray(),
            ttl: (int) (timeToLive > MINIMUM_TIME_TO_LIVE ? timeToLive < MAXIMUM_TIME_TO_LIVE ? timeToLive : MAXIMUM_TIME_TO_LIVE : MINIMUM_TIME_TO_LIVE).TotalSeconds,
            cancellationToken: ct);

}