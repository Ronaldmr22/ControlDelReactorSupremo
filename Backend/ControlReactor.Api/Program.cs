using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Creacion de la conexion con  PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ControlReactorDbContext>(options =>options.UseNpgsql(connectionString));

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