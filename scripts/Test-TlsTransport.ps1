[CmdletBinding()]
param()
. "$PSScriptRoot/Team.Common.ps1"
$msbuild = Find-TeamMSBuild
& $msbuild (Join-Path $script:TeamRoot 'ChuuongTrinhQuanLyKhachSan.sln') /t:Build /p:Configuration=Release '/p:Platform=Any CPU' /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$testDirectory = Join-Path $env:TEMP ('nt106-tls-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testDirectory | Out-Null
Copy-Item -LiteralPath (Join-Path $script:TeamRoot 'HotelServer/bin/Release/HotelServer.exe') -Destination $testDirectory
$compiler = Join-Path (Split-Path $msbuild -Parent) 'Roslyn/csc.exe'
$testExe = Join-Path $testDirectory 'TlsTransportChecks.exe'
& $compiler /nologo /target:exe "/out:$testExe" /r:System.Data.dll /r:System.Configuration.dll /r:System.Web.Extensions.dll `
    "/r:$(Join-Path $testDirectory 'HotelServer.exe')" `
    (Join-Path $script:TeamRoot 'tests/TlsTransportChecks.cs') (Join-Path $script:TeamRoot 'Networking/NetworkClient.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
$certificate = New-SelfSignedCertificate -DnsName 'hotelserver' -Type SSLServerAuthentication `
    -CertStoreLocation 'Cert:\CurrentUser\My' -FriendlyName ('NT106 transport test ' + [guid]::NewGuid().ToString('N')) `
    -KeyAlgorithm RSA -KeyLength 2048 -KeyExportPolicy NonExportable -NotAfter (Get-Date).AddHours(2)
try {
    & $testExe $certificate.Thumbprint
    if ($LASTEXITCODE -ne 0) { throw 'TLS integration checks failed.' }
} finally {
    $ownedCertificatePath = 'Cert:\CurrentUser\My\' + $certificate.Thumbprint
    $certificate.Dispose()
    Remove-Item -LiteralPath $ownedCertificatePath -DeleteKey
}

# Exercise the real setup scripts with dummy config files; no database connection.
$setupDirectory = Join-Path $testDirectory 'setup'
New-Item -ItemType Directory -Path $setupDirectory | Out-Null
$setupConfig = Join-Path $setupDirectory 'HotelServer.exe.config'
Copy-Item -LiteralPath (Join-Path $script:TeamRoot 'HotelServer/App.Azure.config') -Destination $setupConfig
$ownedSetupThumbprint = $null
try {
    & "$PSScriptRoot/Configure-ServerTls.ps1" -ServerDirectory $setupDirectory
    [xml]$firstConfig = Get-Content -LiteralPath $setupConfig -Raw
    $ownedSetupThumbprint = [string]($firstConfig.configuration.appSettings.add | Where-Object { $_.key -eq 'TlsServerCertificateThumbprint' }).value
    $originalSqlSettings = [string]$firstConfig.configuration.connectionStrings.add.connectionString
    & "$PSScriptRoot/Configure-ServerTls.ps1" -ServerDirectory $setupDirectory
    [xml]$secondConfig = Get-Content -LiteralPath $setupConfig -Raw
    $secondThumbprint = [string]($secondConfig.configuration.appSettings.add | Where-Object { $_.key -eq 'TlsServerCertificateThumbprint' }).value
    if ($ownedSetupThumbprint -ne $secondThumbprint) { throw 'Setup did not reuse the valid certificate.' }
    if ([string]$secondConfig.configuration.connectionStrings.add.connectionString -ne $originalSqlSettings) { throw 'TLS setup changed SQL settings.' }
    Write-Host 'PASS: Server TLS setup preserves SQL settings and reuses the valid certificate.'
    $probeConfig = Join-Path $setupDirectory 'HotelServerProbe.exe.config'
    '<configuration><appSettings /></configuration>' | Set-Content -LiteralPath $probeConfig -Encoding UTF8
    & "$PSScriptRoot/Configure-ClientTls.ps1" -ClientDirectory $setupDirectory -CertificatePath (Join-Path $setupDirectory 'HotelServer-public.cer')
    [xml]$configuredProbe = Get-Content -LiteralPath $probeConfig -Raw
    $configuredPin = [string]($configuredProbe.configuration.appSettings.add | Where-Object { $_.key -eq 'TlsCertificateSha256' }).value
    if ($configuredPin -notmatch '\A[0-9A-F]{64}\z') { throw 'Client certificate pin was not saved.' }
    Write-Host 'PASS: Client setup saves the public certificate SHA-256 pin.'
} finally {
    if ($ownedSetupThumbprint) { Remove-Item -LiteralPath ('Cert:\CurrentUser\My\' + $ownedSetupThumbprint) -DeleteKey }
}
