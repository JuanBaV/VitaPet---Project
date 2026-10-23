# Punto 23: los tres webservices

Los servicios ASMX están en `VitaPet Rework/WebServices`. Sus ocho operaciones
expuestas utilizan sesión y validan los permisos y la pertenencia de los datos
en BLL. Las páginas los consumen mediante llamadas C# a las clases de servicio;
la suite prueba además los endpoints SOAP 1.1 y sus contratos WSDL por HTTP.

| Servicio | Operaciones publicadas | Páginas consumidoras |
| --- | --- | --- |
| TurnoService.asmx | ObtenerDisponibilidadPorZona, VerificarDisponibilidadTurno, RegistrarTurno, ObtenerTurnosMascota | Turnos.aspx |
| HistorialClínicoService.asmx | ObtenerHistorialClinico, ObtenerDatosClinicos | HistorialClinico.aspx, DatosClinicos.aspx |
| CoberturaService.asmx | ObtenerCobertura, ObtenerCoberturaCuenta | ContratarPlan.aspx, ConsultaCobertura.aspx |

El servicio clínico conserva mensajes SOAP con tilde (`ObtenerHistorialClínico`
y `ObtenerDatosClínicos`); los nombres de operación del WSDL son los de la tabla.
La prueba obtiene los mensajes y acciones del contrato publicado.
La sobrecarga C# `ObtenerCobertura()` sin parámetros es un acceso interno a
`ObtenerCoberturaCuenta`; no constituye una tercera operación SOAP.

## Prueba manual

1. Iniciar el proyecto desde Visual Studio y entrar como Cliente.
2. Abrir las tres direcciones siguientes en pestañas del mismo navegador.
   Cada una debe mostrar el servicio y sus métodos; agregar `?WSDL` debe
   mostrar su contrato XML. Si Visual Studio usa otro puerto, ajustar la URL.

   - https://localhost:44382/WebServices/TurnoService.asmx
   - https://localhost:44382/WebServices/HistorialCl%C3%ADnicoService.asmx
   - https://localhost:44382/WebServices/CoberturaService.asmx

3. **Cobertura:** abrir Planes, seleccionar un plan y revisar sus prestaciones.
   Si la cuenta ya tiene una contratación activa, abrir Mi perfil → Consultar
   cobertura. Deben coincidir el plan contratado, su vigencia y sus prestaciones.
   Una cuenta sin contratación debe mostrar el aviso correspondiente (ER008).
4. **Información clínica:** abrir Mascotas, seleccionar una mascota propia y
   entrar a Historial clínico. Volver, seleccionar la misma mascota y entrar a
   Datos clínicos. Deben mostrarse únicamente sus registros. Si no tiene registros,
   corresponde el aviso «No existen datos asociados» (ER011). Las pruebas
   automatizadas incluyen registros sintéticos para comprobar ambos resultados
   positivos, además de los casos sin datos.
5. **Solicitud de turno:** abrir Turnos, seleccionar una mascota propia, zona,
   especialidad, día disponible y horario. Pulsar Confirmar solicitud. Debe aparecer
   el número de turno con la mascota, prestación, fecha, hora y zona elegidas.
6. **Consulta de turnos:** abrir Consultar turnos, seleccionar la misma mascota
   y comprobar que aparece el turno registrado una sola vez. Si se intenta
   reservar el mismo horario ocupado, debe rechazarse (ER013).
7. **Aislamiento:** entrar con otro Cliente y comprobar que no obtiene la cobertura
   ni los registros de la cuenta anterior. Para las páginas clínicas puede usarse
   la URL anterior con el identificador de esa mascota: debe rechazarse con ER011.
   Cerrar sesión y volver a una página consumidora debe llevar al inicio de sesión.

Ver el contrato WSDL es una consulta de metadatos; no da acceso a los datos.
Las invocaciones SOAP sin sesión o con los perfiles iniciales Administrador y
Webmaster se prueban automáticamente y deben devolver un fallo de acceso denegado.

## Repetir las pruebas automatizadas

Después de compilar la solución, el proyecto de pruebas y la herramienta de cifrado:

```powershell
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 Servicios-Web.cjs '..\..\artifacts\punto23-servicios' 1 cifrado --clinica
```

La preparación utiliza una base temporal cifrada, datos clínicos sintéticos de
dos clientes y una instancia independiente de IIS Express. Captura el correo
localmente. Al terminar elimina la base temporal y detiene esa instancia.
El informe queda en `artifacts/punto23-servicios/servicios-resultados.json`.
No se necesitan datos clínicos de prueba en la base de uso habitual.
