const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs');
(async()=>{
const path=require('path');
const output=process.argv[3]||path.dirname(process.argv[2])||'artifacts/usuarios-extends-work';fs.mkdirSync(output,{recursive:true});
const env=JSON.parse(fs.readFileSync(process.argv[2]||path.join(output,'environment.json'),'utf8').replace(/^\uFEFF/,''));
const browser=await chromium.launch({headless:true,channel:'msedge'});let checks=0;
const check=(v,m)=>{if(!v)throw Error(m);checks++;console.log('PASS '+m)};
const context=await browser.newContext({viewport:{width:1365,height:980}});
const page=await context.newPage();
const c=(id,p=page)=>p.locator('[id$="'+id+'"]');
async function click(id,p=page){await Promise.all([p.waitForNavigation(),c(id,p).click()]);await p.waitForLoadState('networkidle');}
async function select(email,p=page){
 await p.goto(env.Url+'GestionUsuarios.aspx');
 await c('txtBuscarUsuario',p).fill('');await c('ddlEstado',p).selectOption('Todos');await click('btnBuscar',p);
 let row=c('gvUsuarios',p).locator('tr').filter({hasText:email});
 for(let next=2;await row.count()===0&&next<=100;next++){
   const link=c('gvUsuarios',p).getByRole('link',{name:String(next),exact:true});
   if(await link.count()===0)break;
   await Promise.all([p.waitForNavigation(),link.click()]);
   row=c('gvUsuarios',p).locator('tr').filter({hasText:email});
 }
 if(await row.count()!==1)throw Error('No se encontró la fila de '+email+' en '+p.url());
 await Promise.all([p.waitForNavigation(),row.getByRole('link',{name:'Seleccionar',exact:true}).click()]);
 await p.waitForLoadState('networkidle');
}
try{
for(const name of ['Consultar','Modificar','Bloquear','Desbloquear']){await page.goto(env.Url+name+'Usuario.aspx');check(page.url().includes('Login'),'Anónimo: '+name);}
await c('txtCorreo').fill('webmaster@vitapet.local');await c('txtContrasena').fill('PruebaVitaPet2026!');await click('btnIngresar');
await select('fixture0@test.local');await click('btnConsultar');
check((await c('lblCorreo').innerText())==='fixture0@test.local','CU005 muestra el usuario seleccionado');
check(await c('txtNombre').count()===0 && await c('btnGuardar').count()===0,'CU005 es de solo lectura');
await page.screenshot({path:path.join(output,'consulta.png'),fullPage:true});await click('btnVolver');
check(page.url().includes('GestionUsuarios'),'CU005 vuelve al listado');
await select('fixture0@test.local');await click('btnModificar');
check(await c('txtNombre').inputValue()==='José Prueba 0','CU006 carga nombre');
check(await c('lblIdUsuario').count()===1&&await c('lblBloqueado').count()===1,'CU006 identificador y bloqueo de solo lectura');
await c('txtNombre').fill('');await click('btnGuardar');check((await c('pnlMensaje').innerText()).includes('ER002'),'CU006 ER002');
await c('txtNombre').fill('Usuario Web');await c('txtCorreo').fill('correo-invalido');await click('btnGuardar');check((await c('pnlMensaje').innerText()).includes('ER025'),'CU006 ER025');
await c('txtCorreo').fill('cliente@vitapet.local');await click('btnGuardar');check((await c('pnlMensaje').innerText()).includes('ER001'),'CU006 ER001');
check(await c('txtNombre').inputValue()==='Usuario Web','Error conserva datos editables');
await c('txtCorreo').fill('web-test@test.local');await c('ddlRol').selectOption('Administrador');
await page.screenshot({path:path.join(output,'modificar.png'),fullPage:true});
await click('btnGuardar');check(page.url().includes('GestionUsuarios')&&(await c('pnlResultado').innerText()).includes('modificó'),'CU006 guarda y confirma en listado');
await select('web-test@test.local');await click('btnModificar');await c('txtNombre').fill('Cancelado');await click('btnCancelar');
await select('web-test@test.local');await click('btnConsultar');check((await c('lblNombre').innerText())==='Usuario Web','Cancelar no guarda');await click('btnVolver');
// Dos formularios abiertos: la segunda escritura debe detectar la versión anterior.
await select('web-test@test.local');await click('btnModificar');const anterior=page.url();
const otra=await page.context().newPage();await select('web-test@test.local',otra);await click('btnModificar',otra);await c('txtNombre',otra).fill('Cambio simultáneo');await click('btnGuardar',otra);
await c('txtNombre').fill('Versión anterior');await click('btnGuardar');check((await c('pnlMensaje').innerText()).includes('ER038'),'CU006 rechaza formulario desactualizado');
check(await c('btnGuardar').isDisabled(),'ER038 impide repetir una escritura desactualizada');
await Promise.all([page.waitForNavigation(),c('lnkRecargar').click()]);check(await c('txtNombre').inputValue()==='Cambio simultáneo','Recargar obtiene versión vigente');await click('btnCancelar');await otra.close();
await select('web-test@test.local');await click('btnBloquear');check((await c('dvUsuario').innerText()).includes('web-test@test.local'),'CU007 confirma usuario correcto');
await page.screenshot({path:path.join(output,'bloquear.png'),fullPage:true});await click('btnCancelar');
await select('web-test@test.local');check(await c('btnBloquear').isVisible(),'Cancelar bloqueo conserva estado');await click('btnBloquear');await click('btnConfirmarBloqueo');
check((await c('pnlResultado').innerText()).includes('bloqueó'),'CU007 confirma y vuelve');
await select('web-test@test.local');check(await c('btnDesbloquear').isVisible()&&await c('btnBloquear').count()===0,'Listado refleja bloqueo');
await click('btnDesbloquear');await page.screenshot({path:path.join(output,'desbloquear.png'),fullPage:true});const consumida=page.url();await click('btnConfirmarDesbloqueo');
check((await c('pnlResultado').innerText()).includes('desbloqueó'),'CU008 confirma y vuelve');
await page.goto(consumida);check((await c('pnlMensaje').innerText()).includes('ER036'),'Ticket consumido no se reutiliza');
await page.goto(env.Url+'ModificarUsuario.aspx?operacion=inventada');check((await c('pnlMensaje').innerText()).includes('ER036'),'Selección manipulada rechazada');
await select('web-test@test.local');await click('btnModificar');await page.setViewportSize({width:390,height:844});
await page.screenshot({path:path.join(output,'mobile.png'),fullPage:true});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'Formulario móvil sin desborde');await click('btnCancelar');
// El componente propio de fechas y la exportación XML se conservan.
await page.goto(env.Url+'ConsultaBitacora.aspx');
check(await c('dtpFechaDesde').getAttribute('type')==='date','DateTimePicker propio renderizado desde la DLL de la GUI');
await c('dtpFechaDesde').fill('2026-10-15');await c('dtpFechaHasta').fill('2026-10-14');await click('btnConsultar');
check((await c('pnlMensaje').innerText()).includes('ER027'),'DateTimePicker envía y valida el rango de fechas');
await c('dtpFechaDesde').fill('');await c('dtpFechaHasta').fill('');await click('btnConsultar');
const downloadPromise=page.waitForEvent('download');await c('btnExportarXml').click();
const download=await downloadPromise;await download.saveAs(path.join(output,'bitacora.xml'));
check(fs.readFileSync(path.join(output,'bitacora.xml'),'utf8').includes('Bitacora'),'Exportación XML conservada en la Web Application');
// Editar correo propio mantiene autorización vigente.
await page.setViewportSize({width:1365,height:980});await select('webmaster@vitapet.local');await click('btnModificar');await c('txtCorreo').fill('webmaster-editado@test.local');await click('btnGuardar');
check(page.url().includes('GestionUsuarios')&&await c('pnlResultado').isVisible(),'Editar correo propio actualiza sesión');
await select('webmaster-editado@test.local');await click('btnModificar');await c('ddlRol').selectOption('Cliente');await click('btnGuardar');
check((await c('pnlMensaje').innerText()).includes('modificó')&&await c('lnkInicio').isVisible(),'Cambio de rol propio confirma y retira acceso de Webmaster');
await page.goto(env.Url+'ModificarUsuario.aspx');check(page.url().includes('AccesoDenegado'),'El rol retirado no conserva acceso');
console.log('TOTAL UI '+checks);
}catch(e){for(const [i,p] of context.pages().entries()){fs.writeFileSync(path.join(output,'failure-'+i+'.html'),await p.content());console.error(p.url(),(await p.locator('body').innerText()).slice(-2500));}throw e;}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
