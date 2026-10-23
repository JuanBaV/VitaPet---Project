const {chromium}=require('C:/Users/juanb/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs'),path=require('path'),{execFileSync}=require('child_process');
(async()=>{
 const file=process.argv[2],env=JSON.parse(fs.readFileSync(file,'utf8').replace(/^\uFEFF/,''));
 if(!/^VitaPet_Verificacion_Rework_[a-f0-9]{32}$/.test(env.Database))throw Error('Se requiere una base aislada');
 // El recorrido existente deja una contratación pagada para consultar su cobertura.
 execFileSync(process.execPath,[path.join(__dirname,'Web.cjs'),file],{stdio:'inherit'});
 const out=path.dirname(file),browser=await chromium.launch({headless:true,channel:'msedge'}),results=[],errors=[];
 const ctx=await browser.newContext({viewport:{width:1440,height:1000}}),page=await ctx.newPage();
 page.on('pageerror',e=>errors.push(e.message));
 const check=(ok,text)=>{if(!ok)throw Error(text);results.push(text);console.log('PASS '+text);};
 const c=id=>page.locator('[id$="_'+id+'"], [id$="_'+id+'_0"]');
 const click=async locator=>{await Promise.all([page.waitForNavigation(),(typeof locator==='string'?c(locator):locator).click()]);};
 const go=async url=>{const r=await page.goto(env.Url+url);check(r&&r.status()===200,url+': HTTP 200');};
 const login=async email=>{
  await ctx.clearCookies();await go('Login');await c('txtCorreo').fill(email);await c('txtContrasena').fill('Prueba2026!');await click('btnIngresar');
  check(new URL(page.url()).pathname.endsWith('/Home'),'Sesión de prueba: '+email);
 };
 const namespace='https://vitapet.local/services/';
 const xml=async text=>page.evaluate(text=>{
  const d=new DOMParser().parseFromString(text,'application/xml');
  if(d.querySelector('parsererror'))throw Error('XML inválido: '+text.slice(0,350));
  const all=name=>Array.from(d.getElementsByTagNameNS('*',name));
  const values={};for(const e of Array.from(d.getElementsByTagName('*')))if(!e.children.length)(values[e.localName]??=[]).push(e.textContent);
  const rows={};for(const tag of ['DisponibilidadTurnoBE','TurnoBE','CoberturaBE','HistorialClinicoBE','DatosClinicosBE'])
   rows[tag]=all(tag).map(e=>Object.fromEntries(Array.from(e.children).map(x=>[x.localName,x.textContent])));
  const messages=Object.fromEntries(all('message').filter(e=>e.hasAttribute('name')).map(e=>[e.getAttribute('name'),e.firstElementChild?.getAttribute('element')?.split(':').pop()]));
  const inputs={};for(const p of all('portType'))for(const op of Array.from(p.children))inputs[op.getAttribute('name')]=messages[op.getElementsByTagNameNS('*','input')[0]?.getAttribute('message')?.split(':').pop()];
  const operations=[];for(const op of all('operation')){
   const soap=Array.from(op.children).find(e=>e.namespaceURI==='http://schemas.xmlsoap.org/wsdl/soap/'&&e.localName==='operation');
   if(soap)operations.push({name:op.getAttribute('name'),action:soap.getAttribute('soapAction'),body:inputs[op.getAttribute('name')]});
  }
  return {values,rows,operations,fault:all('Fault').length>0};
 },text);
 const value=(r,name)=>r.values[name]?.[0];
 const services=[
  {file:'TurnoService.asmx',methods:['ObtenerDisponibilidadPorZona','VerificarDisponibilidadTurno','RegistrarTurno','ObtenerTurnosMascota']},
  {file:'HistorialClínicoService.asmx',methods:['ObtenerHistorialClinico','ObtenerDatosClinicos']},
  {file:'CoberturaService.asmx',methods:['ObtenerCobertura','ObtenerCoberturaCuenta']}
 ];
 const operations={};
 const escape=s=>String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
 const soap=async(name,params={},request=ctx.request)=>{
  const op=operations[name];if(!op)throw Error('Operación no publicada: '+name);
  const data='<?xml version="1.0" encoding="utf-8"?><soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><'+op.body+' xmlns="'+namespace+'">'+Object.entries(params).map(([k,v])=>'<'+k+'>'+escape(v)+'</'+k+'>').join('')+'</'+op.body+'></soap:Body></soap:Envelope>';
  const response=await request.post(env.Url+'WebServices/'+encodeURIComponent(op.file),{headers:{'Content-Type':'text/xml; charset=utf-8',SOAPAction:'"'+op.action+'"'},data});
  const body=await response.text();
  fs.writeFileSync(path.join(out,'soap-ultima-respuesta.xml'),body);
  check((response.headers()['content-type']||'').includes('xml'),name+': respuesta XML SOAP');
  return {...await xml(body),status:response.status(),raw:body};
 };
 const ok=(r,name)=>check(r.status===200&&!r.fault&&!value(r,'CodigoError'),name+': respuesta correcta');
 const error=(r,code,name)=>check(r.status===200&&!r.fault&&value(r,'CodigoError')===code,name+': '+code);
 try{
  await go('Login');
  for(const s of services){
   const response=await ctx.request.get(env.Url+'WebServices/'+encodeURIComponent(s.file)+'?WSDL'),body=await response.text();
   check(response.status()===200,s.file+': WSDL disponible');const contract=await xml(body);
   fs.writeFileSync(path.join(out,s.file+'.wsdl'),body);
   check(contract.operations.length===s.methods.length,s.file+': publica exactamente '+s.methods.length+' operaciones SOAP');
   for(const method of s.methods){const op=contract.operations.find(o=>o.name===method);check(op&&op.action&&op.body,s.file+': contrato de '+method);operations[method]={...op,file:s.file};}
  }
  const samples={ObtenerDisponibilidadPorZona:{idMascota:1,zona:'CABA'},VerificarDisponibilidadTurno:{idMascota:1,idAgendaTurno:1,fecha:'2030-01-01T00:00:00',horario:'09:00'},RegistrarTurno:{idMascota:1,idAgendaTurno:1,fecha:'2030-01-01T00:00:00',horario:'09:00'},ObtenerTurnosMascota:{idMascota:1},ObtenerHistorialClinico:{idMascota:1},ObtenerDatosClinicos:{idMascota:1},ObtenerCobertura:{idPlan:1},ObtenerCoberturaCuenta:{}};
  for(const email of [null,'administrador@vitapet.local','webmaster@vitapet.local']){
   if(email)await login(email);else await ctx.clearCookies();
   for(const name of Object.keys(operations)){
    const r=await soap(name,samples[name]);check(r.status===500&&r.fault&&value(r,'faultcode').endsWith(':Client')&&value(r,'faultstring').includes('Acceso denegado.'),name+': deniega '+(email||'sin sesión'));
    check(!r.raw.includes('Historia propia')&&!r.raw.includes('No divulgar')&&!r.raw.includes('System.Data.SqlClient'),name+': rechazo sin datos clínicos ni detalles SQL');
   }
  }
  await login('cliente@vitapet.local');
  let r=await soap('ObtenerCoberturaCuenta');ok(r,'Cobertura propia');const plan=value(r,'IdPlan'),coverage=r.rows.CoberturaBE;
  check(/^\d+$/.test(plan)&&value(r,'NombrePlan')==='Plan Esencial'&&r.rows.CoberturaBE.length>0,'La cobertura devuelve el plan contratado y sus prestaciones');
  r=await soap('ObtenerCobertura',{idPlan:plan});ok(r,'Catálogo de cobertura');check(r.rows.CoberturaBE.length>0,'El catálogo contiene prestaciones y detalles');
  error(await soap('ObtenerCobertura',{idPlan:0}),'ER050','Plan inexistente');
  for(const [name,tag,field,expected] of [['ObtenerHistorialClinico','HistorialClinicoBE','Diagnostico','Control SOAP Luna'],['ObtenerDatosClinicos','DatosClinicosBE','Observaciones','Dato SOAP Luna']]){
   r=await soap(name,{idMascota:1});ok(r,name);check(value(r,'Nombre')==='Luna'&&r.rows[tag].length===1&&r.rows[tag][0][field]===expected,name+': devuelve únicamente el registro de Luna');
   check(!r.raw.includes('Ajena')&&!r.raw.includes('No divulgar'),name+': no mezcla registros de otras mascotas');
   for(const id of [0,2,4,999999])error(await soap(name,{idMascota:id}),'ER011',name+': mascota inválida, vacía o ajena '+id);
  }
  r=await soap('ObtenerDisponibilidadPorZona',{idMascota:1,zona:'CABA'});ok(r,'Disponibilidad de Luna');
  const slots=r.rows.DisponibilidadTurnoBE;check(slots.length>0&&slots.every(s=>s.Zona==='CABA'),'Devuelve horarios de la zona elegida');
  const slot=slots[0],selection={idMascota:1,idAgendaTurno:slot.IdAgendaTurno,fecha:slot.Fecha,horario:slot.Horario};
  r=await soap('VerificarDisponibilidadTurno',selection);ok(r,'Verifica el horario disponible');check(value(r,'Horario')===slot.Horario,'Verificación conserva el horario seleccionado');
  error(await soap('ObtenerDisponibilidadPorZona',{idMascota:1,zona:''}),'ER002','Zona obligatoria');
  error(await soap('ObtenerDisponibilidadPorZona',{idMascota:1,zona:'Inexistente'}),'ER025','Zona inválida');
  for(const name of ['VerificarDisponibilidadTurno','RegistrarTurno']){
   error(await soap(name,{...selection,idMascota:0}),'ER002',name+': mascota obligatoria');
   error(await soap(name,{...selection,horario:'99:99'}),'ER025',name+': formato del horario');
   error(await soap(name,{...selection,idMascota:4}),'ER011',name+': mascota ajena');
  }
  error(await soap('ObtenerDisponibilidadPorZona',{idMascota:4,zona:'CABA'}),'ER011','Sin disponibilidad para una mascota ajena');
  error(await soap('ObtenerTurnosMascota',{idMascota:1}),'ER014','Consulta inicial sin turnos');
  r=await soap('RegistrarTurno',selection);ok(r,'Registro de turno por SOAP');const turn=value(r,'IdTurno');
  check(/^\d+$/.test(turn)&&value(r,'Estado')==='Confirmado'&&value(r,'IdMascota')==='1','El registro devuelve un turno confirmado de Luna');
  error(await soap('RegistrarTurno',selection),'ER013','Repetir la solicitud no duplica el turno');
  error(await soap('VerificarDisponibilidadTurno',selection),'ER013','El horario ocupado deja de estar disponible');
  r=await soap('ObtenerTurnosMascota',{idMascota:1});ok(r,'Consulta de turnos registrados');check(r.rows.TurnoBE.length===1&&r.rows.TurnoBE[0].IdTurno===turn,'La consulta devuelve exactamente el turno creado');
  error(await soap('ObtenerTurnosMascota',{idMascota:4}),'ER011','No consulta turnos de una mascota ajena');
  // Los formularios consumen las mismas clases del servicio desde el servidor.
  for(const [url,grid,text] of [['HistorialClinico?id=1','gvHistorialClinico','Control SOAP Luna'],['DatosClinicos?id=1','gvDatosClinicos','Dato SOAP Luna']]){
   await go(url);check((await c(grid).innerText()).includes(text),'Página consumidora muestra los datos: '+url);await page.screenshot({path:path.join(out,grid+'.png'),fullPage:true});
  }
  await go('ConsultaCobertura');const coverageText=await c('gvCoberturas').innerText();
  check(coverage.every(row=>coverageText.includes(row.Prestacion)&&coverageText.includes(row.Detalle))&&await c('gvCoberturas').locator('tbody tr').count()===coverage.length,'Página consumidora coincide con las prestaciones y detalles del servicio SOAP');
  await go('Turnos?vista=consulta');await click(c('gvMascotas').locator('tr').filter({hasText:'Luna'}).locator('[id*="_lnkSeleccionarMascota"]'));
  check((await c('gvTurnos').innerText()).includes(slot.Horario),'La página consulta el turno creado mediante SOAP');await page.screenshot({path:path.join(out,'turno-soap.png'),fullPage:true});
  await login('otro@test.local');error(await soap('ObtenerCoberturaCuenta'),'ER008','Otro cliente sin contratación no recibe cobertura ajena');
  for(const name of ['ObtenerHistorialClinico','ObtenerDatosClinicos','ObtenerTurnosMascota'])error(await soap(name,{idMascota:1}),'ER011',name+': tampoco permite el acceso inverso entre clientes');
  r=await soap('ObtenerHistorialClinico',{idMascota:4});ok(r,'Historial del segundo cliente');check(value(r,'Diagnostico')==='Historia reservada Ajena'&&!r.raw.includes('Control SOAP Luna'),'El segundo cliente recibe su propio historial');
  await login('webmaster@vitapet.local');await go('BaseDatos');await click('btnVerificar');
  check((await page.locator('body').innerText()).includes('No se detectaron errores de integridad.'),'SOAP y formularios conservan DVH, DVV y referencias en la base cifrada');
  check(!errors.length,'Sin errores JavaScript en los consumidores');
  fs.writeFileSync(path.join(out,'servicios-resultados.json'),JSON.stringify({checks:results.length,operations,results},null,2));
  console.log('TOTAL SERVICIOS: '+results.length+' comprobaciones');
 }catch(e){fs.writeFileSync(path.join(out,'servicios-failure.html'),await page.content());await page.screenshot({path:path.join(out,'servicios-failure.png'),fullPage:true});throw e;}
 finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1);});
