using ApiAgenteFacturasIA.Models;

namespace ApiAgenteFacturasIA.Interfaces
{
    public interface IAgentIAService
    {

        public Task<ResultadoAnalisis>AnalizarFactura(IFormFile archivo);

        public Task< string> AgregarFeedback(FeedbackIA feedback);

        public Task<ChatResponse> Chat(ChatRequest request);

        public Task ChatStream(ChatRequest request, Func<string, Task> writeLine, CancellationToken cancellationToken);

        public string ResolverDocumento(string ruta);
    }
}
