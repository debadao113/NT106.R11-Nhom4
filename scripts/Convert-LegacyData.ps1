<#
One-time, offline conversion. Export JSON from the OLD database outside Git:
  SELECT roomid,roomNo,roomType,bed,price,booked FROM dbo.rooms FOR JSON PATH;
  SELECT cid,cname,mobile,nationality,gender,dob,idproof,address,checkin,checkout,chekout,roomid FROM dbo.customer FOR JSON PATH;
  SELECT eid,ename,mobile,gender,emailid FROM dbo.employee FOR JSON PATH;
Save the complete JSON arrays as rooms.json, customers.json, employees.json (UTF-8).
Do not export staff_accounts, pass or passwords. Protect these files as guest data.

Usage:
  .\scripts\Convert-LegacyData.ps1 -RoomsJson C:\Private\rooms.json -CustomersJson C:\Private\customers.json -EmployeesJson C:\Private\employees.json -OutputSql C:\Private\import.local.sql
Default non-ISO date format: dd/MM/yyyy. Override -LegacyDateFormat if necessary.
The script stops on inconsistent room status or ambiguous/invalid dates.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$RoomsJson,
    [Parameter(Mandatory=$true)][string]$CustomersJson,
    [Parameter(Mandatory=$true)][string]$EmployeesJson,
    [Parameter(Mandatory=$true)][string]$OutputSql,
    [string]$LegacyDateFormat = 'dd/MM/yyyy'
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $OutputSql) { throw 'Output file already exists. Choose another path.' }
$rooms = @(Get-Content -LiteralPath $RoomsJson -Raw -Encoding UTF8 | ConvertFrom-Json | ForEach-Object { $_ })
$customers = @(Get-Content -LiteralPath $CustomersJson -Raw -Encoding UTF8 | ConvertFrom-Json | ForEach-Object { $_ })
$employees = @(Get-Content -LiteralPath $EmployeesJson -Raw -Encoding UTF8 | ConvertFrom-Json | ForEach-Object { $_ })
function TextSql($value) {
    $text = [string]$value
    if ($text.Contains([string][char]0)) { throw 'NUL character in exported data.' }
    return "'" + $text.Replace("'", "''") + "'"
}
function PositiveId($value) {
    $number = [int]$value
    if ($number -le 0) { throw 'IDs must be positive integers.' }
    return [string]$number
}
function DateSql($value, [bool]$Optional = $false) {
    $text = [string]$value
    if ($Optional -and ($text -eq '' -or $text -eq 'NO')) { return 'null' }
    $date = [datetime]::MinValue
    $formats = [string[]]@('yyyy-MM-dd', $LegacyDateFormat)
    if (-not [datetime]::TryParseExact($text,$formats,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::None,[ref]$date)) {
        throw 'Invalid date in exported data. Check -LegacyDateFormat; no date was guessed.'
    }
    return TextSql $date.ToString('yyyy-MM-dd')
}
$roomById = @{}
$activeRooms = @{}
foreach ($room in $rooms) {
    $roomId = PositiveId $room.roomid
    if ($roomById.ContainsKey($roomId)) { throw 'Duplicate room ID in export.' }
    $roomById[$roomId] = $room
}
foreach ($customer in $customers) {
    $roomId = PositiveId $customer.roomid
    if (-not $roomById.ContainsKey($roomId)) { throw 'Customer references a missing room.' }
    $closed = $customer.chekout -eq 'YES' -or ($customer.checkout -ne 'NO' -and [string]$customer.checkout -ne '')
    if ($closed -and ($customer.checkout -eq 'NO' -or [string]$customer.checkout -eq '')) { throw 'A checked-out stay has no checkout date. Correct the export first.' }
    if (-not $closed) {
        if ($activeRooms.ContainsKey($roomId)) { throw 'Multiple active stays for one room.' }
        $activeRooms[$roomId] = $true
    }
}
foreach ($roomId in $roomById.Keys) {
    if (($roomById[$roomId].booked -eq 'YES') -ne $activeRooms.ContainsKey($roomId)) { throw 'Booked status disagrees with active stays. Correct the old data before importing.' }
}
$sql = [Collections.Generic.List[string]]::new()
$sql.Add('begin;')
$sql.Add('set local standard_conforming_strings = on;')
$sql.Add("do `$`$ begin if exists(select 1 from public.rooms) or exists(select 1 from public.stays) or exists(select 1 from public.employees) then raise exception 'Import requires empty hotel tables.'; end if; end; `$`$;")
foreach ($room in $rooms) {
    $price = [long]$room.price
    if ($price -lt 0) { throw 'Negative room price.' }
    $values = @((PositiveId $room.roomid),(TextSql $room.roomNo),(TextSql $room.roomType),(TextSql $room.bed),[string]$price)
    $sql.Add('insert into public.rooms(id,number,room_type,bed,price) values (' + ($values -join ',') + ');')
}
foreach ($customer in $customers) {
    $roomId = PositiveId $customer.roomid
    $values = @((PositiveId $customer.cid),$roomId,(TextSql $customer.cname),(TextSql $customer.mobile),
        (TextSql $customer.nationality),(TextSql $customer.gender),(DateSql $customer.dob $true),
        (TextSql $customer.idproof),(TextSql $customer.address),(DateSql $customer.checkin),
        (DateSql $customer.checkout $true),[string][long]$roomById[$roomId].price)
    $sql.Add('insert into public.stays(id,room_id,name,phone,nationality,gender,birth_date,identity_document,address,check_in,check_out,nightly_price) values (' + ($values -join ',') + ');')
}
foreach ($employee in $employees) {
    $values = @((PositiveId $employee.eid),(TextSql $employee.ename),(TextSql $employee.mobile),(TextSql $employee.gender),(TextSql $employee.emailid))
    $sql.Add('insert into public.employees(id,name,phone,gender,email) values (' + ($values -join ',') + ');')
}
foreach ($table in @('rooms','stays','employees')) {
    $sql.Add("select setval(pg_get_serial_sequence('public.$table','id'), coalesce(max(id),1), count(*)>0) from public.$table;")
}
$sql.Add('commit;')
[IO.File]::WriteAllLines([IO.Path]::GetFullPath($OutputSql),$sql,[Text.UTF8Encoding]::new($false))
Write-Host "Created import SQL: $($rooms.Count) rooms, $($customers.Count) stays, $($employees.Count) employees. No Auth passwords imported."
