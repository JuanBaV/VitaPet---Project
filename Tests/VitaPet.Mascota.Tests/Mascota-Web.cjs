const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),cp=require('child_process');
(async()=>{
 const env=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Mascota_[a-f0-9]{32}$/.test(env.Database))throw Error('Only isolated pet databases');
 const out=path.dirname(process.argv[2]);let checks=0;
 const check=(ok,msg)=>{if(!ok)throw Error(msg);checks++;console.log('PASS '+msg);};
 const sqlcmd='C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/SQLCMD.EXE';
 const sql=q=>cp.execFileSync(sqlcmd,['-S','localhost','-E','-d',env.Database,'-b','-h','-1','-Q',(/^CREATE /i.test(q)?q:'SET NOCOUNT ON; '+q)],{encoding:'utf8'}).trim();
 const count=()=>Number(sql('SELECT COUNT(*) FROM dbo.Mascota;'));
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const context=await browser.newContext({viewport:{width:1440,height:1100}}),page=await context.newPage();
 const c=id=>page.locator('[id$="_'+id+'"]');
 const click=async id=>Promise.all([page.waitForNavigation(),c(id).click()]);
 const login=async email=>{await page.goto(env.Url+'Login.aspx');await c('txtCorreo').fill(email);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');};
 const fill=async(name='  Nube  ',date='2020-01-02')=>{await c('txtNombre').fill(name);await c('txtEspecie').fill(' Perro ');await c('txtRaza').fill(' Mestizo ');await c('dtpFechaNacimiento').fill(date);};
 const raw=async date=>{
  const fields=await page.evaluate(()=>Object.fromEntries(new FormData(document.querySelector('form'))));
  fields[await c('dtpFechaNacimiento').getAttribute('name')]=date;fields[await c('btnRegistrar').getAttribute('name')]='Registrar';
  return await (await context.request.post(page.url(),{form:fields})).text();
 };
 try{
  let before=count();await page.goto(env.Url+'RegistroMascota.aspx');check(/\/Login(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Formulario requiere sesion');
  await login('cliente@vitapet.local');await click('lnkMascotas');await click('lnkRegistrarMascota');check(/\/RegistroMascota(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Acceso desde Mascotas a Registrar mascota');
  check(await c('dtpFechaNacimiento').getAttribute('type')==='date'&&await c('dtpFechaNacimiento').getAttribute('max')!==null,'DateTimePicker propio como selector de fecha');
  check(await c('txtNombre').getAttribute('maxlength')==='60'&&await c('txtEspecie').getAttribute('maxlength')==='40'&&await c('txtRaza').getAttribute('maxlength')==='60','Longitudes de los tres campos');
  check(await c('lblConfirmacion').count()===0&&await page.locator('main input[name*=idCliente],main select').count()===0,'Sin confirmacion inicial ni seleccion de propietario');
  await click('btnRegistrar');check((await c('pnlMensaje').innerText()).includes('ER002')&&count()===before,'Datos obligatorios validados en servidor sin alta');
  await fill('  Futura  ','2099-01-01');await click('btnRegistrar');check((await c('pnlMensaje').innerText()).includes('ER025')&&count()===before,'Fecha futura rechazada en servidor');
  check((await c('txtNombre').inputValue())==='  Futura  ','Conserva campos para corregir errores');
  await c('txtNombre').evaluate(el=>el.removeAttribute('maxlength'));await c('txtNombre').fill('x'.repeat(61));await c('dtpFechaNacimiento').fill('2020-01-02');await click('btnRegistrar');check((await c('pnlMensaje').innerText()).includes('ER025')&&count()===before,'Longitud excesiva rechazada aunque se altere HTML');
  await fill();check((await raw('2020-02-31')).includes('ER025')&&count()===before,'Fecha inexistente rechazada en POST directo');
  await page.goto(env.Url+'RegistroMascota.aspx?idCliente=2');await fill("  Ñube O'Brian  ");await page.screenshot({path:path.join(out,'mascota-formulario.png'),fullPage:true});await click('btnRegistrar');
  check(await c('lblConfirmacion').isVisible()&&count()===before+1,'Registro exitoso y confirmacion en la misma interfaz');
  check(sql("SELECT COUNT(*) FROM dbo.Mascota WHERE nombre='Ñube O''Brian' AND especie='Perro' AND raza='Mestizo' AND idCliente=1 AND estado=1;")==='1','Alta propia activa y normalizada pese a idCliente ajeno en URL');
  check(sql("SELECT COUNT(*) FROM dbo.Bitacora WHERE accion='Registro de mascota' AND modulo='Mascotas' AND idUsuario=1;")==='1','Bitacora de registro asociada al cliente');
  const confirmation=await c('lblConfirmacion').boundingBox(),button=await c('btnRegistrar').boundingBox();check(confirmation.y>=button.y+button.height+10&&await c('dtpFechaNacimiento').isDisabled(),'Confirmacion separada del boton y formulario deshabilitado');
  await page.screenshot({path:path.join(out,'mascota-confirmacion.png'),fullPage:true});
  await page.reload();check(count()===before+1,'Recargar no repite el registro');
  await fill('Falla guardado');sql("CREATE TRIGGER dbo.TestMascotaInsertFail ON dbo.Mascota AFTER INSERT AS THROW 50060,'Test insert failure',1;");
  try{await click('btnRegistrar');check((await c('pnlMensaje').innerText()).includes('ER060')&&await c('lblConfirmacion').count()===0&&count()===before+1,'Falla de guardado muestra ER060 sin alta parcial');}finally{sql('DROP TRIGGER dbo.TestMascotaInsertFail;');}
  await fill('Falla bitacora');sql("CREATE TRIGGER dbo.TestMascotaAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");
  try{await click('btnRegistrar');check((await c('pnlMensaje').innerText()).includes('ER040')&&count()===before+1,'Falla de bitacora revierte el alta y muestra ER040');}finally{sql('DROP TRIGGER dbo.TestMascotaAuditFail;');}
  await page.locator('#btnSelectorIdioma').click();await click('btnIdiomaEn');check((await page.locator('h1').innerText())==='Pet registration'&&(await c('lblFechaNacimiento').innerText())==='Date of birth','Formulario traducido al ingles');
  await click('btnRegistrar');check((await c('pnlMensaje').innerText()).includes('ER002')&&!(await c('pnlMensaje').innerText()).includes('Datos incompletos'),'Errores traducidos al ingles');
  await fill('Mobile pet');await page.setViewportSize({width:390,height:1000});await page.screenshot({path:path.join(out,'mascota-movil.png'),fullPage:true});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Sin desbordamiento horizontal en movil');await page.setViewportSize({width:1440,height:1100});
  const nojs=await browser.newContext({javaScriptEnabled:false,storageState:await context.storageState()});try{const p=await nojs.newPage();await p.goto(env.Url+'RegistroMascota.aspx');await p.locator('[id$="_txtNombre"]').fill('Sin JavaScript');await p.locator('[id$="_txtEspecie"]').fill('Gato');await p.locator('[id$="_txtRaza"]').fill('Mestizo');await p.locator('[id$="_dtpFechaNacimiento"]').fill('2021-03-04');await Promise.all([p.waitForNavigation(),p.locator('[id$="_btnRegistrar"]').click()]);check(await p.locator('[id$="_lblConfirmacion"]').isVisible()&&count()===before+2,'Registro completo sin JavaScript');}finally{await nojs.close();}
  await login('otro@test.local');await page.goto(env.Url+'RegistroMascota.aspx');await fill('Otra cuenta');await click('btnRegistrar');check(sql("SELECT COUNT(*) FROM dbo.Mascota WHERE nombre='Otra cuenta' AND idCliente=2;")==='1','Segunda cuenta registra en su propio cliente');
  await login('sinperfil@test.local');await page.goto(env.Url+'RegistroMascota.aspx');await fill('Sin cliente');await click('btnRegistrar');check((await c('pnlMensaje').innerText()).includes('ER011'),'Cuenta sin cliente asociado muestra ER011');
  fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify({checks,passed:true},null,2));console.log('TOTAL: '+checks+' browser checks passed');
 }catch(e){await page.screenshot({path:path.join(out,'failure.png'),fullPage:true}).catch(()=>{});throw e;}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
