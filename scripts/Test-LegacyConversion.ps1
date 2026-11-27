[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$fixture = Join-Path $env:TEMP ('HotelDesk-legacy-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixture | Out-Null
$rooms = Join-Path $fixture 'rooms.json'
$customers = Join-Path $fixture 'customers.json'
$employees = Join-Path $fixture 'employees.json'
$sqlFile = Join-Path $fixture 'import.local.sql'
ConvertTo-Json -InputObject @(@{ roomid=1; roomNo='101'; roomType='Double'; bed='Double'; price=500000; booked='YES' }) | Set-Content -LiteralPath $rooms -Encoding UTF8
$guest = @{ cid=1; roomid=1; cname="O'Neil"; mobile='0901234567'; nationality='VN'; gender='Other'; dob='2000-01-01';
    idproof='TEST-123'; address='Test'; checkin='2026-01-01'; checkout='NO'; chekout='NO' }
ConvertTo-Json -InputObject @($guest) | Set-Content -LiteralPath $customers -Encoding UTF8
Set-Content -LiteralPath $employees -Value '[]' -Encoding UTF8
& "$PSScriptRoot/Convert-LegacyData.ps1" -RoomsJson $rooms -CustomersJson $customers -EmployeesJson $employees -OutputSql $sqlFile
$sql = Get-Content -LiteralPath $sqlFile -Raw
if (-not $sql.Contains("O''Neil") -or -not $sql.Contains("'0901234567'") -or $sql -match 'password|staff_accounts') {
    throw 'Converter did not preserve quoted names/phone numbers or included credentials.'
}
$guest.checkin = 'not-a-date'
ConvertTo-Json -InputObject @($guest) | Set-Content -LiteralPath $customers -Encoding UTF8
$rejected = $false
try {
    & "$PSScriptRoot/Convert-LegacyData.ps1" -RoomsJson $rooms -CustomersJson $customers -EmployeesJson $employees -OutputSql (Join-Path $fixture 'invalid.local.sql')
} catch { $rejected = $true }
if (-not $rejected -or (Test-Path -LiteralPath (Join-Path $fixture 'invalid.local.sql'))) { throw 'Invalid dates must not produce an import.' }
Write-Host 'PASS: Legacy conversion preserves phone/quoted text, rejects invalid dates and omits credentials.'
