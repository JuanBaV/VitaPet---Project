# Refactorización interna de VitaPet Rework

Fecha: 22/10/2026. Base de revisión: commit `1a7af5d`, sin cambios pendientes
al comenzar. Carpeta: `C:\Users\juanb\OneDrive\TFI\Tecnologia\Codigo\VitaPet Rework`.

## Alcance y criterios

Revisión por módulos de las entidades, BLL, DAL, seguridad, servicios, sus
consumidores Web Forms y los procedimientos definidos en el repositorio.
Las migraciones 001 a 027 definen 73 procedimientos vigentes. Los scripts
históricos de `Database/StoredProcedures` no reemplazan ese mecanismo.

Se aplicó ponytail en nivel full: reutilizar las piezas existentes y modificar
solamente donde hay una reducción concreta de duplicación, consultas o
escrituras. Se conservaron Web Forms, BE/BLL/DAL, la fachada SecurityManager,
SessionManager, los estados de usuario y las páginas base.

Todos los archivos versionados del proyecto web permanecen idénticos a los
iniciales, incluidos páginas, code-behind, controles, JavaScript, estilos,
recursos, servicios ASMX y configuración. La comprobación SHA-256 también
confirmó que las migraciones 001 a 027 y `ConexionDAL.cs` no cambiaron.

## Cambios

| Área | Cambio | Conservación del comportamiento |
| --- | --- | --- |
| Referencias de integridad | Comparar las huellas actuales con las guardadas y escribir únicamente las nuevas o diferentes, incluido el sello. | Se siguen calculando todas las huellas, podando filas eliminadas y verificando las alteraciones. No se agrega caché. |
| Login | Unificar el tratamiento de ingreso exitoso/fallido y reutilizar la lista de usuarios para el DVV. | Relectura del estado vigente, patrón State, límite de intentos, bloqueo y transacción de integridad. Se elimina una lectura completa adicional por operación. |
| Integridad de registros | Reutilizar IntegridadRegistroBLL para mascotas, empresas, contratación y clientes corporativos. | Mismos algoritmos y orden de publicación de referencias. Las operaciones con varias tablas pueden actualizar sus dígitos antes de publicar el conjunto completo. |
| Lectura DAL | Un único mapeo de UsuarioBE para las tres consultas y uno de BitacoraBE para las dos consultas. | Se conservan campos, conversiones, valores nulos y compatibilidad del indicador de restablecimiento obligatorio. |
| Persistencia DVV | Reutilizar DVVDAL con la conexión abierta de UsuarioDAL y BitacoraDAL. | Mismos procedimientos, parámetros SQL tipados y TransactionScope. No se abre otra conexión. |
| Excepciones | Eliminar cuatro bloques catch que únicamente relanzaban la excepción. | El using sigue revirtiendo la transacción si no se completa; los errores continúan propagándose. |
| Recuperación de contraseña | Reutilizar la fachada existente y quitar una sincronización de referencias repetida después del desbloqueo. | Se conservan restablecimiento obligatorio, desbloqueo, referencias históricas y evento de auditoría. |
| SQL de mascotas | Obtener el DVH vigente junto con la comprobación de pertenencia. | Mismos parámetros, orden de validaciones, códigos/mensajes de error y actualización de los cuatro campos. |
| SQL de facturas | Recuperar el identificador de la factura existente en una sola lectura. | Se mantienen UPDLOCK/HOLDLOCK, transacción requerida, reemisión idempotente, numeración y valores facturados. |

Los dos cambios de SQL se distribuyen en `028_OptimizarLecturas.sql`, embebida
automáticamente por VitaPet.DAL. También se agregó esa versión a las
migraciones admitidas por la validación de compatibilidad de backups.
No cambia tablas ni datos y no recalcula verificadores existentes.

La migración se aplica mediante el arranque habitual o DatabaseMigrator,
igual que las anteriores. Durante esta revisión se ejecutó en bases aisladas;
no se ejecutó manualmente contra la base de trabajo del usuario.

## Validación realizada

| Verificación | Resultado |
| --- | --- |
| Compilación inicial y final de `VitaPet Rework.slnx` con MSBuild de Visual Studio | Correcta, incluidos los proyectos web y de capas. |
| Suite existente VitaPet.Facturacion.Tests, antes y después del refactor | 21 verificaciones correctas. Incluye concurrencia, rollback de factura, reenvíos y pagos históricos. |
| Nueva suite VitaPet.Refactor.Tests | 58 verificaciones correctas en una base temporal propia. |
| Recorrido existente en Edge mediante IIS Express y una copia aislada del sitio | 18 verificaciones correctas y comprobación SQL de una suscripción activa, un pago y una factura. |
| Comparación de archivos con SHA-256 | Ningún archivo del proyecto web, conexión o migración anterior modificado. |
| `git diff --check` | Sin errores de espacios. |

La suite nueva compara los procedimientos modificados con sus versiones
anteriores dentro de transacciones reversibles. Comprueba tipos y parámetros,
resultados, códigos/mensajes de error y contenido de Mascota. También verifica
la migración sobre datos poblados, rollback por errores de DVV, permisos,
estados de usuario, idioma, altas y modificaciones, registro corporativo,
restablecimiento obligatorio, turnos, histórico XML y detección de alteraciones.

Dos triggers de prueba demuestran que una sincronización sin cambios no
escribe ninguna referencia y que un cambio de idioma no reescribe las huellas
iguales. Los triggers se eliminan al terminar sus verificaciones.

El recorrido web comprobó sesión obligatoria, perfil vacío, descuento,
simulador existente, confirmación y descarga PDF desde pago/planes/perfil,
continuación de contratación, cobertura, móvil, inglés y rechazo de facturas
ajenas. No se modificaron las pantallas durante esa prueba.

Las instrucciones reproducibles están en
`Tests/VitaPet.Refactor.Tests/README.md`. El registro final queda en
`artifacts/refactor/regresion.txt`; las capturas y PDF de Edge están en
`artifacts/refactor/web-output/`. Esos artefactos están fuera del código
versionado y contienen únicamente datos de las fixtures.

## Límites de la revisión

Se conservaron las validaciones en BLL y SQL: protegen entradas diferentes y
su eliminación cambiaría el contrato. También se conservaron las verificaciones
de integridad, bloqueos y límites transaccionales de pago/factura.
No se agregaron repositorios genéricos, dependencias, índices especulativos ni
un manejo único de errores que altere los códigos particulares de cada caso.

No se midió una aceleración porcentual con carga de producción. Las mejoras
verificadas son reducciones concretas de lecturas/escrituras y código repetido.
No se ejecutó una restauración real de backups ni un recorrido manual de todas
las pantallas; la compatibilidad de la nueva migración se verificó con la
suite aislada. Las pruebas no constituyen una garantía de ausencia absoluta
de errores en recorridos no cubiertos.
