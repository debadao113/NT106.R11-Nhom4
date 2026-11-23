[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$CertificatePath,
    [string]$ClientDirectory,
    [string]$ServerHost = '20.198.74.207',
    [ValidateSet('App.local.config','HotelServerProbe.exe.config','ChuuongTrinhQuanLyKhachSan.exe.config')]
    [string]$ConfigurationFileName
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ClientDirectory)) {
    $ClientDirectory = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($ClientDirectory)) { $ClientDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path }
}
$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new((Resolve-Path -LiteralPath $CertificatePath).Path)
try {
    if ($certificate.NotAfter -le (Get-Date) -or $certificate.NotBefore -gt (Get-Date)) { throw 'Certificate is not valid at the current time.' }
    if ($certificate.GetNameInfo([System.Security.Cryptography.X509Certificates.X509NameType]::DnsName, $false) -ne 'hotelserver') {
        throw 'Expected the HotelServer certificate (DNS name hotelserver).'
    }
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $pin = [BitConverter]::ToString($sha.ComputeHash($certificate.RawData)).Replace('-', '') }
    finally { $sha.Dispose() }
    $settings = @{
        HotelServerHost = $ServerHost
        HotelServerPort = '8888'
        TransportSecurity = 'Tls12'
        TlsTargetHost = 'hotelserver'
        TlsCertificateSha256 = $pin
    }
    $names = if ($ConfigurationFileName) { @($ConfigurationFileName) } else { @('HotelServerProbe.exe.config', 'ChuuongTrinhQuanLyKhachSan.exe.config') }
    $configPaths = $names |
        ForEach-Object { Join-Path $ClientDirectory $_ } | Where-Object { Test-Path -LiteralPath $_ }
    if (@($configPaths).Count -eq 0) { throw 'No client configuration files found in ClientDirectory.' }
    foreach ($path in $configPaths) {
        [xml]$config = Get-Content -LiteralPath $path -Raw
        $appSettingsNode = $config.SelectSingleNode('/configuration/appSettings')
        if ($null -eq $appSettingsNode) { throw 'appSettings is missing.' }
        foreach ($setting in $settings.GetEnumerator()) {
            $entry = $appSettingsNode.SelectNodes('add') | Where-Object { $_.GetAttribute('key') -eq $setting.Key }
            if ($null -eq $entry) {
                $entry = $config.CreateElement('add')
                $entry.SetAttribute('key', $setting.Key)
                [void]$appSettingsNode.AppendChild($entry)
            }
            $entry.SetAttribute('value', [string]$setting.Value)
        }
        $config.Save($path)
    }
    Write-Host ('Client TLS configured. Server: ' + $ServerHost + ':8888')
    Write-Host ('Verify this SHA-256 matches the value printed on the VM: ' + $pin)
} finally { $certificate.Dispose() }
