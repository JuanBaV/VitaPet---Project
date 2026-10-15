const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),cp=require('child_process');
(async()=>{
 const env=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Cobertura_[a-f0-9]{32}$/.test(env.Database))throw Error('Only isolated coverage databases');
 const out=path.dirname(process.argv[2]);let checks=0;
 const check=(ok,msg)=>{if(!ok)throw Error(msg);checks++;console.log('PASS '+msg);};
 const sqlcmd='C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/SQLCMD.EXE';
 const sql=q=>cp.execFileSync(sqlcmd,['-S','localhost','-E','-d',env.Database,'-b','-Q',q],{encoding:'utf8'});
 const migration=path.resolve(__dirname,'../../Database/Migrations/018_ConsultaCobertura.sql');
 const resetProcedures=()=>cp.execFileSync(sqlcmd,['-S','localhost','-E','-d',env.Database,'-b','-i',migration],{encoding:'utf8'});
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const context=await browser.newContext({viewport:{width:1440,height:1100}}),page=await context.newPage();
 const c=id=>page.locator('[id$="_'+id+'"]');
 const click=async id=>Promise.all([page.waitForNavigation(),c(id).click()]);
 const login=async email=>{await page.goto(env.Url+'Login.aspx');await c('txtCorreo').fill(email);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');};
 const soap=()=>context.request.post(env.Url+'WebServices/CoberturaService.asmx',{
  headers:{'Content-Type':'text/xml; charset=utf-8',SOAPAction:'"https://vitapet.local/services/ObtenerCoberturaCuenta"'},
  data:'<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><ObtenerCoberturaCuenta xmlns="https://vitapet.local/services/" /></soap:Body></soap:Envelope>'
 });
 try{
  await page.goto(env.Url+'ConsultaCobertura.aspx');check(/\/Login(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Pantalla requiere sesion');
  check(!(await soap()).ok(),'SOAP de cuenta rechaza consulta anonima');
  const wsdl=await context.request.get(env.Url+'WebServices/CoberturaService.asmx?WSDL');
  const definition=await wsdl.text();check(wsdl.ok()&&definition.includes('ObtenerCoberturaCuenta')&&definition.includes('ObtenerCobertura'),'WSDL publica operacion de cuenta y conserva operacion del catalogo');
  await login('cliente@vitapet.local');await page.goto(env.Url+'Planes.aspx');check(await c('lnkConsultarCobertura').count()===0,'Planes conserva el acceso exclusivo a contratacion');await click('lnkPerfilCliente');check(/\/Cliente(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Mi perfil abre la pantalla de la cuenta');await click('lnkConsultarCobertura');
  check(/\/ConsultaCobertura(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Acceso desde Mi perfil a consulta de cobertura');
  check((await c('lblNombrePlan').innerText())==='Plan Esencial'&&(await c('lblEstadoSuscripcion').innerText())==='Activa','Plan y estado de la cuenta autenticada');
  check(/^\d{2}\/\d{2}\/\d{4}$/.test(await c('lblFechaAlta').innerText())&&/^\d{2}\/\d{2}\/\d{4}$/.test(await c('lblFechaVencimiento').innerText()),'Fechas de alta y vencimiento');
  const grid=await c('gvCoberturas').innerText();
  check(grid.includes('Consulta veterinaria')&&grid.includes('Consulta clínica general.')&&!grid.includes('Prestación inactiva'),'Solo prestaciones activas con detalles');
  check(await page.locator('main input:not([type=hidden]),main select,main textarea').count()===0,'Controles de consulta de solo lectura y sin selector de mascota');
  await page.screenshot({path:path.join(out,'cobertura-es.png'),fullPage:true});
  await click('lnkVolver');check(/\/Cliente(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Volver regresa a Mi perfil');await click('lnkConsultarCobertura');
  let calls=0;page.on('request',r=>{if(r.url().includes('CoberturaService.asmx'))calls++;});
  await page.reload();check(calls===0,'La GUI consume el ASMX mediante C# del servidor');
  let response=await soap(),xml=await response.text();check(response.ok()&&xml.includes('<NombrePlan>Plan Esencial</NombrePlan>')&&xml.includes('Consulta veterinaria'),'SOAP devuelve la cobertura de la cuenta propia');
  await page.goto(env.Url+'ConsultaCobertura.aspx?idUsuario=4&idCliente=2&idSuscripcion=2&idPlan=2');check((await c('lblNombrePlan').innerText())==='Plan Esencial','Ignora identificadores ajenos inyectados en la URL');
  const nojs=await browser.newContext({javaScriptEnabled:false,storageState:await context.storageState()});
  try{const plain=await nojs.newPage();await plain.goto(env.Url+'ConsultaCobertura.aspx');check((await plain.locator('[id$="_gvCoberturas"]').innerText()).includes('Consulta veterinaria'),'Consulta y datos disponibles sin JavaScript');}finally{await nojs.close();}
  await page.locator('#btnSelectorIdioma').click();await click('btnIdiomaEn');
  check((await page.locator('main').innerText()).includes('Coverage details')&&(await c('lblEstadoSuscripcion').innerText())==='Active'&&(await c('gvCoberturas').innerText()).includes('Benefit'),'Pantalla, estado y encabezados traducidos al ingles');
  await page.screenshot({path:path.join(out,'cobertura-en.png'),fullPage:true});
  await page.setViewportSize({width:390,height:844});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Sin desbordamiento horizontal en movil');
  await page.screenshot({path:path.join(out,'cobertura-movil.png'),fullPage:true});await page.setViewportSize({width:1440,height:1100});
  sql("CREATE OR ALTER PROCEDURE dbo.sp_Cobertura_ConsultarCuenta @idUsuario INT,@idSuscripcion INT AS THROW 50058,'Test service failure',1;");
  try{await page.goto(env.Url+'ConsultaCobertura.aspx');check((await c('pnlMensaje').innerText()).includes('ER058')&&(await c('pnlMensaje').innerText()).includes('coverage service'),'Falla del servicio muestra ER058 traducido');check(await c('gvCoberturas').count()===0&&await c('lblNombrePlan').count()===0,'No muestra datos anteriores tras fallar el servicio');await page.screenshot({path:path.join(out,'cobertura-er058.png'),fullPage:true});}finally{resetProcedures();}
  sql("CREATE TRIGGER dbo.TestCoverageWebAudit ON dbo.Bitacora AFTER INSERT AS THROW 50033,'Test audit failure',1;");
  try{await page.goto(env.Url+'ConsultaCobertura.aspx');check((await c('pnlMensaje').innerText()).includes('ER033')&&await c('gvCoberturas').count()===0,'Falla de auditoria muestra ER033 sin confirmar la consulta');}finally{sql('DROP TRIGGER dbo.TestCoverageWebAudit');}
  sql("CREATE OR ALTER PROCEDURE dbo.sp_Suscripcion_ObtenerVigenteCliente @idUsuario INT AS THROW 50008,'Test no coverage',1;");
  try{await page.goto(env.Url+'ConsultaCobertura.aspx');check((await c('pnlMensaje').innerText()).includes('ER008')&&await c('gvCoberturas').count()===0,'Ausencia de cobertura muestra ER008 y oculta la grilla');}finally{resetProcedures();}
  const maintenance=path.join(env.Runtime,'recuperacion','mantenimiento.txt');fs.writeFileSync(maintenance,'Prueba aislada');
  try{check(!(await soap()).ok(),'SOAP respeta mantenimiento');}finally{fs.unlinkSync(maintenance);}
  await context.clearCookies();await login('otro@test.local');await page.goto(env.Url+'ConsultaCobertura.aspx');
  check((await c('lblNombrePlan').innerText())==='Plan Preventivo'&&!(await c('gvCoberturas').innerText()).includes('Consulta veterinaria'),'Segundo cliente solo visualiza su propio plan y prestaciones');
  await context.clearCookies();await login('sinperfil@test.local');await page.goto(env.Url+'ConsultaCobertura.aspx');
  check((await c('pnlMensaje').innerText()).includes('ER011')&&await c('gvCoberturas').count()===0,'Cuenta sin perfil de cliente muestra ER011');
  fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify({checks,status:'passed'},null,2));console.log('TOTAL: '+checks+' browser checks passed');
 }catch(e){fs.writeFileSync(path.join(out,'failure.html'),await page.content());console.error((await page.locator('body').innerText()).slice(0,7000));await page.screenshot({path:path.join(out,'failure.png'),fullPage:true});throw e;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
