using System;

namespace BattleArena.Core
{
    public class ConfiguracionPartida
    {
        public ModoFinalizacion Modo { get; private set; }
        public int Minutos { get; private set; }
        public int PuntosObjetivo { get; private set; }
        public int SegundosCongelamiento { get; private set; }

        // Valores por defecto.
        public ConfiguracionPartida()
        {
            Modo = ModoFinalizacion.PorTiempo;
            Minutos = 5;
            PuntosObjetivo = 100;
            SegundosCongelamiento = 5;
        }

        public void Configurar(ModoFinalizacion modo, int minutos, int puntosObjetivo, int segundosCongelamiento)
        {
            if (minutos <= 0)
                throw new ArgumentException("Los minutos deben ser mayores que cero.");
            if (puntosObjetivo <= 0)
                throw new ArgumentException("Los puntos objetivo deben ser mayores que cero.");
            if (segundosCongelamiento <= 0)
                throw new ArgumentException("Los segundos de congelamiento deben ser mayores que cero.");

            Modo = modo;
            Minutos = minutos;
            PuntosObjetivo = puntosObjetivo;
            SegundosCongelamiento = segundosCongelamiento;
        }
    }
}