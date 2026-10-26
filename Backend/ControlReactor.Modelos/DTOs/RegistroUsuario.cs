namespace ControlReactor.Modelos.DTOs
{
    public class RegistroUsuarioDto
    {
        public string NombreUsuario { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string ConfirmarPassword { get; set; } = string.Empty;
    }
}