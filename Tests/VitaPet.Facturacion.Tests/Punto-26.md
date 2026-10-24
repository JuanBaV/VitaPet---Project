# Punto 26: tratamiento de la bitácora

La baja es automática al superar treinta días y el histórico sigue disponible.
Los registros recientes permanecen activos, aunque haya más de cincuenta. Los
anteriores reciben `archivado = 1` y conservan su fila SQL y una copia XML
verificada. No hay una acción manual de eliminación.

El mantenimiento se ejecuta al iniciar la aplicación, registrar un evento o
consultar la bitácora con SQL íntegro. El corte es fecha y hora local del servidor
menos treinta días. Los vencimientos durante la inactividad se procesan en la
siguiente ejecución de estos controles.

## Prueba manual

1. Detener la ejecución en Visual Studio y volver a iniciar **VitaPet Rework**.
   El inicio valida el esquema actual y ejecuta el mantenimiento. Las
   migraciones aplicadas ya no se ejecutan desde la aplicación.
2. Iniciar sesión como **WebMaster** y abrir **Consulta de bitácora**.
3. Elegir fechas desde hace treinta días hasta hoy y pulsar **Consultar**.
   Comprobar que los eventos corresponden al intervalo. Las fechas de ambos
   extremos se incluyen completas. Probar las cuatro criticidades y la opción
   de todas; cada consulta debe respetar los filtros elegidos.
4. Invertir las fechas: debe aparecer **ER027**. Consultar un intervalo futuro
   sin eventos: debe aparecer **ER019** y deshabilitarse **Exportar a XML**.
5. Consultar un intervalo anterior a treinta días que tenga eventos. Deben seguir
   apareciendo, sin duplicaciones. Si la base aún no tiene eventos de esa edad,
   usar la comprobación aislada que se indica debajo; no cambiar el reloj ni
   modificar las fechas de la base de uso.
6. Si hay más de veinte resultados, pasar a otra página y exportar. El XML debe
   contener **todos** los resultados del filtro, no solamente la página visible.
   Cambiar los filtros y pulsar directamente **Exportar a XML** también debe
   aplicar los nuevos valores. Los eventos dados de baja por esta política
   incluyen `<Archivado>true</Archivado>`.
7. Cerrar la sesión y comprobar que no se puede abrir la consulta. Con Cliente o
   Administrador debe denegarse el acceso.

## Comprobar los treinta días sin esperar ese plazo

Desde una terminal PowerShell ubicada en la raíz de VitaPet Rework, ejecutar el
binario de pruebas ya compilado en la carpeta aislada:

```powershell
& '.\artifacts\sin-migraciones-build\VitaPet.Facturacion.Tests.exe' --bitacora
```

La prueba crea una base temporal con 65 eventos sintéticos: cinco de hace más de
treinta días y sesenta recientes. Comprueba las bajas, el histórico, los filtros,
la integridad, los fallos del XML/SQL y la restauración de respaldos anteriores y
actuales. Al finalizar elimina sus bases temporales. Debe terminar con
`TOTAL: 42 verificaciones de tratamiento de bitácora`.

Para recompilar las pruebas sin actualizar las DLL del sitio que esté abierto:

```powershell
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
$salida = Join-Path (Get-Location) 'artifacts\sin-migraciones-build\'
& $msbuild 'VitaPet Rework.slnx' /t:Build /v:minimal /nologo /m:1 /nr:false "/p:OutDir=$salida"
& $msbuild 'Tests\VitaPet.Facturacion.Tests\VitaPet.Facturacion.Tests.csproj' /t:Build /v:minimal /nologo /m:1 /nr:false /p:BuildProjectReferences=false "/p:OutDir=$salida"
```

## Almacenamiento e integridad

- SQL conserva activos y archivados; la baja no ejecuta un `DELETE`.
- Antes de cambiar el estado, se valida el histórico, se guarda el XML de forma
  atómica y se vuelve a leer para comprobar sus DVH.
- La marca verdadera se incorpora al DVH y a las referencias. Se actualiza el
  DVV sobre todas las filas SQL dentro de la transacción de integridad.
- Un XML alterado o inaccesible impide la baja. Si SQL falla después de escribir
  el archivo, se revierte la transacción y el reintento no duplica la consulta.
- La consulta reúne SQL y XML por identificador y contenido original. También
  admite archivos históricos previos a la incorporación de la marca.
- Los backups SQL conservan las bajas y los respaldos XML de contingencia
  incluyen el histórico. Se aceptan respaldos del esquema actual; los anteriores
  a la incorporación de la baja lógica se rechazan sin convertirlos.
- Si SQL está corrupto, la consulta restringida de evidencia de Integridad no
  ejecuta bajas ni modifica la base.

## Verificación técnica

Las pruebas se ejecutan sobre bases temporales. La suite específica aprobó 42
comprobaciones; el recorrido web sobre una base cifrada aprobó 28 y la regresión
web de facturación, 18. También se aprobaron 59 comprobaciones de regresión
general y 203 de cobertura de integridad. La documentación Word se actualizó en el diccionario de Bitacora,
la política 12.8.6, los verificadores de 12.9.2 y los procedimientos de 12.9.3.
Prueba manual y funcionamiento confirmados por el usuario el 24/10/2026.
