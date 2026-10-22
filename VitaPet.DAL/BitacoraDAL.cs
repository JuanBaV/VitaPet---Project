using System;
using System.Transactions;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using VitaPet.BE;

namespace VitaPet.DAL
{
    public class BitacoraDAL
    {
        private readonly ConexionDAL _conexionDAL;

        private static BitacoraBE LeerRegistro(SqlDataReader lector)
        {
            return new BitacoraBE {
                IdBitacora = Convert.ToInt32(lector["idBitacora"]),
                IdUsuario = lector["idUsuario"] == DBNull.Value ? (int?)null : Convert.ToInt32(lector["idUsuario"]),
                Fecha = Convert.ToDateTime(lector["fecha"]),
                Accion = Convert.ToString(lector["accion"]),
                Modulo = Convert.ToString(lector["modulo"]),
                Criticidad = Convert.ToString(lector["criticidad"]),
                DVH = Convert.ToString(lector["dvh"])
            };
        }

        public BitacoraDAL()
        {
            _conexionDAL = new ConexionDAL();
        }

        public List<BitacoraBE> ObtenerParaIntegridad()
        {
            List<BitacoraBE> registros = new List<BitacoraBE>();

            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand(
                "dbo.sp_Bitacora_ObtenerParaIntegridad",
                conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                conexion.Open();

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        registros.Add(LeerRegistro(lector));
                    }
                }
            }

            return registros;
        }

        public int Registrar(
            BitacoraBE registro,
            DVVBE dvv)
        {
            using (SqlConnection conexion =
                _conexionDAL.CrearConexion())
            {
                using (TransactionScope transaccion =
                    new TransactionScope(TransactionScopeOption.Required))
                {
                    conexion.Open();
                    int idBitacora;

                    using (SqlCommand comandoBitacora =
                        new SqlCommand(
                            "dbo.sp_Bitacora_Registrar",
                            conexion))
                    {
                        comandoBitacora.CommandType =
                            CommandType.StoredProcedure;

                        comandoBitacora.Parameters.Add(
                            "@idUsuario",
                            SqlDbType.Int).Value =
                            registro.IdUsuario.HasValue
                                ? (object)registro.IdUsuario.Value
                                : DBNull.Value;

                        comandoBitacora.Parameters.Add(
                            "@fecha",
                            SqlDbType.DateTime).Value =
                            registro.Fecha;

                        comandoBitacora.Parameters.Add(
                            "@accion",
                            SqlDbType.VarChar,
                            100).Value = registro.Accion;

                        comandoBitacora.Parameters.Add(
                            "@modulo",
                            SqlDbType.VarChar,
                            50).Value = registro.Modulo;

                        comandoBitacora.Parameters.Add(
                            "@criticidad",
                            SqlDbType.VarChar,
                            20).Value = registro.Criticidad;

                        comandoBitacora.Parameters.Add(
                            "@dvh",
                            SqlDbType.VarChar,
                            64).Value = registro.DVH;

                        idBitacora = Convert.ToInt32(
                            comandoBitacora.ExecuteScalar());
                    }

                    DVVDAL.Guardar(conexion, dvv);

                    transaccion.Complete();

                    return idBitacora;
                }
            }
        }

        public List<string> ObtenerDVHs()
        {
            List<string> valores = new List<string>();

            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand("dbo.sp_Bitacora_ObtenerDvhs", conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;

                conexion.Open();

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        valores.Add(Convert.ToString(lector["dvh"]));
                    }
                }
            }

            return valores;
        }

        public List<BitacoraBE> ObtenerExcedentes(int mantener)
        {
            List<BitacoraBE> registros = new List<BitacoraBE>();

            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand("dbo.sp_Bitacora_ObtenerExcedentes", conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.Add("@mantener", SqlDbType.Int).Value = mantener;

                conexion.Open();

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        registros.Add(LeerRegistro(lector));
                    }
                }
            }

            return registros;
        }

        public void ActualizarDVH(int idBitacora, string dvh)
        {
            using (SqlConnection conexion = _conexionDAL.CrearConexion())
            using (SqlCommand comando = new SqlCommand(
                "dbo.sp_Bitacora_ActualizarDVH",
                conexion))
            {
                comando.CommandType = CommandType.StoredProcedure;
                comando.Parameters.Add(
                    "@idBitacora",
                    SqlDbType.Int).Value = idBitacora;
                comando.Parameters.Add(
                    "@dvh",
                    SqlDbType.VarChar,
                    64).Value = dvh;

                conexion.Open();
                comando.ExecuteNonQuery();
            }
        }

        public void EliminarYActualizarDVV(
            IEnumerable<int> idsBitacora,
            DVVBE dvv)
        {
            if (idsBitacora == null)
            {
                throw new ArgumentNullException(nameof(idsBitacora));
            }

            if (dvv == null)
            {
                throw new ArgumentNullException(nameof(dvv));
            }

            using (SqlConnection conexion =
                _conexionDAL.CrearConexion())
            {
                using (TransactionScope transaccion =
                    new TransactionScope(TransactionScopeOption.Required))
                {
                    conexion.Open();
                    foreach (int idBitacora in idsBitacora)
                    {
                        using (SqlCommand comandoBitacora =
                            new SqlCommand(
                                "dbo.sp_Bitacora_Eliminar",
                                conexion))
                        {
                            comandoBitacora.CommandType =
                                CommandType.StoredProcedure;

                            comandoBitacora.Parameters.Add(
                                "@idBitacora",
                                SqlDbType.Int).Value = idBitacora;

                            comandoBitacora.ExecuteNonQuery();
                        }
                    }

                    DVVDAL.Guardar(conexion, dvv);

                    transaccion.Complete();
                }
            }
        }
    }
}
