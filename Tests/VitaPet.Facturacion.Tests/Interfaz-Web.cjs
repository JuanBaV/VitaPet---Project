const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),{execFileSync}=require('child_process');
(async()=>{
 const config=process.argv[2],env=JSON.parse(fs.readFileSync(config,'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Se requiere una base aislada');
 execFileSync(process.execPath,[path.join(__dirname,'Web.cjs'),config],{stdio:'inherit'});
 const out=path.dirname(config),browser=await chromium.launch({headless:true,channel:'msedge'});
 const ctx=await browser.newContext({viewport:{width:1440,height:1000}}),page=await ctx.newPage();
 const errors=[],failures=[],results=[];
 page.on('pageerror',e=>errors.push(e.message));
 page.on('response',r=>{if(r.status()>=400)errors.push(r.status()+' '+r.url());});
 const c=id=>page.locator('[id$="_'+id+'"]');
 const check=(ok,text,detail)=>{results.push({ok,text,detail});if(!ok){failures.push({text,detail});console.log('FAIL '+text+' '+JSON.stringify(detail));}else console.log('PASS '+text);};
 const go=async url=>{const response=await page.goto(env.Url+url);check(response&&response.status()===200,url+': HTTP 200');};
 const click=async locator=>{await Promise.all([page.waitForNavigation(),locator.click()]);};
 async function revisar(role,url){
  await go(url);
  const label=role+' '+url;
  check(await page.locator('h1').count()===1,label+': título principal único');
  const assets=await page.locator('img').evaluateAll(imgs=>imgs.filter(i=>!i.complete||!i.naturalWidth).map(i=>i.src));
  check(!assets.length,label+': imágenes cargadas',assets);
  const unnamed=await page.locator('input:not([type=hidden]):not([type=submit]):not([type=button]),select,textarea').evaluateAll(inputs=>inputs.filter(e=>e.getBoundingClientRect().width&&!e.disabled&&!e.labels?.length&&!e.getAttribute('aria-label')&&!e.getAttribute('aria-labelledby')).map(e=>e.id));
  check(!unnamed.length,label+': campos con etiqueta',unnamed);
  for(const width of [1440,1024,768,390,360]){
   await page.setViewportSize({width,height:1000});await page.evaluate(()=>scrollTo(0,0));
   const geometry=await page.evaluate(()=>{
    const header=document.querySelector('header.navbar')?.getBoundingClientRect()||{bottom:0},title=document.querySelector('h1')?.getBoundingClientRect();
    return {width:innerWidth,document:document.documentElement.scrollWidth,header:header.bottom,title:title?.top,
     hiddenMenu:Array.from(document.querySelectorAll('.navbar-role-menu a')).filter(e=>{const r=e.getBoundingClientRect();return r.width&&(!e.innerText.trim()||r.left<0||r.right>innerWidth+1);}).map(e=>({text:e.innerText,right:e.getBoundingClientRect().right}))};
   });
   check(geometry.document<=width+1,label+' '+width+': sin desbordamiento horizontal',geometry);
   check(geometry.title>=geometry.header-1,label+' '+width+': encabezado no tapa el título',geometry);
   check(!geometry.hiddenMenu.length,label+' '+width+': menú completo dentro de pantalla',geometry.hiddenMenu);
   if(((width===1440||width===390)&&['Home','Login','Cliente','Planes','Turnos','GestionPlanes','GestionPerfiles','ConsultaBitacora','BaseDatos'].includes(url))||(width===768&&url==='Home'))
    await page.screenshot({path:path.join(out,role+'-'+url+'-'+width+'.png'),fullPage:true});
  }
 }
 try{
  await ctx.clearCookies();
  for(const url of ['Home','Login','RegistroCliente','OlvideContrasena','CatalogoErrores','AccesoDenegado'])await revisar('publico',url);
  // El acceso público al inicio y el regreso desde el login se prueban mediante clics.
  await go('Home');await click(c('lnkIniciarSesion'));check(new URL(page.url()).pathname.endsWith('/Login'),'Inicio público abre Login');
  await click(page.locator('.formulario-volver'));check(new URL(page.url()).pathname.endsWith('/Home'),'Login permite regresar al inicio');
  for(const [role,correo] of [['cliente','cliente'],['administrador','administrador'],['webmaster','webmaster']]){
   await page.setViewportSize({width:1440,height:1000});await ctx.clearCookies();await go('Login');
   await c('txtCorreo').fill(correo+'@vitapet.local');await c('txtContrasena').fill('Prueba2026!');await click(c('btnIngresar'));
   await page.locator('#btnSelectorIdioma').click();await click(c('btnIdiomaEs'));
   const links=await page.locator('.navbar-role-menu a[href]:not([href^="javascript:"])').evaluateAll(es=>es.filter(e=>e.getBoundingClientRect().width).map(e=>({id:e.id,href:e.href,text:e.innerText.trim()})));
   check(links.length>3&&links.every(l=>l.text),role+': menú con nombres de destino');
   for(const link of links.filter(l=>!l.href.includes('/Logout'))){
    await go('Home');await click(page.locator('[id="'+link.id+'"]'));
    check(new URL(page.url()).pathname.replace(/\.aspx$/,'')===new URL(link.href).pathname.replace(/\.aspx$/,''),role+': enlace '+link.text+' abre su destino');
    const url=new URL(link.href).pathname.split('/').pop().replace(/\.aspx$/,'');await revisar(role,url);
   }
   await page.setViewportSize({width:390,height:844});
   const salir=page.locator('.navbar-role-menu a[href*="Logout"]');await salir.focus();
   check(await salir.evaluate(e=>document.activeElement===e),role+': cierre de sesión accesible con teclado');
   await Promise.all([page.waitForNavigation(),page.keyboard.press('Enter')]);check(new URL(page.url()).pathname.endsWith('/Logout'),role+': menú abre la confirmación de cierre');
   await revisar(role,'Logout');await click(c('lnkCancelar'));
   check(new URL(page.url()).pathname.endsWith('/Home')&&await c('lnkCerrarSesion').isVisible(),role+': cancelar conserva la sesión y vuelve al inicio');
   await go('Logout');await c('btnConfirmarLogout').focus();await Promise.all([page.waitForNavigation(),page.keyboard.press('Enter')]);
   check(new URL(page.url()).pathname.endsWith('/Home')&&await c('lnkIniciarSesion').isVisible(),role+': confirmar cierra sesión y vuelve al inicio público');
  }
  check(!errors.length,'Sin errores JavaScript ni recursos HTTP fallidos',errors);
  fs.writeFileSync(path.join(out,'interfaz-resultados.json'),JSON.stringify({checks:results.length,failures,errors,results},null,2));
  if(failures.length)throw Error(failures.length+' comprobaciones de interfaz fallaron');
  console.log('TOTAL INTERFAZ: '+results.length+' comprobaciones');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
