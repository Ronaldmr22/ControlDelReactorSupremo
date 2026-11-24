using System;
using System.Collections.Generic;
using ControlReactor.Negocio.Guardianes.Colisiones;

namespace ControlReactor.Negocio.Guardianes
{
    public class AdministradorGuardianes
    {
        private readonly List<Guardian> guardianes;
        private readonly object _sync = new();
        private DatosColisionesMapa? mapaColisiones;

        public AdministradorGuardianes()
        {
            guardianes = new List<Guardian>();
        }

        public void AsignarMapa(DatosColisionesMapa mapa)
        {
            mapaColisiones = mapa ??
                throw new ArgumentNullException(nameof(mapa));
        }

        public void CrearGuardianes(
            (float X, float Y)[] posiciones)
        {
            if (posiciones == null || posiciones.Length != 4)
            {
                throw new ArgumentException(
                    "Se necesitan exactamente 4 posiciones.");
            }

            lock (_sync)
            {
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
        }

        // Los guardianes nuevos usan exactamente la misma clase, IA y colisiones.
        // Se devuelven sus IDs para eliminar únicamente los temporales al terminar.
        public IReadOnlyList<int> AgregarGuardianesTemporales(
            IReadOnlyList<(float X, float Y)> posiciones)
        {
            if (posiciones == null || posiciones.Count != 5)
                throw new ArgumentException("Se requieren exactamente 5 posiciones.");

            lock (_sync)
            {
                int siguienteId = guardianes.Count == 0
                    ? 1
                    : guardianes.Max(g => g.Id) + 1;

                var ids = new List<int>(posiciones.Count);

                foreach (var posicion in posiciones)
                {
                    int id = siguienteId++;
                    guardianes.Add(new Guardian(id, posicion.X, posicion.Y));
                    ids.Add(id);
                }

                return ids;
            }
        }

        public void EliminarGuardianesTemporales(IReadOnlyCollection<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return;

            lock (_sync)
            {
                var identificadores = new HashSet<int>(ids);
                guardianes.RemoveAll(g => identificadores.Contains(g.Id));
            }
        }

        public void Actualizar(float deltaTiempo)
        {
            lock (_sync)
            {
                foreach (Guardian guardian in guardianes)
                {
                    float anteriorX = guardian.X;
                    float anteriorY = guardian.Y;

                    guardian.Actualizar(deltaTiempo);

                    float nuevoX = guardian.X;
                    float nuevoY = guardian.Y;

                    if (mapaColisiones == null ||
                        !DetectorColisionesMapa.CuerpoEnObstaculo(
                            nuevoX, nuevoY, mapaColisiones))
                    {
                        continue;
                    }

                    // Restaurar posición anterior.
                    guardian.X = anteriorX;
                    guardian.Y = anteriorY;

                    if (guardian.EstadoActual is EstadoPatrulla patrulla)
                    {
                        patrulla.CambiarDireccion();
                    }
                    else if (guardian.EstadoActual is EstadoPersecucion)
                    {
                        // Intentar avanzar solamente en X.
                        if (!DetectorColisionesMapa.CuerpoEnObstaculo(
                            nuevoX, anteriorY, mapaColisiones))
                        {
                            guardian.X = nuevoX;
                            continue;
                        }

                        // Intentar avanzar solamente en Y.
                        if (!DetectorColisionesMapa.CuerpoEnObstaculo(
                            anteriorX, nuevoY, mapaColisiones))
                        {
                            guardian.Y = nuevoY;
                        }
                    }
                }
            }
        }
        public bool ObtenerPosicionGuardian(
            int guardianId,
            out float x,
            out float y)
        {
            lock (_sync)
            {
                x = 0;
                y = 0;

                foreach (Guardian guardian in guardianes)
                {
                    if (guardian.Id == guardianId)
                    {
                        x = guardian.X;
                        y = guardian.Y;
                        return true;
                    }
                }

                return false;
            }
        }

        public void CambiarEstadoGuardian(
            Guardian guardian,
            IEstadoGuardian nuevoEstado)
        {
            lock (_sync)
            {
                guardian.CambiarEstado(nuevoEstado);
            }
        }

        public void ActualizarObjetivoPersecucion(
            Guardian guardian,
            float x,
            float y)
        {
            lock (_sync)
            {
                if (guardian.EstadoActual is EstadoPersecucion persecucion)
                {
                    persecucion.ObjetivoX = x;
                    persecucion.ObjetivoY = y;
                }
            }
        }

        public IReadOnlyList<Guardian> ObtenerGuardianes()
        {
            return guardianes.AsReadOnly();
        }
    }
}
