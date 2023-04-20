#Requires -RunAsAdministrator

# sc delete ManagedDHCPServer

$service=Get-Service -Name 'ManagedDHCPServer' -ErrorAction SilentlyContinue
if($null -ne $service)
{
	"Stopping service.."
	$service | Stop-Service
	"Removing service.."
	$service | Remove-Service
}

"Removing firewall rules.."
Get-NetFirewallRule -Name 'ManagedDHCPServerIn' -ErrorAction SilentlyContinue | Remove-NetFireWallRule
Get-NetFirewallRule -Name 'ManagedDHCPServerOut' -ErrorAction SilentlyContinue | Remove-NetFirewallRule

"Done."
