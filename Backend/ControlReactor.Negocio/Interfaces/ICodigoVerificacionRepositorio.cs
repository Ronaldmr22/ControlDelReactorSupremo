using ControlReactor.Modelos.Entidades;

namespace ControlReactor.Negocio.Interfaces
{
    public interface ICodigoVerificacionRepositorio
    {
        Task GuardarAsync(CodigoVerificacion codigo);

        Task<CodigoVerificacion?> ObtenerUltimoAsync(int idUsuario);

        Task ActualizarAsync(CodigoVerificacion codigo);

        // Verifica la cuenta y consume el código en una transacción.
        Task<bool> ConfirmarVerificacionAsync(int idUsuario, int idCodigoVerificacion, string codigo);
        
        // Invalida los códigos anteriores de un usuario
        Task InvalidarCodigosAnterioresAsync(int idUsuario);
    }
}