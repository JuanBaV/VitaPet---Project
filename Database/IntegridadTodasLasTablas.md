# Integridad de todas las tablas funcionales

El formato `sha256-v2` verifica DVH, DVV y referencias por campo de Usuario, Bitacora, Empresa, Cliente, Plan, Cobertura, Suscripcion, Pago, Mascota, Turno, HistorialClinico, Reintegro y ErrorSistema. La migración 012 añade DVH a ErrorSistema; las demás tablas ya tenían esa columna. Una tabla dbo no reconocida produce un error de cobertura: agregar una entidad requiere incorporarla a los controles.

Usuario y Bitacora conservan su cálculo anterior y la compatibilidad del histórico XML. Las otras once tablas utilizan las columnas en orden de esquema, excluyendo DVH, con valores precedidos por longitud y dos puntos. NULL se representa como `-1:`, los booleanos como `1`/`0`, las fechas como `O` y los decimales como `G29`, con cultura invariante. Los DVV concatenan DVH en orden ascendente de la clave primaria, con el mismo prefijo de longitud del cálculo existente. ErrorSistema utiliza su clave textual `codigo`.

Las tablas técnicas no se incluyen recursivamente en su propio DVH/DVV: DVV se contrasta contra los datos supervisados, ReferenciaIntegridad conserva su sello de conjunto, __FormatoIntegridad identifica el formato, y __SchemaMigrations conserva los checksums comprobados por el migrador. Los hashes no sustituyen los permisos de SQL Server.

## Migración

Conservar un backup previo. Ejecutar `DatabaseMigrator.Migrate()` y `ReferenciaIntegridadBLL.Inicializar()` con el sitio detenido para evitar escrituras simultáneas. La inicialización verifica los DVH/DVV y las referencias anteriores antes de establecer la línea base de las tablas que hasta ahora no se supervisaban. En una sola transacción calcula los nuevos verificadores, cambia el formato a v2 y guarda las referencias. Si falla, revierte la inicialización. No corrige automáticamente inconsistencias existentes ni recalcula controles v2 ya inicializados. Los datos antes no supervisados reciben su primera referencia en esa transición.

Las escrituras de Usuario y Bitacora mantienen sus verificadores dentro de las transacciones actuales. Los módulos funcionales aún no implementados deberán comprobar la integridad antes de modificar sus tablas y actualizar DVH, DVV y referencias dentro de la misma transacción. No hay triggers que validen automáticamente cambios hechos directamente en SQL: estos cambios deben detectarse como inconsistencias.

## Recuperación

Los backups con las migraciones vigentes y los del esquema inmediatamente anterior (hasta 011) son compatibles. El respaldo anterior se verifica con sus controles originales y se amplía en una base temporal antes de sustituir la base operativa. La importación XML admite únicamente la adición conocida de la columna DVH de ErrorSistema. Mantiene la comprobación de firma, cuenta, nombre de base de datos, relaciones y controles originales. No se aceptan diferencias arbitrarias de esquema.

## Verificación

La suite de contingencia prueba alteración de campos en cada tabla adicional, ausencia de DVV, inserciones externas, tablas desconocidas, independencia regional de decimales, nulos, migración que rechaza corrupción previa y restauración de backups SQL/XML anteriores. La suite de usuarios conserva las pruebas de operaciones, permisos, archivado y rollback.
