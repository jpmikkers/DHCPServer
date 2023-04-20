# sc delete ManagedDHCPServer

$service=Get-Service -Name 'ManagedDHCPServer'
if($service -ne $null)
{
	$service | Stop-Service
	$service | Remove-Service
}
