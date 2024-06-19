using G6.GandiLiveDns;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;

// ReSharper disable InconsistentNaming - these names must match a third-party library

namespace GandiDynamicDns.Unfucked.Dns;

[GeneratedCode("G6.GandiLiveDns", "1.0.0")]
public class GandiLiveDns: IGandiLiveDns {

    #region Custom

    private readonly G6.GandiLiveDns.GandiLiveDns gandi;

    private readonly bool shouldDisposeHttpClient;

    private GandiLiveDns(G6.GandiLiveDns.GandiLiveDns gandi, bool shouldDisposeHttpClient) {
        this.gandi                   = gandi;
        this.shouldDisposeHttpClient = shouldDisposeHttpClient;

        // MethodInfo prepareRequestMethod = typeof(G6.GandiLiveDns.GandiLiveDns).GetMethod("PrepareRequest", BindingFlags.NonPublic, [typeof(HttpClient), typeof(HttpRequestMessage), typeof(string)])!;
        // prepareRequestMethod.GetMethodBody().GetILAsByteArray()
    }

    public GandiLiveDns(): this(new G6.GandiLiveDns.GandiLiveDns(), true) { }

    public GandiLiveDns(string baseUrl, HttpClient httpClient): this(new G6.GandiLiveDns.GandiLiveDns(baseUrl, httpClient), false) { }

    protected void PrepareRequest2(HttpClient client, HttpRequestMessage request, string url) { }

    /*
     * I added this myself, don't replace it when generating a new version of this file
     */
    public void Dispose() {
        if (shouldDisposeHttpClient) {
            HttpClient httpClient = (HttpClient) typeof(G6.GandiLiveDns.GandiLiveDns).GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(info => info.FieldType == typeof(HttpClient))
                .GetValue(gandi)!;
            httpClient.Dispose();
        }
    }

    #endregion

    #region Third-party

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