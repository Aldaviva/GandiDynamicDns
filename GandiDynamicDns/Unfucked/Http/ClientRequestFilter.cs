namespace GandiDynamicDns.Unfucked.Http;

public interface ClientRequestFilter {

    ValueTask filter(HttpRequestMessage request, CancellationToken cancellationToken);

}