# Revisión de validaciones de VitaPet Rework

Fecha: 21/10/2026. Proyecto: `C:\Users\juanb\OneDrive\TFI\Tecnologia\Codigo\VitaPet Rework`.

## Resultado

Las validaciones del sistema se concentran en la BLL de C# y en los procedimientos almacenados. Los controles de validación ASP.NET se usan en una parte pequeña de la interfaz. Esto no implica que los demás formularios carezcan de validación: muchos delegan expresamente en C# y muestran códigos ER002/ER025.

Se confirmaron tres problemas funcionales/de identidad y dos reglas incompletas o inconsistentes que conviene definir. El problema de mayor prioridad es la recuperación de contraseña mediante una respuesta universal.

Esta revisión no modifica reglas de negocio ni datos. El único archivo agregado al proyecto durante esta revisión es este informe.

## Alcance y comprobaciones

- Inventario de 41 páginas ASPX, controles reutilizables, eventos de envío, páginas base, servicios ASMX y métodos de validación de las capas BLL/DAL.
- Lectura de los procedimientos relevantes y contraste con las definiciones actualmente instaladas en `localhost / VitaPet`: 70 procedimientos inventariados y 26 migraciones aplicadas. No se asumió que una migración antigua representara la definición vigente.
- La base tiene 26 restricciones CHECK/FK; ninguna está deshabilitada ni marcada como no confiable. Esta cifra no incluye las claves primarias ni los índices únicos.
- 52 comprobaciones ejecutadas contra los validadores compilados: entradas válidas, campos vacíos, formatos inválidos, longitudes, rangos, reglas de selección y cinco comportamientos problemáticos reproducidos.
- La comprobación de recuperación únicamente consultó una cuenta existente y llamó al método que valida identidad. No cambió contraseñas, no autorizó una sesión y no completó la recuperación.
- No se registraron clientes, empresas, mascotas, turnos o pagos; no se ejecutaron restauraciones. Las pruebas de pagos llamaron solamente al validador de datos, sin procesar cobros.
- La revisión combina análisis estático y pruebas de métodos; no equivale a una prueba de todos los recorridos mediante navegador ni a una prueba exhaustiva de concurrencia.

## Hallazgos prioritarios

### 1. Alta prioridad: la recuperación no acredita la identidad del titular

Ubicación: `VitaPet.BLL/RecuperacionContrasenaBLL.cs:12`, método `ValidarIdentidad` en línea 26; recorrido público en `VitaPet Rework/OlvideContrasena.aspx.cs`.

El método consulta el correo y compara la respuesta con una cadena fija del código, compartida entre todas las cuentas. No comprueba posesión del correo ni una prueba individual del titular. La página utiliza el resultado para autorizar el restablecimiento en sesión. La misma condición alcanza las cuentas WebMaster.

Comprobación: el método devolvió una cuenta WebMaster existente usando esa respuesta compartida, sin conocer su contraseña. No se ejecutó el cambio de contraseña.

Corrección propuesta: sustituir esa condición por una prueba individual verificable, con un token aleatorio de un solo uso, vencimiento y control de intentos. La autorización debe verificarse nuevamente al actualizar la contraseña. El mecanismo de entrega del token debe definirse para el entorno del proyecto.

### 2. Prioridad media: se puede guardar un correo que el login rechaza

Ubicación: `VitaPet.BLL/UsuarioAdministracionBLL.cs:102`, especialmente línea 111; comparación con `VitaPet Rework/Controls/ValidacionEmail.ascx:33` y `VitaPet.BLL/ClienteBLL.cs:85`.

La edición de usuarios acepta `ana@local` mediante `MailAddress`, mientras que el login y los registros exigen un dominio con punto. Después de guardar un correo de ese tipo desde la administración, el usuario no puede superar la validación del formulario de login con su nuevo correo.

Comprobación: `ValidarCambiosUsuario` aceptó el ejemplo; el patrón empleado por el login y el registro corporativo lo rechazó. No se modificó ninguna cuenta.

Corrección propuesta: reutilizar una misma regla de correo en altas, modificaciones, login y recuperación, con longitud compatible con la columna de 100 caracteres.

### 3. Prioridad media: el máximo de contraseña no se valida en el restablecimiento

