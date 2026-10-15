# VitaPet Web Application

Abrir `VitaPet Rework.slnx` en Visual Studio y establecer **VitaPet Rework** como proyecto de inicio. La GUI utiliza ASP.NET Web Forms como **Web Application de .NET Framework 4.8**. Las páginas, controles, master pages, clases de presentación, recursos y webservices se compilan en `VitaPet.GUI.dll`.

## Estructura

| Carpeta | Responsabilidad |
| --- | --- |
| `VitaPet Rework` | GUI, páginas y sus codebehind/designer, recursos ES/EN compilados, controles propios, CSS, scripts e imágenes |
| `VitaPet Rework/Presentacion` | Template Method de las páginas, `DateTimePicker` y traducción de la presentación |
| `VitaPet Rework/WebServices` | `CoberturaService.asmx` junto a `CoberturaService.asmx.cs` |
| `VitaPet.BE` | Entidades compartidas |
| `VitaPet.BLL` | Reglas, estados, auditoría e integridad |
| `VitaPet.DAL` | SQL Server, conexiones, procedimientos y migraciones embebidas |
| `VitaPet.SECURITY` | Sesiones, estado de aplicación y protección de recuperación |
| `VitaPet.SERVICES` | Servicios compartidos, incluido el idioma |
| `VitaPet.Interfaces` | Contratos compartidos |
| `Database/Migrations` | Esquema y SP versionados |
| `Tests` | Pruebas de usuarios, registro, contratación, pago, cobertura y recuperación |

Todos los proyectos y sus referencias de código y paquetes están dentro de esta ubicación. `App_Start` contiene el inicio de rutas y bundles; la lógica de `Global.asax` está en `Global.asax.cs`. Se conserva la arquitectura GUI → BLL → DAL, con BE, Facade de seguridad, Singleton de sesión, State y Template Method.

## Webservice

`WebServices/CoberturaService.asmx` declara `CodeBehind="CoberturaService.asmx.cs"` y la clase `VitaPet.GUI.WebServices.CoberturaService`. El `.csproj` compila el código y lo agrupa con el ASMX mediante `DependentUpon`, siguiendo el esquema de `BonusService` de “practica pelele”. `ContratarPlan.aspx.cs` crea una instancia y llama a `ObtenerCobertura(idPlan)` desde C#. El endpoint también está disponible por SOAP con la sesión correspondiente. `ConsultaCobertura.aspx.cs` crea la misma clase y llama a `ObtenerCobertura()` sin parámetros para resolver la cuenta autenticada. Esta operación se publica por SOAP como `ObtenerCoberturaCuenta`; la variante `ObtenerCobertura(idPlan)` conserva la consulta del catálogo.

## Datos y archivos

`Web.config` conserva `VitaPetConnection`: SQL Server local, base **VitaPet**, autenticación integrada. La base existente se reutiliza; las pruebas crean y eliminan bases temporales con prefijos de verificación. Las 16 migraciones originales conservan sus archivos y checksums. La migración 017 incorpora Gestión de pago y la 018 agrega los dos SP de Consulta de cobertura, reutilizando las tablas existentes. La 019 incorpora los SP de Consulta de estado de reintegro.

`App_Data` contiene el XML de bitácora, los respaldos, el catálogo firmado y los archivos de recuperación e integridad copiados del proyecto anterior. Sus rutas se resuelven dentro de la GUI nueva. SQL Server conserva permiso Modify para su cuenta `NT SERVICE\MSSQLSERVER` exclusivamente en la carpeta de respaldos, igual que en la ubicación anterior. Los siete respaldos del catálogo fueron verificados desde la carpeta nueva con `RESTORE VERIFYONLY`.

La compatibilidad de respaldos históricos contempla las adaptaciones conocidas 012–020 y continúa verificando el esquema, los checksums, las firmas y los datos antes de restaurar en la base temporal. Los casos que simulan versiones históricas excluyen también las migraciones posteriores de su fixture.

CU-001-003 está implementado con un procesador de prueba local, sin cobros reales.
GestionPago.aspx presenta el resumen por mascotas activas y los datos del medio
de pago; el botón pequeño de la esquina inferior derecha completa datos dummy
sin enviar el formulario. El pago aprobado se registra con bitácora e integridad,
muestra un comprobante y, al continuar, ContratacionPlanBLL activa la suscripción.
La migración 017 agrega cuatro SP y reutiliza Pago y Suscripcion; ErrorSistemaBLL
registra ER054–ER057. Los valores de prueba y las comprobaciones se documentan en
Tests/VitaPet.Pago.Tests/README.md. PagoProcesador=PruebaLocal se configura en Web.config.

