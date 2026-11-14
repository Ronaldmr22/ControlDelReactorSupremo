using System.Collections.Generic;

namespace ControlReactor.Negocio.Guardianes
{
    public class AdministradorGuardianes
    {
        private readonly List<Guardian> guardianes;

        public AdministradorGuardianes()
        {
            guardianes = new List<Guardian>();
        }

        public void CrearGuardianes(
            (float X, float Y)[] posiciones)
        {
            if (posiciones == null || posiciones.Length != 4)
            {
                throw new ArgumentException(
                    "Se necesitan exactamente 4 posiciones.");
            }

            guardianes.Clear();

            for (int i = 0; i < posiciones.Length; i++)
            {
                Guardian guardian = new Guardian(
                    i + 1,
                    posiciones[i].X,
                    posiciones[i].Y
                );

                guardianes.Add(guardian);
            }
        }


        public void Actualizar(float deltaTiempo)
        {
            foreach (Guardian guardian in guardianes)
            {
                guardian.Actualizar(deltaTiempo);
            }
        }

        public IReadOnlyList<Guardian> ObtenerGuardianes()
        {
            return guardianes.AsReadOnly();
        }
    }
}
