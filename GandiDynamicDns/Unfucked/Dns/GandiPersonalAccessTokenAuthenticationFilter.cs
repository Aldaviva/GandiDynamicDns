using GandiDynamicDns.Unfucked.Http;
using System.Net.Http.Headers;

namespace GandiDynamicDns.Unfucked.Dns;

public class GandiPersonalAccessTokenAuthenticationFilter(Func<CancellationToken?, ValueTask<string?>> personalAccessTokenProvider): ClientRequestFilter {

    private const string PERSONAL_ACCESS_TOKEN_AUTHENTICATION_SCHEME = "Bearer";

    public GandiPersonalAccessTokenAuthenticationFilter(string personalAccessToken): this(_ => ValueTask.FromResult<string?>(personalAccessToken)) { }

    public async ValueTask filter(HttpRequestMessage request, CancellationToken cancellationToken) {
        if (request.Headers.Authorization is null or { Scheme: "Apikey", Parameter.Length: 0 } && await personalAccessTokenProvider(cancellationToken) is { } personalAccessToken) {
            request.Headers.Authorization = new AuthenticationHeaderValue(PERSONAL_ACCESS_TOKEN_AUTHENTICATION_SCHEME, personalAccessToken);
        }
    }

}