CU-001-004 se abre desde **Mi perfil → Consultar cobertura**. Consulta el plan de la
cuenta autenticada, la vigencia de su suscripción y sus prestaciones activas.
La BLL conserva la autorización de Cliente y los controles de integridad; registra
el evento en bitácora antes de devolver el resultado. La consulta no cambia la
suscripción ni los pagos. Se incorpora ER058 para fallos del servicio, traducido
a ES/EN. El detalle de arquitectura y pruebas está en Tests/VitaPet.Cobertura.Tests/README.md.

CU-001-005 está disponible desde **Reintegros** en el menú del cliente.
Lista exclusivamente sus reintegros y permite consultar el detalle y descargar
Reintegros.xml en UTF-8 mediante XmlTextWriter. Cada consulta de detalle registra
bitácora con DVH, DVV y referencias; la exportación vuelve a leer el registro propio.
La migración 019 reutiliza Reintegro y Cliente y agrega dos SP. ER059 queda
registrado en ErrorSistema y traducido a ES/EN. La cuenta prueba pruebita tiene
dos reintegros de demostración asociados a Luna: pendiente (12500.50) y aprobado (8000.00).
Las pruebas y los controles se describen en Tests/VitaPet.Reintegros.Tests/README.md.

CU-002-001 se abre desde **Mascotas → Registrar mascota**. Reutiliza el control
propio DateTimePicker y registra nombre, especie, raza y fecha de nacimiento para
el cliente autenticado. El alta activa, la bitácora, los DVH, DVV y las referencias
se confirman en una transacción. La migración 020 incorpora dos SP sin crear tablas;
ER060 se registra por ErrorSistemaBLL. La confirmación usa PRG para que recargar
no repita el alta. La sección Mascotas incorpora el acceso al formulario; el listado
y las acciones de gestión de CU-002-002 quedan fuera de este caso de uso.

## Compilación y verificación

Compilar la solución con Visual Studio/MSBuild. También puede ejecutarse desde PowerShell:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' '.\VitaPet Rework.slnx' /restore /t:Build /p:Configuration=Debug /p:UseSharedCompilation=false
```

La solución incluye las ocho suites de pruebas. Cada ejecutable de `Tests/*/bin/Debug/net48` verifica reglas sobre bases SQL temporales. Los scripts `Run-Web.ps1`, `Run-IdiomaWeb.ps1` y `Run-WebChecks.ps1` verifican la GUI migrada con IIS Express y Microsoft Edge; requieren el runtime Node/Playwright indicado en esos scripts.

La validación anterior comprendió 389 comprobaciones SQL y 148 de navegador. Para CU-001-004 se verificaron 30 comprobaciones SQL y 21 de navegador, junto con las regresiones de contratación (28 SQL y 22 de navegador), pago (44 SQL) y recuperación (108 SQL). También pasaron las compilaciones Debug, Release y de las páginas ASP.NET. Los registros y capturas están en `artifacts`; la plantilla original quedó respaldada en `artifacts/migration/template-original`.

Los textos están en Resources/Textos.resx y Resources/Textos.en.resx. MSBuild
genera Textos.Designer.cs al compilar; el proveedor VitaPetResourceProviderFactory
conecta las expresiones Resources:Textos de las páginas con la misma DLL de recursos.
Esto evita recompilar App_GlobalResources durante las solicitudes.

Los archivos del proyecto quedaron marcados como disponibles localmente en OneDrive.
Las copias desechables de las pruebas desactivan las notificaciones de cambios de
archivos durante la ejecución: su contenido compilado es inmutable y la sincronización
no debe reiniciar sus sesiones. Esta configuración se aplica únicamente a esas copias;
el Web.config de la aplicación conserva las notificaciones habituales.

Las pruebas de operaciones de usuarios y de idiomas comparten el mismo ejecutable de
fixture y deben ejecutarse por separado, para que cada una conserve su base temporal exclusiva.

Para CU-001-005 pasaron 29 comprobaciones SQL, 25 de navegador y las 108 de
regresión de recuperación, además de Debug, Release y la precompilación ASP.NET.

Para CU-002-001 pasaron 32 comprobaciones SQL y 23 de navegador, las 28 de
regresión de contratación y las 108 de recuperación. También pasaron Debug, Release
y la precompilación ASP.NET. Detalles en Tests/VitaPet.Mascota.Tests/README.md.
