using ControlReactor.Modelos.Entidades;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioLogros
    {
        public List<Logro> ObtenerLogros(
            int partidasJugadas,
            int partidasGanadas)
        {
            var logros = new List<Logro>();

            if (partidasJugadas >= 1)
            {
                logros.Add(new Logro
                {
                    IdLogro = 1,
                    Nombre = "Primeros pasos",
                    Descripcion = "Jugar una partida."
                });
            }

            if (partidasGanadas >= 1)
            {
                logros.Add(new Logro
                {
                    IdLogro = 2,
                    Nombre = "Primera victoria",
                    Descripcion = "Ganar una partida."
                });
            }

            return logros;
        }
    }
}