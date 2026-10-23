param(
    [string]$Archivo = (Join-Path ([Environment]::GetFolderPath('UserProfile')) '.vitapet\cifrado.key'),
    [string]$ImportarDesde
)
$ErrorActionPreference = 'Stop'
$Archivo = [IO.Path]::GetFullPath($Archivo)
if (Test-Path -LiteralPath $Archivo) {
    throw 'El archivo de clave ya existe. No se sobrescribe: una clave nueva no descifra los datos anteriores.'
}
if ($ImportarDesde) {
    $clave = [IO.File]::ReadAllText((Resolve-Path -LiteralPath $ImportarDesde).Path).Trim()
    if ([Convert]::FromBase64String($clave).Length -ne 32) { throw 'El archivo debe contener una clave Base64 de 32 bytes.' }
} else {
    $bytes = New-Object byte[] 32
    $generador = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generador.GetBytes($bytes) } finally { $generador.Dispose() }
    $clave = [Convert]::ToBase64String($bytes)
}
[void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Archivo))
$flujo = New-Object IO.FileStream($Archivo, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
try {
    $bytesClave = [Text.Encoding]::ASCII.GetBytes($clave)
    $flujo.Write($bytesClave, 0, $bytesClave.Length)
} finally { $flujo.Dispose() }
$identidad = [Security.Principal.WindowsIdentity]::GetCurrent().User
$acl = New-Object Security.AccessControl.FileSecurity
$acl.SetOwner($identidad)
$acl.SetAccessRuleProtection($true, $false)
$acl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($identidad, 'FullControl', 'Allow')))
Set-Acl -LiteralPath $Archivo -AclObject $acl
Write-Output "Clave configurada en: $Archivo"
Write-Output 'Guarde una copia protegida fuera del repositorio. El contenido de la clave no se muestra.'
