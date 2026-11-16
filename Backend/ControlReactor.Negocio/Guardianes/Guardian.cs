namespace ControlReactor.Negocio.Guardianes
{
    public class Guardian
    {
        public int Id { get; private set; }
        public float X { get; set; }
        public float Y { get; set; }

        public IEstadoGuardian EstadoActual { get; private set; }

        public Guardian(int id, float x, float y)
        {
            Id = id;
            X = x;
            Y = y;
            EstadoActual = new EstadoPatrulla(x, y);
        }

        public void CambiarEstado(IEstadoGuardian nuevoEstado)
        {
            EstadoActual = nuevoEstado;
        }

        public void Actualizar(float deltaTiempo)
        {
            // Actualizar el movimiento según el estado del guardián.
            EstadoActual?.Actualizar(this, deltaTiempo);

            // Evitar que el guardián salga de los límites del mapa.
            LimitesGuardianes.AplicarLimites(this);
        }
        public float CalcularDistancia(float x, float y)
        {
            float diferenciaX = x - X;
            float diferenciaY = y - Y;

            return MathF.Sqrt(
                diferenciaX * diferenciaX +
                diferenciaY * diferenciaY
            );
        }

    }
}