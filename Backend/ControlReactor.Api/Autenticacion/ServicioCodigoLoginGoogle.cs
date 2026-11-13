
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
                return null;

            if (!_codigos.TryGetValue(codigo, out var datos))
                return null;

            if (datos.IdSolicitud != idSolicitud ||
                DateTime.UtcNow > datos.Expira)
                return null;

            byte[] hash = SHA256.HashData(
                Encoding.ASCII.GetBytes(verificador));

            string desafioCalculado = Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            if (desafioCalculado != datos.Desafio)
                return null;

            // El código solo puede utilizarse una vez
            if (!_codigos.TryRemove(codigo, out _))
                return null;

            return datos.IdUsuario;
        }
    }
}
