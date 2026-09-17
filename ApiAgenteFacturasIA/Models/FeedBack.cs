using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiAgenteFacturasIA.Models
{
    public class FeedbackIA
    {
        public int? Id { get; set; }

        public DateTime Fecha { get; set; }

        public Dictionary<string, string> Documentos { get; set; } = new();

        public string? tipo_documento { get; set; }

        public double? autenticidad_original { get; set; }

        public string? estado_original { get; set; }

        public bool correcto { get; set; }

        /// <summary>
        /// Obligatorio cuando correcto=false. Valores: LEGITIMA | SOSPECHOSA | FALSA
        /// </summary>
        public string? estado_correcto { get; set; }

        /// <summary>
        /// Snapshot del paquete de evidencia del análisis (para entrenar).
        /// </summary>
        public JsonElement? evidencia_json { get; set; }

        public List<CorreccionIA> Correcciones { get; set; } = new();

        public string? comentario { get; set; }
    }

    public class CorreccionIA
    {
        public int? Id { get; set; }

        public string Campo { get; set; } = string.Empty;

        public string? valor_sistema { get; set; }

        public string? valor_correcto { get; set; }
        public string? Descripcion { get; set; }
    }
}
