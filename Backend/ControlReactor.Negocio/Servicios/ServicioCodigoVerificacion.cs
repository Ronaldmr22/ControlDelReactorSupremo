using System.Security.Cryptography;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioCodigoVerificacion
    {
        // Genera un código seguro de seis dígitos.
        public string GenerarCodigo()
        {
            int numero = RandomNumberGenerator.GetInt32(0, 1000000);

            return numero.ToString("D6");
        }

        // Calcula cuándo dejará de ser válido.
        public DateTime ObtenerExpiracion()
        {
            return DateTime.UtcNow.AddMinutes(10);
        }
    }
}