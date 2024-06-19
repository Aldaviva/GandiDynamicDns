namespace Tests.Mocks;

/**
 * https://fakeiteasy.github.io/docs/8.2.0/Recipes/faking-http-client/#easier-and-safer-call-configuration
 */
public abstract class FakeHttpMessageHandler: HttpMessageHandler {

    public abstract Task<HttpResponseMessage> fakeSendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken);

    // sealed so FakeItEasy won't intercept calls to this method
    protected sealed override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => fakeSendAsync(request, cancellationToken);

}