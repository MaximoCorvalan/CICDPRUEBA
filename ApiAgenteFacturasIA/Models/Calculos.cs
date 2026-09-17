using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class Calculos
    {
        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }

        [JsonPropertyName("iva")]
        public decimal Iva { get; set; }

        [JsonPropertyName("percepciones")]
        public decimal Percepciones { get; set; }

        [JsonPropertyName("total_calculado")]
        public decimal TotalCalculado { get; set; }
    }
}
