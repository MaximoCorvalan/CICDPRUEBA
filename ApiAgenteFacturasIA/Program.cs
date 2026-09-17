using ApiAgenteFacturasIA.Interfaces;
using ApiAgenteFacturasIA.Middleware;
using ApiAgenteFacturasIA.Services;
using Microsoft.AspNetCore.Http.Features;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls("http://0.0.0.0:5138");
}

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 104_857_600;
});
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 104_857_600;
    o.Limits.KeepAliveTimeout = TimeSpan.FromHours(2);
    o.Limits.MinRequestBodyDataRate = null;
    o.Limits.MinResponseDataRate = null;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<IAgentIAService, ServicesAgentIA>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
    app.MapOpenApi();
}

app.UseCors("ReactApp");
app.UseMiddleware<ErrorMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    servicio = "ApiAgenteFacturasIA",
    health = "/api/AgentIA/Health",
    chat = "POST /api/AgentIA/Chat",
    analizar = "POST /api/AgentIA/Analizar",
}));

app.Run();
