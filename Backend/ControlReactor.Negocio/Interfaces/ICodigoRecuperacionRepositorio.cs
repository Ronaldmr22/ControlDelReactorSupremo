using ControlReactor.Modelos.Entidades;

namespace ControlReactor.Negocio.Interfaces
{
    public interface ICodigoRecuperacionRepositorio
    {
        // Guarda un nuevo código de recuperación.
        Task GuardarAsync(CodigoRecuperacion codigo);

        // Obtiene el código más reciente de un usuario.
        Task<CodigoRecuperacion?> ObtenerUltimoAsync(int idUsuario);

        // Actualiza un código existente.
        Task ActualizarAsync(CodigoRecuperacion codigo);

        // Invalida los códigos anteriores.
        Task InvalidarCodigosAnterioresAsync(int idUsuario);

        // Cambia la contraseña y consume el código en una transacción.
        Task<bool> RestablecerPasswordAsync(int idUsuario,int idCodigoRecuperacion,string codigo,string nuevaPassword);
    }
}