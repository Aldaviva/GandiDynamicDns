using GandiDynamicDns.Unfucked.DependencyInjection;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using System.Net;
using System.Net.Sockets;

namespace GandiDynamicDns;

public static class Extensions {

    /// <summary>
    /// <para>By default, the .NET host only looks for configuration files in the working directory, not the installation directory, which breaks when you run the program from any other directory.</para>
    /// <para>Fix this by also looking for JSON configuration files in the same directory as this executable.</para>
    /// </summary>
    /// <param name="builder"><see cref="HostApplicationBuilder.Configuration"/></param>
    /// <returns>the same <see cref="IConfigurationBuilder"/> for chaining</returns>
    // ExceptionAdjustment: M:System.Collections.Generic.IList`1.Insert(System.Int32,`0) -T:System.NotSupportedException
    public static IConfigurationBuilder AlsoSearchForJsonFilesInExecutableDirectory(this IConfigurationBuilder builder) {
        string? installationDir;
        try {
            installationDir = Path.GetDirectoryName(Environment.ProcessPath);
        } catch (PathTooLongException) {
            return builder;
        }

        if (installationDir != null) {
            PhysicalFileProvider fileProvider = new(installationDir);

            IEnumerable<(int index, IConfigurationSource source)> sourcesToAdd = builder.Sources.SelectMany<IConfigurationSource, (int, IConfigurationSource)>((src, oldIndex) =>
                src is JsonConfigurationSource { Path: { } path } source
                    ? [
                        (oldIndex, new JsonConfigurationSource {
                            FileProvider   = fileProvider,
                            Path           = path,
                            Optional       = true,
                            ReloadOnChange = source.ReloadOnChange,
                            ReloadDelay    = source.ReloadDelay
                        })
                    ]
                    : []).ToList();

            int sourcesAdded = 0;
            foreach ((int index, IConfigurationSource? source) in sourcesToAdd) {
                // this list instance is not read-only
                builder.Sources.Insert(index + sourcesAdded++, source);
            }
        }

        return builder;
    }

    public static IEnumerable<T> Compact<T>(this IEnumerable<T?> source) where T: class {
        return source.Where(item => item != null)!;
    }

    public static IEnumerable<T> Compact<T>(this IEnumerable<T?> source) where T: struct {
        return source.Where(item => item != null).Cast<T>();
    }

    public static async Task<IPEndPoint?> Resolve(this DnsEndPoint host, CancellationToken ct = default) {
        try {
            return await System.Net.Dns.GetHostAddressesAsync(host.Host, host.AddressFamily, ct) is [var firstAddress, ..] ? new IPEndPoint(firstAddress, host.Port) : null;
        } catch (SocketException) {
            return null;
        }
    }

    /*public static IServiceCollection AddTransientProviders(this IServiceCollection services) {
        services.Add(services.Select(descriptor => {
            if (descriptor.Lifetime == ServiceLifetime.Transient /*&& descriptor.ServiceType == typeof(IStunClient5389)#1#) {
                Type serviceType        = descriptor.ServiceType;
                Type providerReturnType = typeof(Func<>).MakeGenericType(serviceType);

                DynamicMethod transientProviderMethod = new("TransientProvider", providerReturnType, [typeof(IServiceProvider)], typeof(Extensions).Module);
                ILGenerator   byteCode                = transientProviderMethod.GetILGenerator();

                // push method parameter 0 "provider" onto stack
                byteCode.Emit(OpCodes.Ldarg_0);

                // push ServiceProviderServiceExtensions.GetRequiredService<T>()
                byteCode.Emit(OpCodes.Ldftn, typeof(ServiceProviderServiceExtensions)
                    .GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), 1, BindingFlags.Public | BindingFlags.Static, null, [typeof(IServiceProvider)], [])!
                    .MakeGenericMethod(serviceType));

                // create new function that calls GetRequiredService<T>()
                byteCode.Emit(OpCodes.Newobj, providerReturnType.GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(object), typeof(IntPtr)])!);

                // return the new function
                byteCode.Emit(OpCodes.Ret);

                var transientProvider = (Func<IServiceProvider, Func<object>>) transientProviderMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IServiceProvider), providerReturnType));
                return new ServiceDescriptor(providerReturnType, transientProvider, ServiceLifetime.Transient);
            } else {
                return null;
            }
        }).Compact().ToList());
        return services;
    }*/

    public static IServiceCollection AddInjectableProviders(this IServiceCollection services) {
        services.TryAddSingleton(typeof(Provider<>), typeof(MicrosoftDependencyInjectionServiceProvider<>));
        services.TryAddSingleton(typeof(OptionalProvider<>), typeof(MicrosoftDependencyInjectionServiceProvider<>));
        return services;
    }

}