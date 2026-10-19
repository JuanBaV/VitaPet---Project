# CU-002-001 Registro de mascota

Acceso: Mascotas → Registrar mascota. RegistroMascota.aspx tiene nombre A,
especie B, raza C, DateTimePicker propio D, Registrar E y confirmación F.
No se permite elegir el propietario ni el estado: la BLL/DAL lo resuelven desde
la cuenta Cliente autenticada y registran la mascota activa. La página Mascotas
incorpora el acceso; el listado y las acciones de CU-002-002 se implementan en los cinco casos siguientes.

Se reutilizan Mascota, Cliente y la infraestructura de bitácora e integridad.
La migración 020 agrega sp_Cliente_ObtenerActivoUsuario y sp_Mascota_Registrar.
El alta y su auditoría comparten OperacionIntegridadDAL, con DVH, DVV y referencias.
La BLL exige Cliente activo y no bloqueado y utiliza SecurityManager y SessionManager;
la GUI conserva PaginaBase/PaginaClientePlanes. No se agrega un webservice ni XML propio.

Datos obligatorios: nombre (60), especie (40), raza (60) y fecha válida no futura.
Los textos se recortan y deben representarse sin pérdida en el VARCHAR de la base.
Se permiten mascotas diferentes con el mismo nombre. La confirmación permanece
en la misma página mediante POST/Redirect/GET; refrescar no repite el POST.
Los campos quedan deshabilitados en la confirmación; reabrir el formulario permite otro alta.

Errores: ER002 incompletos, ER025 inválidos, ER023 integridad, ER003 conexión,
ER011 cliente asociado ausente, ER060 guardado y ER040 bitácora. Las fallas de
persistencia o auditoría revierten el alta completa. ER060 se registra con sus controles
mediante ErrorSistemaBLL. Los mensajes y controles tienen traducciones ES/EN.

Compilar la solución y ejecutar bin/Debug/net48/VitaPet.Mascota.Tests.exe para
las verificaciones SQL. Run-Web.ps1 verifica la GUI con IIS Express y Edge en una
base temporal VitaPet_Verificacion_Mascota_*. Las pruebas eliminan sus bases al terminar.
No se agregaron mascotas de demostración a la base operativa durante esta implementación.

## Gestión, actualización y consultas de mascotas

CU-002-002 usa SelectorMascota.ascx A y las acciones Actualizar datos B,
Consultar datos C, Historial clínico D y Datos clínicos E. Las acciones permanecen
deshabilitadas hasta seleccionar una mascota propia. El registro anterior sigue accesible.

CU-002-003 permite editar nombre A, especie B, raza C y nacimiento D mediante
DateTimePicker; Guardar cambios E confirma la operación y F muestra el resultado.
La edición conserva propietario y estado, usa el DVH original como versión y
devuelve ER064 cuando otra operación modificó la mascota. Usa PRG para que
recargar la confirmación no reenvíe el POST.

CU-002-004 muestra nombre A, especie B, raza C, nacimiento D y estado E, en solo lectura.
CU-004-001 y CU-004-002 muestran la mascota A y el listado B, consumiendo
HistorialClínicoService.asmx desde el codebehind. La BLL obtiene el usuario
autenticado y los SP verifican de nuevo la pertenencia. Las consultas clínicas
sin registros devuelven ER011; no se generan datos clínicos automáticamente.

La migración 021 crea DatosClinicos y cinco SP; el esquema y los SP anteriores
conservan sus checksums. La tabla nueva participa de la integridad y la recuperación.
Los errores nuevos son ER061 (consulta de mascotas), ER062 (actualización),
ER063 (servicio clínico) y ER064 (concurrencia), con traducciones ES/EN.

Ejecutar `bin/Debug/net48/VitaPet.Mascota.Tests.exe --gestion`: 76 comprobaciones.
`Run-GestionWeb.ps1`: 45 comprobaciones de navegador y SOAP en una copia aislada
con IIS Express y Edge. Las capturas y el WSDL quedan en artifacts/gestion-mascotas-web.
Se verifican acceso exclusivo de Cliente, aislamiento entre cuentas, validaciones,
lecturas, concurrencia, reversión ante fallas, integridad, traducciones y vista móvil.
Regresiones: registro 32, contratación 28 y recuperación 112 comprobaciones.
# Turnos CU-003-001 y CU-003-002

`VitaPet.Mascota.Tests.exe --turnos` verifica la agenda local, las cuatro operaciones
del webservice, permisos, pertenencia de mascotas, errores, concurrencia entre
clientes, confirmaciones repetidas, rollback por fallos de registro o bitácora,
XML y detección de corrupción. Utiliza exclusivamente una base temporal.

`Run-TurnosWeb.ps1` crea una copia temporal del sitio en IIS Express (puerto 60339),
ejecuta los recorridos de solicitud y consulta con Playwright y Edge, verifica
SOAP y la descarga de Turnos.xml y elimina su propia base al finalizar.
Las capturas y el resultado quedan dentro de `artifacts/turnos-web`.