Ubicación: `VitaPet.BLL/RecuperacionContrasenaBLL.cs:71`, `VitaPet.BLL/Seguridad/SecurityManager.cs:46` y `VitaPet Rework/RestablecerContrasena.aspx:48`.

El registro limita la contraseña a 100 caracteres. En el restablecimiento, el límite está en `TextBox.MaxLength`; el patrón del validador y la BLL verifican complejidad y mínimo de ocho caracteres, pero no el máximo. Un envío construido fuera de la interfaz puede presentar una contraseña de más de 100 caracteres, que se guarda como hash. Después no se puede introducir completa mediante el login normal, cuyo campo también tiene máximo 100.

Comprobación: una contraseña de 109 caracteres pasó `CumpleRequisitosContrasena`, usado por el restablecimiento, y fue rechazada por el registro particular.

Corrección propuesta: aplicar el mismo rango 8–100 en el servidor para todos los recorridos de contraseña. Mantener `MaxLength` como ayuda de interfaz, no como única comprobación del máximo.

## Reglas incompletas o por unificar

### 4. CUIT: se valida formato y unicidad, pero no el dígito verificador

Ubicación: `VitaPet.BLL/EmpresaBLL.cs:70`; procedimientos vigentes `sp_Empresa_Registrar`, `sp_Empresa_Actualizar` y `sp_Empresa_CuitRegistrado`.

La regla acepta once dígitos, con o sin guiones en la posición esperada; luego normaliza y verifica duplicados. No calcula el dígito verificador. El ejemplo `20123456789` fue aceptado por el validador de C#, aunque no supera esa comprobación matemática. Los SP tampoco incluyen ese cálculo.

Si el requisito es registrar un CUIT válido, falta esa regla. Validar el dígito verificador tampoco demuestra por sí solo que el CUIT exista o pertenezca a la empresa.

### 5. Teléfono: la exigencia cambia según el módulo

Ubicación: `VitaPet.BLL/EmpresaBLL.cs:71` frente a `VitaPet.BLL/ClienteBLL.cs:86`.

En empresas alcanza con que exista un dígito: el teléfono `1` fue aceptado. En clientes particulares y corporativos se requieren al menos seis dígitos. También difieren los caracteres de presentación permitidos.

Debe definirse si esa diferencia es intencional. Si ambos campos representan teléfonos de contacto, conviene compartir las reglas de longitud y normalización. No se fijó una regla nueva durante esta revisión.

## Mapa de validaciones por módulo

“Controles ASP.NET” se refiere a RequiredFieldValidator, RegularExpressionValidator y similares. No incluye un TextBox con MaxLength, un calendario, un selector o lógica de eventos en C#.

