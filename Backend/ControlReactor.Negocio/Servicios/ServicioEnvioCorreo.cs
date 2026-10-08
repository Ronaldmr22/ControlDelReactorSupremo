using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioEnvioCorreo
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _remitente;

        public ServicioEnvioCorreo(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            _apiKey = configuration["Brevo:ApiKey"]?? throw new InvalidOperationException("Falta configurar la API Key de Brevo.");

            _remitente = configuration["Brevo:Remitente"]?? throw new InvalidOperationException("Falta configurar el remitente de Brevo.");
        }

        public async Task EnviarCodigoAsync(string correoDestino, string codigo)
        {
            var contenido = new
            {
                sender = new
                {
                    name = "Control del Reactor",
                    email = _remitente
                },
                to = new[]
                {
                    new { email = correoDestino }
                },
                subject = "Verifica tu cuenta - Control del Reactor",
                textContent =$"Tu código de verificación es: {codigo}. " + "No compartas este código con nadie."
            };

            using var solicitud = new HttpRequestMessage(HttpMethod.Post,"https://api.brevo.com/v3/smtp/email");

            solicitud.Headers.Add("api-key", _apiKey);
            solicitud.Content = JsonContent.Create(contenido);

            try
            {
                // Envía el correo y comprueba la respuesta de Brevo.
                using var respuesta = await _httpClient.SendAsync(solicitud);

                if (!respuesta.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Brevo rechazó el envío. Código HTTP: {(int)respuesta.StatusCode}");
                }
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (TaskCanceledException ex)
            {
                throw new HttpRequestException("Se agotó el tiempo de espera al contactar con Brevo.", ex);
            }
        }
    }
}