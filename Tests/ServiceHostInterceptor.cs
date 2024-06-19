using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace Tests;

/// <summary>
/// <para>Get access to the <see cref="IHostBuilder"/> and <see cref="IHost"/> created by <see cref="Host.CreateDefaultBuilder()"/> or <see cref="Host.CreateApplicationBuilder()"/>.</para>
/// <para>Similar to the <c>Microsoft.AspNetCore.Mvc.Testing</c> package (<see href="https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests"/>), but for console applications instead of ASP.NET Core webapps.</para>
/// </summary>
internal interface IServiceHostInterceptor: IDisposable {

    IHost? host { get; }

    /// <summary>
    /// Useful for modifying registered services
    /// </summary>
    event EventHandler<IHostBuilder>? hostBuilding;

    event EventHandler<IHost>? hostBuilt;

}

/// <inheritdoc cref="IServiceHostInterceptor" />
internal class ServiceHostInterceptor: IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IServiceHostInterceptor {

    private readonly Stack<IDisposable> toDispose = new();

    public IHost? host { get; private set; }

    public event EventHandler<IHostBuilder>? hostBuilding;
    public event EventHandler<IHost>? hostBuilt;

    public ServiceHostInterceptor() {
        toDispose.Push(DiagnosticListener.AllListeners.Subscribe(this));
    }

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(DiagnosticListener listener) {
        if (listener.Name == "Microsoft.Extensions.Hosting") {
            toDispose.Push(listener.Subscribe(this));
        }
    }

    public void OnNext(KeyValuePair<string, object?> diagnosticEvent) {
        switch (diagnosticEvent.Key) {
            case "HostBuilding":
                // you can add, modify, and remove registered services here
                hostBuilding?.Invoke(this, (IHostBuilder) diagnosticEvent.Value!);
                break;
            case "HostBuilt":
                host = (IHost) diagnosticEvent.Value!;
                hostBuilt?.Invoke(this, host);
                break;
        }
    }

    public void Dispose() {
        while (toDispose.TryPop(out IDisposable? disposable)) {
            disposable.Dispose();
        }
    }

}