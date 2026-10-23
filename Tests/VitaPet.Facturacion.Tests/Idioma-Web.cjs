const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),{execFileSync}=require('child_process');
(async()=>{
 const config=process.argv[2],env=JSON.parse(fs.readFileSync(config,'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Se requiere una base aislada');
 execFileSync(process.execPath,[path.join(__dirname,'Web.cjs'),config],{stdio:'inherit'});
 const out=path.dirname(config),browser=await chromium.launch({headless:true,channel:'msedge'});
 const ctx=await browser.newContext({viewport:{width:1440,height:1000}}),page=await ctx.newPage();
 const results=[],failures=[],errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 const check=(ok,text,detail)=>{results.push({ok,text,detail});console.log((ok?'PASS ':'FAIL ')+text+(ok?'':' '+JSON.stringify(detail)));if(!ok)failures.push({text,detail});};
 const c=id=>page.locator('[id$="_'+id+'"]');
 const click=async locator=>{await Promise.all([page.waitForNavigation(),(typeof locator==='string'?c(locator):locator).click()]);};
 const go=async url=>{const r=await page.goto(env.Url+url);check(r&&r.status()===200,url+': HTTP 200');};
 const language=async(code,publico=false)=>{await page.locator(publico?'#btnSelectorIdiomaPublico':'#btnSelectorIdioma').click();await click('btnIdioma'+(publico?'Publico':'')+(code==='en'?'En':'Es'));};
 const login=async correo=>{
  await ctx.clearCookies();await go('Login');await c('txtCorreo').fill(correo+'@vitapet.local');await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');
  check(new URL(page.url()).pathname.toLowerCase().endsWith('/home'),correo+': inicio de sesión');
 };
 const raw=code=>fs.readFileSync(path.join(__dirname,'../../VitaPet Rework/Resources/Textos'+(code==='en'?'.en':'')+'.resx'),'utf8');
 await go('Home');
 const resources=await page.evaluate(([es,en])=>[es,en].map(xml=>Object.fromEntries([...new DOMParser().parseFromString(xml,'text/xml').querySelectorAll('data')].map(d=>[d.getAttribute('name'),d.querySelector('value').textContent]))),[raw('es'),raw('en')]);
 const [es,en]=resources;
 check(JSON.stringify(Object.keys(es).sort())===JSON.stringify(Object.keys(en).sort()),'Ambos idiomas cubren las mismas claves de recursos');
 check(Object.values(en).every(v=>v.trim()),'Los recursos en inglés no están vacíos');
 const source=path.join(__dirname,'../../VitaPet Rework'),missing=[];
 function audit(dir){for(const f of fs.readdirSync(dir,{withFileTypes:true})){
  if(f.isDirectory()){if(!['bin','obj','App_Data','Scripts','Content'].includes(f.name))audit(path.join(dir,f.name));continue;}
  if(!/\.(aspx|ascx|master|cs)$/i.test(f.name)||f.name==='Textos.Designer.cs')continue;
  const text=fs.readFileSync(path.join(dir,f.name),'utf8');
  for(const pattern of [/Resources:Textos,\s*(\w+)/g,/TextosUI\.(?:Obtener|Formato)\("(\w+)"/g])for(const m of text.matchAll(pattern))if(!es[m[1]]||!en[m[1]])missing.push(f.name+':'+m[1]);
 }}
 audit(source);check(!missing.length,'Los recursos utilizados por páginas y controles existen en ambos idiomas',missing);
 const forbidden=Object.fromEntries(Object.keys(es).filter(k=>es[k]!==en[k]&&es[k].length>=3).map(k=>[es[k],en[k]]));
 async function translated(label){
  const content=await page.evaluate(()=>{
   const values=[document.title],walker=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT);
   let node;while(node=walker.nextNode())if(node.parentElement.getClientRects().length&&!node.parentElement.closest('script,style'))values.push(node.textContent.trim());
   for(const e of document.querySelectorAll('input,button,img,[aria-label]'))if(e.getClientRects().length)for(const a of ['placeholder','title','alt','aria-label'])if(e.getAttribute(a))values.push(e.getAttribute(a));
   for(const e of document.querySelectorAll('input[type=submit],input[type=button]'))if(e.getClientRects().length)values.push(e.value);
   return {lang:document.documentElement.lang,values};
  });
  // Nombres editables de la preparación: coinciden con recursos, pero son datos del usuario.
  const originalNames=/ (Mascotas|Turnos)$/.test(label)?['Inactiva']:/ GestionPlanes$/.test(label)?['Plan Corporativo']:[];
  const untranslated=[...new Set(content.values.filter(t=>Object.hasOwn(forbidden,t)&&!originalNames.includes(t)))];
  check(content.lang==='en',label+': idioma HTML inglés',content.lang);
  check(!untranslated.length,label+': textos, botones y atributos traducidos',untranslated);
 }
 try{
  await language('en',true);
  for(const name of ['Home','Login','RegistroCliente','OlvideContrasena','RestablecerContrasena','CatalogoErrores','AccesoDenegado']){await go(name);await translated('Público '+name);}
  await go('Login');await c('btnIngresar').click();check((await page.locator('body').innerText()).includes(en.UI_DebeIngresarSuContrasena),'Validación cliente del login en inglés');
  await language('es',true);check(await page.locator('html').getAttribute('lang')==='es','El visitante puede regresar a español');
  for(const [rol,pages] of [
   ['cliente',['Home','Cliente','Planes','Mascotas','RegistroMascota','ActualizarMascota?id=1','ConsultaMascota?id=1','HistorialClinico?id=1','DatosClinicos?id=1','Turnos','Reintegros','ConsultaCobertura','GestionPago?contratacion=0','ContratarPlan?plan=1','Logout']],
   ['administrador',['Home','Empresas','AltaEmpresa','ConsultaEmpresa?id=0','ModificarEmpresa?id=0','RegistroClienteCorporativo','GestionPlanes','AltaPlan','ConsultaPlan?id=1','ModificarPlan?id=1','ConsultarPerfiles','Logout']],
   ['webmaster',['Home','GestionUsuarios','ConsultarUsuario?operacion=vencida','ModificarUsuario?operacion=vencida','BloquearUsuario?operacion=vencida','DesbloquearUsuario?operacion=vencida','ConsultaBitacora','BaseDatos','GestionPerfiles','ConsultarPerfiles','AltaUsuario','Logout']]
  ]){
   await login(rol);await language('en');
   for(const name of pages){await go(name);await translated(rol+' '+name);
    if(name==='Mascotas')check(await page.getByRole('cell',{name:'Inactiva',exact:true}).isVisible(),'El nombre original de la mascota se conserva en inglés');
    if(name==='GestionPlanes')check(await page.getByRole('cell',{name:'Plan Corporativo',exact:true}).isVisible(),'El nombre editable del plan se conserva en inglés');
   }
   await go('Home');await page.setViewportSize({width:390,height:844});await language('es');
   check((await page.locator('body').innerText()).includes('Cerrar sesión'),rol+': cambio a español desde móvil');
   await language('en');check((await page.locator('body').innerText()).includes(en.BotonSalir),rol+': cambio a inglés desde móvil');
   check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),rol+': inglés sin desbordamiento móvil');
   await page.screenshot({path:path.join(out,'idioma-'+rol+'-movil.png'),fullPage:true});await page.setViewportSize({width:1440,height:1000});
   await login(rol);check(await page.locator('html').getAttribute('lang')==='en',rol+': idioma guardado tras una nueva sesión');
   await go('Logout');await click('btnConfirmarLogout');check(await c('lnkIniciarSesion').isVisible(),rol+': cierre de sesión efectivo');
  }
  await login('webmaster');
  for(const button of ['btnConsultar','btnModificar','btnBloquear']){
   await go('GestionUsuarios');await c('txtBuscarUsuario').fill('Otro');await click('btnBuscar');
   await click(c('gvUsuarios').locator('tr').filter({hasText:'otro@test.local'}).getByRole('link',{name:'Select',exact:true}));await click(button);
   await translated('Operación de usuario '+button);
   if(button==='btnModificar')check(JSON.stringify(await c('ddlRol').locator('option').allTextContents())===JSON.stringify([en.UI_Cliente,en.UI_Administrador,en.UI_Webmaster]),'Los perfiles del formulario de modificación se muestran en inglés');
  }
  await go('GestionPerfiles');
  check(await page.locator('h1').innerText()==='Profiles and permissions','Gestión de perfiles: título traducido');
  check(await c('btnNuevo').inputValue()==='New profile','Gestión de perfiles: acción traducida');
  await c('txtNombre').fill('');await page.evaluate(()=>{window.Page_ClientValidate=()=>true;window.Page_BlockSubmit=false;});await click('btnGuardar');check((await page.locator('body').innerText()).includes(en.Alta_Obligatorio),'Validación servidor del perfil en inglés');
  await c('txtNombre').fill('Perfil idioma QA');await page.getByRole('checkbox',{name:'Plans',exact:true}).check();await click('btnGuardar');
  check((await c('pnlMensaje').innerText()).includes(en.Perfil_Guardado),'Guardar perfil informa el resultado en inglés');
  await c('txtNombre').fill('Perfil idioma QA');await page.getByRole('checkbox',{name:'Plans',exact:true}).check();await click('btnGuardar');
  check((await c('pnlMensaje').innerText()).includes(en.Perfil_Duplicado),'La validación de nombre duplicado se traduce desde BLL');
  const row=()=>c('gvPerfiles').locator('tr').filter({hasText:'Perfil idioma QA'});
  await click(row().locator('[id*="_btnVerDetalle"]'));await translated('Detalle y árbol de permisos');
  check(await c('txtNombre').inputValue()==='Perfil idioma QA','El nombre definido por el usuario conserva su texto original');
  await click(row().locator('[id*="_btnEliminarPerfil"]'));
  check((await c('pnlConfirmar').innerText()).includes(en.Perfil_ConfirmarFormato.replace('{0}','Perfil idioma QA')),'Confirmación dinámica de eliminación en inglés');
  await page.screenshot({path:path.join(out,'idioma-perfiles-en.png'),fullPage:true});await click('btnCancelar');check(await row().count()===1,'Cancelar la eliminación conserva el perfil');
  await click(row().locator('[id*="_btnEliminarPerfil"]'));await click('btnConfirmar');check((await c('pnlMensaje').innerText()).includes(en.Perfil_Eliminado),'Eliminar perfil informa el resultado en inglés');
  await go('AltaUsuario');await c('btnCrear').click();check((await page.locator('body').innerText()).includes(en.Alta_Obligatorio),'Validación cliente del alta en inglés');
  await language('es');check(await page.locator('html').getAttribute('lang')==='es','El usuario puede cambiar idioma después de una validación fallida');await language('en');
  await page.screenshot({path:path.join(out,'idioma-alta-usuario-en.png'),fullPage:true});
  await go('ConsultaBitacora');await translated('Bitácora con altas y bajas de perfiles');
  await go('BaseDatos');await click('btnVerificar');check((await page.locator('body').innerText()).includes(en.UI_NoSeDetectaronErroresDeIntegridad),'Los cambios de idioma y perfiles mantienen la integridad de la base cifrada');
  check(fs.readdirSync(path.join(env.Runtime,'smtp-captura')).some(f=>f.endsWith('.eml')),'La factura del recorrido queda en la captura SMTP local de pruebas');
  check(!errors.length,'Sin errores JavaScript durante los cambios de idioma',errors);
 }catch(e){failures.push({text:e.message});fs.writeFileSync(path.join(out,'idioma-failure.html'),await page.content());console.error(await page.evaluate(()=>({blockSubmit:window.Page_BlockSubmit,valid:window.Page_IsValid,validators:(window.Page_Validators||[]).map(v=>({id:v.id,valid:v.isvalid,message:v.errormessage}))})));await page.screenshot({path:path.join(out,'idioma-failure.png'),fullPage:true});throw e;}
 finally{fs.writeFileSync(path.join(out,'idioma-resultados.json'),JSON.stringify({checks:results.length,failures,errors,results},null,2));await browser.close();}
 if(failures.length)throw Error(failures.length+' comprobaciones multidioma fallaron');
 console.log('TOTAL IDIOMA WEB: '+results.length+' comprobaciones');
})().catch(e=>{console.error(e);process.exit(1);});
