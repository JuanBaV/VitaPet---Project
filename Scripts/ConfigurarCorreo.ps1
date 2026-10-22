param(
    [string]$Remitente = 'vitapet.noreply@gmail.com'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security
$correoSeguro = Read-Host 'Pegá la contraseña de aplicación de Gmail (16 caracteres, no la contraseña de la cuenta)' -AsSecureString
$correoPtr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($correoSeguro)
try {
    $correoClave = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($correoPtr).Replace(' ', '')
    if ($correoClave.Length -ne 16) { throw 'La contraseña de aplicación debe tener 16 caracteres.' }
    $correoProtegida = [Security.Cryptography.ProtectedData]::Protect(
        [Text.Encoding]::UTF8.GetBytes($correoClave), $null,
        [Security.Cryptography.DataProtectionScope]::CurrentUser)
    $correoConfig = [xml]'<appSettings />'
    foreach ($correoPar in @{
        CorreoRemitente = $Remitente
        CorreoClaveProtegida = [Convert]::ToBase64String($correoProtegida)
    }.GetEnumerator()) {
        $correoNodo = $correoConfig.CreateElement('add')
        $correoNodo.SetAttribute('key', $correoPar.Key)
        $correoNodo.SetAttribute('value', $correoPar.Value)
        [void]$correoConfig.DocumentElement.AppendChild($correoNodo)
    }
    $correoArchivo = Join-Path (Split-Path $PSScriptRoot -Parent) 'VitaPet Rework/Correo.local.config'
    $correoConfig.Save($correoArchivo)
    Write-Host "Correo configurado para $Remitente. La factura se enviará al correo ingresado en el pago."
    Write-Host 'La clave quedó cifrada para tu usuario de Windows y excluida de Git. Reiniciá VitaPet si estaba abierto.'
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($correoPtr)
    $correoClave = $null
    $correoSeguro.Dispose()
}
