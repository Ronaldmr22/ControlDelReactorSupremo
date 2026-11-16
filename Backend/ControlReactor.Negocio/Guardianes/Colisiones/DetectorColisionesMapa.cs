using System.Collections.Generic;

namespace ControlReactor.Negocio.Guardianes.Colisiones
{
    public static class DetectorColisionesMapa
    {
        public static bool PuntoDentroPoligono(
            float x,
            float y,
            List<VerticeMapa> vertices)
        {
            if (vertices == null || vertices.Count < 3)
                return false;

            bool dentro = false;
            int j = vertices.Count - 1;

            for (int i = 0; i < vertices.Count; i++)
            {
                float xi = vertices[i].X;
                float yi = vertices[i].Y;
                float xj = vertices[j].X;
                float yj = vertices[j].Y;

                bool cruza =
                    (yi > y) != (yj > y) &&
                    x < (xj - xi) * (y - yi) /
                        (yj - yi) + xi;

                if (cruza)
                    dentro = !dentro;

                j = i;
            }

            return dentro;
        }

        
        public static bool PuntoEnObstaculo(
            float x,
            float y,
            DatosColisionesMapa mapa)
        {
            if (mapa?.Obstaculos == null)
                return false;

            bool dentro = false;

            foreach (ObstaculoMapa obstaculo in mapa.Obstaculos)
            {
                if (PuntoDentroPoligono(
                    x,
                    y,
                    obstaculo.Vertices))
                {
                    dentro = !dentro;
                }
            }

            return dentro;
        }


        public static bool PosicionLibre(
            float x,
            float y,
            DatosColisionesMapa mapa)
        {
            return !PuntoEnObstaculo(x, y, mapa);
        }

        public static bool CuerpoEnObstaculo(
            float x,
            float y,
            DatosColisionesMapa mapa)
        {
            if (mapa?.Obstaculos == null)
                return false;

            float centroX = x + DimensionesGuardian.OffsetX;
            float centroY = y + DimensionesGuardian.OffsetY;

            float mitadAncho = DimensionesGuardian.Ancho / 2f;
            float mitadAlto = DimensionesGuardian.Alto / 2f;

            float izquierda = centroX - mitadAncho;
            float derecha = centroX + mitadAncho;
            float abajo = centroY - mitadAlto;
            float arriba = centroY + mitadAlto;

            // Comprobar esquinas y centro del guardián
            if (PuntoEnObstaculo(izquierda, abajo, mapa) ||
                PuntoEnObstaculo(izquierda, arriba, mapa) ||
                PuntoEnObstaculo(derecha, abajo, mapa) ||
                PuntoEnObstaculo(derecha, arriba, mapa) ||
                PuntoEnObstaculo(centroX, centroY, mapa))
            {
                return true;
            }

            // Comprobar si el cuerpo toca algún contorno.
            foreach (ObstaculoMapa obstaculo in mapa.Obstaculos)
            {
                var vertices = obstaculo.Vertices;

                if (vertices == null || vertices.Count < 3)
                    continue;

                for (int i = 0; i < vertices.Count; i++)
                {
                    VerticeMapa a = vertices[i];
                    VerticeMapa b = vertices[(i + 1) % vertices.Count];

                    if (
                        SegmentosSeCruzan(
                            a.X, a.Y, b.X, b.Y,
                            izquierda, abajo, derecha, abajo) ||

                        SegmentosSeCruzan(
                            a.X, a.Y, b.X, b.Y,
                            derecha, abajo, derecha, arriba) ||

                        SegmentosSeCruzan(
                            a.X, a.Y, b.X, b.Y,
                            derecha, arriba, izquierda, arriba) ||

                        SegmentosSeCruzan(
                            a.X, a.Y, b.X, b.Y,
                            izquierda, arriba, izquierda, abajo))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool SegmentosSeCruzan(
            float ax, float ay,
            float bx, float by,
            float cx, float cy,
            float dx, float dy)
        {
            const float epsilon = 0.000001f;

            float o1 = Orientacion(ax, ay, bx, by, cx, cy);
            float o2 = Orientacion(ax, ay, bx, by, dx, dy);
            float o3 = Orientacion(cx, cy, dx, dy, ax, ay);
            float o4 = Orientacion(cx, cy, dx, dy, bx, by);

            // Cruce entre los segmentos.
            if (((o1 > epsilon && o2 < -epsilon) ||
                (o1 < -epsilon && o2 > epsilon)) &&
                ((o3 > epsilon && o4 < -epsilon) ||
                (o3 < -epsilon && o4 > epsilon)))
            {
                return true;
            }

            // También detectar contacto con los bordes.
            return
                (MathF.Abs(o1) <= epsilon &&
                PuntoEnSegmento(ax, ay, bx, by, cx, cy)) ||
                (MathF.Abs(o2) <= epsilon &&
                PuntoEnSegmento(ax, ay, bx, by, dx, dy)) ||
                (MathF.Abs(o3) <= epsilon &&
                PuntoEnSegmento(cx, cy, dx, dy, ax, ay)) ||
                (MathF.Abs(o4) <= epsilon &&
                PuntoEnSegmento(cx, cy, dx, dy, bx, by));
        }

        private static float Orientacion(
            float ax, float ay,
            float bx, float by,
            float px, float py)
        {
            return (bx - ax) * (py - ay) -
                (by - ay) * (px - ax);
        }

        private static bool PuntoEnSegmento(
            float ax, float ay,
            float bx, float by,
            float px, float py)
        {
            const float epsilon = 0.000001f;

            return px >= MathF.Min(ax, bx) - epsilon &&
                px <= MathF.Max(ax, bx) + epsilon &&
                py >= MathF.Min(ay, by) - epsilon &&
                py <= MathF.Max(ay, by) + epsilon;
        }
    }
}
