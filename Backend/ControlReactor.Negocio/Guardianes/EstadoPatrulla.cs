namespace ControlReactor.Negocio.Guardianes
{
    public class EstadoPatrulla : IEstadoGuardian
    {
        private const float Velocidad = 1.5f;

        public void Actualizar(Guardian guardian, float deltaTiempo)
        {
            guardian.X += Velocidad * deltaTiempo;
        }
    }
}