| Módulo | Controles ASP.NET | C# y presentación | SQL / restricciones |
|---|---|---|---|
| Login | Sí: correo y contraseña obligatorios; formato de correo | Compara hash, verifica integridad, estado, bloqueo e intentos; restringe URL de retorno | Consulta por correo; CHECK de rol, idioma e intentos; correo único |
| Recuperación de contraseña | Sí: correo, respuesta, nueva contraseña y confirmación obligatorios; patrón de contraseña | Autoriza por sesión y compara confirmación; defectos 1 y 3 | Actualiza hash y marca de restablecimiento; comprueba existencia de usuario |
| Registro particular | El correo incluye validadores, pero el botón no los activa | Obligatoriedad, tamaños, correo, teléfono, contraseña, caracteres de control y compatibilidad con VARCHAR; rol Cliente fijado por servidor | Unicidad de correo, transacción, claves y relaciones; el formato depende principalmente de C# |
| Consulta/edición/bloqueo de usuarios | Sin validadores específicos en las operaciones | Rol WebMaster, selección válida, datos obligatorios, roles admitidos, estado permitido, versión DVH y duplicados; defecto 2 | Compara versión bajo bloqueo, verifica existencia y correo duplicado; CHECK de rol y estado de intentos |
| Registro corporativo | El correo tiene controles, pero las acciones delegan en C# | Administrador, empresa activa, correo validado sin cambios posteriores, datos personales, correo único y restablecimiento inicial obligatorio | Revalida administrador, empresa, cuenta disponible, plan corporativo y ausencia de suscripción previa |
| Empresas | Sin validadores ASP.NET de campos | Obligatorios, tamaños, formato de CUIT y teléfono, normalización, unicidad y caracteres representables | Revalida rol Administrador, obligatorios, tamaños, caracteres, formato y CUIT único; observaciones 4 y 5 |
| Administración de planes/coberturas | Sin validadores ASP.NET de campos | Obligatorios, precio positivo con dos decimales y tope, estado, textos, coberturas requeridas y pertenencia de sus identificadores | Rol Administrador, tamaños y precio; nombre único; cobertura perteneciente al plan |
| Contratación de plan | Sin validadores ASP.NET de campos | Identificador, cliente autenticado, cantidad de mascotas, versión de cotización y tope de importe | Cuenta activa, plan contratable, mascotas activas, versión/cantidad actuales y ausencia de suscripciones incompatibles; bloqueos transaccionales |
| Pago | Sin validadores ASP.NET de campos | Medio permitido, titular, número, Luhn, vencimiento, código de seguridad e importe recalculado; modo de prueba explícito | Suscripción propia y pendiente, importe actual, medio permitido y ausencia de pago aprobado previo |
| Registro/actualización de mascota | Sin validadores ASP.NET de campos | Fecha ISO válida, obligatorios, tamaños, fecha no futura, caracteres y versión DVH al modificar | Dueño de la mascota, cuenta activa, campos obligatorios, fecha no futura, comparación de versión y FK |
| Consulta de mascota/historial/datos clínicos | Selección mediante claves de grilla | Verifica identificador, cliente y existencia de resultados | Restringe las consultas a mascotas del cliente; los datos clínicos tienen CHECK de valores positivos |
| Turnos | Sin validadores ASP.NET de campos | Selección guiada de especialidad, fecha y horario; limpieza de elecciones anteriores; formato HH:mm e identificadores | Pertenencia de mascota, agenda activa, fecha futura, horizonte, día y bloque horario permitidos; revalidación al registrar e índice único de turno confirmado |
| Cobertura | No requiere datos de texto | Resuelve al cliente autenticado y exige resultados | Suscripción propia activa y vigente; prestaciones activas |
| Reintegros | Selección mediante claves de grilla | Identificador y lectura nueva al consultar/exportar | Restringe a reintegros del cliente; FK e importe no negativo. La interfaz revisada es de consulta, no de alta |
| Bitácora | Sin validadores ASP.NET para el filtro | Fecha ISO, rango desde/hasta, criticidad permitida, autorización y DVH del XML histórico | FK, criticidad permitida y datos tipados; el filtrado de activos e histórico se realiza en C# |
| Backup/restauración | Sin validadores ASP.NET de campos | WebMaster, GUID, confirmación vinculada a sesión, hash de archivo, catálogo y esquema compatibles; validación en base temporal | BACKUP/RESTORE con CHECKSUM y VERIFYONLY; revalidación de integridad antes/después |
| Recuperación de WebMaster por XML | Sin validadores ASP.NET de campos | Cuenta propia de contingencia, contraseña 12–100, bloqueo por intentos, sesión con vencimiento, HMAC, esquema y base de destino | Reconstrucción temporal, tablas/columnas/tipos esperados y reactivación WITH CHECK CHECK de restricciones |
| Idioma | Selector de valores conocidos | Solo es/en, usuario autenticado y actualización de la cuenta propia | Rechazo de otros idiomas en SP y CHECK |
| Catálogo de errores y páginas informativas | Sin formularios de alta de datos | Consulta y codificación del texto mostrado | Lectura de catálogo; no se encontraron formularios para introducir reglas de error desde la web |

## Inventario de controles de validación

Recuento de declaraciones en el marcado propio del proyecto; un control reutilizado puede tener varias instancias en ejecución.

| Control | Declaraciones |
|---|---:|
| RequiredFieldValidator | 5 |
| RegularExpressionValidator | 2 |
| CompareValidator | 0 |
| RangeValidator | 0 |
| CustomValidator | 0 |
| ValidationSummary | 3 |

