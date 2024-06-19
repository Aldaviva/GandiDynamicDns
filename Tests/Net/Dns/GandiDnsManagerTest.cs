using G6.GandiLiveDns;
using GandiDynamicDns.Net.Dns;
using GandiDynamicDns.Unfucked.Dns;

namespace Tests.Net.Dns;

public class GandiDnsManagerTest {

    private readonly IGandiLiveDns   gandi = A.Fake<IGandiLiveDns>();
    private readonly GandiDnsManager manager;

    public GandiDnsManagerTest() {
        manager = new GandiDnsManager(gandi);
    }

    [Fact]
    public async Task fetchDnsRecords() {
        A.CallTo(() => gandi.GetDomainRecords(A<string>._, A<CancellationToken>._)).Returns([
            new GandiLiveDnsListRecord { rrset_name = "www", rrset_ttl = 500, rrset_type = "A", rrset_values = ["192.0.2.1"] }
        ]);

        IEnumerable<string> actual = await manager.fetchDnsRecords("www", "example.com");

        actual.Should().Equal(["192.0.2.1"]);
    }

    [Fact]
    public async Task fetchNoDnsRecords() {
        A.CallTo(() => gandi.GetDomainRecords(A<string>._, A<CancellationToken>._)).Returns([
            new GandiLiveDnsListRecord { rrset_name = "hargle", rrset_ttl = 500, rrset_type = "A", rrset_values = ["192.0.2.1"] }
        ]);

        IEnumerable<string> actual = await manager.fetchDnsRecords("www", "example.com");

        actual.Should().BeEmpty();
    }

    [Fact]
    public async Task setDnsRecord() {
        await manager.setDnsRecord("www", "example.com", DnsRecordType.A, TimeSpan.Zero, ["192.0.2.1"]);

        A.CallTo(() => gandi.PutDomainRecord("example.com", "www", "A", A<string[]>.That.IsSameSequenceAs(new[] { "192.0.2.1" }), 300, A<CancellationToken>._)).MustHaveHappened();
    }

}