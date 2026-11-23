[CmdletBinding()]
param(
    [ValidateNotNullOrEmpty()][string]$SqlInstance = '.',
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]{0,100}$')][string]$Database = 'dbMyHotel',
    [ValidateNotNullOrEmpty()][string]$ServerHost = '127.0.0.1',
    [ValidateRange(1,65535)][int]$Port = 8888
)
. "$PSScriptRoot/Team.Common.ps1"
Add-Type -AssemblyName System.Data

# Create only missing files. Existing per-machine settings are never overwritten.
foreach ($project in @('', 'HotelServer')) {
    $directory = if ($project) { Join-Path $script:TeamRoot $project } else { $script:TeamRoot }
    $destination = Join-Path $directory 'App.local.config'
    if (Test-Path -LiteralPath $destination) {
        Write-Host "KEEP $destination (edit this file to change existing settings)"
        continue
    }
    [xml]$config = Get-Content -LiteralPath (Join-Path $directory 'App.config') -Raw
    $sql = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
    $sql['Data Source'] = $SqlInstance
    $sql['Initial Catalog'] = $Database
    $sql['Integrated Security'] = $true
    $sql['Connect Timeout'] = 5
    if ($project) { $config.configuration.connectionStrings.add.connectionString = $sql.ConnectionString }
    foreach ($setting in $config.configuration.appSettings.add) {
        if ($setting.key -eq 'HotelServerHost') { $setting.value = $ServerHost }
        if (-not $project -and $setting.key -eq 'TransportSecurity') { $setting.value = 'None' }
        if ($setting.key -in @('HotelServerPort', 'ListenPort')) { $setting.value = [string]$Port }
    }
    $config.Save($destination)
    Write-Host "CREATED $destination"
}
Write-Host 'Next: powershell -NoProfile -File .\scripts\Build-Team.ps1'
