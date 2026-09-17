using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class Qr
    {
        [JsonPropertyName("presente")]
        public bool Presente { get; set; }

        [JsonPropertyName("resultado")]
        public string Resultado { get; set; } = "";
    }
}
