const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),{spawnSync}=require('child_process');
const file=process.argv[2],env=JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));
if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Isolated database required');
const pago=spawnSync(process.execPath,[path.join(__dirname,'Web.cjs'),file],{stdio:'inherit'});
if(pago.status!==0)process.exit(pago.status||1);
(async()=>{
 const browser=await chromium.launch({headless:true,channel:'msedge'});let checks=0,currentPage;
 const check=(ok,text)=>{if(!ok)throw Error(text);checks++;console.log('PASS '+text);};
 try{
  for(const js of [true,false]){
   const suffix=js?'conjs':'sinjs',ctx=await browser.newContext({javaScriptEnabled:js,viewport:{width:1440,height:1000}}),page=await ctx.newPage();
   currentPage=page;
   // La fila inicial de coberturas agrega el sufijo _0 a sus controles.
   const c=id=>page.locator('[id$="_'+id+'"], [id$="_'+id+'_0"]');
   // Se omite HTML5 para comprobar los validadores ASP.NET y el servidor.
   const go=async name=>{await page.goto(env.Url+name);await page.locator('form').evaluate(f=>f.noValidate=true);};
   const click=async id=>{await page.locator('form').evaluate(f=>f.noValidate=true);await c(id).click();await page.waitForLoadState('networkidle');};
   const fill=async fields=>{for(const [id,v] of Object.entries(fields)){await c(id).evaluate(e=>e.removeAttribute('maxlength'));await c(id).fill(v);await c(id).press('Tab');}};
   const visible=async(id,text)=>{await c(id).waitFor({state:'visible'});check(true,suffix+': '+text);};
   const login=async role=>{await ctx.clearCookies();await go('Login');await fill({txtCorreo:role+'@vitapet.local',txtContrasena:'Prueba2026!'});await click('btnIngresar');check(!page.url().includes('Login'),suffix+': login '+role);};
   const datos={txtNombre:'Prueba',txtApellido:'Validacion',txtCorreo:'alta.'+suffix+'@test.local',txtContrasena:'Aa1!aaaa',txtTelefono:'55555555'};
   async function probar({name,button,fields,required,invalid,success}){
    await click(button);
    for(const id of required)await visible(id,name+': obligatorio '+id);
    for(const [id,value,validator] of invalid){
     await fill(fields);await fill({[id]:value});await click(button);
     await visible(validator,name+': formato inválido '+id);
    }
    await page.screenshot({path:path.join(path.dirname(file),name+'-'+suffix+'-rechazo.png'),fullPage:true});
    await fill(fields);await click(button);await success();
    check(true,suffix+': '+name+' admite un alta válida');
   }
   await go('RegistroCliente');
   await probar({name:'registro-publico',button:'btnRegistrar',fields:datos,required:['rfvNombre','rfvApellido','rfvCorreo','rfvContrasena','rfvTelefono'],
    invalid:[['txtNombre','a'.repeat(61),'revNombre'],['txtApellido','a'.repeat(61),'revApellido'],['txtCorreo','sin-correo','revCorreo'],['txtContrasena','Aa1!aa','revContrasena'],['txtTelefono','abc','revTelefono']],
    success:async()=>{check((await page.locator('main').innerText()).includes('La cuenta se creó correctamente'),'Confirmación de registro público');}});
   await login('cliente');await go('RegistroMascota');
   await probar({name:'mascota',button:'btnRegistrar',fields:{txtNombre:'Validacion '+suffix,txtEspecie:'Perro',txtRaza:'Mestizo',dtpFechaNacimiento:'2020-01-01'},required:['rfvNombre','rfvEspecie','rfvRaza','rfvFechaNacimiento'],
    invalid:[['txtNombre','a'.repeat(61),'revNombre'],['txtEspecie','a'.repeat(41),'revEspecie'],['txtRaza','a'.repeat(61),'revRaza'],['dtpFechaNacimiento','2099-01-01','cvFechaNacimiento']],
    success:async()=>visible('lblConfirmacion','Mascota registrada')});
   // La selección de turno permanece bloqueada hasta completar mascota, zona, especialidad, fecha y horario.
   await go('Turnos');check(await c('btnConfirmarSolicitud').isDisabled(),suffix+': no confirma turnos sin selección completa');
   await login('administrador');await go('AltaEmpresa');
   await probar({name:'empresa',button:'btnRegistrarEmpresa',fields:{txtRazonSocial:'Validacion '+suffix,txtCuit:js?'20123456789':'20123456780',txtDireccion:'Calle Prueba 123',txtTelefono:'55555555'},required:['rfvRazonSocial','rfvCuit','rfvDireccion','rfvTelefono'],
    invalid:[['txtRazonSocial','a'.repeat(101),'revRazonSocial'],['txtCuit','123','revCuit'],['txtDireccion','a'.repeat(151),'revDireccion'],['txtTelefono','abc','revTelefono']],
    success:async()=>visible('lblConfirmacion','Empresa registrada')});
   await go('AltaPlan');
   await probar({name:'plan-coberturas',button:'btnRegistrarPlan',fields:{txtNombrePlan:'Validacion '+suffix,txtPrecioPlan:'25.50',txtDescripcionPlan:'Plan de prueba',txtPrestacion:'Consulta',txtDetalle:'Consulta veterinaria'},required:['rfvNombrePlan','rfvPrecioPlan','rfvDescripcionPlan','rfvPrestacion','rfvDetalle'],
    invalid:[['txtNombrePlan','a'.repeat(81),'revNombrePlan'],['txtPrecioPlan','-1','revPrecioPlan'],['txtDescripcionPlan','a'.repeat(256),'revDescripcionPlan'],['txtPrestacion','a'.repeat(101),'revPrestacion'],['txtDetalle','a'.repeat(256),'revDetalle']],
    success:async()=>check(new URL(page.url()).searchParams.has('resultado')&&await c('btnRegistrarPlan').isDisabled(),'Plan registrado y editor de confirmación bloqueado')});
   await go('RegistroClienteCorporativo');
   const empresa=page.locator('tr').filter({hasText:'Validacion '+suffix});
   const empresaId=(await empresa.locator('td').first().innerText()).trim();
   check(/^\d+$/.test(empresaId),'Identificador de empresa leído del listado');
   await go('RegistroClienteCorporativo?id='+empresaId);
   await page.locator('form').evaluate(f=>f.noValidate=true);
   check(await c('btnRegistrarCliente').isDisabled(),suffix+': alta corporativa requiere validar el correo');
   await click('btnValidarCorreo');await visible('rfvCorreo','Corporativo: correo obligatorio');
   await fill({txtCorreo:'sin-correo'});await click('btnValidarCorreo');await visible('revCorreo','Corporativo: formato de correo');
   await fill({txtCorreo:'corporativo.'+suffix+'@test.local'});await click('btnValidarCorreo');
   await probar({name:'corporativo',button:'btnRegistrarCliente',fields:{txtNombre:'Prueba',txtApellido:'Corporativo',txtTelefono:'55555555'},required:['rfvNombre','rfvApellido','rfvTelefono'],
    invalid:[['txtNombre','a'.repeat(61),'revNombre'],['txtApellido','a'.repeat(61),'revApellido'],['txtTelefono','abc','revTelefono']],
    success:async()=>visible('lblConfirmacion','Cliente corporativo registrado')});
   await login('webmaster');await go('AltaUsuario');
   await probar({name:'usuario',button:'btnCrear',fields:{...datos,txtCorreo:'usuario.'+suffix+'@test.local'},required:['rfvNombre','rfvApellido','rfvCorreo','rfvContrasena','rfvTelefono'],
    invalid:[['txtNombre','a'.repeat(61),'revNombre'],['txtApellido','a'.repeat(61),'revApellido'],['txtCorreo','sin-correo','revCorreo'],['txtContrasena','Aa1!aa','revContrasena'],['txtTelefono','abc','revTelefono']],
    success:async()=>check((await page.locator('main').innerText()).includes('El usuario se creó correctamente'),'Confirmación del alta de usuario')});
   await go('GestionPerfiles');await click('btnGuardar');await visible('rfvNombre','Perfil: nombre obligatorio');
   await fill({txtNombre:'Validación '+suffix});await click('btnGuardar');await visible('cvComponentes','Perfil: componentes obligatorios');
   await fill({txtNombre:'Perfil!'});await page.locator('[id$="_chkComponentes"] input[type=checkbox]').last().check();await click('btnGuardar');await visible('revNombre','Perfil: caracteres inválidos');
   await page.screenshot({path:path.join(path.dirname(file),'perfil-'+suffix+'-rechazo.png'),fullPage:true});
   await fill({txtNombre:'Validación '+suffix});await click('btnGuardar');
   check((await c('gvPerfiles').innerText()).includes('Validación '+suffix),suffix+': crea perfil con nombre válido y permisos');
   await click('btnNuevo');check(await c('txtNombre').inputValue()==='',suffix+': Nuevo perfil no queda bloqueado por validaciones');
   if(js){
    await page.locator('#btnSelectorIdioma').click();await click('btnIdiomaEn');
    await go('AltaUsuario');await click('btnCrear');check((await c('resumen').innerText()).includes('This field is required'), 'Validaciones de usuario en inglés');
    await go('GestionPerfiles');await click('btnGuardar');check((await c('vsPerfil').innerText()).includes('This field is required'),'Validaciones de perfil en inglés');
    await page.setViewportSize({width:390,height:844});
    check(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Mensajes de perfil sin desbordamiento en móvil');
    await page.screenshot({path:path.join(path.dirname(file),'perfil-ingles-movil.png'),fullPage:true});
    await page.setViewportSize({width:1440,height:1000});
    await page.locator('#btnSelectorIdioma').click();await click('btnIdiomaEs');
   }
   await ctx.close();
  }
  fs.writeFileSync(path.join(path.dirname(file),'altas-results.json'),JSON.stringify({checks,forms:7,javascript:[true,false]},null,2));
  console.log('TOTAL ALTAS: '+checks+' comprobaciones de siete formularios');
 }catch(error){
  if(currentPage&&!currentPage.isClosed()){
   fs.writeFileSync(path.join(path.dirname(file),'altas-failure.html'),await currentPage.content());
   console.error((await currentPage.locator('body').innerText()).slice(0,5000));
   await currentPage.screenshot({path:path.join(path.dirname(file),'altas-failure.png'),fullPage:true});
  }
  throw error;
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
