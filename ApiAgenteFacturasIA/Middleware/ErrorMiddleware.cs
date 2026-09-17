using System.Net;
using System.Text.Json;

namespace ApiAgenteFacturasIA.Middleware
{
    public class ErrorMiddleware 
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorMiddleware> _logger;

        public ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var respuesta = new
            {
                mensaje = "Ocurrió un error interno.",
                detalle = ex.Message
            };

            var json = JsonSerializer.Serialize(respuesta);

            await context.Response.WriteAsync(json);
        }
    }
}
