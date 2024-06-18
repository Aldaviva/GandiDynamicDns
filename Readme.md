<img src="https://github.com/Aldaviva/GandiDynamicDns/raw/master/GandiDynamicDns/favicon.ico" height="24" alt="Gandi" /> GandiDynamicDns
===

[![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/GandiDynamicDns/dotnet.yml?branch=master&logo=github)](https://github.com/Aldaviva/GandiDynamicDns/actions/workflows/dotnet.yml)

Automatically update a DNS A record in Gandi LiveDNS whenever your computer's public IP address changes, detected automatically using [STUN](https://developer.mozilla.org/en-US/docs/Web/API/WebRTC_API/Protocols#stun).

<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2" -->

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Execution](#execution)

<!-- /MarkdownTOC -->

## Prerequisites
- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later
- [Gandi domain name](https://www.gandi.net/en-US/domain)
    - ✅ Domain must be using [LiveDNS](https://www.gandi.net/en-US/domain/dns), the default for new domains (`ns-*-*.gandi.net`)
    - ❌ Classic DNS (`*.dns.gandi.net`) is incompatible; you will need to [migrate to LiveDNS](https://docs.gandi.net/en/domain_names/common_operations/changing_nameservers.html#switching-to-livedns)
    - ❌ External nameservers (with glue records) are incompatible; you will need to update the record on the external nameserver instead of Gandi's nameservers
- Your computer must have a public WAN IPv4 address
    - IPv6 is not supported because router NATs don't support IPv6 port forwarding

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

|Key|Type|Examples|Description|
|-|-|-|-|
|`gandiApiKey`|`string`|`abcdefg`|Generate an API key under [Developer access](https://account.gandi.net/en/users/_/security) in your Gandi [Account](https://account.gandi.net/en)|
|`domain`|`string`|`example.com`<br>`example.co.uk`|The second-level domain name that you registered|
|`subdomain`|`string`|`www`<br>`@`<br>`en.www`|The subdomain whose DNS record you want to update, not including `domain` or a trailing period. To update `domain` itself, set `subdomain` to `@`. Can also be a multi-level subdomain like `en.www`.|
|`updateInterval`|`TimeSpan`|`0.00:05:00`|How frequently this program will check if your public IP address has changed and update DNS. Format is `d.hh:mm:ss`.|
|`dnsRecordTimeToLive`|`TimeSpan`|`0.00:05:00`|How long DNS resolvers can cache your record before they must look it up again. Gandi's minimum is 5 minutes.|

## Execution
- **Manually**: `./GandiDynamicDns`
- **Windows service**: `sc start GandiDynamicDns`, or use `services.msc`
- **Linux systemd service**: `sudo systemctl start gandidynamicdns`