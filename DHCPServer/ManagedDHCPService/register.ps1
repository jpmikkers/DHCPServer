# sc create ManagedDHCPServer binPath="C:\Users\mikmak\Desktop\tmptmp\ManagedDHCPServer\bin\Release\net7.0-windows\win-x64\publish\ManagedDHCPServer.exe"
$serviceExe=get-item ".\ManagedDHCPService.exe"

$service=Get-Service -Name 'ManagedDHCPServer'
if($service -ne $null)
{
	"Service already registered"
}
else
{
	New-Service -Name 'ManagedDHCPServer' -DisplayName 'Managed DHCP Server' -BinaryPathName $serviceExe.FullName -StartupType Manual
}
