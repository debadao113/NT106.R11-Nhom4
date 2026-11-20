[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
. "$PSScriptRoot/Team.Common.ps1"
$msbuild = Find-TeamMSBuild
$solution = Join-Path $script:TeamRoot 'ChuuongTrinhQuanLyKhachSan.sln'
$outputs = @(
    (Join-Path $script:TeamRoot "bin/$Configuration/ChuuongTrinhQuanLyKhachSan.exe"),
    (Join-Path $script:TeamRoot "HotelServer/bin/$Configuration/HotelServer.exe")
)
foreach ($process in @(Get-Process -Name ChuuongTrinhQuanLyKhachSan,HotelServer -ErrorAction SilentlyContinue)) {
    if ($process.Path -and $outputs -contains $process.Path) {
        throw "Close $($process.ProcessName) (PID $($process.Id)) before rebuilding. No process was stopped."
    }
}
Write-Host "MSBuild: $msbuild"
& $msbuild $solution /t:Restore /p:RestorePackagesConfig=true /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed. Check Internet access and the nuget.org source.' }
& $msbuild $solution /t:Build "/p:Configuration=$Configuration" '/p:Platform=Any CPU' /nologo /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw 'Build failed. Check the first MSBuild error above; install the .NET Framework 4.7.2 Developer Pack if reference assemblies are missing.' }
Write-Host "BUILD OK. Server: HotelServer\bin\$Configuration\HotelServer.exe"
Write-Host "Client: bin\$Configuration\ChuuongTrinhQuanLyKhachSan.exe"
