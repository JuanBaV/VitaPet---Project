param([Parameter(Mandatory=$true)][string]$RespaldoPrevio)
$ErrorActionPreference = 'Stop'
$raiz = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$proyecto = Join-Path $raiz 'Tools\VitaPet.Cifrado\VitaPet.Cifrado.csproj'
$msbuild = Get-Command MSBuild.exe -ErrorAction SilentlyContinue
if ($msbuild) { $compilador = $msbuild.Source } else {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    $compilador = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}
if (-not $compilador) { throw 'No se encontró MSBuild de Visual Studio.' }
& $compilador $proyecto /restore /t:Build /v:minimal /nologo /m:1 /nr:false /p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar la herramienta.' }
foreach ($capa in @('BE','DAL','BLL','SECURITY','SERVICES','Interfaces')) {
    Copy-Item -LiteralPath (Join-Path $raiz ('VitaPet.'+$capa+'\bin\Debug\net48\VitaPet.'+$capa+'.dll')) -Destination (Join-Path $raiz 'VitaPet Rework\Bin') -Force
}
$ejecutable = Join-Path $raiz 'Tools\VitaPet.Cifrado\bin\Debug\net48\VitaPet.Cifrado.exe'
[xml]$web = Get-Content -LiteralPath (Join-Path $raiz 'VitaPet Rework\Web.config') -Raw
[xml]$config = '<configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" /></startup></configuration>'
[void]$config.DocumentElement.AppendChild($config.ImportNode($web.configuration.connectionStrings,$true))
[void]$config.DocumentElement.AppendChild($config.ImportNode($web.configuration.appSettings,$true))
$config.Save($ejecutable+'.config')
& $ejecutable --aplicar $RespaldoPrevio
if ($LASTEXITCODE -ne 0) { throw 'El cifrado no se completó. Revise el mensaje anterior antes de volver a ejecutar.' }
