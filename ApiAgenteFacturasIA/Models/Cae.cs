using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class Cae
    {
        [JsonPropertyName("presente")]
        public bool Presente { get; set; }

        [JsonPropertyName("validado")]
        public bool Validado { get; set; }

        [JsonPropertyName("resultado")]
        public string Resultado { get; set; } = "";
    }

}
