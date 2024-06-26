using System.Diagnostics.Metrics;
using System.Net;
using System.Reflection;

#pragma warning disable IDE0066 // broken switch statement refactoring

namespace GandiDynamicDns.Unfucked.Http;

public interface IFilteringHttpClientHandler {

    IList<ClientRequestFilter> requestFilters { get; }
    IList<ClientResponseFilter> responseFilters { get; }
    public HttpMessageHandler actualHandler { get; }

}

public class FilteringHttpClientHandler: HttpMessageHandler, IFilteringHttpClientHandler, IHttpClientHandler {

    public HttpMessageHandler actualHandler { get; }

    public IList<ClientRequestFilter> requestFilters { get; } = [];
    public IList<ClientResponseFilter> responseFilters { get; } = [];

    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> actualSendAsync;

    public FilteringHttpClientHandler(): this(new SocketsHttpHandler()) { }

    public FilteringHttpClientHandler(HttpMessageHandler actualHandler) {
        this.actualHandler = actualHandler;

        MethodInfo sendAsyncMethod = typeof(HttpMessageHandler).GetMethod(nameof(SendAsync), BindingFlags.NonPublic | BindingFlags.Instance, [typeof(HttpRequestMessage), typeof(CancellationToken)])!;
        actualSendAsync = sendAsyncMethod.CreateDelegate<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>>(actualHandler);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        foreach (ClientRequestFilter requestFilter in requestFilters) {
            await requestFilter.filter(request, cancellationToken);
        }

        HttpResponseMessage response = await actualSendAsync(request, cancellationToken);

        foreach (ClientResponseFilter responseFilter in responseFilters) {
            await responseFilter.filter(response, cancellationToken);
        }

        return response;
    }

    private NotSupportedException notSupported => new($"{actualHandler.GetType().Name} is neither {nameof(SocketsHttpHandler)} nor {typeof(HttpClientHandler)}");

    #region Delegated properties

    /// <inheritdoc />
    public bool allowAutoRedirect {
        get => actualHandler switch {
            SocketsHttpHandler h => h.AllowAutoRedirect,
            HttpClientHandler h  => h.AllowAutoRedirect,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.AllowAutoRedirect = value;
                    break;
                case HttpClientHandler h:
                    h.AllowAutoRedirect = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public DecompressionMethods automaticDecompression {
        get => actualHandler switch {
            SocketsHttpHandler h => h.AutomaticDecompression,
            HttpClientHandler h  => h.AutomaticDecompression,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.AutomaticDecompression = value;
                    break;
                case HttpClientHandler h:
                    h.AutomaticDecompression = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public CookieContainer cookieContainer {
        get => actualHandler switch {
            SocketsHttpHandler h => h.CookieContainer,
            HttpClientHandler h  => h.CookieContainer,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.CookieContainer = value;
                    break;
                case HttpClientHandler h:
                    h.CookieContainer = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public ICredentials? credentials {
        get => actualHandler switch {
            SocketsHttpHandler h => h.Credentials,
            HttpClientHandler h  => h.Credentials,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.Credentials = value;
                    break;
                case HttpClientHandler h:
                    h.Credentials = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public ICredentials? defaultProxyCredentials {
        get => actualHandler switch {
            SocketsHttpHandler h => h.DefaultProxyCredentials,
            HttpClientHandler h  => h.DefaultProxyCredentials,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.DefaultProxyCredentials = value;
                    break;
                case HttpClientHandler h:
                    h.DefaultProxyCredentials = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public int maxAutomaticRedirections {
        get => actualHandler switch {
            SocketsHttpHandler h => h.MaxAutomaticRedirections,
            HttpClientHandler h  => h.MaxAutomaticRedirections,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.MaxAutomaticRedirections = value;
                    break;
                case HttpClientHandler h:
                    h.MaxAutomaticRedirections = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public int maxConnectionsPerServer {
        get => actualHandler switch {
            SocketsHttpHandler h => h.MaxConnectionsPerServer,
            HttpClientHandler h  => h.MaxConnectionsPerServer,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.MaxConnectionsPerServer = value;
                    break;
                case HttpClientHandler h:
                    h.MaxConnectionsPerServer = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public int maxResponseHeadersLength {
        get => actualHandler switch {
            SocketsHttpHandler h => h.MaxResponseHeadersLength,
            HttpClientHandler h  => h.MaxResponseHeadersLength,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.MaxResponseHeadersLength = value;
                    break;
                case HttpClientHandler h:
                    h.MaxResponseHeadersLength = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public IMeterFactory? meterFactory {
        get => actualHandler switch {
            SocketsHttpHandler h => h.MeterFactory,
            HttpClientHandler h  => h.MeterFactory,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.MeterFactory = value;
                    break;
                case HttpClientHandler h:
                    h.MeterFactory = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public bool preAuthenticate {
        get => actualHandler switch {
            SocketsHttpHandler h => h.PreAuthenticate,
            HttpClientHandler h  => h.PreAuthenticate,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.PreAuthenticate = value;
                    break;
                case HttpClientHandler h:
                    h.PreAuthenticate = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public IDictionary<string, object?> properties => actualHandler switch {
        SocketsHttpHandler h => h.Properties,
        HttpClientHandler h  => h.Properties,
        _                    => throw notSupported
    };

    /// <inheritdoc />
    public IWebProxy? proxy {
        get => actualHandler switch {
            SocketsHttpHandler h => h.Proxy,
            HttpClientHandler h  => h.Proxy,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.Proxy = value;
                    break;
                case HttpClientHandler h:
                    h.Proxy = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public bool useCookies {
        get => actualHandler switch {
            SocketsHttpHandler h => h.UseCookies,
            HttpClientHandler h  => h.UseCookies,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.UseCookies = value;
                    break;
                case HttpClientHandler h:
                    h.UseCookies = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    /// <inheritdoc />
    public bool useProxy {
        get => actualHandler switch {
            SocketsHttpHandler h => h.UseProxy,
            HttpClientHandler h  => h.UseProxy,
            _                    => throw notSupported
        };
        set {
            switch (actualHandler) {
                case SocketsHttpHandler h:
                    h.UseProxy = value;
                    break;
                case HttpClientHandler h:
                    h.UseProxy = value;
                    break;
                default:
                    throw notSupported;
            }
        }
    }

    #endregion

}