# CU-002-001 Registro de mascota

Acceso: Mascotas → Registrar mascota. RegistroMascota.aspx tiene nombre A,
especie B, raza C, DateTimePicker propio D, Registrar E y confirmación F.
No se permite elegir el propietario ni el estado: la BLL/DAL lo resuelven desde
la cuenta Cliente autenticada y registran la mascota activa. La página Mascotas
incorpora el acceso; el listado y las acciones de CU-002-002 se implementarán en ese caso.

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
