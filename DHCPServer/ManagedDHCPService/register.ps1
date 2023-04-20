#Requires -RunAsAdministrator

# sc create ManagedDHCPServer binPath="C:\Users\mikmak\Desktop\tmptmp\ManagedDHCPServer\bin\Release\net7.0-windows\win-x64\publish\ManagedDHCPServer.exe"
$serviceExe=get-item ".\ManagedDHCPService.exe"

if($null -eq $serviceExe)
{
	throw "Can't find ManagedDHCPService.exe"
}

$service=Get-Service -Name 'ManagedDHCPServer' -ErrorAction SilentlyContinue
if($null -eq $service)
{
	# scrub stale firewall rules..
	Get-NetFirewallRule -Name 'ManagedDHCPServerIn' -ErrorAction SilentlyContinue | Remove-NetFireWallRule
	Get-NetFirewallRule -Name 'ManagedDHCPServerOut' -ErrorAction SilentlyContinue | Remove-NetFirewallRule

	"Creating firewall rules.."

	# create new firewall rules
	New-NetFirewallRule -Name 'ManagedDHCPServerIn' -DisplayName 'Managed DHCP Server (inbound)' -Direction Inbound -Action Allow -Protocol UDP -LocalPort 67 -Program $serviceExe.FullName | Out-Null
	New-NetFirewallRule -Name 'ManagedDHCPServerOut' -DisplayName 'Managed DHCP Server (outbound)' -Direction Outbound -Action Allow -Protocol UDP -LocalPort 67 -Program $serviceExe.FullName | Out-Null

	"Registering service .."

	# register service
	New-Service -Name 'ManagedDHCPServer' -DisplayName 'Managed DHCP Server' -Description "Managed DHCP Server: $($serviceExe.FullName)" -BinaryPathName $serviceExe.FullName -StartupType Manual -DependsOn ('tcpip') | Out-Null
}
else
{
	"Service already registered"
}

"Done."