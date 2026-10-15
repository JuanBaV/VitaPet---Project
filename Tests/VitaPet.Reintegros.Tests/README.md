# CU-001-005 Consulta de estado de reintegro

Se accede desde Reintegros en el menú del cliente. La interfaz presenta A listado,
B Consultar por fila, C número, D fecha de solicitud, E importe, F estado,
G detalle y H Exportar a XML. H permanece deshabilitado hasta consultar el detalle.
Los campos son de solo lectura. No requiere seleccionar una mascota.

La GUI sigue PaginaClientePlanes / PaginaBase, la BLL usa SecurityManager,
SessionManager y OperacionIntegridadDAL, y la DAL ejecuta SP con parámetros tipados.
sp_Reintegro_ListarCliente y sp_Reintegro_ConsultarCliente restringen las lecturas
al cliente activo del usuario autenticado. Se reutilizan Cliente y Reintegro;
la auditoría usa Bitácora, DVH, DVV y referencias. No se agrega otro webservice.

El XML se genera en memoria con XmlTextWriter, en UTF-8 y con decimales invariantes.
Incluye un solo reintegro (idReintegro, fechaSolicitud, importe, estado y detalle),
sin identificadores de cuenta ni controles internos. Se vuelve a validar integridad
y a consultar el estado actual antes de generar la descarga Reintegros.xml.

Errores: ER003 conexión, ER009 listado vacío, ER011 datos propios ausentes,
ER023 integridad, ER033 bitácora, ER015 XML y ER059 consulta de reintegros.
ER015 mantiene el detalle para reintentar. Los demás errores descartan el detalle
y deshabilitan la exportación. ER059 se registra mediante ErrorSistemaBLL.

Compilar la solución y ejecutar bin/Debug/net48/VitaPet.Reintegros.Tests.exe
para las verificaciones SQL. Run-Web.ps1 usa IIS Express / Edge para comprobar
la GUI, errores, descarga, idiomas y móvil. Ambas pruebas crean y eliminan bases
temporales VitaPet_Verificacion_Reintegros_*. No apuntan a la base operativa.
--refresh solo actualiza los controles de Reintegro en el fixture aislado del navegador.

Los dos reintegros de demostración se cargaron explícitamente para prueba pruebita,
asociados a su mascota activa Luna. El inicializador de artifacts/reintegros_implementation
es idempotente y verifica la integridad antes y después; no se ejecuta al iniciar la app.
