using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ControlReactor.Api.Autenticacion
{
    public class ServicioCodigoLoginGoogle
    {
        private readonly ConcurrentDictionary<string,
            (int IdUsuario, string IdSolicitud, string Desafio, DateTime Expira)>
            _codigos = new();

        public string GenerarCodigo(
            int idUsuario,
            string idSolicitud,
            string desafio)
        {
            string codigo = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));

            _codigos[codigo] = (
                idUsuario,
                idSolicitud,
                desafio,
                DateTime.UtcNow.AddMinutes(2));

            return codigo;
        }

        public int? ConsumirCodigo(
            string codigo,
            string idSolicitud,
            string verificador)
        {
            if (string.IsNullOrWhiteSpace(codigo) ||
                string.IsNullOrWhiteSpace(idSolicitud) ||
                string.IsNullOrWhiteSpace(verificador))
            {
                Console.WriteLine("GOOGLE: Faltan datos para el canje.");
                return null;
            }

            if (!_codigos.TryGetValue(codigo, out var datos))
            {
                Console.WriteLine("GOOGLE: Código no encontrado en memoria.");
                return null;
            }

            if (datos.IdSolicitud != idSolicitud)
            {
                Console.WriteLine("GOOGLE: IdSolicitud no coincide.");
                return null;
            }

            if (DateTime.UtcNow > datos.Expira)
            {
                Console.WriteLine("GOOGLE: Código expirado.");
                return null;
            }

            byte[] hash = SHA256.HashData(
                Encoding.ASCII.GetBytes(verificador));

            string desafioCalculado = Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            if (desafioCalculado != datos.Desafio)
            {
                Console.WriteLine("GOOGLE: Desafío PKCE no coincide.");
                return null;
            }

            if (!_codigos.TryRemove(codigo, out _))
            {
                Console.WriteLine("GOOGLE: No se pudo consumir el código.");
                return null;
            }

            Console.WriteLine("GOOGLE: Código validado correctamente.");
            return datos.IdUsuario;
        }
        public string? ConsultarCodigo(string idSolicitud)
        {
            if (string.IsNullOrWhiteSpace(idSolicitud))
                return null;

            foreach (var registro in _codigos)
            {
                var datos = registro.Value;

                if (datos.IdSolicitud != idSolicitud)
                    continue;

                if (DateTime.UtcNow > datos.Expira)
                {
                    _codigos.TryRemove(registro.Key, out _);
                    continue;
                }

                return registro.Key;
            }

            return null;
        }
        public string? ConsultarCodigoSeguro(
            string idSolicitud,
            string verificador)
        {
            if (string.IsNullOrWhiteSpace(idSolicitud) ||
                string.IsNullOrWhiteSpace(verificador))
                return null;

            byte[] hash = SHA256.HashData(
                Encoding.ASCII.GetBytes(verificador));

            string desafioCalculado = Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            foreach (var registro in _codigos)
            {
                var datos = registro.Value;

                if (datos.IdSolicitud != idSolicitud)
                    continue;

                if (DateTime.UtcNow > datos.Expira)
                {
                    _codigos.TryRemove(registro.Key, out _);
                    continue;
                }

                if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(desafioCalculado),
                    Encoding.ASCII.GetBytes(datos.Desafio)))
                    return null;

                return registro.Key;
            }

            return null;
        }
    }
}
