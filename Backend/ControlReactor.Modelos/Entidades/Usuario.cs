namespace ControlReactor.Modelos.Entidades
{
    public class Usuario
    {
        public int IdUsuario { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public bool CorreoVerificado { get; set; } = false;

        public string? AvatarSeleccionado { get; set; }
    }
}