const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),{execFileSync}=require('child_process');

(async()=>{
 const config=process.argv[2],env=JSON.parse(fs.readFileSync(config,'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Se requiere una base aislada');
 // Reutiliza el recorrido de facturación y sus datos, sin duplicar la preparación.
 execFileSync(process.execPath,[path.join(__dirname,'Web.cjs'),config],{stdio:'inherit'});
 const out=path.dirname(config),browser=await chromium.launch({headless:true,channel:'msedge'});
 const context=await browser.newContext({viewport:{width:1440,height:1000}}),page=await context.newPage();
 const errors=[],results=[];page.on('pageerror',e=>errors.push(e.message));
 const check=(ok,text)=>{if(!ok)throw Error(text);results.push(text);console.log('PASS '+text);};
 const c=id=>page.locator('[id$="_'+id+'"], [id*="_'+id+'_"]');
 const go=async url=>{const response=await page.goto(env.Url+url);if(response&&response.status()>=400)throw Error(url+': HTTP '+response.status());};
 const click=async target=>{const locator=typeof target==='string'?c(target):target;await Promise.all([page.waitForNavigation(),locator.click()]);};
 const language=async code=>{await page.locator('#btnSelectorIdioma').click();await click(code==='en'?'btnIdiomaEn':'btnIdiomaEs');};
 const login=async(correo,idioma='es')=>{
  await context.clearCookies();await go('Login.aspx');await c('txtCorreo').fill(correo);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');
  check(!/\/Login(?:\.aspx)?$/i.test(new URL(page.url()).pathname),'Sesión de prueba: '+correo);await language(idioma);
 };
 const back=async(destination,label,keyboard=false)=>{
  const link=page.locator('.formulario-volver');
  check(await link.count()===1&&await link.isVisible(),label+': un control Volver visible');
  const layout=await link.evaluate(a=>{
   const b=a.getBoundingClientRect(),card=a.closest('.login-card,.planes-card,.usuario-operacion,header'),r=card.getBoundingClientRect(),style=getComputedStyle(card);
   const title=card.querySelector('h1').getBoundingClientRect();
   return {text:a.innerText,top:b.bottom<=title.top+1,left:Math.abs(b.left-r.left-parseFloat(style.paddingLeft)-parseFloat(style.borderLeftWidth||0))<4,height:b.height};
  });
  check(/^\s*←\s*(Volver|Back)\s*$/.test(layout.text)&&layout.top&&layout.left&&layout.height>=44,label+': flecha, texto y ubicación superior izquierda');
  await link.focus();check(await link.evaluate(a=>document.activeElement===a&&getComputedStyle(a).outlineStyle!=='none'),label+': foco de teclado visible');
  if(keyboard)await Promise.all([page.waitForNavigation(),page.keyboard.press('Enter')]);else await click(link);
  check(new URL(page.url()).pathname.replace(/\.aspx$/i,'').endsWith('/'+destination.replace(/\.aspx$/i,'')),label+': regresa a '+destination);
 };
 const child=async(url,destination,label,keyboard=false)=>{await go(url);await back(destination,label,keyboard);};
 const userOperation=async(button)=>{
  await go('GestionUsuarios.aspx');await c('txtBuscarUsuario').fill('Otro');await click('btnBuscar');
  await click(page.locator('[id$="_gvUsuarios"] tr').filter({hasText:'otro@test.local'}).getByRole('link',{name:'Seleccionar',exact:true}));
  await click(button);
 };
 let empresaId,planId;
 try{
  await child('RegistroCliente.aspx','Login.aspx','Registro de cliente',true);
  await child('OlvideContrasena.aspx','Login.aspx','Recuperación de contraseña');
  await go('OlvideContrasena.aspx');await c('txtCorreo').fill('cliente@vitapet.local');await c('txtRespuesta').fill('UAI');await click('btnContinuar');
  await back('OlvideContrasena.aspx','Restablecimiento de contraseña');
  await child('CatalogoErrores.aspx','Login.aspx','Catálogo de errores');

  await login('administrador@vitapet.local');
  await child('AltaEmpresa.aspx','Empresas.aspx','Alta de empresa vacía',true);
  await go('AltaEmpresa.aspx');
  for(const [id,value] of Object.entries({txtRazonSocial:'Empresa navegación',txtCuit:'30123456789',txtDireccion:'Calle 123',txtTelefono:'55555555'}))await c(id).fill(value);
  await click('btnRegistrarEmpresa');await back('Empresas.aspx','Confirmación de alta de empresa');
  for(const button of ['btnConsultar','btnModificar']){
   await click(page.locator('[id*="_lnkSeleccionarEmpresa"]').first());await click(button);
   empresaId=new URL(page.url()).searchParams.get('id');
   if(button==='btnModificar')await page.screenshot({path:path.join(out,'modificar-empresa.png'),fullPage:true});
   await back('Empresas.aspx',button==='btnConsultar'?'Consulta de empresa':'Modificar empresa');
  }
  await child('AltaPlan.aspx','GestionPlanes.aspx','Alta de plan vacía');
  for(const button of ['btnConsultarPlan','btnModificarPlan']){
   await click(page.locator('[id*="_lnkSeleccionarPlan"]').first());await click(button);planId=new URL(page.url()).searchParams.get('id');
   await back('GestionPlanes.aspx',button==='btnConsultarPlan'?'Consulta de plan':'Modificar plan');
  }
  await go('RegistroClienteCorporativo.aspx');check(await page.locator('.formulario-volver').count()===0,'Registro corporativo inicial conserva navegación principal');
  await click(page.locator('[id*="_lnkSeleccionarEmpresa"]').first());
  await back('RegistroClienteCorporativo.aspx','Formulario de cliente corporativo');
  check(!new URL(page.url()).search,'Volver de cliente corporativo retorna al selector de empresas');

  await login('webmaster@vitapet.local');
  for(const button of ['btnConsultar','btnModificar','btnBloquear']){
   await userOperation(button);await back('GestionUsuarios.aspx','Operación de usuario '+button,true);
   check(await c('txtBuscarUsuario').inputValue()==='Otro','Regreso conserva filtro de usuarios');
  }
  await userOperation('btnBloquear');page.once('dialog',d=>d.accept());await click('btnConfirmarBloqueo');
  await userOperation('btnDesbloquear');await back('GestionUsuarios.aspx','Desbloquear usuario');
  await userOperation('btnDesbloquear');page.once('dialog',d=>d.accept());await click('btnConfirmarDesbloqueo');
  await child('ModificarUsuario.aspx?operacion=vencida','GestionUsuarios.aspx','Selección de usuario vencida');

  await login('cliente@vitapet.local');
  for(const name of ['RegistroMascota','ActualizarMascota','ConsultaMascota','HistorialClinico','DatosClinicos'])
   await child(name+'.aspx'+(name==='RegistroMascota'?'':'?id=1'),'Mascotas.aspx',name);
  await child('ConsultaCobertura.aspx','Cliente.aspx','Consulta de cobertura');
  await go('Planes.aspx');await click('lnkConsultarFactura');await back('Planes.aspx','Factura desde planes');
  await go('Cliente.aspx');await click('lnkConsultarFactura');await back('Cliente.aspx','Factura desde mi perfil');
  await child('GestionPago.aspx?contratacion=0&origen=https://example.com','Planes.aspx','Origen de factura desconocido usa destino local');

  await login('otro@test.local');await go('Planes.aspx');await click(page.locator('[id*="_lnkSeleccionarPlan"]').first());
  const contratacion=page.url().slice(env.Url.length);await back('Planes.aspx','Detalle para contratar');
  await go(contratacion);await click('btnContratar');const pendiente=page.url().slice(env.Url.length);
  await c('txtNumeroTarjeta').fill('incompleta');await back('ContratarPlan.aspx','Pago pendiente vuelve al detalle sin validar tarjeta');
  check(new URL(page.url()).searchParams.has('plan'),'Regreso de pago conserva el plan seleccionado');

  await page.setViewportSize({width:390,height:844});
  await login('administrador@vitapet.local','en');
  for(const [url,destino] of [['ModificarEmpresa.aspx?id='+empresaId,'Empresas.aspx'],['ConsultaEmpresa.aspx?id='+empresaId,'Empresas.aspx'],['AltaEmpresa.aspx','Empresas.aspx'],['ModificarPlan.aspx?id='+planId,'GestionPlanes.aspx'],['ConsultaPlan.aspx?id='+planId,'GestionPlanes.aspx'],['AltaPlan.aspx','GestionPlanes.aspx'],['RegistroClienteCorporativo.aspx?id='+empresaId,'RegistroClienteCorporativo.aspx']]){
   await go(url);check((await page.locator('.formulario-volver').innerText()).includes('Back'),url+': traducción inglesa');
   check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),url+': sin desbordamiento móvil');
   if(url.startsWith('ModificarEmpresa'))await page.screenshot({path:path.join(out,'modificar-empresa-movil-en.png'),fullPage:true});
   await back(destino,url+' móvil');
  }
  await login('webmaster@vitapet.local','en');
  for(const name of ['ConsultarUsuario','ModificarUsuario','BloquearUsuario','DesbloquearUsuario'])
   await child(name+'.aspx?operacion=vencida','GestionUsuarios.aspx',name+' móvil');
  await login('cliente@vitapet.local','en');
  for(const name of ['RegistroMascota','ActualizarMascota','ConsultaMascota','HistorialClinico','DatosClinicos'])
   await child(name+'.aspx'+(name==='RegistroMascota'?'':'?id=1'),'Mascotas.aspx',name+' móvil');
  await child('ConsultaCobertura.aspx','Cliente.aspx','Cobertura móvil');
  await go('Cliente.aspx');await click('lnkConsultarFactura');await back('Cliente.aspx','Factura móvil en inglés');
  await login('otro@test.local','en');await child(pendiente,'ContratarPlan.aspx','Pago pendiente móvil en inglés');
  await go(contratacion);await back('Planes.aspx','Detalle de contratación móvil');

  await context.clearCookies();
  for(const name of ['RegistroCliente','OlvideContrasena','RestablecerContrasena','CatalogoErrores'])
   await child(name+'.aspx',name==='RestablecerContrasena'?'OlvideContrasena.aspx':'Login.aspx',name+' móvil');
  await login('cliente@vitapet.local');
  for(const name of ['Home','Cliente','Planes','Mascotas','Turnos','Reintegros']){
   await go(name+'.aspx');check(await page.locator('.formulario-volver').count()===0,name+': formulario principal sin control innecesario');
  }
  check(errors.length===0,'Sin errores JavaScript en los recorridos');
  fs.writeFileSync(path.join(out,'navegacion-resultados.json'),JSON.stringify({checks:results.length,results,errors},null,2));
  console.log('TOTAL: '+results.length+' verificaciones de navegación');
 }catch(e){fs.writeFileSync(path.join(out,'navegacion-failure.html'),await page.content());console.error((await page.locator('body').innerText()).slice(0,5000));await page.screenshot({path:path.join(out,'navegacion-failure.png'),fullPage:true});throw e;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
