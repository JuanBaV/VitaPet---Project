# Punto 24: validación de los cinco patrones documentados

El requisito original solicita seis patrones. La sección 12.7 de la carpeta
documenta cinco, y el usuario acordó validar el punto con esos cinco. Se conserva
la diferencia de cantidad; esta revisión no incorpora un sexto patrón.

| Patrón | Implementación | Uso comprobado |
| --- | --- | --- |
| Singleton | `VitaPet.SECURITY/SessionManager.cs` | Una instancia del gestor, creada mediante `Lazy<SessionManager>`, compartida por las solicitudes. El usuario, idioma y autorización de recuperación pertenecen a cada sesión HTTP. |
| Facade | `VitaPet.BLL/Seguridad/SecurityManager.cs` | Interfaz común para validaciones, autorizaciones, hash, DVH/DVV, diagnóstico de integridad y registro de auditoría. Coordina las clases que ejecutan estas operaciones. |
| State | `VitaPet.BLL/Estados/UsuarioStateBLL.cs`, `IUsuarioStateBLL.cs`, `UsuarioActivoStateBLL.cs` y `UsuarioBloqueadoStateBLL.cs` | El contexto delega en el estado activo o bloqueado. Al tercer intento fallido bloquea la cuenta; desbloquearla reinicia los intentos. Una cuenta inactiva o con restablecimiento pendiente no inicia sesión. |
| Template Method | `VitaPet Rework/Presentacion/PaginaBase.cs` | `OnLoad` está sellado. Comprueba sesión y permisos antes de continuar con `base.OnLoad`, `CargarEncabezado`, `MostrarMensajes` y `EjecutarFuncionalidad`. Las páginas concretas implementan este último paso; las dos primeras extensiones tienen implementación vacía por defecto. |
| Composite | `VitaPet.BE/PermisoComponenteBE.cs` y `VitaPet.BLL/PerfilBLL.cs` | `PermisoComponenteBE` es la abstracción común, `PermisoPantallaBE` es la hoja y `PerfilBE` es el compuesto. `RolBE` deriva de `PerfilBE`. El recorrido obtiene las pantallas propias y heredadas, elimina duplicados y rechaza ciclos. |

## Prueba manual

1. **Singleton:** abrir dos navegadores o perfiles de navegador independientes.
   Ingresar como Cliente en uno y Webmaster en el otro. Comprobar que cada
   encabezado muestra su usuario y menú. Cambiar el idioma o cerrar sesión
   en uno no debe cambiar el idioma ni cerrar la sesión del otro. Dos pestañas
   de la misma sesión del navegador comparten sus cookies y no sirven para
   comprobar el aislamiento entre usuarios.
2. **Facade:** como Webmaster, abrir Base de datos y ejecutar Verificar integridad.
   Sobre datos sanos debe informar que no hay errores. Revisar la bitácora
   después de una operación que registre auditoría, por ejemplo una consulta
   de cobertura realizada por el Cliente. Las pruebas automatizadas comprueban
   además hash, validaciones, DVH, DVV y persistencia de auditoría.
3. **State:** usar una cuenta de prueba activa y fallar tres veces la contraseña.
   Debe quedar bloqueada y rechazar el ingreso incluso con la contraseña correcta.
   Como Webmaster, abrir Gestión de usuarios, seleccionar esa cuenta y desbloquearla.
   La cuenta debe poder iniciar sesión nuevamente con su contraseña válida.
4. **Template Method:** sin sesión, abrir una página protegida, por ejemplo
   `/GestionPlanes`; debe llevar a Login. Como Cliente, abrir `/GestionPerfiles`;
   debe mostrar Acceso denegado. Al entrar a una página permitida, debe ejecutar
   su función normalmente. Para revisar la estructura en Visual Studio, poner un
   breakpoint en `PaginaBase.OnLoad` y en `EjecutarFuncionalidad` de una página
   concreta; la implementación base fija la secuencia y la derivada aporta su paso.
   Las validaciones anticipadas de Global y OnPreInit también pueden rechazar
   la solicitud antes de llegar a OnLoad.
5. **Composite:** como Webmaster, crear un perfil de prueba con Consultar plan.
   Crear un segundo perfil que incluya al primero y revisar el árbol de permisos
   heredados. Modificar el primero y comprobar el cambio en el segundo. Intentar
   incluir el segundo dentro del primero debe rechazarse por crear un ciclo.
   Un usuario asignado al segundo perfil debe recibir sus permisos heredados.

Los patrones son estructuras internas: la prueba manual verifica sus efectos y
la revisión de las clases verifica su organización. Las páginas públicas como
Home y Login no están obligadas a heredar de PaginaBase.

## Pruebas reproducibles

Compilar el proyecto de pruebas y ejecutar:

```powershell
& Tests/VitaPet.Facturacion.Tests/bin/Debug/net48/VitaPet.Facturacion.Tests.exe --patrones
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 Accesos-Web.cjs '..\..\artifacts\punto24-accesos' 1 cifrado
```

La primera suite comprueba el Singleton con accesos concurrentes y contextos
HTTP separados, las transiciones State en memoria y persistidas, las operaciones
de Facade y la suite existente de Composite (perfiles, herencia, ciclos y permisos).
La segunda repite la matriz web de permisos, accesos GET/POST, menús y logout
sobre una base temporal cifrada. Conserva el recorrido previo de facturación.
Ambas preparaciones crean y eliminan bases aisladas; no modifican las cuentas
ni los permisos del sitio de uso habitual.

## Resultado técnico

- 33 comprobaciones de Singleton, State y Facade aprobadas.
- 29 comprobaciones de perfiles y Composite aprobadas.
- 309 comprobaciones web de acceso sobre 32 pantallas aprobadas.
- 18 comprobaciones de regresión de facturación aprobadas.

La compilación del proyecto de pruebas y la integridad al finalizar la suite
BLL fueron correctas. Se revisaron las implementaciones y sus consumidores,
incluido el OnLoad sellado de PaginaBase. No fue necesario modificar los
patrones de la aplicación. El cierre manual corresponde al alcance de cinco
patrones acordado con el usuario.
