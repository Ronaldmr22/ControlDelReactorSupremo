
namespace ControlReactor.Negocio.Guardianes
{
    public static class LimitesGuardianes
    {
        // Límites del escenario mapa1
        public const float MinX = -43.2f;
        public const float MaxX = 49.8f;

        public const float MinY = -14.3f;
        public const float MaxY = 34.7f;

        // Mantiene al guardián dentro del escenario.
        public static void AplicarLimites(Guardian guardian)
        {
            guardian.X = Math.Clamp(guardian.X, MinX, MaxX);
            guardian.Y = Math.Clamp(guardian.Y, MinY, MaxY);
        }
    }
}
