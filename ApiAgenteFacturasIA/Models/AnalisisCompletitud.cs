using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class AnalisisCompletitud
    {
        [JsonPropertyName("cumple")]
        public bool Cumple { get; set; }

        [JsonPropertyName("faltantes")]
        public List<string> Faltantes { get; set; } = new();

        [JsonPropertyName("detalle")]
        public string Detalle { get; set; } = "";
    }
}
