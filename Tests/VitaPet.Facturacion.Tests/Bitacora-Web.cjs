const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),{execFileSync}=require('child_process');
(async()=>{
 const file=process.argv[2],env=JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Se requiere base aislada');
 execFileSync(process.execPath,[path.join(__dirname,'Web.cjs'),file],{stdio:'inherit'});
 const out=path.dirname(file),browser=await chromium.launch({headless:true,channel:'msedge'}),results=[],errors=[];
 const context=await browser.newContext({viewport:{width:1440,height:1000},acceptDownloads:true}),page=await context.newPage();
 page.on('pageerror',e=>errors.push(e.message));
 const c=id=>page.locator('[id$="_'+id+'"]');
 const check=(ok,text)=>{if(!ok)throw Error(text);results.push(text);console.log('PASS '+text);};
 const go=async name=>{const r=await page.goto(env.Url+name);check(r&&r.status()===200,name+': HTTP 200');};
 const click=async id=>Promise.all([page.waitForNavigation(),c(id).click()]);
 const login=async email=>{await context.clearCookies();await go('Login');await c('txtCorreo').fill(email);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');check(new URL(page.url()).pathname.endsWith('/Home'),'Sesión '+email);};
 const descargar=async nombre=>{
  const [d]=await Promise.all([page.waitForEvent('download'),c('btnExportarXml').click()]);
  const dest=path.join(out,nombre+'.xml');await d.saveAs(dest);
  const texto=new TextDecoder('utf-8',{fatal:true}).decode(fs.readFileSync(dest));
  check(d.suggestedFilename().startsWith('bitacora-'),'Nombre de descarga de bitácora');
  const rows=await page.evaluate(text=>{const d=new DOMParser().parseFromString(text,'application/xml');if(d.querySelector('parsererror'))throw Error('XML inválido');return Array.from(d.querySelectorAll('Registro')).map(r=>Object.fromEntries(Array.from(r.children).map(n=>[n.nodeName,n.textContent])));},texto);
  return rows;
 };
 try{
  await login('webmaster@vitapet.local');await go('ConsultaBitacora');
  const rows=await descargar('todos');check(rows.filter(r=>r.Modulo==='Prueba P26').length===65,'Todos los 65 eventos de prueba consultables después del mantenimiento');
  check(new Set(rows.filter(r=>r.Modulo==='Prueba P26').map(r=>r.IdBitacora)).size===65,'Consulta y XML sin eventos duplicados');
  check(rows.filter(r=>r.Modulo==='Prueba P26'&&r.Archivado==='true').length===5,'Sólo cinco eventos mayores a 30 días dados de baja');
  const antiguas=rows.filter(r=>r.Modulo==='Prueba P26'&&r.Archivado==='true');const dia=antiguas[0].Fecha.slice(0,10);
  const pager=c('gvBitacora').locator('.bitacora-paginacion a').filter({hasText:/^2$/});
  check(await pager.count()===1,'Grilla permite pasar a la segunda página de 20 eventos');
  await Promise.all([page.waitForNavigation(),pager.click()]);
  const segunda=await descargar('desde-pagina-2');check(JSON.stringify(segunda)===JSON.stringify(rows),'Exportación incluye todas las páginas y conserva la consulta seleccionada');
  await c('dtpFechaDesde').fill(dia);await c('dtpFechaHasta').fill(dia);await c('ddlCriticidad').selectOption('Grave');
  const filtradas=await descargar('historico-filtrado');
  check(filtradas.length===1&&filtradas[0].Criticidad==='Grave'&&filtradas[0].Fecha.startsWith(dia),'Exportar aplica fechas y criticidad editadas aunque no se haya pulsado Consultar');
  check(filtradas[0].Archivado==='true'&&filtradas[0].Accion.includes('& revisión <histórica>'),'Exportación conserva baja lógica y caracteres especiales');
  await c('ddlCriticidad').selectOption('');await click('btnConsultar');
  const historico=await descargar('historico-dia');check(historico.length===5,'Filtro de un mismo día incluye todos sus eventos históricos');
  await c('dtpFechaDesde').fill('2099-01-01');await click('btnConsultar');check((await c('pnlMensaje').innerText()).includes('ER027'),'Fechas invertidas muestran ER027');
  await c('dtpFechaHasta').fill('2099-01-01');await click('btnConsultar');check((await c('pnlMensaje').innerText()).includes('ER019')&&await c('btnExportarXml').isDisabled(),'Consulta vacía informa ER019 y deshabilita exportación');
  await c('dtpFechaDesde').fill(dia);await c('dtpFechaHasta').fill(dia);await click('btnConsultar');
  await page.screenshot({path:path.join(out,'bitacora-historico.png'),fullPage:true});
  await page.locator('#btnSelectorIdioma').click();await click('btnIdiomaEn');
  await c('dtpFechaDesde').fill(dia);await c('dtpFechaHasta').fill(dia);await click('btnConsultar');
  const ingles=await descargar('historico-en');check(JSON.stringify(ingles)===JSON.stringify(historico),'Cambio de idioma conserva contenido del histórico');
  await page.setViewportSize({width:390,height:844});check(await c('btnConsultar').isVisible()&&await c('btnExportarXml').isVisible(),'Filtros y exportación accesibles en móvil');
  await page.screenshot({path:path.join(out,'bitacora-movil.png'),fullPage:true});
  for(const email of ['cliente@vitapet.local','administrador@vitapet.local']){await login(email);await page.goto(env.Url+'ConsultaBitacora');check(new URL(page.url()).pathname.endsWith('/AccesoDenegado'),'Acceso restringido para '+email);}
  await context.clearCookies();await page.goto(env.Url+'ConsultaBitacora');check(new URL(page.url()).pathname.endsWith('/Login'),'Consulta sin sesión deriva a Login');
  check(!errors.length,'Sin errores JavaScript durante los recorridos');
  fs.writeFileSync(path.join(out,'results.json'),JSON.stringify(results,null,2));console.log('TOTAL: '+results.length+' verificaciones web de bitácora');
 }catch(e){fs.writeFileSync(path.join(out,'failure.html'),await page.content());await page.screenshot({path:path.join(out,'failure.png'),fullPage:true});throw e;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
