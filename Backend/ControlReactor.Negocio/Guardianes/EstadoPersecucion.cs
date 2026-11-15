using System;
using ControlReactor.Negocio.Guardianes.Colisiones;

namespace ControlReactor.Negocio.Guardianes
{
    public class EstadoPersecucion : IEstadoGuardian
    {
        private const float Velocidad = 1.5f;

        public float ObjetivoX { get; set; }
        public float ObjetivoY { get; set; }

        public void Actualizar(Guardian guardian, float deltaTiempo)
        {
            float diferenciaX = ObjetivoX - guardian.X;
            float diferenciaY = ObjetivoY - guardian.Y;

            float distancia = guardian.CalcularDistancia(
                ObjetivoX,
                ObjetivoY
            );

            if (distancia <= 0.01f)
                return;

            float desplazamiento = MathF.Min(
                Velocidad * deltaTiempo,
                distancia
            );

            guardian.X += diferenciaX / distancia * desplazamiento;
            guardian.Y += diferenciaY / distancia * desplazamiento;
        }
    }
}
