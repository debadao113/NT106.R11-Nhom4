[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ProjectUrl,
    [Parameter(Mandatory=$true)][string]$PublishableKey
)
$ErrorActionPreference = 'Stop'
$projectUri = $null
if (-not [Uri]::TryCreate($ProjectUrl, [UriKind]::Absolute, [ref]$projectUri) -or
    $projectUri.Scheme -ne 'https' -or $projectUri.AbsolutePath -ne '/' -or
    $projectUri.UserInfo -or $projectUri.Query -or $projectUri.Fragment) {
    throw 'ProjectUrl must be an HTTPS project origin, e.g. https://PROJECT.supabase.co'
}
$isPublicKey = $PublishableKey.StartsWith('sb_publishable_')
if (-not $isPublicKey -and $PublishableKey.StartsWith('eyJ')) {
    try {
        $payload = $PublishableKey.Split('.')[1].Replace('-','+').Replace('_','/')
        $payload = $payload.PadRight([int]([Math]::Ceiling($payload.Length / 4.0) * 4), '=')
        $claims = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload)) | ConvertFrom-Json
        $isPublicKey = $claims.role -eq 'anon'
    } catch { $isPublicKey = $false }
}
if (-not $isPublicKey) { throw 'Use a publishable key or anon key. Never put secret/service_role keys in the app.' }
$projectRoot = Split-Path $PSScriptRoot -Parent
[xml]$config = Get-Content -LiteralPath (Join-Path $projectRoot 'App.config') -Raw
foreach ($setting in $config.configuration.appSettings.add) {
    if ($setting.key -eq 'SupabaseUrl') { $setting.value = $projectUri.AbsoluteUri.TrimEnd('/') }
    if ($setting.key -eq 'SupabasePublishableKey') { $setting.value = $PublishableKey }
}
$config.Save((Join-Path $projectRoot 'App.local.config'))
Write-Host 'Saved App.local.config. Build the client again to apply these settings.'
