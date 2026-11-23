[CmdletBinding()]
param([string]$ServerDirectory = 'C:\HotelServer')

$ErrorActionPreference = 'Stop'
$configPath = Join-Path $ServerDirectory 'HotelServer.exe.config'
if (-not (Test-Path -LiteralPath $configPath)) { throw 'HotelServer.exe.config was not found.' }
if (@(Get-Process HotelServer -ErrorAction SilentlyContinue).Count -gt 0) {
    throw 'Close HotelServer before configuring TLS.'
}
[xml]$config = Get-Content -LiteralPath $configPath -Raw
$appSettingsNode = $config.SelectSingleNode('/configuration/appSettings')
if ($null -eq $appSettingsNode) { throw 'appSettings is missing from the server config.' }
$thumbprintEntry = $appSettingsNode.SelectNodes('add') | Where-Object { $_.GetAttribute('key') -eq 'TlsServerCertificateThumbprint' }
$certificate = $null
if ($null -ne $thumbprintEntry -and -not [string]::IsNullOrWhiteSpace($thumbprintEntry.value)) {
    $certPath = 'Cert:\CurrentUser\My\' + $thumbprintEntry.value
    if (Test-Path -LiteralPath $certPath) {
        $existingCertificate = Get-Item -LiteralPath $certPath
        if ($existingCertificate.HasPrivateKey -and $existingCertificate.NotAfter -gt (Get-Date).AddDays(7) -and
            $existingCertificate.GetNameInfo([System.Security.Cryptography.X509Certificates.X509NameType]::DnsName, $false) -eq 'hotelserver') {
            $certificate = $existingCertificate
        }
    }
}
if ($null -eq $certificate) {
    $certificate = New-SelfSignedCertificate -DnsName 'hotelserver' -Type SSLServerAuthentication `
        -CertStoreLocation 'Cert:\CurrentUser\My' -FriendlyName 'NT106 HotelServer TLS' `
        -KeyAlgorithm RSA -KeyLength 2048 -HashAlgorithm SHA256 -KeyExportPolicy NonExportable `
        -NotAfter (Get-Date).AddMonths(3)
}
foreach ($setting in @{
    TransportSecurity = 'Tls12'
    TlsServerCertificateThumbprint = $certificate.Thumbprint
}.GetEnumerator()) {
    $entry = $appSettingsNode.SelectNodes('add') | Where-Object { $_.GetAttribute('key') -eq $setting.Key }
    if ($null -eq $entry) {
        $entry = $config.CreateElement('add')
        $entry.SetAttribute('key', $setting.Key)
        [void]$appSettingsNode.AppendChild($entry)
    }
    $entry.SetAttribute('value', [string]$setting.Value)
}
$config.Save($configPath)
$publicCertPath = Join-Path $ServerDirectory 'HotelServer-public.cer'
[void](Export-Certificate -Cert $certificate -FilePath $publicCertPath -Type CERT)
$sha = [System.Security.Cryptography.SHA256]::Create()
try { $pin = [BitConverter]::ToString($sha.ComputeHash($certificate.RawData)).Replace('-', '') }
finally { $sha.Dispose() }
Write-Host 'TLS configured for the current Windows user. SQL settings were preserved.'
Write-Host ('Certificate SHA-256: ' + $pin)
Write-Host ('Copy only this PUBLIC certificate back to the client PC: ' + $publicCertPath)
Write-Host 'Start the updated HotelServer.exe and check that its status says TLS 1.2.'
