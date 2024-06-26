namespace GandiDynamicDns.Unfucked.Http;

public interface ClientResponseFilter {

    ValueTask filter(HttpResponseMessage response, CancellationToken cancellationToken);

}