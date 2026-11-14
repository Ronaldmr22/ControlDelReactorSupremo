
using System;
using System.Collections.Generic;

namespace ControlReactor.Negocio.Guardianes
{
    public class AdministradorGuardianesPorSala
    {
        private readonly Dictionary<string, AdministradorGuardianes> _salas = new(StringComparer.OrdinalIgnoreCase);

        private readonly object _sync = new();

        public AdministradorGuardianes ObtenerOCrear(
            string codigoSala,
            (float X, float Y)[] posiciones)
        {
            lock (_sync)
            {
                string codigo = codigoSala.Trim().ToUpperInvariant();

                if (!_salas.TryGetValue(codigo, out var administrador))
                {
                    administrador = new AdministradorGuardianes();

                    administrador.CrearGuardianes(posiciones);

                    _salas.Add(codigo, administrador);
                }

                return administrador;
            }
        }

        public void EliminarSala(string codigoSala)
        {
            lock (_sync)
            {
                _salas.Remove(codigoSala.Trim().ToUpperInvariant());
            }
        }
    }
}
