using System.Text.Json;

namespace ApiAgenteFacturasIA.Models
{
    public class ChatMensaje
    {
        public string role { get; set; } = "user";
        public string text { get; set; } = "";
    }

    public class ChatPerfil
    {
        public string? nombre { get; set; }
    }

    public class ChatAdjuntoHistorial
    {
        public string ruta { get; set; } = "";
        public string? nombre { get; set; }
        public string? actualizado { get; set; }
    }

    public class ChatRequest
    {
        public string mensaje { get; set; } = "";
        public List<ChatMensaje> historial { get; set; } = new();
        public string? ruta_documento { get; set; }
        public JsonElement? resultado_contexto { get; set; }
        public ChatPerfil? perfil { get; set; }
        /// <summary>Hechos persistentes entre chats (no es el historial de mensajes).</summary>
        public string? memoria_durable { get; set; }
        /// <summary>Hechos UI a sincronizar a SQLite (one-shot / idempotente).</summary>
        public List<string>? hechos_sync { get; set; }
        public int? documentos_en_cola { get; set; }
        /// <summary>Rutas reales de la cola para verificar en-proceso.</summary>
        public List<string>? rutas_cola { get; set; }
        /// <summary>Adjuntos de chats (ruta + fecha) para pedidos por periodo.</summary>
        public List<ChatAdjuntoHistorial>? adjuntos_historial { get; set; }
        /// <summary>Expediente a reanudar (persistido en SQLite del agente).</summary>
        public string? caso_id { get; set; }
    }

    public class ChatResponse
    {
        public string respuesta { get; set; } = "";
        public bool fuera_de_ambito { get; set; }
        public List<string> herramientas_usadas { get; set; } = new();
        public ResultadoAnalisis? resultado_analisis { get; set; }
        public List<ResultadoAnalisis>? resultados_analisis { get; set; }
        /// <summary>descargar_pdf (verificar ya no se delega al front).</summary>
        public string? accion { get; set; }
        /// <summary>PDF generado cuando accion = descargar_pdf.</summary>
        public List<string>? rutas { get; set; }
        public string? caso_id { get; set; }
        public string? caso_estado { get; set; }
        public string? caso_objetivo { get; set; }
        public List<string>? caso_pendientes { get; set; }
    }
}
