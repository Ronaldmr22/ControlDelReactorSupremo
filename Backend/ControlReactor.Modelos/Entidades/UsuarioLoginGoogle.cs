namespace ControlReactor.Modelos.Entidades
{
    public class UsuarioLoginGoogle
    {
        public int IdUsuarioLoginGoogle { get; set; }
        public int IdUsuario { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public string IdGoogle { get; set; } = string.Empty;
        public Usuario Usuario { get; set; } = null!;
    }
}