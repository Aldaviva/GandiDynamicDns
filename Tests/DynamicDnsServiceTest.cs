using GandiDynamicDns;
using GandiDynamicDns.Net.Dns;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using Unfucked.STUN;

namespace Tests;

public class DynamicDnsServiceTest {

    private DynamicDnsServiceImpl service;

    private readonly DnsManager               dns      = A.Fake<DnsManager>();
    private readonly ISelfWanAddressClient    stun     = A.Fake<ISelfWanAddressClient>();
    private readonly IHostApplicationLifetime lifetime = A.Fake<IHostApplicationLifetime>();

    private readonly IOptions<Configuration> config = new OptionsWrapper<Configuration>(new Configuration {
        domain              = "example.com",
        subdomain           = "www",
        gandiApiKey         = "abcdef",
        dnsRecordTimeToLive = TimeSpan.FromMinutes(5),
        updateInterval      = TimeSpan.Zero
    });

    public DynamicDnsServiceTest() {
        service = new DynamicDnsServiceImpl(dns, stun, config, new NullLogger<DynamicDnsServiceImpl>(), lifetime);
    }

    [Fact]
    public async Task updateRecord() {
        A.CallTo(() => dns.fetchDnsRecords(A<string>._, A<string>._, A<DnsRecordType>._, A<CancellationToken>._)).Returns(["192.0.2.1"]);
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._))
            .Returns(new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example.com", 3478), IPEndPoint.Parse("192.0.2.3")));

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;

        A.CallTo(() => dns.fetchDnsRecords("www", "example.com", DnsRecordType.A, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => dns.setDnsRecord("www", "example.com", DnsRecordType.A, TimeSpan.FromMinutes(5), A<IEnumerable<string>>.That.IsSameSequenceAs(new[] { "192.0.2.2" }), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => lifetime.StopApplication()).MustHaveHappened();
    }

    [Fact]
    public async Task invalidExistingRecord() {
        A.CallTo(() => dns.fetchDnsRecords(A<string>._, A<string>._, A<DnsRecordType>._, A<CancellationToken>._)).Returns(["hargle"]);
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._))
            .Returns(new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example.com", 3478), IPEndPoint.Parse("192.0.2.3")));

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;

        A.CallTo(() => dns.fetchDnsRecords("www", "example.com", DnsRecordType.A, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => dns.setDnsRecord("www", "example.com", DnsRecordType.A, TimeSpan.FromMinutes(5), A<IEnumerable<string>>.That.IsSameSequenceAs(new[] { "192.0.2.2" }), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => lifetime.StopApplication()).MustHaveHappened();
    }

    [Fact]
    public async Task unchanged() {
        A.CallTo(() => dns.fetchDnsRecords(A<string>._, A<string>._, A<DnsRecordType>._, A<CancellationToken>._)).Returns(["192.0.2.1"]);
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._))
            .Returns(new SelfWanAddressResponse(IPAddress.Parse("192.0.2.1"), new DnsEndPoint("example.com", 3478), IPEndPoint.Parse("192.0.2.3")));

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;

        A.CallTo(() => dns.fetchDnsRecords("www", "example.com", DnsRecordType.A, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => dns.setDnsRecord(A<string>._, A<string>._, A<DnsRecordType>._, A<TimeSpan>._, A<IEnumerable<string>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
        A.CallTo(() => lifetime.StopApplication()).MustHaveHappened();
    }

    [Fact]
    public async Task loop() {
        CancellationTokenSource cts = new();

        service = new DynamicDnsServiceImpl(dns, stun, new OptionsWrapper<Configuration>(new Configuration {
            domain              = "example.com",
            subdomain           = "www",
            gandiApiKey         = "abcdef",
            dnsRecordTimeToLive = TimeSpan.FromMinutes(5),
            updateInterval      = TimeSpan.FromMilliseconds(50)
        }), new NullLogger<DynamicDnsServiceImpl>(), lifetime);

        CountdownEvent latch = new(3);

        A.CallTo(() => dns.fetchDnsRecords(A<string>._, A<string>._, A<DnsRecordType>._, A<CancellationToken>._)).Returns(["192.0.2.1"]);
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._))
            .Invokes(() => {
                try {
                    latch.Signal();
                } catch (InvalidOperationException) { }
            })
            .Returns(new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example.com", 3478), IPEndPoint.Parse("192.0.2.3")));

        await service.StartAsync(cts.Token);
        latch.Wait(10_000);
        await cts.CancelAsync();
        try {
            await service.ExecuteTask!;
        } catch (TaskCanceledException) { }

        A.CallTo(() => dns.fetchDnsRecords("www", "example.com", DnsRecordType.A, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._)).MustHaveHappenedANumberOfTimesMatching(i => i >= 3);
        A.CallTo(() => dns.setDnsRecord("www", "example.com", DnsRecordType.A, TimeSpan.FromMinutes(5), A<IEnumerable<string>>.That.IsSameSequenceAs(new[] { "192.0.2.2" }), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => lifetime.StopApplication()).MustNotHaveHappened();
    }

    [Fact]
    public void disposal() {
        service.Dispose();
    }

    [Fact]
    public async Task unanimousAgreement() {
        service = new DynamicDnsServiceImpl(dns, stun, new OptionsWrapper<Configuration>(new Configuration {
            domain              = "example.com",
            subdomain           = "www",
            gandiApiKey         = "abcdef",
            dnsRecordTimeToLive = TimeSpan.FromMinutes(5),
            updateInterval      = TimeSpan.Zero,
            unanimity           = 3
        }), new NullLogger<DynamicDnsServiceImpl>(), lifetime);

        A.CallTo(() => dns.fetchDnsRecords(A<string>._, A<string>._, A<DnsRecordType>._, A<CancellationToken>._)).Returns(["192.0.2.1"]);
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._)).ReturnsNextFromSequence(
            new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example.com", 3478), IPEndPoint.Parse("192.0.2.3")),
            new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example2.com", 3478), IPEndPoint.Parse("192.1.2.3")),
            new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example3.com", 3478), IPEndPoint.Parse("192.2.2.3"))
        );

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;

        A.CallTo(() => dns.fetchDnsRecords("www", "example.com", DnsRecordType.A, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._)).MustHaveHappened(3, Times.Exactly);
        A.CallTo(() => dns.setDnsRecord("www", "example.com", DnsRecordType.A, TimeSpan.FromMinutes(5), A<IEnumerable<string>>.That.IsSameSequenceAs(new[] { "192.0.2.2" }), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => lifetime.StopApplication()).MustHaveHappened();
    }

    [Fact]
    public async Task unanimousDisagreement() {
        service = new DynamicDnsServiceImpl(dns, stun, new OptionsWrapper<Configuration>(new Configuration {
            domain              = "example.com",
            subdomain           = "www",
            gandiApiKey         = "abcdef",
            dnsRecordTimeToLive = TimeSpan.FromMinutes(5),
            updateInterval      = TimeSpan.Zero,
            unanimity           = 3
        }), new NullLogger<DynamicDnsServiceImpl>(), lifetime);

        A.CallTo(() => dns.fetchDnsRecords(A<string>._, A<string>._, A<DnsRecordType>._, A<CancellationToken>._)).Returns(["192.0.2.1"]);
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._)).ReturnsNextFromSequence(
            new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example.com", 3478), IPEndPoint.Parse("192.0.2.3")),
            new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example2.com", 3478), IPEndPoint.Parse("192.1.2.3")),
            new SelfWanAddressResponse(IPAddress.Parse("192.0.2.3"), new DnsEndPoint("example3.com", 3478), IPEndPoint.Parse("192.2.2.3"))
        );

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;

        A.CallTo(() => dns.fetchDnsRecords("www", "example.com", DnsRecordType.A, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._)).MustHaveHappened(3, Times.Exactly);
        A.CallTo(() => dns.setDnsRecord(A<string>._, A<string>._, DnsRecordType.A, A<TimeSpan>._, A<IEnumerable<string>>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => lifetime.StopApplication()).MustHaveHappened();
    }

    [Fact]
    public async Task duplicateServers() {
        service = new DynamicDnsServiceImpl(dns, stun, new OptionsWrapper<Configuration>(new Configuration {
            domain              = "example.com",
            subdomain           = "www",
            gandiApiKey         = "abcdef",
            dnsRecordTimeToLive = TimeSpan.FromMinutes(5),
            updateInterval      = TimeSpan.Zero,
            unanimity           = 3
        }), new NullLogger<DynamicDnsServiceImpl>(), lifetime);

        A.CallTo(() => dns.fetchDnsRecords(A<string>._, A<string>._, A<DnsRecordType>._, A<CancellationToken>._)).Returns(["192.0.2.1"]);
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._)).ReturnsNextFromSequence(
            new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example.com", 3478), IPEndPoint.Parse("192.0.2.3")),
            new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example2.com", 3478), IPEndPoint.Parse("192.1.2.3")),
            new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example2.com", 3478), IPEndPoint.Parse("192.1.2.3")),
            new SelfWanAddressResponse(IPAddress.Parse("192.0.2.2"), new DnsEndPoint("example3.com", 3478), IPEndPoint.Parse("192.2.2.3"))
        );

        await service.StartAsync(CancellationToken.None);
        await service.ExecuteTask!;

        A.CallTo(() => dns.fetchDnsRecords("www", "example.com", DnsRecordType.A, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => stun.GetSelfWanAddress(A<CancellationToken>._)).MustHaveHappened(4, Times.Exactly);
        A.CallTo(() => dns.setDnsRecord("www", "example.com", DnsRecordType.A, TimeSpan.FromMinutes(5), A<IEnumerable<string>>.That.IsSameSequenceAs(new[] { "192.0.2.2" }), A<CancellationToken>._))
            .MustHaveHappened();
        A.CallTo(() => lifetime.StopApplication()).MustHaveHappened();
    }

}