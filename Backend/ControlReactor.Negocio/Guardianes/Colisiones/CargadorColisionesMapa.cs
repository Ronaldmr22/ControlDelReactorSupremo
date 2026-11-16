using System;
using System.IO;
using System.Text.Json;

namespace ControlReactor.Negocio.Guardianes.Colisiones
{
    public static class CargadorColisionesMapa
    {
        public static DatosColisionesMapa Cargar(string rutaArchivo)
        {
            if (!File.Exists(rutaArchivo))
            {
                throw new FileNotFoundException(
                    "No se encontró el archivo de colisiones.",
                    rutaArchivo
                );
            }

            string json = File.ReadAllText(rutaArchivo);

            var opciones = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            DatosColisionesMapa? datos =
                JsonSerializer.Deserialize<DatosColisionesMapa>(
                    json,
                    opciones
                );

            if (datos == null)
            {
                throw new InvalidDataException(
                    "No se pudieron leer las colisiones del mapa."
                );
            }

            return datos;
        }
    }
}
