[CmdletBinding()]
param(
    [string]$Username,
    [string]$DisplayName = 'Team developer',
    [System.Security.SecureString]$Password,
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug'
)
. "$PSScriptRoot/Team.Common.ps1"
if ($PSVersionTable.PSEdition -eq 'Core') { throw 'Use Windows PowerShell 5.1: powershell -NoProfile -File .\scripts\Initialize-TeamDatabase.ps1' }
$assemblyPath = Join-Path $script:TeamRoot "HotelServer/bin/$Configuration/HotelServer.exe"
if (-not (Test-Path -LiteralPath $assemblyPath)) { throw 'Build first using scripts/Build-Team.ps1.' }
$settings = Get-TeamSqlSettings (Get-TeamConfig 'HotelServer')
Write-Host "Initialize schema: $($settings.DataSource) / $($settings.InitialCatalog)"
$null = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$null = [HotelData.HotelDatabase]::InitializeAsync($settings.ConnectionString).GetAwaiter().GetResult()
Write-Host 'SCHEMA OK (existing records retained)'
if ($Username) {
    $repository = [HotelServer.StaffAccountRepository]::new($settings.ConnectionString)
    $accounts = $repository.GetAccountsAsync().GetAwaiter().GetResult()
    if (@($accounts | Where-Object { $_.username -eq $Username }).Count -gt 0) {
        Write-Host 'Account already exists. Its password was kept.'
        return
    }
    if (-not $Password) { $Password = Read-Host "Password for $Username" -AsSecureString }
    $pointer = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
    try {
        $plain = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
        $null = $repository.CreateAsync($Username, $DisplayName, $plain).GetAwaiter().GetResult()
        Write-Host "ACCOUNT CREATED: $Username"
    } finally {
        $plain = $null
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}
