using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class ResultadoAnalisis
    {
        [JsonPropertyName("tipo_documento")]
        public string TipoDocumento { get; set; } = "";

        [JsonPropertyName("datos_extraidos")]
        public DatosExtraidos DatosExtraidos { get; set; } = new();

        [JsonPropertyName("analisis_completitud")]
        public AnalisisCompletitud AnalisisCompletitud { get; set; } = new();

        [JsonPropertyName("analisis_coherencia")]
        public AnalisisCoherencia AnalisisCoherencia { get; set; } = new();

        [JsonPropertyName("elementos_seguridad")]
        public ElementosSeguridad ElementosSeguridad { get; set; } = new();

        [JsonPropertyName("verificacion_fiscal")]
        public VerificacionFiscal VerificacionFiscal { get; set; } = new();

        [JsonPropertyName("senales_alerta")]
        public List<string> SenalesAlerta { get; set; } = new();

        [JsonPropertyName("evidencia")]
        public JsonElement? Evidencia { get; set; }

        [JsonPropertyName("veredicto")]
        public Veredicto Veredicto { get; set; } = new();

        [JsonPropertyName("recomendacion")]
        public string Recomendacion { get; set; } = "";

        [JsonPropertyName("archivo_origen")]
        public string? ArchivoOrigen { get; set; }

        [JsonPropertyName("nombre_archivo")]
        public string? NombreArchivo { get; set; }

        [JsonPropertyName("ruta_informe_pdf")]
        public string? RutaInformePdf { get; set; }

        [JsonPropertyName("nombre_informe_pdf")]
        public string? NombreInformePdf { get; set; }

        [JsonPropertyName("informe_matching")]
        public JsonElement? InformeMatching { get; set; }
    }
}
