using System.Net;
using System.Net.Sockets;

using HttpClient http = new();

string lines = await http.GetStringAsync("https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts.txt");

IList<string> hostnames = lines.Trim().Split('\n').Select(line => line.Split(':', 2)[0]).ToList();

Console.WriteLine("hostname,ip addresses");
IEnumerable<Task> dnsLookups = hostnames.Select(async hostname => {
    IEnumerable<IPAddress> ipAddresses = await Dns.GetHostAddressesAsync(hostname, AddressFamily.InterNetwork);
    Console.WriteLine($"{hostname},{string.Join(';', ipAddresses)}");
});

await Task.WhenAll(dnsLookups);