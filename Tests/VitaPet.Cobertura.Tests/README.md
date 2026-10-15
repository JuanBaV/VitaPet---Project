# CU-001-004 Consulta de cobertura

Desde **Mi perfil → Consultar cobertura** se abre `ConsultaCobertura.aspx`. Muestra
el plan, descripción, estado y fechas de la suscripción y sus prestaciones activas.
La consulta utiliza la cuenta autenticada; no recibe identificadores desde la URL
y no requiere seleccionar mascotas. Una suscripción pendiente, vencida o futura
no habilita la consulta. El flujo termina al mostrar la información.

La GUI utiliza `PaginaClientePlanes` / `PaginaBase`, `SessionManager` y una instancia
C# de `CoberturaService`, cuyo código continúa junto al ASMX en `WebServices`.
`ObtenerCobertura()` consulta la cuenta; se expone por SOAP mediante la operación
`ObtenerCoberturaCuenta`, sin parámetros. `ObtenerCobertura(idPlan)` se conserva
para las coberturas del catálogo de CU-001-002.

`ConsultaCoberturaBLL` valida al cliente y la integridad, consulta por DAL y registra
la bitácora dentro de `OperacionIntegridadDAL`. La información se devuelve solo
después de confirmar la auditoría, su DVH, DVV y referencias. Las tablas de negocio
se mantienen en modo de lectura. Los errores previstos son ER003, ER008, ER011,
ER023, ER033 y ER058; este último se registra desde ErrorSistemaBLL.

La migración `018_ConsultaCobertura.sql` agrega dos SP, reutilizando Cliente,
Suscripcion, Plan y Cobertura. No agrega tablas, webservices ni salidas XML propias.
Un plan corporativo puede consultarse si está asociado a una suscripción vigente;
continúa excluido de la contratación particular.

Compilar la solución y ejecutar `bin/Debug/net48/VitaPet.Cobertura.Tests.exe` para
las verificaciones SQL. `Run-Web.ps1` prueba las pantallas, SOAP, autorización,
idiomas, errores y móvil en IIS Express / Edge. Ambos crean bases temporales con
prefijo `VitaPet_Verificacion_Cobertura_` y las eliminan al terminar. Las capturas
del navegador se guardan en `artifacts/cobertura-web`.
