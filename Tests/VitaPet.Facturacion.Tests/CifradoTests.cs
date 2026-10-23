using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using VitaPet.BE;
using VitaPet.BLL;
using VitaPet.BLL.Seguridad;
using VitaPet.DAL;

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

    public static void Run(string runtime)
    {
        string nombre = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["VitaPetConnection"].ConnectionString).InitialCatalog;
        if (!nombre.StartsWith("VitaPet_Verificacion_Rework_", StringComparison.Ordinal)) throw new Exception("Sólo base aislada.");
        string previo = Environment.GetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO");
        string archivo = Path.Combine(runtime,"cifrado.key");
        Environment.SetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO", archivo);
        var seguridad = new SecurityManager();
        var usuario = new UsuarioDAL().ObtenerPorId(1);
        string referencias = (string)Scalar("SELECT STRING_AGG(CONVERT(varchar(max),huella),'|') WITHIN GROUP(ORDER BY tabla,fila,columna) FROM dbo.ReferenciaIntegridad");
        try
        {
            Rechaza(() => new MigracionCifradoBLL().Ejecutar(),"Sin archivo de clave no se inicia la migración");
            Check(!DatabaseMigrator.EstaAplicada(CifradoDatosDAL.Migracion),"El rechazo no cambia el esquema");
            byte[] bytes=new byte[32]; using(var rng=RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            string clave=Convert.ToBase64String(bytes);File.WriteAllText(archivo,clave);
            using(var tx = new OperacionIntegridadDAL())
            {
                new MigracionCifradoBLL().Ejecutar();
                Check(seguridad.VerificarIntegridad(),"Migración completa e íntegra antes del commit");
            }
            Check(!DatabaseMigrator.EstaAplicada(CifradoDatosDAL.Migracion),"Rollback revierte columnas, vistas, procedimientos y clave SQL");
            new MigracionCifradoBLL().Ejecutar();
            new MigracionCifradoBLL().Ejecutar();
            Check(seguridad.VerificarIntegridad(),"Migración aplicada una sola vez; integridad conservada");
            Check(referencias==(string)Scalar("SELECT STRING_AGG(CONVERT(varchar(max),huella),'|') WITHIN GROUP(ORDER BY tabla,fila,columna) FROM dbo.ReferenciaIntegridad"),"No se recalcularon las referencias para aceptar los datos migrados");
            Check((string)Scalar("SELECT nombre FROM dbo.Usuario WHERE idUsuario=1")!=usuario.Nombre,"Tabla física no guarda el nombre en texto plano");
            Check((string)Scalar("SELECT contrasena FROM dbo.Usuario WHERE idUsuario=1")==usuario.Contrasena,"La contraseña conserva su hash");
            Check(Scalar("SELECT nombre FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=1")==DBNull.Value,"Una sesión SQL sin clave no descifra las vistas");
            var recuperado=new UsuarioDAL().ObtenerPorCorreo(usuario.CorreoElectronico);
            Check(recuperado!=null&&recuperado.Nombre==usuario.Nombre,"Login busca el correo y recupera el nombre original");
            Check(new UsuarioDAL().ListarUsuarios(new FiltroUsuariosBE {Nombre=usuario.Nombre.Substring(0,2)}).Usuarios.Any(u=>u.IdUsuario==1),"Filtro por nombre funciona sobre datos cifrados");
            string texto="Á Ñ 中文 😺";
            using(var c=new ConexionDAL().CrearConexion())using(var cmd=new SqlCommand("SELECT dbo.fn_DescifrarDato(dbo.fn_CifrarDato(@v,N'prueba'),N'prueba')",c))
            {cmd.Parameters.AddWithValue("@v",texto);c.Open();Check((string)cmd.ExecuteScalar()==texto,"Cifrado conserva Unicode");}
            Check((bool)Scalar("SELECT CONVERT(bit,CASE WHEN dbo.fn_CifrarDato(N'igual',N'prueba')<>dbo.fn_CifrarDato(N'igual',N'prueba') THEN 1 ELSE 0 END)",true),"Cada cifrado usa aleatoriedad aunque el dato sea igual");
            Check(Scalar("SELECT dbo.fn_DescifrarDato(dbo.fn_CifrarDato(N'dato',N'campo1'),N'campo2')",true)==DBNull.Value,"El autenticador impide trasladar ciphertext entre campos");
            string copia=Path.Combine(runtime,"clave-importada.key");File.Copy(archivo,copia);
            Environment.SetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO",copia);
            Check(seguridad.VerificarIntegridad(),"La misma clave importada desde otro archivo permite leer toda la base");
            File.WriteAllText(copia,Convert.ToBase64String(new byte[32]));
            Rechaza(()=>new UsuarioDAL().ObtenerPorId(1),"Otra clave se rechaza incluso con conexiones del pool");
            File.Delete(copia);
            Rechaza(()=>new UsuarioDAL().ObtenerPorId(1),"No hay lectura en texto plano si falta la clave");
            Environment.SetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO",archivo);
            Console.WriteLine("TOTAL: "+checks+" verificaciones de cifrado");
        }
        finally { Environment.SetEnvironmentVariable("VITAPET_CLAVE_ARCHIVO",previo); }
    }
}
