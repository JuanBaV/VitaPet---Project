# VitaPet con el esquema actual

La aplicación utiliza directamente la base actual de VitaPet. Se retiraron
`MigracionRolesBLL`, `MigracionCifradoBLL`, `DatabaseMigrator`, la herramienta
`Tools/VitaPet.Cifrado` y `Scripts/AplicarCifrado.ps1`.

El inicio valida la versión sin actualizar la estructura ni reinicializar
controles para aceptar otro esquema. El cifrado de nuevas escrituras, el
descifrado por sesión y los controles DVH/DVV siguen funcionando. Las operaciones
legítimas y el mantenimiento de bitácora conservan sus actualizaciones de integridad.

## Respaldo anterior a la limpieza

El punto de control previo se encuentra en:

```text
C:\Users\juanb\OneDrive\TFI\Tecnologia\PuntosControl\20261023-205607-antes-retirar-migraciones
```

Contiene un bundle Git con historial completo y los cambios pendientes, el backup
SQL, la documentación y el checklist anteriores. El material de recuperación y
la copia de la clave están en la carpeta `Privado`, con acceso restringido.
`punto-control.json` identifica la etiqueta, el commit de captura y el hash del
backup. La copia SQL fue restaurada en una base temporal y pasó la comprobación
de descifrado e integridad antes de editar el código.

La etiqueta Git es `checkpoint/20261023-205607-antes-retirar-migraciones`.
El snapshot es `500517c61e7a1a04e7889d08fe1dd0daee055a9a`.

## Esquema y recuperación

- `Database/EsquemaActual.manifest` conserva las versiones y checksums esperados.
  El historial aplicado permanece en SQL y se valida en modo de solo lectura.
- `Database/EsquemaActual.sql` define el esquema cifrado vigente sin datos de
  usuarios. Sirve para reconstruir respaldos XML en una base nueva; no es una
  instalación con usuarios o catálogos precargados.
- La restauración SQL acepta backups del mismo esquema y verifica una copia
  temporal antes de reemplazar el destino.
- La recuperación XML valida la firma y la versión, crea una base temporal con
  el esquema fijo y la clave existente, importa las filas y verifica los controles
  originales. Puede recuperar una base principal inexistente.
- Los respaldos de versiones anteriores se rechazan sin convertirlos ni modificar
  el destino. Para regresar a la versión previa del programa se conserva el punto
  de control anterior a esta limpieza.
- Los scripts históricos quedan como fuentes de fixtures en los proyectos de
  pruebas. No se incrustan ni se ejecutan en las DLL de la aplicación.

## Comprobación manual

1. Detener la ejecución actual y volver a iniciar VitaPet Rework desde Visual
   Studio para compilar las capas actualizadas.
2. Iniciar sesión y comprobar que se muestran los nombres y demás datos personales.
3. Como WebMaster, abrir Base de datos y ejecutar Verificar integridad.
4. Crear un backup actual y comprobar que se ofrece en el selector.
5. Consultar la bitácora y una factura para comprobar el acceso a datos cifrados.

La restauración completa y los casos de base ausente se prueban automáticamente
sobre bases temporales; no es necesario modificar la base de uso para verificarlos.

## Compilación y pruebas aisladas

Desde la raíz del repositorio:

```powershell
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
$salida = Join-Path (Get-Location) 'artifacts\sin-migraciones-build\'
& $msbuild 'VitaPet Rework.slnx' /t:Build /v:minimal /nologo /m:1 /nr:false "/p:OutDir=$salida"
& $msbuild 'Tests\VitaPet.Facturacion.Tests\VitaPet.Facturacion.Tests.csproj' /t:Build /v:minimal /nologo /m:1 /nr:false /p:BuildProjectReferences=false "/p:OutDir=$salida"
& '.\artifacts\sin-migraciones-build\VitaPet.Facturacion.Tests.exe' --esquema-actual
& '.\artifacts\sin-migraciones-build\VitaPet.Facturacion.Tests.exe' --cifrado
& '.\artifacts\sin-migraciones-build\VitaPet.Facturacion.Tests.exe' --bitacora
```

Los ejecutables crean y eliminan bases `VitaPet_Verificacion_*` con nombres
aleatorios. La infraestructura de fixtures rechaza el nombre de la base de uso.
La salida de compilación separada permite verificar el cambio sin reemplazar las
DLL de una instancia abierta en Visual Studio.

Resultados: 31 comprobaciones de aplicación sin migraciones, 36 de cifrado,
42 de bitácora, 59 de regresión general, 203 de integridad y 28 de bitácora web
más 18 de facturación web. La base VitaPet actual también pasó una validación
de esquema, descifrado e integridad en modo de solo lectura.
