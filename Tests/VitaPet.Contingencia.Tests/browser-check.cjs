const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'), path=require('path'), {execFileSync}=require('child_process');
(async()=>{
 const env=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Contingencia_[a-f0-9]{32}$/.test(env.Database))throw Error('Isolated fixture required');
 const out=path.dirname(process.argv[2]); let checks=0;
 const check=(ok,label)=>{if(!ok)throw Error(label);checks++;console.log('PASS '+label)};
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const context=await browser.newContext({viewport:{width:1280,height:900}});const page=await context.newPage();
 const c=id=>page.locator('[id$="'+id+'"]');
 const click=async id=>{await Promise.all([page.waitForNavigation(),c(id).click()]);};
 try{
  await page.goto(env.Url+'RecuperacionWebMaster.aspx');
  check(page.url().includes('Login'),'Unconfigured recovery redirects to login');
  check(await c('lnkContingencia').count()===0,'Normal login has no recovery link');
  await page.goto(env.Url+'Login.aspx');await c('txtCorreo').fill('webmaster@vitapet.local');await c('txtContrasena').fill('WebMaster2026!Prueba');await click('btnIngresar');
  check(page.url().includes('Home'),'Normal webmaster login preserved');
  await page.goto(env.Url+'BaseDatos.aspx');
  await c('txtCorreoEmergencia').fill('emergencia@test.local');await c('txtClaveEmergencia').fill('weak');await click('btnConfigurarEmergencia');
  check((await c('pnlMensaje').innerText()).includes('12 a 100'),'Weak emergency credential rejected');
  await c('txtClaveEmergencia').fill('Contingencia2026!Prueba');await click('btnConfigurarEmergencia');
  check(page.url().includes('Login'),'Provision creates backup and closes session');
  await page.goto(env.Url+'RecuperacionWebMaster.aspx');check(page.url().includes('Login'),'Healthy system blocks direct recovery URL');
  const denied=await context.request.post(env.Url+'RecuperacionWebMaster',{form:{'__EVENTTARGET':'btnIngresar'},maxRedirects:0});
  check(denied.status()===302 && denied.headers().location.includes('Login'),'Healthy system blocks direct recovery POST');
  execFileSync('C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/SQLCMD.EXE', ['-S','localhost','-E','-b','-d',env.Database,'-Q',"UPDATE dbo.Usuario SET nombre='WebMaster alterado' WHERE rol='Webmaster'"]);
  fs.writeFileSync(path.join(env.Runtime,'recuperacion','mantenimiento.txt'),'web-fixture');
  await page.goto(env.Url+'Login.aspx');
  check(page.url().includes('RecuperacionWebMaster'),'Login automatically starts recovery when needed');
  await c('txtCorreo').fill('emergencia@test.local');await c('txtClave').fill('bad');await click('btnIngresar');
  check((await c('pnlMensaje').innerText()).includes('No se pudo autorizar'),'Wrong recovery credential rejected in UI');
  await c('txtClave').fill('Contingencia2026!Prueba');await click('btnIngresar');
  check(await c('pnlRestaurar').isVisible() && (await c('ddlRespaldos').locator('option').count())>0,'Recovery displays verified snapshots');
  for(const route of ['Home','GestionUsuarios','BaseDatos','ConsultaBitacora','ModificarUsuario','Contact','OlvideContrasena','Login']){
   let response=await context.request.get(env.Url+route);check([403,503].includes(response.status()),'Emergency GET denied: '+route);
   response=await context.request.post(env.Url+route,{form:{'__EVENTTARGET':'btnConfirmar'}});check([403,503].includes(response.status()),'Emergency POST denied: '+route);
  }
  await page.screenshot({path:path.join(out,'recuperacion.png'),fullPage:true});
  await click('btnSeleccionar');check(await c('pnlConfirmar').isVisible(),'Restore requires confirmation');
  await click('btnCancelar');check(!await c('pnlConfirmar').isVisible(),'Cancel consumes confirmation');
  await click('btnSeleccionar');await page.screenshot({path:path.join(out,'confirmacion.png'),fullPage:true});await click('btnConfirmar');
  check(page.url().includes('Login') && page.url().includes('restaurado=1'),'Successful restore ends emergency session');
  await page.goto(env.Url+'BaseDatos.aspx');check(page.url().includes('Login') || (await page.content()).includes('403'),'Old emergency session cannot access administrator screen');
  await page.goto(env.Url+'Login.aspx');await c('txtCorreo').fill('webmaster@vitapet.local');await c('txtContrasena').fill('WebMaster2026!Prueba');await click('btnIngresar');
  check(page.url().includes('Home'),'Original webmaster can log in after XML restore');
  await page.goto(env.Url+'RecuperacionWebMaster.aspx');check(page.url().includes('Login'),'Recovery URL closes after restoration');
  check(await c('lnkContingencia').count()===0,'Restored login has no recovery link');
  await page.goto(env.Url+'Logout.aspx');
  check(await c('btnConfirmarLogout').isVisible(),'Logout displays confirmation in the Web Application');
  await click('btnConfirmarLogout');check(page.url().includes('Login'),'Confirmed logout returns to login');
  await page.goto(env.Url+'Home.aspx');check(page.url().includes('Login'),'Closed session cannot reopen protected Home');
  const audit=execFileSync('C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/SQLCMD.EXE', ['-S','localhost','-E','-b','-d',env.Database,'-h','-1','-W','-Q',"SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.Bitacora WHERE accion='Logout'"],{encoding:'utf8'});
  check(Number(audit.trim())===1,'Logout audit recorded exactly once');
  console.log('TOTAL UI: '+checks);
 }catch(error){fs.writeFileSync(path.join(out,'failure.html'),await page.content());await page.screenshot({path:path.join(out,'failure.png'),fullPage:true});throw error;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
