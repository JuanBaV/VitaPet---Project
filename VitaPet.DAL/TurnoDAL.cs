using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;
namespace VitaPet.DAL
{
    public sealed class TurnoDAL
    {
        private List<T> Leer<T>(string sp,Action<SqlParameterCollection> parametros,Func<SqlDataReader,T> convertir)
        {
            using(var cn=new ConexionDAL().CrearConexion())
            using(var cmd=new SqlCommand(sp,cn))
            {
                cmd.CommandType=CommandType.StoredProcedure;parametros(cmd.Parameters);cn.Open();var lista=new List<T>();
                using(var r=cmd.ExecuteReader())while(r.Read())lista.Add(convertir(r));return lista;
            }
        }
        private static void Cuenta(SqlParameterCollection p,int usuario,int mascota)
        {p.Add("@idUsuario",SqlDbType.Int).Value=usuario;p.Add("@idMascota",SqlDbType.Int).Value=mascota;}
        private static void Seleccion(SqlParameterCollection p,int usuario,int mascota,int agenda,DateTime fecha,string horario)
        {
            Cuenta(p,usuario,mascota);p.Add("@idAgendaTurno",SqlDbType.Int).Value=agenda;
            p.Add("@fecha",SqlDbType.Date).Value=fecha.Date;p.Add("@horario",SqlDbType.VarChar,20).Value=horario;
        }
        private static DisponibilidadTurnoBE Disponible(SqlDataReader r)
        {return new DisponibilidadTurnoBE {IdAgendaTurno=(int)r["idAgendaTurno"],Fecha=(DateTime)r["fecha"],Horario=(string)r["horario"],Prestacion=(string)r["prestacion"],Zona=(string)r["zona"]};}
        private static TurnoBE Turno(SqlDataReader r)
        {return new TurnoBE {IdTurno=(int)r["idTurno"],IdMascota=(int)r["idMascota"],Fecha=(DateTime)r["fecha"],Horario=(string)r["horario"],Prestacion=(string)r["prestacion"],Zona=(string)r["zona"],Estado=(string)r["estado"]};}
        public List<int> InicializarAgenda(){return Leer("dbo.sp_AgendaTurno_Inicializar",p=>{},r=>r.GetInt32(0));}
        public List<string> Zonas(int usuario){return Leer("dbo.sp_AgendaTurno_ListarZonas",p=>p.Add("@idUsuario",SqlDbType.Int).Value=usuario,r=>r.GetString(0));}
        public List<DisponibilidadTurnoBE> Disponibilidad(int usuario,int mascota,string zona)
        {return Leer("dbo.sp_Turno_ConsultarDisponibilidad",p=>{Cuenta(p,usuario,mascota);p.Add("@zona",SqlDbType.VarChar,60).Value=zona;},Disponible);}
        public DisponibilidadTurnoBE Verificar(int usuario,int mascota,int agenda,DateTime fecha,string horario)
        {return Leer("dbo.sp_Turno_VerificarDisponibilidad",p=>Seleccion(p,usuario,mascota,agenda,fecha,horario),Disponible)[0];}
        public TurnoBE Registrar(int usuario,int mascota,int agenda,DateTime fecha,string horario)
        {return Leer("dbo.sp_Turno_Registrar",p=>Seleccion(p,usuario,mascota,agenda,fecha,horario),Turno)[0];}
        public List<TurnoBE> Consultar(int usuario,int mascota)
        {return Leer("dbo.sp_Turno_ConsultarMascota",p=>Cuenta(p,usuario,mascota),Turno);}
    }
}
