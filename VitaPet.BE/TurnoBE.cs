using System;
using System.Collections.Generic;
namespace VitaPet.BE
{
    public class DisponibilidadTurnoBE
    {
        public int IdAgendaTurno { get; set; }
        public DateTime Fecha { get; set; }
        public string Horario { get; set; }
        public string Prestacion { get; set; }
        public string Zona { get; set; }
    }
    public class TurnoBE
    {
        public int IdTurno { get; set; }
        public int IdMascota { get; set; }
        public DateTime Fecha { get; set; }
        public string Horario { get; set; }
        public string Prestacion { get; set; }
        public string Zona { get; set; }
        public string Estado { get; set; }
    }
    public class ResultadoDisponibilidadTurnoBE
    {
        public string CodigoError { get; set; }
        public List<DisponibilidadTurnoBE> Turnos { get; set; }
    }
    public class ResultadoValidacionTurnoBE
    {
        public string CodigoError { get; set; }
        public DisponibilidadTurnoBE Turno { get; set; }
    }
    public class ResultadoRegistroTurnoBE
    {
        public string CodigoError { get; set; }
        public TurnoBE Turno { get; set; }
    }
    public class ResultadoConsultaTurnosBE
    {
        public string CodigoError { get; set; }
        public MascotaBE Mascota { get; set; }
        public List<TurnoBE> Turnos { get; set; }
    }
}
