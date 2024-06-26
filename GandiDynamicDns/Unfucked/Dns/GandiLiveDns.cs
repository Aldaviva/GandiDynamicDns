using G6.GandiLiveDns;
using GandiDynamicDns.Unfucked.Http;
using System.CodeDom.Compiler;
using System.Diagnostics;

// ReSharper disable InconsistentNaming - these names must match a third-party library

namespace GandiDynamicDns.Unfucked.Dns;

[GeneratedCode("G6.GandiLiveDns", "1.0.0")]
public class GandiLiveDns: IGandiLiveDns {

    #region New

    private const string DEFAULT_BASE_URL = "https://api.gandi.net/v5/livedns";

    private readonly HttpClient                   httpClient;
    private readonly G6.GandiLiveDns.GandiLiveDns gandi;

    public GandiLiveDns(): this(new FilteringHttpClientHandler()) { }

    public GandiLiveDns(FilteringHttpClientHandler httpClientHandler) {
        httpClient = new HttpClient(httpClientHandler, true);
        gandi      = new G6.GandiLiveDns.GandiLiveDns(DEFAULT_BASE_URL, httpClient);

        httpClientHandler.requestFilters.Add(new GandiPersonalAccessTokenAuthenticationFilter(_ => ValueTask.FromResult(PersonalAccessToken)));
    }

    public string? PersonalAccessToken { get; set; } = null;

    public void Dispose() {
        httpClient.Dispose();
    }

    #endregion

    #region Delegated

    public string BaseUrl {
        [DebuggerStepThrough] get => gandi.BaseUrl;
        [DebuggerStepThrough] set => gandi.BaseUrl = value;
    }

    public string ApiKey {
        [DebuggerStepThrough] get => gandi.Apikey;
        [DebuggerStepThrough] set => gandi.Apikey = value;
    }

    public bool ReadResponseAsString {
        [DebuggerStepThrough] get => gandi.ReadResponseAsString;
        [DebuggerStepThrough] set => gandi.ReadResponseAsString = value;
    }

    [DebuggerStepThrough]
    public async Task<IEnumerable<GandiLiveDnsListRecord>> GetDomainRecords(string domain, CancellationToken cancellationToken) {
        return await gandi.GetDomainRecords(domain, cancellationToken);
    }

    [DebuggerStepThrough]
    public async Task<bool> PostDomainRecord(string domain, string name, string type, string[] values, int ttl, CancellationToken cancellationToken) {
        return await gandi.PostDomainRecord(domain, name, type, values, ttl, cancellationToken);
    }

    [DebuggerStepThrough]
    public async Task<bool> PutDomainRecord(string domain, string name, string type, string[] values, int ttl, CancellationToken cancellationToken) {
        return await gandi.PutDomainRecord(domain, name, type, values, ttl, cancellationToken);
    }

    [DebuggerStepThrough]
    public async Task<bool> DeleteDomainRecord(string domain, string name, string type, CancellationToken cancellationToken) {
        return await gandi.DeleteDomainRecord(domain, name, type, cancellationToken);
    }

    #endregion

}