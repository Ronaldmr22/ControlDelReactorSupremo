using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace ControlReactor.Negocio.Servicios
{
    public class ServicioValidacionCorreo
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public ServicioValidacionCorreo(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            _apiKey = configuration["AbstractApi:ApiKey"] ?? throw new InvalidOperationException("Falta configurar la API Key de Abstract API.");
        }

        public async Task<bool> CorreoExisteAsync(string correo)
        {
            string url =
                "https://emailreputation.abstractapi.com/v1/" + $"?api_key={_apiKey}&email={Uri.EscapeDataString(correo)}";

            var respuesta = await _httpClient.GetFromJsonAsync<RespuestaAbstractApi>(url);

            if (respuesta == null || respuesta.EmailDeliverability == null)
            {
                return false;
            }

            return respuesta.EmailDeliverability.Status == "deliverable";
        }

        private class RespuestaAbstractApi
        {
            [JsonPropertyName("email_deliverability")]
            public DatosEntregabilidad? EmailDeliverability { get; set; }
        }

        private class DatosEntregabilidad
        {
            [JsonPropertyName("status")]
            public string Status { get; set; } = string.Empty;
        }
    }
}