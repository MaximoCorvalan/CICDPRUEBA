using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class Veredicto
    {
        [JsonPropertyName("autenticidad")]
        public int Autenticidad { get; set; }

        [JsonPropertyName("estado")]
        public string Estado { get; set; } = "";

        [JsonPropertyName("detalle")]
        public string Detalle { get; set; } = "";

        /// <summary>
        /// origen: modelo_local | reglas
        /// </summary>
        [JsonPropertyName("origen")]
        public string Origen { get; set; } = "reglas";
    }
}
