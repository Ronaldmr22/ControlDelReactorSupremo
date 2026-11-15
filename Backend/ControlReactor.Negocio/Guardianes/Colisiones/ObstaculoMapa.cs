using System.Collections.Generic;

namespace ControlReactor.Negocio.Guardianes.Colisiones
{
    public class ObstaculoMapa
    {
        public List<VerticeMapa> Vertices { get; set; } = new();
    }

    public class VerticeMapa
    {
        public float X { get; set; }
        public float Y { get; set; }
    }
}
