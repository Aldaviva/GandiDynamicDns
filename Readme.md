GandiDynamicDns
===

Automatically update an A record in Gandi LiveDNS whenever your computer's public IP address changes.

## Prerequisites
- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later
- [Gandi domain name](https://www.gandi.net/en-US/domain)
- Public IPv4 address for your computer
    - IPv6 is not supported because router NATs don't support IPv6 firewall traversal

## Installation
1. Download the [latest release](https://github.com/Aldaviva/GandiDynamicDns/releases/latest)
1. Extract the archive to a directory, such as `C:\Program Files\GandiDynamicDns\`
1. Install the service
    - Windows: `& '.\Install service.ps1'`
    - Linux:
        ```sh
        sudo cp gandidynamicdns.service /etc/systemd/system/
        sudo systemctl daemon-reload
        sudo systemctl enable gandidynamicdns.service
        ```

## Configuration
Open `appsettings.json` in a text editor.

|Key|Type|Example value|Description|
|-|-|-|-|
|`gandiApiKey`|`string`|`abcdefg`|Generate an API key under [Developer access](https://account.gandi.net/en/users/_/security) in your Gandi [Account](https://account.gandi.net/en)|
|`domain`|`string`|`example.com`|The second-level domain name that you registered|
|`subdomain`|`string`|`www`|The subdomain whose DNS record you want to update, not including `domain` or a trailing period. To update `domain` itself, set `subdomain` to `@`.|
|`updateInterval`|`TimeSpan`|`0:05:00`|How frequently this program will check if your public IP address has changed and update DNS|
|`dnsRecordTimeToLive`|`TimeSpan`|`0:05:00`|How long DNS resolvers can cache your record before they must look it up again. Minimum is 5 minutes.|

## Running
- **Manually**: `./GandiDynamicDns`
- **Windows service**: `sc start GandiDynamicDns`
- **Linux systemd service**: `sudo systemctl start gandidynamicdns`