using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class ElementosSeguridad
    {
        [JsonPropertyName("qr")]
        public Qr Qr { get; set; } = new();

        [JsonPropertyName("cae")]
        public Cae Cae { get; set; } = new();

        [JsonPropertyName("verificacion_arca")]
        public VerificacionArca VerificacionArca { get; set; } = new();
    }
}
