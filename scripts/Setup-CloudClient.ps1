[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$CertificatePath,
    [string]$ServerHost='20.198.74.207'
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$destination=Join-Path $root 'App.local.config'
$source=if(Test-Path -LiteralPath $destination) { $destination } else { Join-Path $root 'App.config' }
[xml]$config=Get-Content -LiteralPath $source -Raw
$sql=$config.SelectSingleNode('/configuration/connectionStrings')
if($null -ne $sql) { [void]$sql.ParentNode.RemoveChild($sql) }
$config.Save($destination)
& "$PSScriptRoot/Configure-ClientTls.ps1" -CertificatePath $CertificatePath -ClientDirectory $root -ServerHost $ServerHost -ConfigurationFileName 'App.local.config'
Write-Host 'Cloud client ready. Build again. No local database or Azure SQL password is needed.'
