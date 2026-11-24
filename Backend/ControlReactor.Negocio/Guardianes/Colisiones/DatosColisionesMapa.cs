using System.Collections.Generic;

namespace ControlReactor.Negocio.Guardianes.Colisiones
{
    public class DatosColisionesMapa
    {
        public int IdEscenario { get; set; }

        public List<ObstaculoMapa> Obstaculos { get; set; } = new();
    }
}