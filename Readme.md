<img src="https://github.com/Aldaviva/GandiDynamicDns/raw/master/GandiDynamicDns/favicon.ico" height="24" alt="Gandi" /> GandiDynamicDns
===

[![GitHub Actions](https://img.shields.io/github/actions/workflow/status/Aldaviva/GandiDynamicDns/dotnet.yml?branch=master&logo=github)](https://github.com/Aldaviva/GandiDynamicDns/actions/workflows/dotnet.yml) [![Testspace](https://img.shields.io/testspace/tests/Aldaviva/Aldaviva:GandiDynamicDns/master?passed_label=passing&failed_label=failing&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCA4NTkgODYxIj48cGF0aCBkPSJtNTk4IDUxMy05NCA5NCAyOCAyNyA5NC05NC0yOC0yN3pNMzA2IDIyNmwtOTQgOTQgMjggMjggOTQtOTQtMjgtMjh6bS00NiAyODctMjcgMjcgOTQgOTQgMjctMjctOTQtOTR6bTI5My0yODctMjcgMjggOTQgOTQgMjctMjgtOTQtOTR6TTQzMiA4NjFjNDEuMzMgMCA3Ni44My0xNC42NyAxMDYuNS00NFM1ODMgNzUyIDU4MyA3MTBjMC00MS4zMy0xNC44My03Ni44My00NC41LTEwNi41UzQ3My4zMyA1NTkgNDMyIDU1OWMtNDIgMC03Ny42NyAxNC44My0xMDcgNDQuNXMtNDQgNjUuMTctNDQgMTA2LjVjMCA0MiAxNC42NyA3Ny42NyA0NCAxMDdzNjUgNDQgMTA3IDQ0em0wLTU1OWM0MS4zMyAwIDc2LjgzLTE0LjgzIDEwNi41LTQ0LjVTNTgzIDE5Mi4zMyA1ODMgMTUxYzAtNDItMTQuODMtNzcuNjctNDQuNS0xMDdTNDczLjMzIDAgNDMyIDBjLTQyIDAtNzcuNjcgMTQuNjctMTA3IDQ0cy00NCA2NS00NCAxMDdjMCA0MS4zMyAxNC42NyA3Ni44MyA0NCAxMDYuNVMzOTAgMzAyIDQzMiAzMDJ6bTI3NiAyODJjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjY3IDE0LjY3LTEwNiA0NHMtNDQgNjUtNDQgMTA3YzAgNDEuMzMgMTQuNjcgNzYuODMgNDQgMTA2LjVTNjY2LjY3IDU4NCA3MDggNTg0em0tNTU3IDBjNDIgMCA3Ny42Ny0xNC44MyAxMDctNDQuNXM0NC02NS4xNyA0NC0xMDYuNWMwLTQyLTE0LjY3LTc3LjY3LTQ0LTEwN3MtNjUtNDQtMTA3LTQ0Yy00MS4zMyAwLTc2LjgzIDE0LjY3LTEwNi41IDQ0UzAgMzkxIDAgNDMzYzAgNDEuMzMgMTQuODMgNzYuODMgNDQuNSAxMDYuNVMxMDkuNjcgNTg0IDE1MSA1ODR6IiBmaWxsPSIjZmZmIi8%2BPC9zdmc%2B)](https://aldaviva.testspace.com/spaces/277280) [![Coveralls](https://img.shields.io/coveralls/github/Aldaviva/GandiDynamicDns?logo=coveralls)](https://coveralls.io/github/Aldaviva/GandiDynamicDns?branch=master)

Automatically update a DNS A record in Gandi LiveDNS whenever your computer's public IP address changes, detected automatically using a [large, auto-updating pool](https://github.com/pradt2/always-online-stun) of public [STUN](https://developer.mozilla.org/en-US/docs/Web/API/WebRTC_API/Protocols#stun) servers.

This is an alternative to filling out monthly CAPTCHAs for [No-IP](https://www.noip.com) or paying for [DynDNS](https://account.dyn.com), if you happen to already be paying for a domain name from the world's greatest domain registrar.

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
    - ❌ External nameservers (with glue records) are incompatible; you will need to update the record on the external nameserver instead of on Gandi's nameservers
- Your computer must have a public WAN IPv4 address
    - IPv6 is not supported because router NATs don't support IPv6 port forwarding

## Installation
1. Download the [latest release](https://github.com/Aldaviva/GandiDynamicDns/releases/latest) ZIP archive for your operating system and CPU architecture
1. Extract the ZIP archive to a directory, such as `C:\Program Files\GandiDynamicDns\` or `/opt/gandidynamicdns/`
    - Only extract `appsettings.json` during a new installation, not when upgrading an existing installation
1. Install the service
    - Windows: `& '.\Install service.ps1'`
    - Linux with systemd:
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