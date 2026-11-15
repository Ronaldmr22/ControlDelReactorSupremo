using System;

namespace ControlReactor.Negocio.Guardianes
{
    public static class ConfiguracionPosicionesGuardianes
    {
        public static (float X, float Y)[] ObtenerPosiciones(int idEscenario)
        {
            switch (idEscenario)
            {
                case 1:
                    return new (float X, float Y)[]
                    {
                        (-12.14749f, 0.85289f),
                        (-18.76749f, -1.41711f),
                        (-13.61749f, 0.82289f),
                        (-23.31749f, -5.46711f)
                    };
                case 2:
                    return new (float X, float Y)[]
                    {
                        (-0.2674904f, -4.92711f),
                        (-15.91749f, -2.01711f),
                        (-8.397491f, -8.27711f),
                        (-9.197491f, -0.04710996f)
                    };
                case 3:
                    return new (float X, float Y)[]
                    {
                        (-1.947491f, -2.63711f),
                        (-9.787491f, -2.01711f),
                        (-8.397491f, -10.96711f),
                        (-8.042509f, -5.61711f)
                    };
                default:
                    throw new ArgumentException(
                        "El escenario todavía no tiene posiciones de guardianes configuradas."
                    );
            }
        }
    }
}
