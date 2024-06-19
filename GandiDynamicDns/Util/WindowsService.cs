namespace GandiDynamicDns.Util;

public readonly struct WindowsService {

    public static void configure(WindowsServiceLifetimeOptions options) => options.ServiceName = "GandiDynamicDns";

}