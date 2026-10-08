using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;
using ControlReactor.Negocio.Servicios;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Datos.Repositorios;
using ControlReactor.Modelos.DTOs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<ServicioValidacionCorreo>();

builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();

builder.Services.AddScoped<ServicioRegistroUsuario>();


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


app.MapPost("/api/usuarios/registro", async (RegistroUsuarioDto dto,ServicioRegistroUsuario servicio) =>
{
    var resultado = await servicio.RegistrarAsync(dto);

    if (!resultado.Exito)
    {
        return Results.BadRequest(new
        {
            mensaje = resultado.Mensaje
        });
    }

    return Results.Ok(new
    {
        mensaje = resultado.Mensaje
    });
});

app.Run();