using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class VerificacionArca
    {
        [JsonPropertyName("resultado")]
        public string Resultado { get; set; } = "";

        [JsonPropertyName("detalle")]
        public string Detalle { get; set; } = "";
    }
}
