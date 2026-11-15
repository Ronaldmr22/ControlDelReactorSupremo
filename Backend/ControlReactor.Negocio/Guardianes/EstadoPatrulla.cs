using System;
namespace ControlReactor.Negocio.Guardianes
{
    public class EstadoPatrulla : IEstadoGuardian
    {
        private const float Velocidad = 1.0f;
        private const float DistanciaPatrulla = 2.5f;

        private readonly float posicionInicialX;
        private readonly float posicionInicialY;

        private float destinoX;
        private float destinoY;
        private float tiempoEspera;

        private static readonly Random random = new Random();

        public EstadoPatrulla(float posicionInicialX, float posicionInicialY)
        {
            this.posicionInicialX = posicionInicialX;
            this.posicionInicialY = posicionInicialY;

            ElegirNuevoDestino();
        }

        private void ElegirNuevoDestino()
        {
            float angulo = (float)(random.NextDouble() * Math.PI * 2);
            float distancia = (float)random.NextDouble() * DistanciaPatrulla;

            destinoX = posicionInicialX + MathF.Cos(angulo) * distancia;
            destinoY = posicionInicialY + MathF.Sin(angulo) * distancia;
        }

        public void CambiarDireccion()
        {
            ElegirNuevoDestino();
        }

        public void Actualizar(Guardian guardian, float deltaTiempo)
        {
            if (tiempoEspera > 0f)
            {
                tiempoEspera -= deltaTiempo;
                return;
            }

            float diferenciaX = destinoX - guardian.X;
            float diferenciaY = destinoY - guardian.Y;

            float distancia = MathF.Sqrt(
                diferenciaX * diferenciaX +
                diferenciaY * diferenciaY
            );

            if (distancia <= 0.1f)
            {
                tiempoEspera = 0.5f;
                ElegirNuevoDestino();
                return;
            }

            float desplazamiento = MathF.Min(
                Velocidad * deltaTiempo,
                distancia
            );

            guardian.X += diferenciaX / distancia * desplazamiento;
            guardian.Y += diferenciaY / distancia * desplazamiento;
        }
    }
}