Solo login, olvido de contraseña y restablecimiento llaman explícitamente a `Page.Validate(...)` y consultan `Page.IsValid`. En registro particular y corporativo, el componente de correo contiene validadores, pero las acciones usan deliberadamente la validación de la BLL. La coincidencia de contraseñas se comprueba manualmente en C#; no usar CompareValidator no constituye por sí mismo un defecto.

## Consideraciones de las capas

- MaxLength, el tipo HTML del campo y los botones deshabilitados ayudan al usuario, pero no reemplazan las comprobaciones del servidor.
- No todos los SP repiten las reglas de la BLL. Por ejemplo, los SP de registro particular verifican transacción y duplicados, pero no vuelven a validar formato de correo, teléfono o campos vacíos. La ruta web está cubierta por ClienteBLL; una integración que invoque directamente esos SP no obtiene la misma validación. Esto es una limitación del contrato SQL, no evidencia de un bypass en la página pública.
- Las comprobaciones de propiedad de registros aparecen en BLL y en los SP de mascotas, turnos, cobertura y reintegros. Una FK por sí sola no acredita que el registro pertenezca al usuario autenticado.
- DVH/DVV, referencias de integridad y firmas verifican consistencia o alteraciones. No sustituyen reglas como formato de correo, identidad del titular o dígito verificador de CUIT.
- El sistema usa parámetros SQL para los datos de los formularios revisados. El SQL dinámico de backups e importación delimita identificadores o restringe nombres temporales; no se observó interpolación directa de campos del formulario en esas consultas.

## Evidencia de ejecución

Las 52 comprobaciones produjeron los resultados esperados de la auditoría. Cinco de esos resultados confirman las debilidades descriptas; por eso no deben interpretarse como “52 reglas correctas”.

```text
OK: Cliente: datos validos
OK: Cliente: nulo
OK: Cliente: nombre vacio
OK: Cliente: apellido largo
OK: Cliente: dominio sin punto
OK: Cliente: telefono de un digito
OK: Cliente: clave debil
OK: Cliente: clave de mas de 100 caracteres
OK: Cliente: caracteres de control
OK: Cliente: texto no representable en VARCHAR
OK: Usuario: rol no permitido
OK: Usuario: estado faltante
OK: HALLAZGO: modificar usuario acepta correo rechazado por login
OK: Corporativo: mismo correo rechazado
OK: Corporativo: normalizacion de correo
OK: HALLAZGO: CUIT con digito verificador incorrecto aceptado
OK: HALLAZGO: empresa acepta telefono de un digito
OK: Empresa: longitud de CUIT invalida
OK: Empresa: normaliza guiones
OK: Plan: datos validos
OK: Plan: precio cero
OK: Plan: precio negativo
OK: Plan: mas de dos decimales
OK: Plan: precio fuera de rango
OK: Plan: sin coberturas
OK: Plan: identificadores de cobertura duplicados
OK: Mascota: datos validos
OK: Mascota: nacimiento futuro
OK: Mascota: nacimiento faltante
OK: Mascota: nombre largo
OK: Bitacora: rango de fechas invertido
OK: Bitacora: criticidad invalida
OK: Usuarios: filtro largo
OK: HALLAZGO: validador reutilizado por restablecimiento permite mas de 100 caracteres
OK: HALLAZGO: respuesta universal autoriza recuperar un WebMaster sin conocer su clave
OK: Pago: tarjeta de prueba con formato y Luhn validos
OK: Pago: Luhn incorrecto
OK: Pago: codigo de seguridad corto
OK: Pago: tarjeta vencida
OK: Pago: medio no permitido
OK: Pago: datos faltantes
OK: Turno: formato de seleccion valido
OK: Turno: mascota no seleccionada
OK: Turno: hora fuera de rango
OK: Turno: formato no canonico
OK: Turno: fecha no seleccionada
OK: Contingencia: rechaza nombre de base fuera del prefijo temporal
OK: Contingencia: acepta nombre temporal con GUID
OK: Backup: esquema identico compatible
OK: Backup: rechaza migracion con checksum distinto
OK: Backup: rechaza manifiesto incompleto
OK: Contingencia: XML con DTD rechazado sin resolver entidades
COMPROBACIONES: 52. No se ejecutaron altas, actualizaciones ni recuperaciones reales.
```
