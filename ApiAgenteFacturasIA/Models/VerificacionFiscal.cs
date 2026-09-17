using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class VerificacionFiscal
    {
        [JsonPropertyName("resultado_arca")]
        public string ResultadoArca { get; set; } = "";

        [JsonPropertyName("cuits_validos")]
        public bool CuitsValidos { get; set; }

        [JsonPropertyName("coherencia_pdf_qr")]
        public bool CoherenciaPdfQr { get; set; }

        [JsonPropertyName("detalle")]
        public string Detalle { get; set; } = "";
    }
}
