# Verificación de perfiles y usuarios

El Webmaster administra perfiles en GestionPerfiles.aspx y crea usuarios en
AltaUsuario.aspx. El Administrador consulta perfiles en ConsultarPerfiles.aspx.
RegistroCliente.aspx sigue asignando Cliente automáticamente y no permite
elegir ni enviar un rol.

PerfilBE es el Composite; PermisoPantallaBE es la hoja. Un perfil puede incluir
pantallas y otros perfiles. Se rechazan ciclos y versiones desactualizadas.
Cliente, Administrador y Webmaster conservan sus nombres y no pueden eliminarse.
Los perfiles adicionales pueden eliminarse si no están asignados a usuarios
ni incluidos en otro perfil. La gestión de perfiles y el alta de usuarios
permanecen reservadas al Webmaster.

La autorización se comprueba antes de los eventos de página, también al entrar
por URL directa. El menú usa los permisos vigentes. Los procedimientos SQL
adaptados consultan el mismo Composite y conservan las restricciones de datos
propios y cuentas activas. Las tablas nuevas tienen DVH, DVV y referencias.

Cada página nueva sigue el patrón del proyecto: ASPX, code-behind y designer;
DependentUpon y ASPXCodeBehind mantienen los archivos anidados en Visual Studio.

## Prueba manual

1. Ingresar como Webmaster y abrir Perfiles. Verificar los tres perfiles iniciales.
2. Crear Revisor con Gestión de planes y Consultar plan. Guardarlo y ver su detalle.
3. Crear Supervisor incluyendo Revisor. Verificar en el árbol los permisos heredados.
4. Modificar Revisor y comprobar que Supervisor refleje sus permisos. Intentar
   incluir Supervisor dentro de Revisor: debe rechazarse el ciclo.
5. Crear un perfil temporal sin asignaciones. Editarlo y eliminarlo, probando
   primero Cancelar y luego Confirmar eliminación. Los tres perfiles iniciales
   no deben poder eliminarse.
6. Abrir Alta de usuarios. Seleccionar Supervisor y revisar las pantallas que
   heredará. Completar los datos y crear el usuario; un correo inválido debe
   ser rechazado. Ingresar con esa cuenta y comprobar sus accesos.
7. Con un perfil que permita consultar pero no modificar planes, abrir
   ModificarPlan por URL directa: debe mostrarse Acceso denegado.
8. Ingresar como Administrador: Perfiles muestra el árbol sin controles de
   edición. GestionPerfiles y AltaUsuario deben rechazarse por URL directa.
9. Abrir RegistroCliente: no debe haber selector de perfil. Una cuenta creada
   desde ese formulario debe tener el rol Cliente.

## Validación realizada

- Compilación web y carga de las tres páginas con sus designers.
- 29 comprobaciones de perfiles, altas, registro público y restricciones.
- 41 comprobaciones de facturación y pagos.
- 58 comprobaciones de regresión de los módulos existentes.
- 315 comprobaciones de integridad y recuperación, incluidas las tablas de permisos.
- Recorridos web con Webmaster, Administrador y un perfil adicional; creación,
  modificación, herencia, eliminación, alta, login y rechazo de URL sin permiso.
- Formularios nuevos a 390 px sin desbordamiento horizontal y sin errores de consola.

Se utilizaron bases de prueba aisladas. El cierre de los puntos 12–14 queda
pendiente de la prueba y confirmación del usuario.
