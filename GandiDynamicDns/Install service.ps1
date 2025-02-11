$binaryPathName = Resolve-Path(join-path $PSScriptRoot "GandiDynamicDns.exe")

New-Service -Name "GandiDynamicDns" -DisplayName "Gandi Dynamic DNS" -Description "Keep a Gandi DNS A record updated with this computer's public IP address." -BinaryPathName $binaryPathName.Path -DependsOn Tcpip
sc.exe failure GandiDynamicDns actions= restart/60000/restart/60000/restart/60000 reset= 3600
