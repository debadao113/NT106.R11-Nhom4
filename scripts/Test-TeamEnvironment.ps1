[CmdletBinding()]
param([switch]$CheckSql, [switch]$CheckDatabase, [switch]$CheckServer)
. "$PSScriptRoot/Team.Common.ps1"
$failed = 0
try { Write-Host "OK MSBuild: $(Find-TeamMSBuild)" } catch { Write-Host "FAIL $($_.Exception.Message)"; $failed++ }
$references = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.2/System.Windows.Forms.dll'
if (Test-Path -LiteralPath $references) { Write-Host 'OK .NET Framework 4.7.2 targeting pack' }
else { Write-Host 'FAIL Install .NET Framework 4.7.2 Developer Pack (runtime alone is insufficient).'; $failed++ }
$guna = Join-Path $script:TeamRoot 'packages/Guna.UI2.WinForms.2.0.3.5/lib/net40/Guna.UI2.dll'
if (Test-Path -LiteralPath $guna) { Write-Host 'OK Guna.UI2.WinForms 2.0.3.5 restored' }
else { Write-Host 'INFO Guna is not restored yet; run scripts/Build-Team.ps1.' }
try {
    $clientConfig = Get-TeamConfig
    $serverConfig = Get-TeamConfig 'HotelServer'
    $clientSql = Get-TeamSqlSettings $clientConfig
    $serverSql = Get-TeamSqlSettings $serverConfig
    if ($clientSql.DataSource -ne $serverSql.DataSource -or $clientSql.InitialCatalog -ne $serverSql.InitialCatalog) {
        throw 'Client and server SQL instance/database differ. Several client pages still use SQL directly.'
    }
    Write-Host "OK Client/server SQL target: $($clientSql.DataSource) / $($clientSql.InitialCatalog)"
    if ($CheckSql) {
        foreach ($original in @($clientSql, $serverSql)) {
            $settings = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($original.ConnectionString)
            $settings['Initial Catalog'] = 'master'
            $settings['Connect Timeout'] = 5
            $connection = [System.Data.SqlClient.SqlConnection]::new($settings.ConnectionString)
            try { $connection.Open(); Write-Host 'OK SQL connection (master); database initialization is a separate step.' }
            finally { $connection.Dispose() }
        }
    }
    if ($CheckDatabase) {
        $connection = [System.Data.SqlClient.SqlConnection]::new($serverSql.ConnectionString)
        try {
            $connection.Open()
            $required = @{
                rooms = 'roomid,roomNo,roomType,bed,price,booked'
                customer = 'cid,cname,mobile,nationality,gender,dob,idproof,address,checkin,checkout,chekout,roomid'
                employee = 'eid,ename,mobile,gender,emailid,username,pass'
                staff_accounts = 'accountId,username,displayName,passwordHash,passwordSalt,passwordIterations,isActive,createdAt'
            }
            foreach ($table in $required.Keys) {
                foreach ($column in $required[$table].Split(',')) {
                    $command = $connection.CreateCommand()
                    try {
                        $command.CommandText = 'SELECT COL_LENGTH(@table, @column)'
                        $null = $command.Parameters.AddWithValue('@table', "dbo.$table")
                        $null = $command.Parameters.AddWithValue('@column', $column)
                        if ($command.ExecuteScalar() -is [DBNull]) { throw "Missing dbo.$table.$column or SQL metadata permission. Check schema/migration." }
                    } finally { $command.Dispose() }
                }
            }
            $command = $connection.CreateCommand()
            try {
                $command.CommandText = 'SELECT COUNT(*) FROM dbo.staff_accounts WHERE isActive=1'
                $count = [int]$command.ExecuteScalar()
                if ($count -eq 0) { throw 'No active login account. Use Initialize-TeamDatabase.ps1 -Username nhom4 or the HotelServer account window.' }
                Write-Host "OK Database tables/required columns; active login accounts: $count"
            } finally { $command.Dispose() }
        } finally { $connection.Dispose() }
    }
    if ($CheckServer) {
        $hostEntry = $clientConfig.configuration.appSettings.add | Where-Object { $_.key -eq 'HotelServerHost' }
        $portEntry = $clientConfig.configuration.appSettings.add | Where-Object { $_.key -eq 'HotelServerPort' }
        $tcp = [System.Net.Sockets.TcpClient]::new()
        try {
            $pending = $tcp.ConnectAsync([string]$hostEntry.value, [int]$portEntry.value)
            if (-not $pending.Wait(5000)) { throw 'HotelServer TCP connection timed out.' }
            $null = $pending.GetAwaiter().GetResult()
            Write-Host "OK HotelServer TCP $($hostEntry.value):$($portEntry.value) (login not tested)"
        } finally { $tcp.Dispose() }
    }
} catch { Write-Host "FAIL $($_.Exception.GetBaseException().Message)"; $failed++ }
if ($failed -gt 0) { throw "$failed environment check(s) failed. See docs/TEAM_SETUP.md." }
Write-Host 'ENVIRONMENT CHECKS OK'
