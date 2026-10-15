# Idioma de la interfaz

La BLL expone `ActualizarIdioma(int idUsuario, string nuevoIdioma)` para `es` y
`en`. Solo permite cambiar la preferencia del usuario autenticado y activo.
La DAL recibe también el DVH calculado por la BLL para conservar las capas.
Idioma, DVH, DVV y referencias se confirman en una única operación de integridad.
La sesión recibe el usuario completo actualizado después del commit.

La migración `014_IdiomaUsuario.sql` queda embebida en VitaPet.DAL y se ejecuta
automáticamente durante Application_Start. Recompilar las bibliotecas y copiar
sus DLL al Bin del sitio antes de iniciar la aplicación.

`PaginaConIdioma.InitializeCulture()` aplica la cultura antes de crear los controles.
`PaginaBase` hereda de esa clase y conserva las validaciones de sesión y roles.
Las páginas protegidas ya heredan de ella, directamente o mediante
`OperacionUsuarioPage`. Para agregar una nueva página protegida, su code-behind
debe heredar de `VitaPet.GUI.Presentacion.PaginaBase` e implementar
`RolesPermitidos` y `EjecutarFuncionalidad()`, como Home.aspx.cs. La directiva
Inherits del .aspx sigue apuntando a su propia clase de code-behind.

Las páginas públicas heredan de `PaginaConIdioma`, que no exige autenticación.
El idioma proviene del usuario en sesión, con español como valor predeterminado.
El atributo `lang` de los documentos también refleja la cultura seleccionada.
Los recursos cubren navegación, formularios, validadores, grillas, paginación,
mensajes de error, confirmaciones y pantallas públicas de la aplicación.

El selector del header muestra un ícono de traducción, el código ES/EN actual
y un desplegable Bootstrap con Español e English. Admite teclado, Escape y
cierre al hacer clic fuera, y marca el idioma activo mediante aria-current.
Las opciones omiten la validación de formularios. Si la escritura falla, el
header muestra el error traducido y conserva la preferencia anterior. Al guardar,
redirige a Request.RawUrl con CompleteRequest, conservando la URL y sus parámetros.

Agregar claves con `<%$ Resources:Textos, Clave %>` en controles de servidor o
`Resources.Textos.Clave` en el code-behind. No se precisa generar un Designer.cs
manualmente: App_GlobalResources se compila con el sitio WebForms.

`TextosUI` centraliza la traducción de mensajes recibidos desde BLL/DAL y errores
por código ER. Los roles, criticidades y acciones de bitácora se traducen solo
al mostrarlos. Los valores de filtros, datos de usuarios, registros de auditoría,
hashes y exportaciones conservan sus valores originales. Nuevos mensajes deben
tener una clave en ambos recursos; los textos desconocidos conservan su original.

Validación web reproducible: ejecutar
`Tests/VitaPet.Usuarios.Tests/Run-IdiomaWeb.ps1`. La prueba crea una base aislada
y una copia del sitio, prueba ES/EN y el desplegable en Edge, y luego cierra su
IIS Express y elimina exclusivamente su base de pruebas. Las capturas quedan
en `artifacts/idioma-web`.
