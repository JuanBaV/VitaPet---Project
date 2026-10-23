const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),{execFileSync}=require('child_process');
(async()=>{
 const file=process.argv[2],env=JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Se requiere una base aislada');
 execFileSync(process.execPath,[path.join(__dirname,'Web.cjs'),file],{stdio:'inherit'});
 const out=path.dirname(file),browser=await chromium.launch({headless:true,channel:'msedge'}),results=[],errors=[];
 const ctx=await browser.newContext({viewport:{width:1440,height:1000},acceptDownloads:true}),page=await ctx.newPage();
 page.on('pageerror',e=>errors.push(e.message));
 const check=(ok,text)=>{if(!ok)throw Error(text);results.push(text);console.log('PASS '+text);};
 const c=id=>page.locator('[id$="_'+id+'"], [id$="_'+id+'_0"]');
 const go=async url=>{const r=await page.goto(env.Url+url);check(r&&r.status()===200,url+': HTTP 200');};
 const click=async locator=>Promise.all([page.waitForNavigation(),(typeof locator==='string'?c(locator):locator).click()]);
 const login=async email=>{await ctx.clearCookies();await go('Login');await c('txtCorreo').fill(email);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');check(new URL(page.url()).pathname.endsWith('/Home'),'Sesión: '+email);};
 const language=async en=>{await page.locator('#btnSelectorIdioma').click();await click(en?'btnIdiomaEn':'btnIdiomaEs');};
 const parse=async text=>page.evaluate(text=>{
  const d=new DOMParser().parseFromString(text,'application/xml');if(d.querySelector('parsererror'))throw Error('XML inválido o contenido HTML agregado a la descarga');
  const fields=e=>Object.fromEntries(Array.from(e.children).filter(x=>!x.children.length).map(x=>[x.localName,x.textContent]));
  return {root:d.documentElement.localName,fields:fields(d.documentElement),rows:Object.fromEntries(['Reintegro','Turno','Prestacion'].map(tag=>[tag,Array.from(d.getElementsByTagName(tag)).map(fields)]))};
 },text);
 const download=async(name,prefix)=>{
  const [d,response]=await Promise.all([page.waitForEvent('download'),page.waitForResponse(r=>r.request().method()==='POST'&&(r.headers()['content-type']||'').includes('application/xml')),c('btnExportarXml').click()]);
  check(d.suggestedFilename()===name,'Nombre del archivo: '+name);
  const headers=response.headers();check(headers['content-type'].toLowerCase().includes('charset=utf-8'),'Tipo XML y charset UTF-8: '+name);
  check((headers['content-disposition']||'').includes('attachment;')&&(headers['cache-control']||'').includes('no-store'),'Descarga adjunta sin caché: '+name);
  const dest=path.join(out,prefix+'-'+name);await d.saveAs(dest);const bytes=fs.readFileSync(dest),text=new TextDecoder('utf-8',{fatal:true,ignoreBOM:true}).decode(bytes);
  check(text.startsWith('<?xml')&&text.includes('encoding="utf-8"'),'Declaración XML completa sin BOM: '+name);
  const parsed=await parse(text);check(!/<(?:html|form)\b/i.test(text),'La descarga termina en XML, sin HTML de Web Forms: '+name);
  return {...parsed,text,bytes};
 };
 try{
  await login('cliente@vitapet.local');const reference={};
  for(const en of [false,true]){
   await go('Cliente');await language(en);const mode=en?'en':'es';
   await go('Reintegros');check(await c('btnExportarXml').isDisabled(),'Reintegros '+mode+': no exporta sin seleccionar');
   const row=c('gvReintegros').getByRole('cell',{name:'1',exact:true}).locator('..');await click(row.locator('[id*="_btnConsultar"]'));
   const detail=await c('lblDetalle').innerText(),r=await download('Reintegros.xml',mode);
   check(r.root==='Reintegros'&&r.rows.Reintegro.length===1&&r.rows.Reintegro[0].idReintegro==='1','Reintegros '+mode+': solo el registro seleccionado');
   check(r.rows.Reintegro[0].importe==='1234.56'&&r.rows.Reintegro[0].detalle===detail&&detail==='Revisión & análisis <Luna>','Reintegros '+mode+': contenido real, acentos y decimal invariante');
   check(!r.text.includes('Ajeno reservado'),'Reintegros: sin datos de otro cliente');
   await go('Turnos?vista=consulta');check(await c('btnExportarXml').isDisabled(),'Turnos '+mode+': no exporta sin mascota');
   await click(c('gvMascotas').locator('tr').filter({hasText:'Luna'}).locator('[id*="_lnkSeleccionarMascota"]'));
   const t=await download('Turnos.xml',mode),turns=t.rows.Turno;
   check(t.root==='Turnos'&&turns.length===2&&turns.some(x=>x.prestacion==='Control & revisión <Luna>'),'Turnos '+mode+': todos los turnos de Luna con texto íntegro');
   check(turns.every(x=>/^\d{4}-\d{2}-\d{2}$/.test(x.fecha)&&/^\d{2}:\d{2}$/.test(x.horario)),'Turnos: fechas y horarios independientes del idioma');
   const rows=await c('gvTurnos').innerText();check(turns.every(x=>rows.includes(x.prestacion)&&rows.includes(x.horario)&&rows.includes(x.zona)),'Turnos: el archivo coincide con la grilla');
   await click(c('gvMascotas').locator('tr').filter({hasText:'Sol'}).locator('[id*="_lnkSeleccionarMascota"]'));
   check(await c('btnExportarXml').isDisabled(),'Turnos: cambiar a una mascota sin turnos deshabilita la descarga anterior');
   await go('ConsultaCobertura');check(await c('btnExportarXml').isVisible()&&(await c('btnExportarXml').inputValue()).includes(en?'Export':'Exportar'),'Cobertura '+mode+': botón disponible y traducido');
   const name=await c('lblNombrePlan').innerText(),cover=await download('Cobertura.xml',mode);
   check(cover.root==='Cobertura'&&cover.fields.nombrePlan===name&&cover.fields.estadoSuscripcion==='Activa','Cobertura '+mode+': plan y contratación propios');
   check(/^\d+$/.test(cover.fields.idPlan)&&/^\d+$/.test(cover.fields.idSuscripcion)&&!Number.isNaN(Date.parse(cover.fields.fechaAlta))&&!Number.isNaN(Date.parse(cover.fields.fechaVencimiento)),'Cobertura: identificadores y vigencia válidos');
   const grid=await c('gvCoberturas').innerText();check(cover.rows.Prestacion.length>0&&cover.rows.Prestacion.every(x=>grid.includes(x.nombre)&&grid.includes(x.detalle)),'Cobertura: todas las prestaciones coinciden con la consulta');
   check(cover.rows.Prestacion.some(x=>x.detalle==='Cobertura & atención <preventiva>'),'Cobertura: conserva los caracteres especiales del negocio');
   if(en)check(r.bytes.equals(reference.reintegros)&&t.bytes.equals(reference.turnos)&&cover.bytes.equals(reference.cobertura),'Los tres XML conservan su contenido y formato entre español e inglés');
   else Object.assign(reference,{reintegros:r.bytes,turnos:t.bytes,cobertura:cover.bytes});
   await page.setViewportSize({width:390,height:844});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Cobertura '+mode+': nueva acción sin desbordamiento en móvil');
   await page.screenshot({path:path.join(out,'cobertura-xml-'+mode+'.png'),fullPage:true});await page.setViewportSize({width:1440,height:1000});
  }
  await go('ConsultaCobertura?idSuscripcion=999999&idPlan=999999');const manipulated=await download('Cobertura.xml','parametros-ignorados');
  check(manipulated.bytes.equals(reference.cobertura),'Cobertura: parámetros de URL no cambian la contratación que se exporta');
  await login('otro@test.local');await go('ConsultaCobertura');check(!await c('btnExportarXml').count()&&(await c('pnlMensaje').innerText()).includes('ER008'),'Cobertura: no ofrece descarga a una cuenta sin contratación');
  await go('Reintegros');check(!await c('gvReintegros').getByRole('cell',{name:'1',exact:true}).count(),'El segundo cliente no encuentra el reintegro del primero');
  await click(c('gvReintegros').getByRole('cell',{name:'3',exact:true}).locator('..').locator('[id*="_btnConsultar"]'));
  const own=await download('Reintegros.xml','segundo-cliente');check(own.rows.Reintegro.length===1&&own.rows.Reintegro[0].detalle==='Ajeno reservado','El segundo cliente solo descarga su reintegro');
  await go('Turnos?vista=consulta');await click(c('gvMascotas').locator('tr').filter({hasText:'Ajena'}).locator('[id*="_lnkSeleccionarMascota"]'));
  const other=await download('Turnos.xml','segundo-cliente');check(other.rows.Turno.length===1&&other.rows.Turno[0].prestacion==='Ajeno reservado','El segundo cliente solo descarga sus turnos');
  for(const email of [null,'administrador@vitapet.local','webmaster@vitapet.local']){
   if(email)await login(email);else await ctx.clearCookies();
   for(const route of ['Reintegros','Turnos?vista=consulta','ConsultaCobertura']){
    const response=await ctx.request.post(env.Url+route,{form:{'ctl00$MainContent$btnExportarXml':'Exportar a XML'}}),body=await response.text();
    check(!(response.headers()['content-type']||'').includes('application/xml')&&!response.headers()['content-disposition']&&!/<(?:Reintegros|Turnos|Cobertura)>/.test(body),route+': POST no descarga XML para '+(email||'sin sesión'));
   }
  }
  await go('BaseDatos');await click('btnVerificar');check((await page.locator('body').innerText()).includes('No se detectaron errores de integridad.'),'Exportaciones conservan DVH, DVV y referencias en la base cifrada');
  check(!errors.length,'Sin errores JavaScript en las descargas XML');
  fs.writeFileSync(path.join(out,'xml-resultados.json'),JSON.stringify({checks:results.length,exports:['Reintegros.xml','Turnos.xml','Cobertura.xml'],results},null,2));
  console.log('TOTAL XML WEB: '+results.length+' comprobaciones');
 }catch(e){try{await page.waitForLoadState('load');fs.writeFileSync(path.join(out,'xml-failure.html'),await page.content());await page.screenshot({path:path.join(out,'xml-failure.png'),fullPage:true});}catch{}throw e;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
