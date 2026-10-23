# Punto 25: tres salidas XML de negocio

Las tres salidas de este punto son Reintegros.xml, Turnos.xml y Cobertura.xml.
La exportación de Bitácora corresponde a auditoría y no se cuenta como una
salida de negocio. Se incorporó la descarga de cobertura para completar la tercera.

| Archivo | Página | Contenido |
| --- | --- | --- |
| Reintegros.xml | Reintegros.aspx | El reintegro propio seleccionado: identificador, fecha, importe, estado y detalle. |
| Turnos.xml | Turnos.aspx, vista Consultar turnos | Todos los turnos de la mascota propia seleccionada: identificador, prestación, fecha, horario, zona y estado. |
| Cobertura.xml | ConsultaCobertura.aspx | Suscripción y plan contratado, nombre, descripción, estado, vigencia y todas las prestaciones con sus detalles. |

## Prueba manual

1. Iniciar sesión como Cliente. Para probar las descargas positivas, la cuenta
   debe tener un reintegro, una mascota con turnos y una contratación activa.
   Las pruebas automatizadas preparan estos datos en una base temporal.
2. **Reintegros:** abrir Reintegros. Exportar a XML debe estar deshabilitado hasta
   consultar una fila. Consultar un reintegro, pulsar Exportar a XML y abrir
   Reintegros.xml. Debe contener un solo `Reintegro`, coincidente con el detalle
   seleccionado, dentro de la raíz `Reintegros`.
3. **Turnos:** abrir Consultar turnos y seleccionar una mascota con turnos.
   Pulsar Exportar a XML y abrir Turnos.xml. La raíz debe ser `Turnos` y debe
   contener todos los turnos de esa mascota que aparecen en la consulta.
   Cambiar a una mascota sin turnos debe deshabilitar la descarga anterior.
4. **Cobertura:** abrir Mi perfil → Consultar cobertura. Pulsar Exportar a XML
   debajo de las prestaciones. Abrir Cobertura.xml y comparar el nombre del plan,
   la vigencia y las prestaciones con la pantalla. Debe contener únicamente la
   contratación de la cuenta autenticada.
5. Cambiar a inglés y repetir las descargas. El botón debe traducirse y los
   archivos deben conservar la estructura XML y los mismos valores de negocio.
   Los importes utilizan punto decimal y las fechas, formato ISO.
6. Entrar con otro Cliente y comprobar que solo accede a sus propios registros.
   Una cuenta sin contratación debe mostrar ER008 y no ofrecer la descarga de
   cobertura. Sin reintegros corresponde ER009; sin turnos, ER014. No se genera
   un archivo con datos inventados para completar una consulta vacía.

En XML, `&` y `<` se representan como `&amp;` y `&lt;` dentro del texto. Al leer
el documento se recupera el contenido original. Los archivos usan UTF-8 y no
deben incluir HTML de la página después de la raíz XML.

## Contrato de Cobertura.xml

La raíz `Cobertura` contiene `idSuscripcion`, `idPlan`, `nombrePlan`,
`descripcionPlan`, `estadoSuscripcion`, `fechaAlta`, `fechaVencimiento` y
`Prestaciones`. Este último elemento contiene un `Prestacion` por cada fila,
con sus elementos `nombre` y `detalle`.

La BLL vuelve a consultar la contratación de la cuenta autorizada al exportar.
No recibe identificadores del cliente, plan ni suscripción desde el navegador.
La respuesta se descarga como adjunto `application/xml; charset=utf-8`, con
la caché deshabilitada. El Render de la página omite el HTML durante la descarga.

## Pruebas reproducibles

Después de compilar la solución, el proyecto de pruebas y la herramienta de cifrado:

```powershell
& Tests/VitaPet.Facturacion.Tests/bin/Debug/net48/VitaPet.Facturacion.Tests.exe --xml
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 Xml-Web.cjs '..\..\artifacts\punto25-xml' 1 cifrado --xml
```

La primera suite prepara una contratación por el flujo de pago existente y
comprueba los tres contratos XML, formatos, valores y permisos de la BLL.
La segunda comprueba los archivos descargados, cabeceras, idiomas, presentación
móvil, coincidencia con las consultas y rechazo de POST sin permiso. Ejecuta
también la regresión previa de facturación.

Ambas suites usan bases aisladas. La web emplea una base cifrada y captura los
correos localmente. Las bases se eliminan al finalizar; no se modifican los
registros, la clave ni los permisos de la aplicación habitual. El informe web
queda en `artifacts/punto25-xml/xml-resultados.json`.

## Resultado técnico

- 77 comprobaciones BLL aprobadas.
- 113 comprobaciones web de los tres XML aprobadas.
- 18 comprobaciones de regresión de facturación aprobadas.

Solución compilada e integridad final de la base cifrada correctas. Descargas
revisadas en español e inglés, incluyendo el botón de cobertura a 390 px.
La sección 12.6 del documento técnico enumera las tres salidas de negocio.
