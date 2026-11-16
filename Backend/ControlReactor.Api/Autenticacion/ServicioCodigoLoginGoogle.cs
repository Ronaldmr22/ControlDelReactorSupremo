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
    }
}
