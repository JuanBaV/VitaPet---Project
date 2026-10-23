const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),{execFileSync}=require('child_process');
(async()=>{
 const file=process.argv[2],env=JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Se requiere una base aislada');
 execFileSync(process.execPath,[path.join(__dirname,'Web.cjs'),file],{stdio:'inherit'});
 const out=path.dirname(file),browser=await chromium.launch({headless:true,channel:'msedge'}),results=[],errors=[];
 let page;
 const check=(ok,text)=>{if(!ok)throw Error(text);results.push(text);console.log('PASS '+text);};
 const c=id=>page.locator('[id$="_'+id+'"], [id$="_'+id+'_0"]');
 const go=async url=>{const r=await page.goto(env.Url+url);check(r&&r.status()===200,url+': HTTP 200');await page.locator('form').evaluate(f=>f.noValidate=true);};
 const click=async(target,navigation=true)=>{await page.locator('form').evaluate(f=>f.noValidate=true);const locator=typeof target==='string'?c(target):target;if(navigation)await Promise.all([page.waitForNavigation(),locator.click()]);else await locator.click();};
 const language=async en=>{await page.locator('#btnSelectorIdioma').click();await click(en?'btnIdiomaEn':'btnIdiomaEs');};
 const login=async(ctx,correo)=>{await ctx.clearCookies();await go('Login');await c('txtCorreo').fill(correo);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');check(new URL(page.url()).pathname.endsWith('/Home'),'Inicio de sesión de prueba: '+correo);};
 try{
  for(const js of [true,false]){
   const ctx=await browser.newContext({javaScriptEnabled:js,viewport:{width:1440,height:1000}});page=await ctx.newPage();page.on('pageerror',e=>errors.push(e.message));
   const mode=js?'con JavaScript':'sin JavaScript';
   await go('Login');await c('txtContrasena').fill('Prueba2026!');
   for(const [value,validator] of [['','rfvCorreo'],['sin-correo','revCorreo'],['a@@ejemplo.com','revCorreo'],['a b@ejemplo.com','revCorreo'],['a'.repeat(150)+'@ejemplo.com','revLongitudCorreo']]){
    await c('txtCorreo').evaluate(e=>e.removeAttribute('maxlength'));await c('txtCorreo').fill(value);await click('btnIngresar',!js);
    await c(validator).waitFor({state:'visible'});check(true,'ValidacionEmail '+mode+': rechaza '+(value||'vacío'));
   }
   await login(ctx,'administrador@vitapet.local');if(js)await language(false);
   await go('AltaPlan');
   const name='Control numérico '+(js?'JS':'servidor');
   for(const [id,value] of Object.entries({txtNombrePlan:name,txtDescripcionPlan:'Prueba de control reutilizable',txtPrestacion:'Consulta',txtDetalle:'Cobertura de prueba'}))await c(id).fill(value);
   check(await c('txtNumero').getAttribute('inputmode')==='decimal','ValidacionNumerica '+mode+': teclado decimal');
   check(await c('txtNumero').evaluate(e=>e.labels.length===1),'ValidacionNumerica '+mode+': etiqueta asociada al campo');
   for(const [value,validator] of [['','rfvNumero'],['abc','revNumero'],['-1','revNumero'],['1e3','revNumero'],['12.345','revNumero'],['1.234,56','revNumero'],['NaN','revNumero'],['0','cvDatosPlan'],['100000000','cvDatosPlan'],['9'.repeat(40),'cvDatosPlan']]){
    await c('txtNumero').fill(value);await c('txtNumero').press('Tab');await click('btnRegistrarPlan',!js||validator==='cvDatosPlan');await c(validator).waitFor({state:'visible'});check(true,'ValidacionNumerica '+mode+': rechaza '+(value||'vacío'));
    check(!new URL(page.url()).searchParams.has('resultado'),'Precio inválido no registra el plan');
   }
   await page.screenshot({path:path.join(out,'numero-rechazo-'+js+'.png'),fullPage:true});
   await c('txtNumero').fill('25,50');await click('btnRegistrarPlan');
   await page.waitForURL(url=>url.searchParams.has('resultado'));
   check(new URL(page.url()).searchParams.has('resultado')&&await c('btnRegistrarPlan').isDisabled(),'ValidacionNumerica '+mode+': guarda coma decimal una sola vez');
   check((await c('txtNumero').inputValue()).replace(',','.')==='25.50','El control recupera el precio guardado');
   await go('GestionPlanes');const row=c('gvPlanes').locator('tr').filter({hasText:name}),id=(await row.locator('td').first().innerText()).trim();
   check(/^\d+$/.test(id),'El plan registrado aparece en la consulta');await go('ModificarPlan?id='+id);
   check((await c('txtNumero').inputValue()).replace(',','.')==='25.50','El mismo control carga datos en modificación');
   await c('txtNumero').fill('26.75');await click('btnGuardarPlan');
   await go('ModificarPlan?id='+id);
   check((await c('txtNumero').inputValue()).replace(',','.')==='26.75','ValidacionNumerica '+mode+': modifica usando punto decimal');
   if(js){
    await language(true);await go('AltaPlan');await c('txtNumero').fill('abc');await click('btnRegistrarPlan',false);await c('revNumero').waitFor({state:'visible'});
    check((await c('revNumero').innerText()).includes('non-negative number'),'El control numérico muestra su validación en inglés');
    await page.setViewportSize({width:390,height:844});check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'El nuevo control conserva el formulario en móvil');
    await page.screenshot({path:path.join(out,'numero-ingles-movil.png'),fullPage:true});await language(false);
   }
   await ctx.close();
  }
  const ctx=await browser.newContext({viewport:{width:1440,height:1000}});page=await ctx.newPage();page.on('pageerror',e=>errors.push(e.message));
  await login(ctx,'cliente@vitapet.local');await language(false);await go('Mascotas');
  check(!await c('gvMascotas').getByRole('cell',{name:'Ajena',exact:true}).count(),'SelectorMascota no ofrece mascotas de otro cliente');
  check(await c('btnConsultarDatos').isDisabled(),'SelectorMascota inicia sin una selección autorizada');
  for(const name of ['Luna','Sol']){
   await go('Mascotas');await click(c('gvMascotas').locator('tr').filter({hasText:name}).locator('[id*="_lnkSeleccionarMascota"]'));
   check(await c('btnConsultarDatos').isEnabled(),'Seleccionar '+name+' habilita las acciones de la página');await click('btnConsultarDatos');
   check(await c('lblNombre').innerText()===name,'El evento del selector consulta la mascota elegida: '+name);
  }
  await go('ConsultaMascota?id=4');check(!await c('pnlDatos').count(),'Una URL manipulada no permite consultar la mascota ajena');
  await go('Turnos');check(await c('ddlZona').isDisabled(),'FiltroZona inicia bloqueado hasta elegir mascota');
  await click(c('gvMascotas').locator('tr').filter({hasText:'Luna'}).locator('[id*="_lnkSeleccionarMascota"]'));
  check(await c('ddlZona').isEnabled(),'SelectorMascota también habilita el filtro de zona en Turnos');
  const zones=await c('ddlZona').locator('option').evaluateAll(es=>es.map(e=>e.value).filter(Boolean));check(zones.length>=2,'FiltroZona recibe las zonas disponibles');
  await Promise.all([page.waitForNavigation(),c('ddlZona').selectOption(zones[0])]);
  check(await c('ddlEspecialidad').isEnabled(),'ZonaCambiada actualiza las especialidades disponibles');
  const specialty=await c('ddlEspecialidad').locator('option').evaluateAll(es=>es.map(e=>e.value).find(Boolean));await Promise.all([page.waitForNavigation(),c('ddlEspecialidad').selectOption(specialty)]);
  await click(page.locator('.turnos-dia-disponible a').first());await Promise.all([page.waitForNavigation(),c('rblHorarios').locator('input').first().check()]);
  check(await c('btnConfirmarSolicitud').isEnabled()&&(await c('lblTurnoSeleccionado').innerText()).includes(zones[0]),'El filtro permite preparar un horario de la zona elegida');
  await Promise.all([page.waitForNavigation(),c('ddlZona').selectOption(zones[1])]);
  check(await c('btnConfirmarSolicitud').isDisabled()&&!await c('pnlCalendario').count(),'Cambiar zona elimina la selección de fecha y horario anterior');
  await click(c('gvMascotas').locator('tr').filter({hasText:'Sol'}).locator('[id*="_lnkSeleccionarMascota"]'));
  check(await c('ddlZona').inputValue()===''&&await c('ddlEspecialidad').isDisabled(),'Cambiar mascota reinicia el filtro dependiente');
  await page.screenshot({path:path.join(out,'selector-y-zona.png'),fullPage:true});
  await login(ctx,'otro@test.local');await go('Planes');
  check(await c('gvPlanes').locator('tr').count()>2,'ListadoPlanes muestra el catálogo disponible');
  const selected=[];
  for(const index of [0,1]){
   await go('Planes');await click(c('gvPlanes').locator('[id*="_lnkSeleccionarPlan"]').nth(index));
   const plan=new URL(page.url()).searchParams.get('plan');check(/^\d+$/.test(plan)&&await c('pnlDetalle').isVisible(),'PlanSeleccionado abre el detalle del plan de la fila '+index);selected.push(plan);
  }
  check(selected[0]!==selected[1],'Seleccionar otra fila no conserva un identificador anterior');
  await page.screenshot({path:path.join(out,'listado-plan-seleccionado.png'),fullPage:true});
  await login(ctx,'webmaster@vitapet.local');await go('BaseDatos');await click('btnVerificar');
  check((await page.locator('body').innerText()).includes('No se detectaron errores de integridad.'),'Los cinco controles conservan la integridad de la base cifrada');
  check(!errors.length,'Sin errores JavaScript durante las pruebas de controles');await ctx.close();
  fs.writeFileSync(path.join(out,'controles-resultados.json'),JSON.stringify({checks:results.length,controls:['ValidacionEmail','ValidacionNumerica','SelectorMascota','FiltroZona','ListadoPlanes'],results},null,2));
  console.log('TOTAL CONTROLES: '+results.length+' comprobaciones');
 }catch(e){if(page&&!page.isClosed()){try{await page.waitForLoadState('load');fs.writeFileSync(path.join(out,'controles-failure.html'),await page.content());await page.screenshot({path:path.join(out,'controles-failure.png'),fullPage:true});}catch{}}throw e;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
