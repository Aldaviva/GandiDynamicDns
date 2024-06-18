using G6.GandiLiveDns;

namespace GandiDynamicDns.Dns;

public interface DnsManager {

    Task<IEnumerable<string>> fetchDnsRecords(string domain, string subdomain = "@", DnsRecordType type = DnsRecordType.A, CancellationToken ct = default);

    Task setDnsRecord(string domain, string subdomain, DnsRecordType type, TimeSpan timeToLive, IEnumerable<string> values, CancellationToken ct = default);

}

public class GandiDnsManager(GandiLiveDns gandi): DnsManager {

    public static readonly TimeSpan MINIMUM_TIME_TO_LIVE = TimeSpan.FromSeconds(300);

    public async Task<IEnumerable<string>> fetchDnsRecords(string domain, string subdomain = "@", DnsRecordType type = DnsRecordType.A, CancellationToken ct = default) =>
        from record in await gandi.GetDomainRecords(domain, ct)
        where record.rrset_type == type.ToString() && record.rrset_name == subdomain && record.rrset_values.LongLength != 0
        select record.rrset_values[0];

    public async Task setDnsRecord(string domain, string subdomain, DnsRecordType type, TimeSpan timeToLive, IEnumerable<string> values, CancellationToken ct = default) =>
        await gandi.PutDomainRecord(domain: domain,
            name: subdomain,
            type: type.ToString(),
            values: values.ToArray(),
            ttl: (int) (timeToLive > MINIMUM_TIME_TO_LIVE ? timeToLive : MINIMUM_TIME_TO_LIVE).TotalSeconds,
            cancellationToken: ct);

}