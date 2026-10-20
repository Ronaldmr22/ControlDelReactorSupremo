using System.Threading.Tasks;
using ControlReactor.Modelos.DTOs;

namespace ControlReactor.Negocio.Interfaces
{
    public interface ISalaRepositorio
    {
        Task<(SalaDto? Sala, string Mensaje)> CrearAsync(int idUsuario);

        Task<(SalaDto? Sala, string Mensaje)> UnirseAsync(
            int idUsuario, string codigoSala);

        Task<SalaDto?> ObtenerPorCodigoAsync(string codigoSala);
        Task<(bool Exito, string Mensaje)> RegistrarActividadAsync(
            int idUsuario, string codigoSala);
        Task<(bool Exito, string Mensaje)> SalirAsync(
            int idUsuario, string codigoSala);
    }
}

