$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
Set-Location -LiteralPath $repo
$node = 'C:\Users\juanb\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
& (Join-Path $PSScriptRoot 'bin\Debug\net48\VitaPet.Mascota.Tests.exe') --web
if ($LASTEXITCODE -ne 0) { throw 'Fixture failed' }
$fixture = Get-Content (Join-Path $PSScriptRoot 'bin\Debug\net48\web-environment.txt')
if ($fixture[0] -notmatch '^VitaPet_Verificacion_Mascota_[a-f0-9]{32}$') { throw 'Unsafe fixture name' }
$web = Join-Path $repo ('artifacts\mascota-site-' + [guid]::NewGuid().ToString('N'))
$output = Join-Path $repo 'artifacts\mascota-web'
$proc = $null
try {
    New-Item -ItemType Directory -Path $web | Out-Null
    & robocopy (Join-Path $repo 'VitaPet Rework') $web /E /XD App_Data obj .vs /NFL /NDL /NJH /NJS /NP | Out-Null
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
        if ($setting.key -eq 'RutaBitacoraXml') { $setting.value=Join-Path $fixture[1] 'Bitacora.xml' }
    }
    # La copia de prueba es inmutable; ignorar notificaciones de sincronización de OneDrive.
    $config.configuration.'system.web'.httpRuntime.SetAttribute('fcnMode','Disabled')
    $config.Save($configPath)
    $proc = Start-Process -FilePath 'C:\Program Files\IIS Express\iisexpress.exe' -ArgumentList ('/path:"'+$web+'"'),'/port:60337','/systray:false' -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $fixture[1] 'iis.out') -RedirectStandardError (Join-Path $fixture[1] 'iis.err')
    New-Item -ItemType Directory -Force $output | Out-Null
    @{Url='http://localhost:60337/';Database=$fixture[0];Runtime=$fixture[1];Web=$web;Pid=$proc.Id} | ConvertTo-Json | Set-Content -Encoding utf8 (Join-Path $output 'environment.json')
    & $node (Join-Path $PSScriptRoot 'Mascota-Web.cjs') (Join-Path $output 'environment.json')
    if ($LASTEXITCODE -ne 0) { throw 'Pet registration browser checks failed' }
} finally {
    if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id }
    [System.Data.SqlClient.SqlConnection]::ClearAllPools()
    $connection = New-Object System.Data.SqlClient.SqlConnection('Data Source=localhost;Initial Catalog=master;Integrated Security=True;')
    try {
        $connection.Open(); $command=$connection.CreateCommand()
        $command.CommandText='IF DB_ID(N'''+$fixture[0]+''') IS NOT NULL BEGIN ALTER DATABASE ['+$fixture[0]+'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE ['+$fixture[0]+']; END'
        $command.ExecuteNonQuery() | Out-Null
    } finally { $connection.Dispose() }
}
