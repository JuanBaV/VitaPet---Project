using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.SessionState;
using VitaPet.SECURITY;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;
using VitaPet.Pruebas;

static class CifradoTests
{
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks++; Console.WriteLine("PASS " + text); }
    static object Scalar(string sql, bool abrirClave = false)
    {
        using (var c = abrirClave ? new ConexionDAL().CrearConexion() : new SqlConnection(ConfigurationManager.ConnectionStrings["VitaPetConnection"].ConnectionString))
        using (var cmd = new SqlCommand(sql, c)) { c.Open(); return cmd.ExecuteScalar(); }
    }
    static void Rechaza(Action action, string text)
    { bool rechazo=false; try { action(); } catch (ConfigurationErrorsException) { rechazo=true; } catch (SqlException) { rechazo=true; } Check(rechazo,text); }

    static void Sesion(int id)
    {
        var ctx=new HttpContext(new HttpRequest("","http://localhost/",""),new HttpResponse(new StringWriter()));
        SessionStateUtility.AddHttpSessionStateToContext(ctx,new HttpSessionStateContainer(Guid.NewGuid().ToString(),new SessionStateItemCollection(),new HttpStaticObjectsCollection(),20,true,HttpCookieMode.UseCookies,SessionStateMode.InProc,false));
        HttpContext.Current=ctx;SessionManager.Instancia.IniciarSesion(new UsuarioDAL().ObtenerPorId(id));
    }

    static PagoBE Pagar()
    {
        var contratos=new ContratacionPlanBLL();var detalle=contratos.ConsultarDetalle(contratos.ConsultarPlanes()[0].IdPlan);
        var contrato=contratos.PrepararContratacion(detalle.Plan.IdPlan,detalle.Plan.Version,detalle.CantidadMascotas);
        return new GestionPagoBLL().Pagar(contrato.IdSuscripcion,contrato.ImporteUSD,new DatosPagoBE {MedioPago=GestionPagoBLL.Debito,Titular="José Ñ 😺",NumeroTarjeta="1111111111111111",CodigoSeguridad="111",Vencimiento="12/2030",CorreoFactura="cifrado@example.test"});
    }

    public static void Run(string runtime)
    {
        string nombre = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["VitaPetConnection"].ConnectionString).InitialCatalog;
        if (!nombre.StartsWith("VitaPet_Verificacion_Rework_", StringComparison.Ordinal)) throw new Exception("Sólo base aislada.");
        string previo = Environment.GetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO");
        string archivo = Path.Combine(runtime,"cifrado.key");
        Environment.SetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO", archivo);
        var seguridad = new SecurityManager();
        var usuario = new UsuarioDAL().ObtenerPorId(1);
        var facturaAnterior=Pagar().Factura;
        Check(facturaAnterior!=null,"Factura previa disponible para comprobar la migración de datos Unicode");
        string xmlAnterior=new ContingenciaDatosDAL().ExportarXml();
        string referencias = (string)Scalar("SELECT STRING_AGG(CONVERT(varchar(max),huella),'|') WITHIN GROUP(ORDER BY tabla,fila,columna) FROM dbo.ReferenciaIntegridad");
        try
        {
            Rechaza(() => BasePruebas.Cifrar(),"Sin archivo de clave no se inicia la migración");
            Check(!BasePruebas.TieneCifrado,"El rechazo no cambia el esquema");
            byte[] bytes=new byte[32]; using(var rng=RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            string clave=Convert.ToBase64String(bytes);File.WriteAllText(archivo,clave);
            using(var tx = new OperacionIntegridadDAL())
            {
                BasePruebas.Cifrar();
                Check(seguridad.VerificarIntegridad(),"Migración completa e íntegra antes del commit");
            }
            Check(!BasePruebas.TieneCifrado,"Rollback revierte columnas, vistas, procedimientos y clave SQL");
            BasePruebas.Cifrar();
            BasePruebas.Cifrar();
            Check(seguridad.VerificarIntegridad(),"Migración aplicada una sola vez; integridad conservada");
            Check(((byte[])Scalar("SELECT dbo.fn_HmacDatos(0x4869205468657265,0x0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b0b)",true)).SequenceEqual(Enumerable.Range(0,32).Select(i=>Convert.ToByte("b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7".Substring(i*2,2),16))),"HMAC-SHA256 coincide con el vector oficial RFC 4231");
            Check(referencias==(string)Scalar("SELECT STRING_AGG(CONVERT(varchar(max),huella),'|') WITHIN GROUP(ORDER BY tabla,fila,columna) FROM dbo.ReferenciaIntegridad"),"No se recalcularon las referencias para aceptar los datos migrados");
            Check((string)Scalar("SELECT nombre FROM dbo.Usuario WHERE idUsuario=1")!=usuario.Nombre,"Tabla física no guarda el nombre en texto plano");
            Check((string)Scalar("SELECT contrasena FROM dbo.Usuario WHERE idUsuario=1")==usuario.Contrasena,"La contraseña conserva su hash");
            Check(Scalar("SELECT nombre FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=1")==DBNull.Value,"Una sesión SQL sin clave no descifra las vistas");
            var recuperado=new UsuarioDAL().ObtenerPorCorreo(usuario.CorreoElectronico);
            Check(recuperado!=null&&recuperado.Nombre==usuario.Nombre,"Login busca el correo y recupera el nombre original");
            Check(new UsuarioDAL().ListarUsuarios(new FiltroUsuariosBE {Nombre=usuario.Nombre.Substring(0,2)}).Usuarios.Any(u=>u.IdUsuario==1),"Filtro por nombre funciona sobre datos cifrados");
            Check(new FacturaDAL().Obtener(1,facturaAnterior.IdSuscripcion).Titular=="José Ñ 😺","La migración conserva el titular Unicode de una factura existente");
            Sesion(4);
            var nuevoPago=Pagar();
            Check(nuevoPago.Factura!=null&&nuevoPago.Factura.Titular=="José Ñ 😺"&&nuevoPago.Factura.CorreoFactura=="cifrado@example.test","Pago nuevo cifra titular y destinatario; factura recupera ambos");
            Check(new FacturaBLL().Descargar(nuevoPago.IdSuscripcion).Length>1000,"PDF de la factura se genera después del cifrado");
            var registro=new RegistroClienteBE {Nombre="Ana",Apellido="Pérez",CorreoElectronico="ana.cifrado@example.test",Telefono="1122334455",Contrasena="Prueba2026!"};
            int alta=new ClienteBLL().RegistrarCliente(registro);
            Check(new UsuarioDAL().ObtenerPorCorreo(registro.CorreoElectronico).IdUsuario==alta,"Registro público sigue creando usuarios y clientes cifrados");
            bool duplicado=false;try{new ClienteBLL().RegistrarCliente(registro);}catch(RegistroClienteException e){duplicado=e.Codigo=="ER001";}
            Check(duplicado,"Correo duplicado se rechaza aunque el cifrado sea aleatorio");
            Sesion(3);
            var edicion=new UsuarioBLL();var version=edicion.PrepararOperacionUsuario(alta,OperacionUsuario.Modificar);
            edicion.ModificarUsuario(alta,version.Version,new CambiosUsuarioBE {Nombre="Ana Actualizada",CorreoElectronico="ana.nueva@example.test",IdRol=version.Usuario.IdRol,Estado=true});
            Check(new UsuarioDAL().ObtenerPorCorreo("ana.nueva@example.test").Nombre=="Ana Actualizada","Modificación de usuario mantiene cifrados nombre y correo");
            int altaWm=new AltaUsuarioBLL().Registrar(new RegistroClienteBE {Nombre="Alta",Apellido="Cifrada",CorreoElectronico="alta.cifrada@example.test",Telefono="1122334455",Contrasena="Prueba2026!"},version.Usuario.IdRol);
            Check(new UsuarioDAL().ObtenerPorId(altaWm).Nombre=="Alta Cifrada","Alta del Webmaster usa el mismo cifrado");
            Sesion(2);var empresas=new EmpresaBLL();
            var datosEmpresa=new DatosEmpresaBE {RazonSocial="Empresa Ñ",Cuit="30712345678",Direccion="Calle 123",Telefono="1122334455"};
            var empresa=empresas.Registrar(datosEmpresa);
            Check(empresas.Consultar(empresa.IdEmpresa).Cuit==datosEmpresa.Cuit,"Empresa cifra y recupera CUIT, domicilio, teléfono y razón social");
            duplicado=false;try{empresas.Registrar(datosEmpresa);}catch(EmpresaException e){duplicado=e.Codigo=="ER068";}
            Check(duplicado,"CUIT duplicado se rechaza con almacenamiento cifrado");
            datosEmpresa.Direccion="Otra calle 456";empresas.Actualizar(empresa.IdEmpresa,datosEmpresa);
            Check(empresas.Consultar(empresa.IdEmpresa).Direccion==datosEmpresa.Direccion,"Modificación de empresa vuelve a cifrar los campos");
            Check(CifradoDatosDAL.Diagnosticar().Count==0&&seguridad.VerificarIntegridad(),"Los 14 campos sensibles conservan cifrado válido e integridad tras las escrituras");
            using(var tx=new OperacionIntegridadDAL())
            {
                Scalar("UPDATE dbo.DetallePago SET correoFactura='VP1:00'; SELECT 1");
                Check(!seguridad.VerificarIntegridad(),"Ciphertext adulterado no pasa la verificación");
            }
            using(var tx=new OperacionIntegridadDAL())
            {
                Scalar("DELETE dbo.__SchemaMigrations WHERE migrationId='034_CifradoDatosPersonales.sql'; SELECT 1");
                Check(!seguridad.VerificarIntegridad(),"Eliminar la marca de migración de cifrado se detecta");
            }
            string texto="Á Ñ 中文 😺";
            using(var c=new ConexionDAL().CrearConexion())using(var cmd=new SqlCommand("SELECT dbo.fn_DescifrarDato(dbo.fn_CifrarDato(@v,N'prueba'),N'prueba')",c))
            {cmd.Parameters.AddWithValue("@v",texto);c.Open();Check((string)cmd.ExecuteScalar()==texto,"Cifrado conserva Unicode");}
            Check((bool)Scalar("SELECT CONVERT(bit,CASE WHEN dbo.fn_CifrarDato(N'igual',N'prueba')<>dbo.fn_CifrarDato(N'igual',N'prueba') THEN 1 ELSE 0 END)",true),"Cada cifrado usa aleatoriedad aunque el dato sea igual");
            Check(Scalar("SELECT dbo.fn_DescifrarDato(dbo.fn_CifrarDato(N'dato',N'campo1'),N'campo2')",true)==DBNull.Value,"El autenticador impide trasladar ciphertext entre campos");
            Check(Scalar("DECLARE @v varchar(2048)=dbo.fn_CifrarDato(N'dato',N'campo1'); SELECT dbo.fn_DescifrarDato(STUFF(@v,5,64,REPLICATE('Z',64)),N'campo1')",true)==DBNull.Value,"Un MAC malformado se rechaza antes del descifrado");
            string copia=Path.Combine(runtime,"clave-importada.key");File.Copy(archivo,copia);
            Environment.SetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO",copia);
            Check(seguridad.VerificarIntegridad(),"La misma clave importada desde otro archivo permite leer toda la base");
            File.WriteAllText(copia,Convert.ToBase64String(new byte[32]));
            Rechaza(()=>new UsuarioDAL().ObtenerPorId(1),"Otra clave se rechaza incluso con conexiones del pool");
            File.Delete(copia);
            Rechaza(()=>new UsuarioDAL().ObtenerPorId(1),"No hay lectura en texto plano si falta la clave");
            Environment.SetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO",archivo);
            string temporal="VitaPet_Verificacion_"+Guid.NewGuid().ToString("N");
            var backup=new BackupDAL();
            try
            {
                EsquemaDAL.CrearTemporal(temporal);
                new ContingenciaDatosDAL().ImportarTemporal(temporal,new ContingenciaDatosDAL().ExportarXml());
                using(ConexionDAL.UsarBaseTemporal(temporal))
                    Check(seguridad.VerificarIntegridad()&&new UsuarioDAL().ObtenerPorCorreo("ana.nueva@example.test")!=null,"Recuperación XML en base temporal reconstruye la clave SQL desde el mismo archivo");
            }
            finally {backup.EliminarTemporal(temporal);}
            temporal="VitaPet_Verificacion_"+Guid.NewGuid().ToString("N");
            try
            {
                EsquemaDAL.CrearTemporal(temporal);
                bool rechazo=false;try{new ContingenciaDatosDAL().ImportarTemporal(temporal,xmlAnterior);}catch(InvalidDataException){rechazo=true;}
                Check(rechazo&&seguridad.VerificarIntegridad(),"El XML anterior al esquema cifrado se rechaza sin convertirlo ni modificar la base actual");
            }
            finally {backup.EliminarTemporal(temporal);}
            string carpeta=Path.Combine(Directory.GetCurrentDirectory(),"VitaPet Rework","App_Data","Backups");
            string bak=Path.Combine(carpeta,nombre+".bak");
            try
            {
                backup.Crear(bak);backup.VerificarArchivo(bak);temporal=backup.CrearTemporal(bak);
                try
                {
                    using(ConexionDAL.UsarBaseTemporal(temporal))
                        Check(seguridad.VerificarIntegridad()&&new UsuarioDAL().ObtenerPorCorreo("ana.nueva@example.test")!=null,"Backup SQL restaurado en otra base se descifra con el mismo archivo de clave");
                }
                finally {backup.EliminarTemporal(temporal);}
            }
            finally {if(File.Exists(bak))File.Delete(bak);}
            Console.WriteLine("TOTAL: "+checks+" verificaciones de cifrado");
        }
        finally { Environment.SetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO",previo); }
    }
}
