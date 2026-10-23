const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),crypto=require('crypto'),{execFileSync}=require('child_process');
(async()=>{
 const config=process.argv[2],env=JSON.parse(fs.readFileSync(config,'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Se requiere una base aislada');
 execFileSync(process.execPath,[path.join(__dirname,'Web.cjs'),config],{stdio:'inherit'});
 // Solo metadatos de prueba para abrir la confirmación: nunca se ejecuta una restauración ni un backup.
 const id=crypto.randomUUID().replaceAll('-',''),catalogo=path.join(env.Runtime,'recuperacion','catalogo');
 const xml='<BackupBE><Id>'+id+'</Id><BaseDatos>'+env.Database+'</BaseDatos><FechaUtc>'+new Date().toISOString()+'</FechaUtc><IdOperador>3</IdOperador><HashArchivo>fixture</HashArchivo><Esquema>fixture</Esquema><Verificado>true</Verificado></BackupBE>';
 fs.mkdirSync(catalogo,{recursive:true});fs.writeFileSync(path.join(catalogo,id+'.xml'),xml);
 fs.writeFileSync(path.join(catalogo,id+'.xml.firma'),'sha256:'+crypto.createHash('sha256').update(xml).digest('hex'));
 const out=path.dirname(config),browser=await chromium.launch({headless:true,channel:'msedge'});
 const ctx=await browser.newContext(),page=await ctx.newPage();let checks=0;
 const check=(ok,text)=>{if(!ok)throw Error(text);checks++;console.log('PASS '+text);};
 const c=id=>page.locator('[id$="_'+id+'"]');
 const panels=()=>page.locator('[id$="_pnlConfirmacion"],[id$="_pnlConfirmacionCrear"]');
 const click=async id=>{await Promise.all([page.waitForNavigation(),c(id).click()]);};
 async function posicion(panel,button,width){
  check(await panels().count()===1,'Una sola confirmación visible a '+width+' px');
  const actual=await c(panel).evaluate((e,btn)=>({previous:e.previousElementSibling.id,top:e.getBoundingClientRect().top,bottom:document.querySelector('[id$="_'+btn+'"]').getBoundingClientRect().bottom}),button);
  check(actual.previous.endsWith('_'+button)&&actual.top>=actual.bottom-1,panel+' inmediatamente debajo de '+button+' a '+width+' px');
 }
 try{
  await page.goto(env.Url+'Login');await c('txtCorreo').fill('webmaster@vitapet.local');await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');
  for(const width of [1440,768,390]){
   await page.setViewportSize({width,height:1000});await page.goto(env.Url+'BaseDatos');
   check(await panels().count()===0,'Sin confirmación al abrir Base de datos a '+width+' px');
   await click('btnCrear');await posicion('pnlConfirmacionCrear','btnCrear',width);
   check(/creaci[oó]n/i.test(await c('pnlConfirmacionCrear').innerText()),'Crear muestra el mensaje de creación');
   await page.screenshot({path:path.join(out,'confirmacion-crear-'+width+'.png'),fullPage:true});
   await click('btnCancelarCrear');check(await panels().count()===0,'Cancelar creación oculta la confirmación');
   await click('btnRestaurar');await posicion('pnlConfirmacion','btnRestaurar',width);
   check(/restaur/i.test(await c('pnlConfirmacion').innerText()),'Restaurar muestra el mensaje de restauración');
   await page.screenshot({path:path.join(out,'confirmacion-restaurar-'+width+'.png'),fullPage:true});
   await click('btnCrear');await posicion('pnlConfirmacionCrear','btnCrear',width);
   await click('btnRestaurar');await posicion('pnlConfirmacion','btnRestaurar',width);
   await c('btnCancelar').focus();await Promise.all([page.waitForNavigation(),page.keyboard.press('Enter')]);
   check(await panels().count()===0,'Cancelar restauración con teclado oculta la confirmación');
  }
  fs.writeFileSync(path.join(out,'backup-confirmacion-resultados.json'),JSON.stringify({checks,widths:[1440,768,390]},null,2));
  console.log('TOTAL CONFIRMACIÓN BACKUP: '+checks+' comprobaciones');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
