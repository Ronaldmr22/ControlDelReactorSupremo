var builder = WebApplication.CreateBuilder(args);

// Servicios del backend
builder.Services.AddOpenApi();

var app = builder.Build();

// OpenAPI disponible durante desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Endpoint temporal para comprobar que el servidor funciona
app.MapGet("/api/prueba", () =>
{
    return "Servidor de Control del Reactor funcionando";
});

app.Run();