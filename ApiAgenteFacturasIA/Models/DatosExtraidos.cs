using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class DatosExtraidos
    {
        [JsonPropertyName("emisor")]
        public string Emisor { get; set; } = "";

        [JsonPropertyName("cuit_emisor")]
        public string CuitEmisor { get; set; } = "";

        [JsonPropertyName("receptor")]
        public string Receptor { get; set; } = "";

        [JsonPropertyName("cuit_receptor")]
        public string CuitReceptor { get; set; } = "";

        [JsonPropertyName("tipo_numero")]
        public string TipoNumero { get; set; } = "";

        [JsonPropertyName("fecha")]
        public string Fecha { get; set; } = "";

        [JsonPropertyName("cae")]
        public string Cae { get; set; } = "";

        [JsonPropertyName("vencimiento_cae")]
        public string VencimientoCae { get; set; } = "";

        /// <summary>
        /// Null si el agente no pudo extraer el total. Acepta número, string AR/US o null.
        /// </summary>
        [JsonPropertyName("total")]
        public decimal? Total { get; set; }
    }
}
