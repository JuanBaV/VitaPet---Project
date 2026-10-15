# Migraciones de VitaPet

La aplicación ejecuta automáticamente estas migraciones, en orden, durante
`Application_Start`. Las versiones aplicadas quedan registradas en
`dbo.__SchemaMigrations` junto con su checksum.

Para cambiar el esquema, los datos de referencia o los procedimientos:

1. Agregar un archivo `.sql` con el siguiente número correlativo.
2. Hacer que la migración sea compatible con bases que ya contienen datos.
3. No modificar una migración que ya haya sido aplicada.

Los separadores `GO` deben aparecer solos en su línea. Cada migración se aplica
dentro de una transacción; si una sentencia falla, esa versión no queda marcada
como aplicada.

## Migración 006: integridad sin clave externa

Al iniciar, VitaPet verifica los DVH y DVV existentes. Si son válidos, convierte
una sola vez las referencias por campo a SHA-256 y marca el formato en
`dbo.__FormatoIntegridad`. Los inicios siguientes verifican esas referencias
sin volver a generarlas. `integridad.key` ya no se usa y no es necesario
trasladarlo entre PCs.

El catálogo de backups anterior se convierte al leerlo si el archivo `.bak`
coincide con el hash que figura en sus metadatos. La antigua firma HMAC no se
puede comprobar sin la clave original. Las huellas nuevas detectan cambios
accidentales, pero quien pueda editar los datos y las huellas puede recalcularlas.

## Migración 010: Gestión de usuarios

Agrega `sp_Usuario_Listar` con parámetros de nombre y estado, orden por nombre e
identificador y proyección sin credenciales. Activo/Inactivo filtran `estado`;
Bloqueado/Desbloqueado filtran `bloqueado`, independientemente del estado activo.
La búsqueda parcial interpreta literalmente `%`, `_` y `[`.

ER028 corresponde a la ausencia de coincidencias de CU-007-004. El mensaje que
usaba recuperación de contraseña pasa a ER035, junto con su referencia en
`OlvideContrasena.aspx.cs`. No se modifican las migraciones aplicadas previamente.

## Migración 011: operaciones de usuarios

Agrega `sp_Usuario_ObtenerPorId`, `sp_Usuario_GuardarAdministracion` y los errores
ER036–ER040 para CU-007-005 a CU-007-008. Conserva los usuarios existentes.
La actualización exige una transacción, verifica la versión DVH y el correo
único. La BLL incluye los cambios, los dígitos, las referencias protegidas y el
evento de bitácora en una única `OperacionIntegridadDAL`.

## Migración 015: Registro de cliente

Agrega `sp_Usuario_RegistrarCliente` y `sp_Cliente_Registrar`. El alta pública
fija rol Cliente, estado activo, cero intentos, sin bloqueo e idioma español.
La BLL guarda cuenta, cliente, dígitos verificadores, referencias y bitácora
en una misma transacción. El correo se comprueba con bloqueo y también queda
protegido por la restricción única existente.

ER049 se agrega mediante `ErrorSistemaBLL.AsegurarErroresRegistroCliente`
durante el inicio, después de verificar la integridad del catálogo; conserva
el procedimiento usado para ER046–ER048 y no altera migraciones aplicadas.

## Migración 016: Contratación de planes para la cuenta

Reutiliza Plan, Cobertura, Cliente, Mascota y Suscripcion. El catálogo inicial
contiene Esencial (USD 19), Preventivo (USD 28) e Integral (USD 39), por mes y
por mascota activa. Corporativo sólo existe para convenios, con precio NULL
si aún no se definió; no aparece ni se admite en contratación particular.
La nullabilidad de Plan.precio no cambia los valores de registros existentes.
El catálogo se agrega desde la BLL, después de verificar integridad, con sus
DVH/DVV/referencias. No se sobrescriben planes o coberturas ya registrados.

El total se calcula en el servidor según las mascotas activas del cliente.
Preparar la operación crea una suscripción Pendiente para la cuenta y la
audita en una transacción. Reintentar el mismo plan reutiliza el pendiente;
un plan activo o un pendiente de otro plan impide otra contratación.
Los cambios de precio o cantidad exigen consultar nuevamente el detalle.
CU-001-003 se implementará después: no se registran pagos ni se activa el plan.
WebServices/CoberturaService.asmx expone ObtenerCobertura con sesión y
autorización Cliente. Su implementación está en WebServices/CoberturaService.asmx.cs,
compilada por la Web Application. ContratarPlan.aspx.cs lo consume creando una instancia y
llamando a ObtenerCobertura desde C#, igual que el ejemplo de BonusService.
ListadoPlanes.ascx se reutiliza desde Planes.aspx.

La Web Application admite la adaptación verificada de respaldos históricos con
las migraciones conocidas 012 a 016. Los metadatos deben conservar el prefijo
completo del esquema actual; una migración desconocida o un checksum alterado
no habilita la restauración. La validación de datos se realiza en la base temporal.
