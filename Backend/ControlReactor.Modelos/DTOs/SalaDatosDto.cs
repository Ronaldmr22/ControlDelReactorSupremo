using System.Collections.Generic;

namespace ControlReactor.Modelos.DTOs
{
    public class SalaDto
    {
        public int IdPartida { get; set; }
        public string CodigoSala { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int IdUsuarioCreador { get; set; }
        public List<ParticipanteSalaDto> Participantes { get; set; } = new();
    }

    public class ParticipanteSalaDto
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public int AvatarSeleccionado { get; set; }
        public string? FotoPerfil { get; set; }
    }
}
