const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),cp=require('child_process');
(async()=>{
 const env=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Reintegros_[a-f0-9]{32}$/.test(env.Database))throw Error('Only isolated reimbursement databases');
 const out=path.dirname(process.argv[2]);let checks=0;
 const check=(ok,msg)=>{if(!ok)throw Error(msg);checks++;console.log('PASS '+msg);};
 const sqlcmd='C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/SQLCMD.EXE';
 const sql=q=>cp.execFileSync(sqlcmd,['-S','localhost','-E','-d',env.Database,'-b','-Q',q],{encoding:'utf8'});
 const refresh=()=>cp.execFileSync(path.join(__dirname,'bin/Debug/net48/VitaPet.Reintegros.Tests.exe'),['--refresh'],{encoding:'utf8'});
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const context=await browser.newContext({viewport:{width:1440,height:1100}}),page=await context.newPage();
 const c=id=>page.locator('[id$="_'+id+'"]');
 const click=async id=>Promise.all([page.waitForNavigation(),c(id).click()]);
 const row=async n=>Promise.all([page.waitForNavigation(),c('gvReintegros').locator('tr').nth(n+1).locator('[id*="_btnConsultar"]').click()]);
 const login=async email=>{await page.goto(env.Url+'Login.aspx');await c('txtCorreo').fill(email);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');};
 try{
  await page.goto(env.Url+'Reintegros.aspx');check(/\/Login(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Consulta requiere sesion');
  await login('cliente@vitapet.local');await click('lnkReintegros');check(/\/Reintegros(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Acceso desde Reintegros del menu');
  check(await c('gvReintegros').locator('tr').count()===3,'Lista los dos reintegros de la cuenta');
  check(!(await c('gvReintegros').innerText()).includes('Rechazado'),'No muestra reintegros de otra cuenta');
  check(await c('btnExportarXml').isDisabled()&&await c('pnlDetalle').count()===0,'Detalle vacio y exportacion deshabilitada al ingresar');
  await page.screenshot({path:path.join(out,'reintegros-listado.png'),fullPage:true});
  await row(1);check((await c('lblNumeroReintegro').innerText())==='1'&&(await c('lblEstado').innerText())==='Pendiente','Consultar obtiene el detalle seleccionado');
  check((await c('lblFechaSolicitud').innerText())==='10/10/2026 10:30'&&(await c('lblImporte').innerText()).replace(/\D/g,'')==='1250050','Fecha e importe con dos decimales');
  check((await c('lblDetalle').innerText())==='Consulta & medicación <prueba>'&&await c('lblDetalle').locator('prueba').count()===0,'Detalle codificado como texto, sin interpretar HTML');
  check(await page.locator('main input:not([type=hidden]):not([type=submit]), main select, main textarea').count()===0,'Detalle de solo lectura');
  check(await c('btnExportarXml').isEnabled(),'Exportacion habilitada despues de consultar');
  await page.screenshot({path:path.join(out,'reintegros-detalle.png'),fullPage:true});
  let [download]=await Promise.all([page.waitForEvent('download'),c('btnExportarXml').click()]);let file=await download.path();let xml=fs.readFileSync(file,'utf8');
  check(download.suggestedFilename()==='Reintegros.xml'&&xml.includes('<idReintegro>1</idReintegro>'),'Descarga XML del reintegro seleccionado');
  check(xml.includes('encoding="utf-8"')&&xml.includes('<importe>12500.50</importe>')&&xml.includes('medicación &lt;prueba&gt;'),'XML UTF-8 con decimales invariantes y caracteres escapados');
  check(!xml.includes('<html')&&!xml.includes('<!DOCTYPE')&&!xml.includes('<idCliente>')&&!xml.includes('<dvh>'),'Descarga sin HTML agregado ni datos internos');
  sql("UPDATE dbo.Reintegro SET estado='Aprobado' WHERE idReintegro=1;");refresh();
  [download]=await Promise.all([page.waitForEvent('download'),c('btnExportarXml').click()]);xml=fs.readFileSync(await download.path(),'utf8');check(xml.includes('<estado>Aprobado</estado>'),'Exportacion reconsulta el estado vigente');
  await page.goto(env.Url+'Reintegros.aspx?idCliente=2&idReintegro=3');check(!(await c('gvReintegros').innerText()).includes('Rechazado'),'Ignora identificadores ajenos en la URL');
  const nojs=await browser.newContext({javaScriptEnabled:false,storageState:await context.storageState()});try{let p=await nojs.newPage();await p.goto(env.Url+'Reintegros.aspx');check(await p.locator('[id$="_gvReintegros"] tr').count()===3,'Listado disponible sin JavaScript');}finally{await nojs.close();}
  await page.locator('#btnSelectorIdioma').click();await click('btnIdiomaEn');await row(1);check((await page.locator('h1').innerText())==='Reimbursement status'&&(await c('lblEstado').innerText())==='Approved','Interfaz y estado traducidos al ingles');
  await page.setViewportSize({width:390,height:1000});await page.screenshot({path:path.join(out,'reintegros-movil.png'),fullPage:true});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Sin desbordamiento de pagina en movil');await page.setViewportSize({width:1440,height:1100});
  sql("UPDATE dbo.Reintegro SET detalle=CHAR(1) WHERE idReintegro=1;");refresh();await click('btnExportarXml');check((await c('pnlMensaje').innerText()).includes('ER015')&&await c('pnlDetalle').isVisible(),'Falla XML muestra ER015 y conserva detalle');sql("UPDATE dbo.Reintegro SET detalle='Restaurado' WHERE idReintegro=1;");refresh();
  sql("CREATE TRIGGER dbo.TestReintegroAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50033,'Test audit failure',1;");try{await page.goto(env.Url+'Reintegros.aspx');await row(1);check((await c('pnlMensaje').innerText()).includes('ER033')&&await c('pnlDetalle').count()===0&&await c('btnExportarXml').isDisabled(),'Falla de bitacora impide confirmar el detalle');}finally{sql('DROP TRIGGER dbo.TestReintegroAuditFail;');}
  await page.goto(env.Url+'Reintegros.aspx');await row(1);sql('DELETE dbo.Reintegro WHERE idReintegro=1;');refresh();await click('btnExportarXml');check((await c('pnlMensaje').innerText()).includes('ER011')&&await c('btnExportarXml').isDisabled(),'Reintegro eliminado impide exportacion posterior');
  sql("CREATE OR ALTER PROCEDURE dbo.sp_Reintegro_ListarCliente @idUsuario INT AS THROW 50059,'Test query failure',1;");try{await page.goto(env.Url+'Reintegros.aspx');check((await c('pnlMensaje').innerText()).includes('ER059')&&await c('pnlDetalle').count()===0,'Falla de consulta muestra ER059 sin datos anteriores');await page.screenshot({path:path.join(out,'reintegros-er059.png'),fullPage:true});}finally{cp.execFileSync(sqlcmd,['-S','localhost','-E','-d',env.Database,'-b','-i',path.resolve(__dirname,'../../Database/Migrations/019_ConsultaReintegros.sql')]);}
  sql('DELETE dbo.Reintegro WHERE idCliente=1;');refresh();await page.goto(env.Url+'Reintegros.aspx');check((await c('pnlMensaje').innerText()).includes('ER009')&&await c('btnExportarXml').isDisabled(),'Cuenta sin reintegros muestra ER009');
  await login('otro@test.local');await page.goto(env.Url+'Reintegros.aspx');await row(0);check((await c('lblNumeroReintegro').innerText())==='3','Otra cuenta accede solo a su propio reintegro');
  await login('sinperfil@test.local');await page.goto(env.Url+'Reintegros.aspx');check((await c('pnlMensaje').innerText()).includes('ER011'),'Cuenta sin cliente asociado muestra ER011');
  fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify({checks,passed:true},null,2));console.log('TOTAL: '+checks+' browser checks passed');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
