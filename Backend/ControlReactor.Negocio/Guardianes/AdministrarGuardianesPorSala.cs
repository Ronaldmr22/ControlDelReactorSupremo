using System;
using System.Collections.Generic;
using System.IO;
using ControlReactor.Negocio.Guardianes.Colisiones;

namespace ControlReactor.Negocio.Guardianes
{
    public class AdministradorGuardianesPorSala
    {
        private readonly Dictionary<string, AdministradorGuardianes> _salas =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly object _sync = new();

        private readonly string _rutaMapas;

        public AdministradorGuardianesPorSala(string rutaMapas)
        {
            _rutaMapas = rutaMapas;
        }

        public AdministradorGuardianes ObtenerOCrear(
            string codigoSala,
            int idEscenario)
        {
            lock (_sync)
            {
                string codigo = codigoSala.Trim().ToUpperInvariant();

                if (!_salas.TryGetValue(codigo, out var administrador))
                {
                    var posiciones =
                        ConfiguracionPosicionesGuardianes.ObtenerPosiciones(idEscenario);

                    string rutaArchivo = Path.Combine(
                        _rutaMapas,
                        $"ColisionesMapa{idEscenario}.json"
                    );

                    DatosColisionesMapa mapa =
                        CargadorColisionesMapa.Cargar(rutaArchivo);

                    if (mapa.IdEscenario != idEscenario)
                    {
                        throw new InvalidDataException(
                            "El archivo de colisiones no corresponde al escenario.");
                    }

                    administrador = new AdministradorGuardianes();
                    administrador.AsignarMapa(mapa);
                    administrador.CrearGuardianes(posiciones);

                    // Próximo paso: asignar el mapa al administrador.
                    // Por ahora solo comprobamos que pueda cargarse.

                    _salas.Add(codigo, administrador);
                }

                return administrador;
            }
        }
        public AdministradorGuardianes? ObtenerExistente(
            string codigoSala)
        {
            lock (_sync)
            {
                if (string.IsNullOrWhiteSpace(codigoSala))
                    return null;

                string codigo = codigoSala.Trim().ToUpperInvariant();

                if (_salas.TryGetValue(codigo, out var administrador))
                {
                    return administrador;
                }

                return null;
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
