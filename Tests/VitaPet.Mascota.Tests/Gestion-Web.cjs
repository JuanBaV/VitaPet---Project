const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),cp=require('child_process');
(async()=>{
 const env=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Mascota_[a-f0-9]{32}$/.test(env.Database))throw Error('Only isolated databases');
 const out=path.dirname(process.argv[2]);let checks=0;
 const check=(ok,text)=>{if(!ok)throw Error(text);console.log('PASS '+text);checks++;};
 const sqlcmd='C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/SQLCMD.EXE';
 const sql=q=>cp.execFileSync(sqlcmd,['-S','localhost','-E','-d',env.Database,'-b','-h','-1','-Q',(/^CREATE /i.test(q)?q:'SET NOCOUNT ON; '+q)],{encoding:'utf8'}).trim();
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const context=await browser.newContext({viewport:{width:1440,height:1100}}),page=await context.newPage();
 const c=(id,p=page)=>p.locator('[id$="_'+id+'"]');
 const click=(id,p=page)=>Promise.all([p.waitForNavigation(),c(id,p).click()]);
 const login=async email=>{await page.goto(env.Url+'Login.aspx');await c('txtCorreo').fill(email);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');};
 const select=async()=>{await page.goto(env.Url+'Mascotas.aspx');await Promise.all([page.waitForNavigation(),c('gvMascotas').getByRole('row').filter({hasText:/Luna/}).getByRole('link').click()]);};
 const error=async code=>(await c('pnlMensaje').innerText()).includes(code);
 try {
  for(const route of ['Mascotas.aspx','ActualizarMascota.aspx?id=1','ConsultaMascota.aspx?id=1','HistorialClinico.aspx?id=1','DatosClinicos.aspx?id=1'])
  {await page.goto(env.Url+route);check(/\/Login(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Sesion requerida: '+route);}
  await login('cliente@vitapet.local');await click('lnkMascotas');
  check(await c('gvMascotas').getByRole('row').count()===4&&!(await c('gvMascotas').innerText()).includes('Ajena'),'Lista propias y excluye mascotas ajenas');
  check((await c('gvMascotas').innerText()).includes('Inactiva'),'Lista tambien mascotas inactivas');
  for(const id of ['btnActualizarDatos','btnConsultarDatos','btnHistorialClinico','btnDatosClinicos'])check(await c(id).isDisabled(),'Accion deshabilitada sin seleccion: '+id);
  await select();check(await page.locator('.mascota-seleccionada').count()===1,'Selector conserva la fila seleccionada');
  for(const id of ['btnActualizarDatos','btnConsultarDatos','btnHistorialClinico','btnDatosClinicos'])check(await c(id).isEnabled(),'Accion habilitada con mascota propia: '+id);
  await page.screenshot({path:path.join(out,'mascotas-seleccion.png'),fullPage:true});
  await click('btnConsultarDatos');check((await c('lblNombre').innerText())==='Luna'&&(await c('lblEstado').innerText())==='Activa'&&await page.locator('main input,main select').count()===0,'Consulta de mascota en solo lectura');
  await page.screenshot({path:path.join(out,'mascota-consulta.png'),fullPage:true});
  await select();await click('btnActualizarDatos');check((await c('txtNombre').inputValue())==='Luna'&&(await c('dtpFechaNacimiento').inputValue())==='2020-01-01','Actualizacion precargada con DateTimePicker propio');
  await c('txtNombre').fill('');await click('btnGuardarCambios');check(await error('ER002'),'Validacion de obligatorios en servidor');
  await c('txtNombre').fill('Luna');await c('dtpFechaNacimiento').fill('2099-01-01');await click('btnGuardarCambios');check(await error('ER025'),'Rechaza nacimiento futuro');
  await c('dtpFechaNacimiento').fill('2020-01-01');await c('txtNombre').evaluate(e=>e.removeAttribute('maxlength'));await c('txtNombre').fill('n'.repeat(61));await click('btnGuardarCambios');check(await error('ER025'),'Rechaza exceso de longitud aunque se altere el HTML');
  await c('txtNombre').fill('  Luna editada  ');await page.screenshot({path:path.join(out,'mascota-actualizar.png'),fullPage:true});
  const audit=sql("SELECT COUNT(*) FROM dbo.Bitacora WHERE accion='Actualización de datos de mascota';");
  await click('btnGuardarCambios');check(await c('lblConfirmacion').isVisible()&&sql("SELECT nombre FROM dbo.Mascota WHERE idMascota=1;")==='Luna editada','Guarda datos normalizados y presenta confirmacion');
  check(await c('dtpFechaNacimiento').isDisabled()&&await c('btnGuardarCambios').isDisabled(),'Formulario confirmado queda deshabilitado');
  await page.screenshot({path:path.join(out,'mascota-actualizada.png'),fullPage:true});await page.reload();
  check(Number(sql("SELECT COUNT(*) FROM dbo.Bitacora WHERE accion='Actualización de datos de mascota';"))===Number(audit)+1,'Recargar no repite la actualizacion');
  const otra=await context.newPage();await otra.goto(env.Url+'ActualizarMascota.aspx?id=1');await page.goto(env.Url+'ActualizarMascota.aspx?id=1');
  await c('txtNombre').fill('Luna nueva');await click('btnGuardarCambios');await c('txtNombre',otra).fill('Luna vieja');await click('btnGuardarCambios',otra);
  check((await c('pnlMensaje',otra).innerText()).includes('ER064')&&sql('SELECT nombre FROM dbo.Mascota WHERE idMascota=1;')==='Luna nueva','Dos pestanas no sobrescriben cambios concurrentes');await otra.close();
  await select();await click('btnHistorialClinico');check((await c('gvHistorialClinico').innerText()).includes('Control general')&&!(await c('gvHistorialClinico').innerText()).includes('Consulta ajena'),'Historial clinico propio mediante webservice');
  await page.screenshot({path:path.join(out,'mascota-historial.png'),fullPage:true});
  await select();await click('btnDatosClinicos');check((await c('gvDatosClinicos').innerText()).includes('12,50')&&(await c('gvDatosClinicos').innerText()).includes('38,40')&&(await c('gvDatosClinicos').innerText()).includes('90'),'Datos clinicos con peso, temperatura y frecuencia');
  check(await page.locator('main input,main select').count()===0,'Datos clinicos en solo lectura');await page.screenshot({path:path.join(out,'mascota-datos-clinicos.png'),fullPage:true});
  await page.goto(env.Url+'HistorialClinico.aspx?id=2');check(await error('ER011')&&await c('pnlDatos').count()===0,'Historial inexistente muestra ER011 sin datos anteriores');
  await page.goto(env.Url+'DatosClinicos.aspx?id=2');check(await error('ER011')&&await c('pnlDatos').count()===0,'Mediciones inexistentes muestran ER011');
  for(const route of ['ActualizarMascota.aspx','ConsultaMascota.aspx','HistorialClinico.aspx','DatosClinicos.aspx'])
  {await page.goto(env.Url+route+'?id=4');check(await error('ER011')&&!(await page.locator('main').innerText()).includes('Ajena'),'URL no permite acceder a mascota ajena: '+route);}
  await page.goto(env.Url+'ConsultaMascota.aspx');check(await error('ER011'),'Sin identificador de mascota muestra ER011');
  sql("CREATE TRIGGER dbo.TestGestionWebAudit ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");
  try {
   await page.goto(env.Url+'HistorialClinico.aspx?id=1');check(await error('ER033'),'Historial propaga falla de bitacora');
   await page.goto(env.Url+'ActualizarMascota.aspx?id=1');await c('txtNombre').fill('No guardar');await click('btnGuardarCambios');
   check(await error('ER040')&&sql('SELECT nombre FROM dbo.Mascota WHERE idMascota=1;')==='Luna nueva','Actualizacion revierte cambios si falla bitacora');
  }finally{sql('DROP TRIGGER dbo.TestGestionWebAudit;');}
  const endpoint=env.Url+'WebServices/'+encodeURIComponent('HistorialClínicoService.asmx');
  const wsdl=await(await context.request.get(endpoint+'?WSDL')).text();fs.writeFileSync(path.join(out,'historial.wsdl'),wsdl);
  check(wsdl.includes('ObtenerHistorialClínico')&&wsdl.includes('ObtenerDatosClínicos'),'Endpoint ASMX expone las dos operaciones documentadas');
  const soap=async id=>await context.request.post(endpoint,{headers:{'Content-Type':'text/xml; charset=utf-8','SOAPAction':'"https://vitapet.local/services/ObtenerDatosClínicos"'},data:`<?xml version="1.0"?><soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><ObtenerDatosClínicos xmlns="https://vitapet.local/services/"><idMascota>${id}</idMascota></ObtenerDatosClínicos></soap:Body></soap:Envelope>`});
  let response=await soap(1),body=await response.text();fs.writeFileSync(path.join(out,'soap-propio.xml'),body);check(response.status()===200&&body.includes('<Peso>12.50</Peso>'),'Consumo SOAP autenticado devuelve mediciones');
  body=await(await soap(4)).text();check(body.includes('ER011')&&!body.includes('Dato ajeno'),'SOAP tampoco expone mascotas de otra cuenta');
  await page.goto(env.Url+'Mascotas.aspx');await page.locator('#btnSelectorIdioma').click();await click('btnIdiomaEn');
  check((await c('btnActualizarDatos').inputValue())==='Update details'&&(await c('gvMascotas').innerText()).includes('Inactive'),'Controles y estados traducidos al ingles');
  await page.goto(env.Url+'DatosClinicos.aspx?id=2');check(await error('ER011')&&!(await c('pnlMensaje').innerText()).includes('No existen'),'Errores clinicos traducidos al ingles');
  await page.goto(env.Url+'Mascotas.aspx');await page.setViewportSize({width:390,height:1000});await page.screenshot({path:path.join(out,'mascotas-movil.png'),fullPage:true});
  check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Gestion sin desbordamiento horizontal en movil');await page.setViewportSize({width:1440,height:1100});
  await login('sinmascotas@test.local');await page.goto(env.Url+'Mascotas.aspx');check(await error('ER010')&&await c('btnConsultarDatos').isDisabled(),'Cliente sin mascotas muestra ER010 y acciones deshabilitadas');
  await login('otro@test.local');await page.goto(env.Url+'Mascotas.aspx');check((await c('gvMascotas').innerText()).includes('Ajena')&&!(await c('gvMascotas').innerText()).includes('Luna nueva'),'Segunda cuenta lista exclusivamente sus mascotas');
  fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify({checks,passed:true},null,2));console.log('TOTAL: '+checks+' browser checks passed');
 }catch(e){await page.screenshot({path:path.join(out,'failure.png'),fullPage:true}).catch(()=>{});throw e;}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
