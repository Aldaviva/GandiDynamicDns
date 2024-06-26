using System.Diagnostics.Metrics;
using System.Net;

namespace GandiDynamicDns.Unfucked.Http;

/// <summary>
/// The intersection of the public properties of <see cref="SocketsHttpHandler"/> and <see cref="HttpClientHandler"/>.
/// </summary>
public interface IHttpClientHandler {

    /// <inheritdoc cref="SocketsHttpHandler.AllowAutoRedirect"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    bool allowAutoRedirect { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.AutomaticDecompression"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    DecompressionMethods automaticDecompression { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.CookieContainer"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    CookieContainer cookieContainer { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.Credentials"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    ICredentials? credentials { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.DefaultProxyCredentials"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    ICredentials? defaultProxyCredentials { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.MaxAutomaticRedirections"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    int maxAutomaticRedirections { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.MaxConnectionsPerServer"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    int maxConnectionsPerServer { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.MaxResponseHeadersLength"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    int maxResponseHeadersLength { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.MeterFactory"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    IMeterFactory? meterFactory { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.PreAuthenticate"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    bool preAuthenticate { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.Properties"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    IDictionary<string, object?> properties { get; }

    /// <inheritdoc cref="SocketsHttpHandler.Proxy"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    IWebProxy? proxy { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.UseCookies"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    bool useCookies { get; set; }

    /// <inheritdoc cref="SocketsHttpHandler.UseProxy"/>
    /// <exception cref="NotSupportedException" accessor="get">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    /// <exception cref="NotSupportedException" accessor="set">The concrete handler class is neither a <see cref="SocketsHttpHandler"/> nor a <see cref="HttpClientHandler"/></exception>
    bool useProxy { get; set; }

}