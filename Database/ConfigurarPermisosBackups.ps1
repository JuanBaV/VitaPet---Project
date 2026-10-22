[CmdletBinding()]
param(
    [string]$ServicioSql = 'MSSQLSERVER',
    [string]$IdentidadSitio
)

$ErrorActionPreference = 'Stop'
# Ejecutar en el equipo que aloja el sitio y la instancia SQL local.
$webConfig = Join-Path $PSScriptRoot '..\VitaPet Rework\Web.config'
[xml]$config = Get-Content -LiteralPath $webConfig -Raw
$setting = $config.configuration.appSettings.add | Where-Object { $_.key -eq 'RutaBackups' }
$ruta = if ($setting) { [string]$setting.value } else { '~/App_Data/Backups' }
$sitio = Split-Path -Parent ([IO.Path]::GetFullPath($webConfig))
if (-not [IO.Path]::IsPathRooted($ruta)) {
    $ruta = Join-Path $sitio ($ruta -replace '^~[/\\]', '')
}
$ruta = [IO.Path]::GetFullPath($ruta)

$servicio = Get-CimInstance Win32_Service | Where-Object { $_.Name -eq $ServicioSql }
if (-not $servicio) { throw "No se encontro el servicio SQL '$ServicioSql' en este equipo." }
$cuentaSql = switch ($servicio.StartName) {
    'LocalSystem' { 'S-1-5-18'; break }
    'NT AUTHORITY\LocalService' { 'S-1-5-19'; break }
    'NT AUTHORITY\NetworkService' { 'S-1-5-20'; break }
    default { $servicio.StartName }
}
$identidades = @($cuentaSql)
if ($IdentidadSitio) { $identidades += $IdentidadSitio }
# Resolver todas las cuentas antes de modificar permisos.
$sids = foreach ($identidad in $identidades) {
    if ($identidad -match '^S-1-') {
        New-Object Security.Principal.SecurityIdentifier($identidad)
    } else {
        $cuenta = New-Object Security.Principal.NTAccount($identidad)
        $cuenta.Translate([Security.Principal.SecurityIdentifier])
    }
}
[IO.Directory]::CreateDirectory($ruta) | Out-Null
$acl = Get-Acl -LiteralPath $ruta
foreach ($sid in $sids) {
    $regla = New-Object Security.AccessControl.FileSystemAccessRule(
        $sid,
        [Security.AccessControl.FileSystemRights]::Modify,
        ([Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit),
        [Security.AccessControl.PropagationFlags]::None,
        [Security.AccessControl.AccessControlType]::Allow
    )
    $acl.AddAccessRule($regla)
}
Set-Acl -LiteralPath $ruta -AclObject $acl
Write-Output "Permiso Modificar aplicado a '$($servicio.StartName)' en '$ruta'."
if ($IdentidadSitio) { Write-Output "Permiso Modificar aplicado a '$IdentidadSitio'." }