$ErrorActionPreference = 'Stop'
$browserScript = if ($args.Count -gt 0) { $args[0] } else { 'Web.cjs' }
$outputDirectory = if ($args.Count -gt 1) { $args[1] } else { 'web-output' }
$expectedActiveSubscriptions = if ($args.Count -gt 2) { [int]$args[2] } else { 1 }
$probarCifrado = $args.Count -gt 3 -and $args[3] -eq 'cifrado'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location -LiteralPath $repo
$node = 'C:\Users\juanb\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
& (Join-Path $PSScriptRoot 'bin\Debug\net48\VitaPet.Facturacion.Tests.exe') --web
if ($LASTEXITCODE -ne 0) { throw 'Fixture failed' }
$fixture = Get-Content (Join-Path $PSScriptRoot 'bin\Debug\net48\web-environment.txt')
if ($fixture[0] -notmatch '^VitaPet_Verificacion_Rework_[a-f0-9]{32}$') { throw 'Unsafe fixture name' }
# Fuera de OneDrive: sus notificaciones de directorio pueden reiniciar App_Code
# durante los POST y perder la sesión InProc de una copia recién preparada.
$web = Join-Path ([IO.Path]::GetTempPath()) ('VitaPet-rework-site-' + [guid]::NewGuid().ToString('N'))
$output = Join-Path $PSScriptRoot $outputDirectory
$proc = $null
$claveAnterior = $env:VITAPET_CLAVE_ARCHIVO
$respaldoCifrado = $null
try {
    if ($probarCifrado) {
        $env:VITAPET_CLAVE_ARCHIVO = Join-Path $fixture[1] 'cifrado.key'
        & (Join-Path $repo 'Scripts\ConfigurarClaveCifrado.ps1') -Archivo $env:VITAPET_CLAVE_ARCHIVO
        $herramienta = Join-Path $repo 'Tools\VitaPet.Cifrado\bin\Debug\net48\VitaPet.Cifrado.exe'
        if (-not (Test-Path -LiteralPath $herramienta)) { throw 'Compilar Tools/VitaPet.Cifrado antes de probar el cifrado web.' }
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'bin\Debug\net48\VitaPet.Facturacion.Tests.exe.config') -Destination ($herramienta+'.config') -Force
        # Carpeta del proyecto ya habilitada para backups SQL y accesible para limpiar el archivo de prueba.
        $respaldoCifrado=Join-Path (Join-Path $repo 'VitaPet Rework\App_Data\Backups') ($fixture[0]+'_previo.bak')
        & $herramienta --aplicar $respaldoCifrado
        if ($LASTEXITCODE -ne 0) { throw 'La migración cifrada de la base temporal falló.' }
    }
    New-Item -ItemType Directory -Path $web | Out-Null
    & robocopy (Join-Path $repo 'VitaPet Rework') $web /E /XD App_Data obj /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw 'Site copy failed' }
    foreach ($layer in @('BE','DAL','BLL','SECURITY','SERVICES','Interfaces')) {
        Copy-Item -LiteralPath (Join-Path $repo ('VitaPet.'+$layer+'\bin\Debug\net48\VitaPet.'+$layer+'.dll')) -Destination (Join-Path $web 'Bin') -Force
    }
    $configPath = Join-Path $web 'Web.config'
    [xml]$config = [IO.File]::ReadAllText($configPath)
    $config.configuration.connectionStrings.add.connectionString = 'Data Source=localhost;Initial Catalog='+$fixture[0]+';Integrated Security=True;'
    foreach ($setting in $config.configuration.appSettings.add) {
        if ($setting.key -eq 'RutaRecuperacion') { $setting.value=Join-Path $fixture[1] 'recuperacion' }
        if ($setting.key -eq 'RutaBackups') { $setting.value=Join-Path $fixture[1] 'backups' }
        if ($setting.key -eq 'RutaPagosSimulados') { $setting.value=Join-Path $fixture[1] 'pagos' }
        if ($setting.key -eq 'RutaBitacoraXml') { $setting.value=Join-Path $fixture[1] 'Bitacora.xml' }
    }
    $config.Save($configPath)
    $proc = Start-Process -FilePath 'C:\Program Files\IIS Express\iisexpress.exe' -ArgumentList ('/path:"'+$web+'"'),'/port:60336','/systray:false' -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $fixture[1] 'iis.out') -RedirectStandardError (Join-Path $fixture[1] 'iis.err')
    New-Item -ItemType Directory -Force $output | Out-Null
    @{Url='http://localhost:60336/';Database=$fixture[0];Runtime=$fixture[1];Web=$web;Pid=$proc.Id} | ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $output 'environment.json')
    & $node (Join-Path $PSScriptRoot $browserScript) (Join-Path $output 'environment.json')
    if ($LASTEXITCODE -ne 0) { throw 'Payment browser checks failed' }
    $verification = New-Object System.Data.SqlClient.SqlConnection('Data Source=localhost;Initial Catalog='+$fixture[0]+';Integrated Security=True;')
    try {
        $verification.Open(); $query=$verification.CreateCommand()
        $query.CommandText="SELECT COUNT(*) FROM dbo.Suscripcion WHERE estado='Activa'"
        if ($query.ExecuteScalar() -ne $expectedActiveSubscriptions) { throw 'Unexpected active subscription count after browser flow' }
        $query.CommandText='SELECT COUNT(*) FROM dbo.Pago'
        if ($query.ExecuteScalar() -ne 1) { throw 'Browser flow duplicated a payment' }
        $query.CommandText='SELECT COUNT(*) FROM dbo.Factura'
        if ($query.ExecuteScalar() -ne 1) { throw 'Browser flow duplicated an invoice' }
        Write-Output ('PASS Base del recorrido web: '+$expectedActiveSubscriptions+' suscripciones activas, un pago y una factura')
    } finally { $verification.Dispose() }
} finally {
    $env:VITAPET_CLAVE_ARCHIVO = $claveAnterior
    if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id }
    [System.Data.SqlClient.SqlConnection]::ClearAllPools()
    $connection = New-Object System.Data.SqlClient.SqlConnection('Data Source=localhost;Initial Catalog=master;Integrated Security=True;')
    try {
        $connection.Open(); $command=$connection.CreateCommand()
        $command.CommandText='IF DB_ID(N'''+$fixture[0]+''') IS NOT NULL BEGIN ALTER DATABASE ['+$fixture[0]+'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ['+$fixture[0]+']; END'
        $command.ExecuteNonQuery() | Out-Null
    } finally { $connection.Dispose() }
    if ($respaldoCifrado -and (Test-Path -LiteralPath $respaldoCifrado)) { Remove-Item -LiteralPath $respaldoCifrado }
}
