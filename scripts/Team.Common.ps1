Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:TeamRoot = Split-Path $PSScriptRoot -Parent

function Find-TeamMSBuild {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
    if (-not (Test-Path -LiteralPath $vswhere)) {
        throw 'Install Visual Studio 2022+ or Build Tools with .NET desktop development.'
    }
    $candidates = @(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild Microsoft.VisualStudio.Component.Roslyn.Compiler -find 'MSBuild/Current/Bin/MSBuild.exe')
    if ($LASTEXITCODE -ne 0 -or $candidates.Count -eq 0) {
        throw 'MSBuild with the C# compiler was not found. Install the .NET desktop development workload.'
    }
    return $candidates[0]
}
