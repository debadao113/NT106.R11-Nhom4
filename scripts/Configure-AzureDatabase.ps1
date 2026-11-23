[CmdletBinding()]
param([string]$ServerDirectory)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ServerDirectory)) {
    $ServerDirectory = $PSScriptRoot
    if ([string]::IsNullOrWhiteSpace($ServerDirectory)) {
        $ServerDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
    }
}
Add-Type -AssemblyName System.Data
$serverConfigPath = Join-Path $ServerDirectory 'HotelServer.exe.config'
if (-not (Test-Path -LiteralPath $serverConfigPath)) {
    throw 'HotelServer.exe.config was not found. Run this script from the extracted deployment folder.'
}

$secureSqlPassword = Read-Host 'Azure SQL password for nhom4 (not the Windows VM password)' -AsSecureString
$passwordPointer = [IntPtr]::Zero
$sqlConnection = $null
$sqlSettings = $null
$setupStage = 'build connection settings'
try {
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureSqlPassword)
    $sqlSettings = [System.Data.SqlClient.SqlConnectionStringBuilder]::new()
    # Windows PowerShell adapts this builder as a dictionary. Use SQL keyword
    # keys rather than CLR property names (DataSource is not a SQL keyword).
    $sqlSettings['Data Source'] = 'tcp:nhom4.database.windows.net,1433'
    $sqlSettings['Initial Catalog'] = 'dbMyHotel'
    $sqlSettings['User ID'] = 'nhom4'
    $sqlSettings['Password'] = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    $sqlSettings['Encrypt'] = $true
    $sqlSettings['TrustServerCertificate'] = $false
    $sqlSettings['Connect Timeout'] = 60
    $sqlConnection = [System.Data.SqlClient.SqlConnection]::new($sqlSettings.ConnectionString)
    $setupStage = 'connect to Azure SQL'
    Write-Host 'Checking Azure SQL connection and migrated tables...'
    $sqlConnection.Open()
    $setupStage = 'validate migrated schema'
    $schemaCommand = $sqlConnection.CreateCommand()
    $schemaCommand.CommandText = @'
SELECT TOP (0) roomid, roomNo, roomType, bed, price, booked FROM dbo.rooms;
SELECT TOP (0) cid, cname, mobile, nationality, gender, dob, idproof, address, checkin, checkout, chekout, roomid FROM dbo.customer;
SELECT TOP (0) accountId, username, displayName, passwordHash, passwordSalt, passwordIterations, isActive, createdAt FROM dbo.staff_accounts;
SELECT TOP (0) eid, ename, mobile, gender, emailid, username, pass FROM dbo.employee;
'@
    try { [void]$schemaCommand.ExecuteNonQuery() } finally { $schemaCommand.Dispose() }
    $setupStage = 'save VM configuration'
    [xml]$serverConfig = Get-Content -LiteralPath $serverConfigPath -Raw
    $serverConfig.configuration.connectionStrings.add.connectionString = $sqlSettings.ConnectionString

    # Keep the SQL password readable only by the current Windows account,
    # local administrators and SYSTEM before writing it to the config file.
    $configAcl = [System.Security.AccessControl.FileSecurity]::new()
    $configAcl.SetAccessRuleProtection($true, $false)
    $configReaders = @(
        [System.Security.Principal.WindowsIdentity]::GetCurrent().User,
        [System.Security.Principal.SecurityIdentifier]::new('S-1-5-32-544'),
        [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18')
    )
    foreach ($configReader in $configReaders) {
        $configAcl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new(
            $configReader, 'FullControl', 'Allow'))
    }
    Set-Acl -LiteralPath $serverConfigPath -AclObject $configAcl
    $serverConfig.Save($serverConfigPath)
    Write-Host 'OK: Azure SQL connection and schema verified; configuration saved on this VM.'
} catch {
    $diagnosticException = $_.Exception
    while ($diagnosticException -isnot [System.Data.SqlClient.SqlException] -and $null -ne $diagnosticException.InnerException) {
        $diagnosticException = $diagnosticException.InnerException
    }
    Write-Host ('Failed stage: ' + $setupStage)
    Write-Host ('Error type: ' + $diagnosticException.GetType().FullName)
    if ($diagnosticException -is [System.Data.SqlClient.SqlException]) {
        foreach ($sqlErrorNumber in @($diagnosticException.Errors | ForEach-Object { $_.Number } | Select-Object -Unique)) {
            Write-Host ('Azure SQL error number: ' + $sqlErrorNumber)
        }
    }
    throw 'Azure SQL setup failed. Check the SQL password, VM public IP firewall rule, database and table schema. No password is printed.'
} finally {
    if ($null -ne $sqlConnection) { $sqlConnection.Dispose() }
    if ($null -ne $sqlSettings) { $sqlSettings['Password'] = '' }
    if ($passwordPointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }
    $secureSqlPassword.Dispose()
}
