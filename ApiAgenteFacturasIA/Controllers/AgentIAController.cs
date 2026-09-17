using ApiAgenteFacturasIA.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using ApiAgenteFacturasIA.Models;
namespace ApiAgenteFacturasIA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AgentIAController : ControllerBase
    {
        private readonly IAgentIAService _agentIAService;
        public AgentIAController(ILogger<AgentIAController> logger, IAgentIAService agentIAService)
        {
            _agentIAService = agentIAService; 
        }

        [HttpGet("Health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                ok = true,
                servicio = "ApiAgenteFacturasIA",
                chat = "POST /api/AgentIA/Chat",
                analizar = "POST /api/AgentIA/Analizar",
            });
        }

        [HttpPost("Analizar")]
        public async Task<IActionResult> Analizar([FromForm] IFormFile file)
        {
            var result = await _agentIAService.AnalizarFactura(file);
            return Ok(result);
        }

        [HttpPost("Feedback")]
        public async Task<string> AgregarFeedback([FromBody] FeedbackIA feedback)
        {
            var result = await _agentIAService.AgregarFeedback(feedback);
            return result;
        }

        [HttpPost("Chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.mensaje))
            {
                return BadRequest(new { error = "El campo 'mensaje' es obligatorio." });
            }

            var result = await _agentIAService.Chat(request);
            return Ok(result);
        }

        [HttpPost("ChatStream")]
        public async Task ChatStream([FromBody] ChatRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.mensaje))
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                await Response.WriteAsJsonAsync(new { error = "El campo 'mensaje' es obligatorio." }, cancellationToken);
                return;
            }

            Response.ContentType = "application/x-ndjson; charset=utf-8";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
            var buffering = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
            buffering?.DisableBuffering();
            await Response.StartAsync(cancellationToken);

            await _agentIAService.ChatStream(
                request,
                async (line) =>
                {
                    await Response.WriteAsync(line + "\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                },
                cancellationToken);
        }

        [HttpGet("Documento")]
        public IActionResult Documento([FromQuery] string ruta)
        {
            string archivo;
            try
            {
                archivo = _agentIAService.ResolverDocumento(ruta);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { error = "No se encontro el documento en la maquina del agente." });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "El documento esta fuera del ambito del agente." });
            }

            var ext = Path.GetExtension(archivo).ToLowerInvariant();
            var tipo = ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                _ => "application/pdf",
            };
            return PhysicalFile(archivo, tipo);
        }
    }
}
