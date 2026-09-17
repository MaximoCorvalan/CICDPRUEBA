using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class AnalisisCoherencia
    {
        [JsonPropertyName("correcto")]
        public bool Correcto { get; set; }

        [JsonPropertyName("calculos")]
        public Calculos Calculos { get; set; } = new();

        [JsonPropertyName("validaciones")]
        public List<Validacion> Validaciones { get; set; } = new();
    }
}
