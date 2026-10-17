using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;
using ControlReactor.Negocio.Servicios;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Datos.Repositorios;
using ControlReactor.Modelos.DTOs;


var builder = WebApplication.CreateBuilder(args);

// Servicios para registro y verificación de usuarios.
builder.Services.AddHttpClient<ServicioValidacionCorreo>();
builder.Services.AddHttpClient<ServicioEnvioCorreo>();
builder.Services.AddScoped<ServicioRegistroUsuario>();
builder.Services.AddScoped<ServicioCodigoVerificacion>();
builder.Services.AddScoped<ServicioVerificacionCorreo>();
builder.Services.AddScoped<ServicioLoginUsuario>();
builder.Services.AddScoped<ServicioRecuperacionPassword>();

// Repositorios.
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<ICodigoVerificacionRepositorio, CodigoVerificacionRepositorio>();
builder.Services.AddScoped<ICodigoRecuperacionRepositorio, CodigoRecuperacionRepositorio>();

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

// Verifica el correo mediante el código recibido.
app.MapPost("/api/usuarios/verificar-correo",
    async (VerificarCorreoDto dto, ServicioVerificacionCorreo servicio) =>
    {
        var resultado = await servicio.VerificarAsync(
            dto.Correo,
            dto.Codigo
        );

        if (!resultado.Exito)
            return Results.BadRequest(new { mensaje = resultado.Mensaje });

        return Results.Ok(new { mensaje = resultado.Mensaje });
    });

// Permite solicitar un nuevo código de verificación
app.MapPost("/api/usuarios/reenviar-codigo", async (ReenviarCodigoDto datos, ServicioVerificacionCorreo servicio) =>
{
    // Solicitar el reenvío del código
    var resultado = await servicio.ReenviarCodigoAsync(datos.Correo);

    // Devolver la respuesta al cliente
    return resultado.Exito
        ? Results.Ok(new { mensaje = resultado.Mensaje })
        : Results.BadRequest(new { mensaje = resultado.Mensaje });
});
 // Inicio de sesión de usuarios.
app.MapPost("/api/usuarios/login", async (
    LoginUsuarioDto dto,
    ServicioLoginUsuario servicio) =>
{
    var resultado = await servicio.LoginAsync(dto);

    if (!resultado.Exito)
    {
        return Results.Json(new { mensaje = resultado.Mensaje }, statusCode: 401);
    }

    return Results.Ok(new
    {
        mensaje = resultado.Mensaje,
        idUsuario = resultado.Usuario!.IdUsuario,
        nombreUsuario = resultado.Usuario.NombreUsuario
    });
});

// Solicitar un código para recuperar la contraseña.
app.MapPost("/api/usuarios/solicitar-recuperacion", async (SolicitarRecuperacionDto dto,ServicioRecuperacionPassword servicio) =>
{
    var resultado = await servicio.SolicitarRecuperacionAsync(dto.Correo);

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

// Verificar el código de recuperación.
app.MapPost("/api/usuarios/verificar-codigo-recuperacion",async (VerificarCodigoRecuperacionDto dto,ServicioRecuperacionPassword servicio) =>
{
    var resultado = await servicio.VerificarCodigoAsync(dto.Correo, dto.Codigo);

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

// Restablecer la contraseña del usuario.
app.MapPost("/api/usuarios/cambiar-password", async (CambiarPasswordDto dto, ServicioRecuperacionPassword servicio) =>
{
    var resultado = await servicio.CambiarPasswordAsync( dto.Correo, dto.Codigo, dto.NuevaPassword);

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