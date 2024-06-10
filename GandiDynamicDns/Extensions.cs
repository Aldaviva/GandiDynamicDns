using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.FileProviders;

namespace GandiDynamicDns;

public static class Extensions {

    /// <summary>
    /// <para>By default, the .NET host only looks for configuration files in the working directory, not the installation directory, which breaks when you run the program from any other directory.</para>
    /// <para>Fix this by also looking for JSON configuration files in the same directory as this executable.</para>
    /// </summary>
    /// <param name="builder"><see cref="HostApplicationBuilder.Configuration"/></param>
    /// <returns>the same <see cref="IConfigurationBuilder"/> for chaining</returns>
    public static IConfigurationBuilder AlsoSearchForJsonFilesInExecutableDirectory(this IConfigurationBuilder builder) {
        if (Path.GetDirectoryName(Environment.ProcessPath) is { } installationDir) {
            PhysicalFileProvider fileProvider = new(installationDir);

            IEnumerable<(int index, IConfigurationSource source)> sourcesToAdd = builder.Sources.SelectMany<IConfigurationSource, (int, IConfigurationSource)>((src, oldIndex) =>
                src is JsonConfigurationSource { Path: { } path } source
                    ? [(oldIndex, new JsonConfigurationSource { FileProvider = fileProvider, Path = path, Optional = true, ReloadOnChange = source.ReloadOnChange, ReloadDelay = source.ReloadDelay })]
                    : []).ToList();

            int sourcesAdded = 0;
            foreach ((int index, IConfigurationSource? source) in sourcesToAdd) {
                builder.Sources.Insert(index + sourcesAdded++, source);
            }
        }

        return builder;
    }

}