using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class Validacion
    {
        [JsonPropertyName("campo")]
        public string Campo { get; set; } = "";

        [JsonPropertyName("resultado")]
        public string Resultado { get; set; } = "";

        [JsonPropertyName("observacion")]
        public string Observacion { get; set; } = "";
    }
}
