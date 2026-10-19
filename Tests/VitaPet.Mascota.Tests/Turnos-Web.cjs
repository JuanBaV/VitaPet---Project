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
 page.setDefaultTimeout(20000);
 const c=(id,p=page)=>p.locator('[id$="_'+id+'"]');
 const click=(id,p=page)=>Promise.all([p.waitForNavigation(),c(id,p).click()]);
 const login=async email=>{await page.goto(env.Url+'Login.aspx');await c('txtCorreo').fill(email);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');};
 const select=async(name='Luna',p=page)=>Promise.all([p.waitForNavigation(),c('gvMascotas',p).getByRole('row').filter({hasText:name}).getByRole('link').click()]);
 const zone=async(value,p=page)=>Promise.all([p.waitForNavigation(),c('ddlZona',p).selectOption(value)]);
 const choose=async(p=page)=>Promise.all([p.waitForNavigation(),c('gvDisponibilidad',p).getByRole('link',{name:'Seleccionar',exact:true}).first().click()]);
 const error=async(code,p=page)=>(await c('pnlMensaje',p).innerText()).includes(code);
 const prepare=async(p=page)=>{await p.goto(env.Url+'Turnos.aspx');await select('Luna',p);await zone('CABA',p);await click('btnBuscarDisponibilidad',p);await choose(p);};
 const soap=(request,method,fields)=>request.post(env.Url+'WebServices/TurnoService.asmx',{headers:{'Content-Type':'text/xml; charset=utf-8',SOAPAction:'"https://vitapet.local/services/'+method+'"'},data:'<?xml version="1.0" encoding="utf-8"?><soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><'+method+' xmlns="https://vitapet.local/services/">'+fields+'</'+method+'></soap:Body></soap:Envelope>'});
 try{
  for(const route of ['Turnos.aspx','Turnos.aspx?vista=consulta']){await page.goto(env.Url+route);check(/\/Login(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Turnos requiere sesion: '+route);}
  await login('cliente@vitapet.local');await click('lnkTurnos');
  check(await page.locator('h1').innerText()==='Solicitar turno','Acceso desde el menu Turnos');
  check(await c('ddlZona').isDisabled()&&await c('btnBuscarDisponibilidad').isDisabled()&&await c('btnConfirmarSolicitud').isDisabled(),'Zona, busqueda y confirmacion deshabilitadas inicialmente');
  check((await c('gvMascotas').innerText()).includes('Luna')&&!(await c('gvMascotas').innerText()).includes('Ajena'),'Selector propio sin mascotas de otros clientes');
  await select();check(await c('ddlZona').isEnabled()&&await c('btnBuscarDisponibilidad').isEnabled(),'Seleccionar mascota habilita filtro y busqueda');
  check((await c('ddlZona').locator('option').allTextContents()).join('|')==='Seleccioná una zona|CABA|Norte|Oeste|Sur','FiltroZona carga zonas desde la base');
  await click('btnBuscarDisponibilidad');check(await error('ER002')&&await c('btnConfirmarSolicitud').isDisabled(),'Falta de zona informa ER002');
  await zone('CABA');await click('btnBuscarDisponibilidad');
  check(await c('gvDisponibilidad').getByRole('link',{name:'Seleccionar',exact:true}).count()===12,'Disponibilidad paginada con 12 bloques por pagina');
  check((await c('gvDisponibilidad').innerText()).includes('Vacunación')&&(await c('gvDisponibilidad').innerText()).includes('Consulta general'),'Listado incluye las prestaciones de la agenda');
  await page.screenshot({path:path.join(out,'turnos-disponibilidad.png'),fullPage:true});
  await choose();check(await c('btnConfirmarSolicitud').isEnabled()&&await c('lblTurnoSeleccionado').isVisible()&&Number(sql('SELECT COUNT(*) FROM dbo.Turno'))===0,'Seleccion verifica sin registrar ni reservar');
  await Promise.all([page.waitForNavigation(),c('gvDisponibilidad').getByRole('link',{name:'2',exact:true}).click()]);
  check(await c('btnConfirmarSolicitud').isDisabled()&&await c('lblTurnoSeleccionado').count()===0,'Paginar descarta la seleccion anterior');
  await choose();await zone('Norte');check(await c('gvDisponibilidad').count()===0&&await c('btnConfirmarSolicitud').isDisabled(),'Cambiar zona limpia disponibilidad y confirmacion');
  await click('btnBuscarDisponibilidad');await choose();await select('Sol');
  check(await c('ddlZona').inputValue()===''&&await c('gvDisponibilidad').count()===0&&await c('btnConfirmarSolicitud').isDisabled(),'Cambiar mascota reinicia la busqueda');
  await prepare();const other=await context.newPage();await prepare(other);
  await click('btnConfirmarSolicitud');
  check(await c('lblConfirmacion').isVisible()&&(await c('lblConfirmacion').innerText()).includes('El turno se registró correctamente')&&await c('btnConfirmarSolicitud').isDisabled(),'Registro muestra confirmacion y deshabilita envio repetido');
  check(Number(sql('SELECT COUNT(*) FROM dbo.Turno'))===1&&sql('SELECT estado FROM dbo.Turno')==='Confirmado','Reserva persistida en SQL Server');
  await page.screenshot({path:path.join(out,'turnos-confirmado.png'),fullPage:true});
  await click('btnConfirmarSolicitud',other);check(await error('ER013',other)&&Number(sql('SELECT COUNT(*) FROM dbo.Turno'))===1,'Dos pestanas no pueden confirmar el mismo turno');await other.close();
  await click('lnkConsultar');check(await c('btnExportarXml').isDisabled()&&await c('gvTurnos').count()===0,'Consulta independiente inicia sin seleccion ni resultados');
  await select();check(await c('gvTurnos').getByRole('row').count()===2&&await c('btnExportarXml').isEnabled(),'Seleccionar mascota consulta sus turnos mediante el servicio');
  check(await page.locator('main input[type=text],main textarea').count()===0,'Turnos consultados en modo de solo lectura');
  await page.screenshot({path:path.join(out,'turnos-consulta.png'),fullPage:true});
  const downloadPromise=page.waitForEvent('download');await c('btnExportarXml').click();const download=await downloadPromise;await download.saveAs(path.join(out,'Turnos.xml'));
  const xml=fs.readFileSync(path.join(out,'Turnos.xml'),'utf8');
  check(download.suggestedFilename()==='Turnos.xml'&&xml.startsWith('<?xml')&&xml.includes('<Turnos>')&&xml.includes('<estado>Confirmado</estado>')&&!xml.includes('<html'),'Descarga XML UTF-8 sin HTML adicional');
  check((xml.match(/<Turno>/g)||[]).length===1&&xml.includes('<idTurno>'+sql('SELECT idTurno FROM dbo.Turno')+'</idTurno>'),'XML solo exporta turnos de la mascota consultada');
  await select('Sol');check(await error('ER014')&&await c('gvTurnos').count()===0&&await c('btnExportarXml').isDisabled(),'Mascota sin turnos informa ER014 y limpia resultados anteriores');
  const ws=await soap(context.request,'ObtenerTurnosMascota','<idMascota>4</idMascota>');check((await ws.text()).includes('<CodigoError>ER011</CodigoError>'),'SOAP no revela turnos de una mascota ajena');
  const anonymous=await browser.newContext();const denied=await soap(anonymous.request,'ObtenerTurnosMascota','<idMascota>1</idMascota>');
  check(denied.status()===500&&(await denied.text()).includes('Acceso denegado'),'Webservice rechaza llamadas sin sesion');await anonymous.close();
  await prepare();sql("CREATE TRIGGER dbo.TestUiTurnoFail ON dbo.Turno AFTER INSERT AS THROW 50066,'Test booking failure',1;");
  try{await click('btnConfirmarSolicitud');check(await error('ER066')&&Number(sql('SELECT COUNT(*) FROM dbo.Turno'))===1,'Error de registro informa ER066 y no crea un turno parcial');}finally{sql('DROP TRIGGER dbo.TestUiTurnoFail');}
  await prepare();sql("CREATE TRIGGER dbo.TestUiTurnoAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");
  try{await click('btnConfirmarSolicitud');check(await error('ER040')&&Number(sql('SELECT COUNT(*) FROM dbo.Turno'))===1,'Falla de auditoria revierte turno y muestra ER040');}finally{sql('DROP TRIGGER dbo.TestUiTurnoAuditFail');}
  await page.goto(env.Url+'Turnos.aspx?vista=consulta');sql("CREATE TRIGGER dbo.TestUiConsultaAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");
  try{await select();check(await error('ER033')&&await c('btnExportarXml').isDisabled(),'Falla de auditoria de consulta informa ER033');}finally{sql('DROP TRIGGER dbo.TestUiConsultaAuditFail');}
  await page.goto(env.Url+'Turnos.aspx');await Promise.all([page.waitForNavigation(),c('btnIdiomaEn').evaluate(el=>el.click())]);
  check(await page.locator('h1').innerText()==='Request an appointment','Pantalla de turnos traducida al ingles');
  await login('sinmascotas@test.local');await page.goto(env.Url+'Turnos.aspx');check(await error('ER010')&&await c('btnConfirmarSolicitud').isDisabled(),'Cuenta sin mascotas informa ER010');
  await login('sinperfil@test.local');await page.goto(env.Url+'Turnos.aspx?vista=consulta');check(await error('ER011')&&await c('btnExportarXml').isDisabled(),'Cuenta sin cliente asociado informa ER011');
  await login('otro@test.local');await page.goto(env.Url+'Turnos.aspx?vista=consulta');check(!(await c('gvMascotas').innerText()).includes('Luna')&&(await c('gvMascotas').innerText()).includes('Ajena'),'Segunda cuenta conserva aislamiento por propietario');
  fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify({checks,passed:true},null,2));console.log('TOTAL: '+checks+' browser checks passed');
 }catch(e){await page.screenshot({path:path.join(out,'failure.png'),fullPage:true}).catch(()=>{});throw e;}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
