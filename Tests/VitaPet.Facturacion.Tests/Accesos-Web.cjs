const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),{execFileSync}=require('child_process');
(async()=>{
 const config=process.argv[2],env=JSON.parse(fs.readFileSync(config,'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Se requiere una base aislada');
 execFileSync(process.execPath,[path.join(__dirname,'Web.cjs'),config],{stdio:'inherit'});
 const grupos={
  Cliente:['Cliente','Planes','ContratarPlan','GestionPago','ConsultaCobertura','Reintegros','RegistroMascota','Mascotas','ActualizarMascota','ConsultaMascota','HistorialClinico','DatosClinicos','Turnos'],
  Administrador:['Empresas','AltaEmpresa','ConsultaEmpresa','ModificarEmpresa','RegistroClienteCorporativo','GestionPlanes','AltaPlan','ConsultaPlan','ModificarPlan','ConsultarPerfiles'],
  Webmaster:['ConsultaBitacora','BaseDatos','GestionUsuarios','ConsultarUsuario','ModificarUsuario','BloquearUsuario','DesbloquearUsuario','GestionPerfiles','AltaUsuario']
 };
 const pantallas=Object.entries(grupos).flatMap(([rol,nombres])=>nombres.map(nombre=>({nombre,rol})));
 // Detectar páginas nuevas sin cubrir en esta matriz de permisos esperados.
 const fuente=fs.readFileSync(path.join(__dirname,'../../VitaPet.BLL/PerfilBLL.cs'),'utf8');
 const catalogo=[...fuente.matchAll(/\{"([^"]+)","(Cliente|Administrador|Webmaster)"\}/g)].map(m=>m[1]+':'+m[2]).sort();
 if(JSON.stringify(catalogo)!==JSON.stringify(pantallas.map(p=>p.nombre+':'+p.rol).sort()))throw Error('Actualizar la matriz de accesos para el catálogo vigente');
 const out=path.dirname(config),browser=await chromium.launch({headless:true,channel:'msedge'}),results=[];
 const check=(ok,text)=>{if(!ok)throw Error(text);results.push(text);console.log('PASS '+text);};
 const ruta=url=>new URL(url,env.Url).pathname.replace(/\.aspx$/i,'').toLowerCase();
 async function rechaza(ctx,nombre,method,destino){
  let url=env.Url+nombre,currentMethod=method;
  for(let paso=0;paso<5;paso++){
   const options={maxRedirects:0,failOnStatusCode:false};
   if(currentMethod==='POST')options.form={__EVENTTARGET:'ctl00$MainContent$btnCrear','ctl00$MainContent$txtNombre':'Intento no autorizado'};
   const response=currentMethod==='POST'?await ctx.request.post(url,options):await ctx.request.get(url,options);
   const status=response.status(),location=response.headers().location;
   if([301,302,303,307,308].includes(status)&&location){
    if(ruta(location)==='/'+destino.toLowerCase()){check(true,method+' '+nombre+' rechaza hacia '+destino);return;}
    url=new URL(location,url).href;
    if(![307,308].includes(status))currentMethod='GET';
   }else throw Error(method+' '+nombre+': esperaba rechazo, recibió HTTP '+status+' en '+url);
  }
  throw Error('Demasiadas redirecciones: '+nombre);
 }
 try{
  for(const [rol,correo,ejemplo] of [['Sin sesión',null,'GestionUsuarios'],['Cliente','cliente','GestionUsuarios'],['Administrador','administrador','GestionPerfiles'],['Webmaster','webmaster','GestionPago']]){
   const ctx=await browser.newContext({viewport:{width:1440,height:1000}}),page=await ctx.newPage(),c=id=>page.locator('[id$="_'+id+'"]');
   const click=async id=>{await Promise.all([page.waitForNavigation(),c(id).click()]);};
   await page.goto(env.Url+'Login');
   if(correo){
    await c('txtCorreo').fill(correo+'@vitapet.local');await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');
    check(!ruta(page.url()).endsWith('/login'),rol+': inicio de sesión válido');
    await page.locator('#btnSelectorIdioma').click();await click('btnIdiomaEs');
    const permitida={Cliente:'Mascotas',Administrador:'GestionPlanes',Webmaster:'GestionPerfiles'}[rol];
    await page.goto(env.Url+permitida);check(ruta(page.url())==='/'+permitida.toLowerCase(),rol+': conserva acceso a '+permitida);
   }
   const prohibidas=pantallas.filter(p=>p.rol!==rol&&!(rol==='Webmaster'&&p.nombre==='ConsultarPerfiles'));
   const menu=await page.locator('.navbar-role-menu a[href]').evaluateAll(es=>es.map(e=>e.href));
   check(prohibidas.every(p=>!menu.some(h=>ruta(h)==='/'+p.nombre.toLowerCase())),rol+': el menú no ofrece páginas prohibidas');
   for(const pantalla of prohibidas){
    const destino=correo?'AccesoDenegado':'Login';
    await rechaza(ctx,pantalla.nombre,'GET',destino);
    await rechaza(ctx,pantalla.nombre+'.aspx','GET',destino);
    await rechaza(ctx,pantalla.nombre,'POST',destino);
   }
   await page.goto(env.Url+ejemplo);
   check(ruta(page.url())===(correo?'/accesodenegado':'/login'),rol+': URL directa muestra la pantalla de rechazo esperada');
   await page.screenshot({path:path.join(out,'acceso-'+(correo||'anonimo')+'.png'),fullPage:true});
   if(correo){
    await click('lnkVolverInicio');check(ruta(page.url())==='/home',rol+': el rechazo permite volver al inicio');
    if(rol==='Webmaster'){await page.goto(env.Url+'ConsultarPerfiles');check(ruta(page.url())==='/consultarperfiles','Webmaster conserva la consulta compartida de perfiles');}
    await page.goto(env.Url+'Logout');await click('btnConfirmarLogout');
    await rechaza(ctx,ejemplo,'GET','Login');
    check(await c('lnkIniciarSesion').isVisible(),rol+': cerrar sesión elimina el acceso autenticado');
   }
   await ctx.close();
  }
  fs.writeFileSync(path.join(out,'accesos-resultados.json'),JSON.stringify({checks:results.length,pantallas:pantallas.length,results},null,2));
  console.log('TOTAL ACCESOS WEB: '+results.length+' comprobaciones sobre '+pantallas.length+' pantallas');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
