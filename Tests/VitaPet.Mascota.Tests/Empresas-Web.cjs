const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),cp=require('child_process');
(async()=>{
 const env=JSON.parse(fs.readFileSync(process.argv[2],'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Mascota_[a-f0-9]{32}$/.test(env.Database))throw Error('Only isolated databases');
 const out=path.dirname(process.argv[2]);let checks=0;
 const check=(ok,msg)=>{if(!ok)throw Error(msg);console.log('PASS '+msg);checks++;};
 const sql=q=>cp.execFileSync('C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/SQLCMD.EXE',['-S','localhost','-E','-d',env.Database,'-b','-h','-1','-Q',/^CREATE /i.test(q)?q:'SET NOCOUNT ON; '+q],{encoding:'utf8'}).trim();
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const context=await browser.newContext({viewport:{width:1440,height:1100}}),page=await context.newPage();page.setDefaultTimeout(20000);
 const c=(id,p=page)=>p.locator('[id$="_'+id+'"]');
 const click=(id,p=page)=>Promise.all([p.waitForNavigation(),c(id,p).click()]);
 const login=async email=>{await page.goto(env.Url+'Login.aspx');await c('txtCorreo').fill(email);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');};
 const fill=async(name='Empresa & Compañía',cuit='20-12345678-9',p=page)=>{await c('txtRazonSocial',p).fill(name);await c('txtCuit',p).fill(cuit);await c('txtDireccion',p).fill('Av. Prueba 100');await c('txtTelefono',p).fill('+54 (11) 5555-0101');};
 const select=async()=>Promise.all([page.waitForNavigation(),c('gvEmpresas').getByRole('row').filter({hasText:'Empresa & Compañía'}).getByRole('link',{name:'Seleccionar',exact:true}).click()]);
 const error=async code=>(await c('pnlMensaje').innerText()).includes(code);
 const routes=['Empresas.aspx','AltaEmpresa.aspx','ConsultaEmpresa.aspx?id=1','ModificarEmpresa.aspx?id=1'];
 try{
  for(let retry=0;;retry++){try{await page.goto(env.Url+'Login.aspx');break;}catch(e){if(retry===15)throw e;await new Promise(r=>setTimeout(r,1000));}}
  for(const route of routes){await page.goto(env.Url+route);check(/\/Login(?:\.aspx)?$/.test(new URL(page.url()).pathname),'Requiere sesion: '+route);}
  for(const email of ['cliente@vitapet.local','webmaster@vitapet.local']){await login(email);for(const route of routes){await page.goto(env.Url+route);check(/AccesoDenegado/.test(page.url()),'Rol no autorizado: '+email+' '+route);}}
  await login('administrador@vitapet.local');await click('lnkGestionEmpresas');
  check(await page.locator('h1').innerText()==='Empresas asociadas'&&await error('ER017')&&await c('btnConsultar').isDisabled()&&await c('btnModificar').isDisabled(),'Acceso del administrador y listado vacio con acciones deshabilitadas');
  await click('lnkAltaEmpresa');await click('btnRegistrarEmpresa');check(await error('ER002'),'Alta incompleta informa ER002');
  await fill();await c('txtCuit').fill('123');await click('btnRegistrarEmpresa');check(await error('ER025')&&await c('txtRazonSocial').inputValue()==='Empresa & Compañía','Formato invalido informa ER025 y conserva los datos');
  await c('txtCuit').fill('20-12345678-9');await click('btnRegistrarEmpresa');
  check(await c('lblConfirmacion').isVisible()&&(await c('lblConfirmacion').innerText()).includes('Empresa & Compañía')&&await c('btnRegistrarEmpresa').isDisabled(),'Alta muestra identificador, razon social y deshabilita Registrar');
  check(sql('SELECT cuit FROM dbo.Empresa')==='20123456789'&&Number(sql('SELECT COUNT(*) FROM dbo.Empresa WHERE estado=1'))===1,'Alta persiste CUIT normalizado y estado activo');
  await page.screenshot({path:path.join(out,'alta-empresa.png'),fullPage:true});await page.reload();check(Number(sql('SELECT COUNT(*) FROM dbo.Empresa'))===1,'Refrescar despues del alta no duplica el registro');
  await page.goto(env.Url+'AltaEmpresa.aspx');await fill('Duplicada','20123456789');await click('btnRegistrarEmpresa');check(await error('ER068')&&Number(sql('SELECT COUNT(*) FROM dbo.Empresa'))===1,'Duplicado de CUIT informa ER068');
  await click('lnkGestionEmpresas');await select();check(await c('btnConsultar').isEnabled()&&await c('btnModificar').isEnabled()&&await c('btnModificar').inputValue()==='Modificar'&&await page.locator('tr.empresa-seleccionada').count()===1,'Seleccion resalta la empresa y habilita Consultar y Modificar');
  await page.screenshot({path:path.join(out,'gestion-empresas.png'),fullPage:true});await click('btnConsultar');
  check(await c('lblRazonSocial').innerText()==='Empresa & Compañía'&&await page.locator('main input[type=text],main select,main textarea').count()===0,'Consulta presenta seis datos en modo de solo lectura');
  await page.screenshot({path:path.join(out,'consulta-empresa.png'),fullPage:true});
  await click('lnkGestionEmpresas');await select();await click('btnModificar');const editUrl=page.url();
  check(await c('lblEstado').innerText()==='Activo'&&await c('lblIdEmpresa').innerText()==='1'&&await c('lblConfirmacion').count()===0,'Modificacion conserva identificador y estado de solo lectura; sin H');
  await c('txtDireccion').fill('Direccion modificada 200');await click('btnGuardarCambios');
  check(sql('SELECT direccion FROM dbo.Empresa WHERE idEmpresa=1')==='Direccion modificada 200'&&await c('txtDireccion').inputValue()==='Direccion modificada 200'&&await c('lblConfirmacion').count()===0,'Guardar actualiza los datos sin confirmacion visual');
  const audits=Number(sql("SELECT COUNT(*) FROM dbo.Bitacora WHERE accion='Modificación de empresa asociada'"));await page.reload();check(Number(sql("SELECT COUNT(*) FROM dbo.Bitacora WHERE accion='Modificación de empresa asociada'"))===audits,'Refrescar despues de guardar no repite la modificacion');
  await page.screenshot({path:path.join(out,'modificacion-empresa.png'),fullPage:true});
  const other=await context.newPage();await other.goto(editUrl);await c('txtTelefono').fill('111111');await click('btnGuardarCambios');await c('txtTelefono',other).fill('222222');await click('btnGuardarCambios',other);
  check(sql('SELECT telefono FROM dbo.Empresa WHERE idEmpresa=1')==='222222'&&!(await other.locator('main').innerText()).includes('ER071'),'Dos ediciones no usan control de versiones ER071');await other.close();
  await page.goto(env.Url+'AltaEmpresa.aspx');await fill('Otra empresa','30-12345678-9');await click('btnRegistrarEmpresa');await page.goto(editUrl);await c('txtCuit').fill('30123456789');await click('btnGuardarCambios');check(await error('ER068')&&sql('SELECT cuit FROM dbo.Empresa WHERE idEmpresa=1')==='20123456789','Modificacion rechaza CUIT de otra empresa');
  for(const route of ['ConsultaEmpresa.aspx?id=abc','ModificarEmpresa.aspx?id=999999']){await page.goto(env.Url+route);check(await error('ER011')&&await c('pnlDatos').count()===0,'Empresa no disponible: '+route);}
  await page.goto(editUrl);await Promise.all([page.waitForNavigation(),c('btnIdiomaEn').evaluate(el=>el.click())]);check(await page.locator('h1').innerText()==='Edit company'&&await c('btnGuardarCambios').inputValue()==='Save changes','Pantalla de empresa traducida al ingles');
  await page.setViewportSize({width:390,height:844});check(await page.locator('.empresa-campos').evaluate(el=>el.scrollWidth<=el.clientWidth+1),'Formulario adaptable a movil sin desbordamiento');await page.screenshot({path:path.join(out,'empresa-movil.png'),fullPage:true});
  await Promise.all([page.waitForNavigation(),c('btnIdiomaEs').evaluate(el=>el.click())]);await page.setViewportSize({width:1440,height:1100});
  sql("CREATE TRIGGER dbo.TestEmpresaWebAuditFail ON dbo.Bitacora AFTER INSERT AS THROW 50040,'Test audit failure',1;");
  try{await page.goto(env.Url+'Empresas.aspx');check(await error('ER033')&&await c('gvEmpresas').count()===0,'Falla de auditoria informa ER033 sin listado');await page.goto(env.Url+'AltaEmpresa.aspx');await fill('No guardar','30999999999');await click('btnRegistrarEmpresa');check(await error('ER040')&&Number(sql('SELECT COUNT(*) FROM dbo.Empresa'))===2,'Falla de auditoria en alta informa ER040 sin empresa parcial');}finally{sql('DROP TRIGGER dbo.TestEmpresaWebAuditFail');}
  sql("UPDATE dbo.Empresa SET direccion='Alterada sin integridad' WHERE idEmpresa=1");
  for(const route of ['Empresas.aspx','ConsultaEmpresa.aspx?id=1','ModificarEmpresa.aspx?id=1']){const response=await page.goto(env.Url+route);check(response.status()===200&&await error('ER023'),'Inconsistencia informa ER023 dentro de la pantalla: '+route);}
  await page.goto(env.Url+'AltaEmpresa.aspx');await fill('No guardar','30999999999');await click('btnRegistrarEmpresa');check(await error('ER023')&&Number(sql('SELECT COUNT(*) FROM dbo.Empresa'))===2,'Alta bloquea escritura e informa ER023 ante inconsistencia');
  fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify({checks,passed:true},null,2));console.log('TOTAL: '+checks+' browser checks passed');
 }catch(e){await page.screenshot({path:path.join(out,'failure.png'),fullPage:true}).catch(()=>{});throw e;}finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
