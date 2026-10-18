namespace ControlReactor.Modelos.DTOs
{
    public class CambiarPasswordDto
    {
        public string Correo { get; set; } = string.Empty;

        public string Codigo { get; set; } = string.Empty;

        public string NuevaPassword { get; set; } = string.Empty;
    }
}