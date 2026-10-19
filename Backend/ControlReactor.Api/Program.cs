using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;
using ControlReactor.Negocio.Servicios;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Datos.Repositorios;
using ControlReactor.Modelos.DTOs;
using ControlReactor.Api.Servicios;


var builder = WebApplication.CreateBuilder(args);

// Servicios para registro y verificación de usuarios.
builder.Services.AddHttpClient<ServicioValidacionCorreo>();
builder.Services.AddHttpClient<ServicioEnvioCorreo>();
builder.Services.AddScoped<ServicioRegistroUsuario>();
builder.Services.AddScoped<ServicioCodigoVerificacion>();
builder.Services.AddScoped<ServicioVerificacionCorreo>();
builder.Services.AddScoped<ServicioLoginUsuario>();
builder.Services.AddScoped<ServicioRecuperacionPassword>();
builder.Services.AddScoped<ServicioAvatarUsuario>();

// Revisa automáticamente las partidas activas
builder.Services.AddHostedService<MonitorPartidas>();

// Servicio de puntuación.
builder.Services.AddScoped<ServicioPuntuacion>();

// Servicio para determinar el ganador.
builder.Services.AddScoped<ServicioGanador>();

// Repositorios.
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<ICodigoVerificacionRepositorio, CodigoVerificacionRepositorio>();
builder.Services.AddScoped<IJugadorPartidaRepositorio, JugadorPartidaRepositorio>();
builder.Services.AddScoped<IPartidaRepositorio, PartidaRepositorio>();
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


// Consulta el puntaje de un participante.
app.MapGet("/api/puntuacion/{idJugadorPartida:int}", async (
    int idJugadorPartida,
    IJugadorPartidaRepositorio repositorio) =>
{
    var jugador = await repositorio.ObtenerPorIdAsync(idJugadorPartida);

    if (jugador == null)
    {
        return Results.NotFound(new
        {
            mensaje = "No se encontró al participante."
        });
    }

    return Results.Ok(new
    {
        idJugadorPartida = jugador.IdJugadorPartida,
        idUsuario = jugador.IdUsuario,
        idPartida = jugador.IdPartida,
        puntaje = jugador.Puntaje
    });
});

// Registra un movimiento y suma un punto al participante.
app.MapPost("/api/puntuacion/movimiento/{idJugadorPartida:int}", async (
    int idJugadorPartida,
    ServicioPuntuacion servicio) =>
{
    var puntaje = await servicio.RegistrarMovimiento(idJugadorPartida);

    if (puntaje == null)
    {
        return Results.BadRequest(new
        {
            mensaje = "El participante no existe o la partida no está activa."
        });
    }

    return Results.Ok(new
    {
        mensaje = "Movimiento registrado correctamente.",
        puntaje = puntaje.Value
    });
});

// Consulta quién tiene el mayor puntaje de una partida.
app.MapGet("/api/partidas/{idPartida:int}/ganador", async (
    int idPartida,
    ServicioGanador servicio) =>
{
    var resultado = await servicio.DeterminarGanadorAsync(idPartida);

    if (resultado.HayEmpate)
    {
        return Results.Ok(new
        {
            mensaje = "Hay empate en el primer lugar.",
            hayEmpate = true,
            idGanador = (int?)null
        });
    }

    if (resultado.IdGanador == null)
    {
        return Results.NotFound(new
        {
            mensaje = "No hay participantes en esta partida."
        });
    }

    return Results.Ok(new
    {
        mensaje = "Se identificó al jugador con mayor puntaje.",
        hayEmpate = false,
        idGanador = resultado.IdGanador
    });
});

// Endpoint temporal para probar la finalización de partidas.
app.MapPost("/api/partidas/{idPartida:int}/finalizar", async (
    int idPartida,
    ServicioGanador servicio) =>
{
    var mensaje = await servicio.FinalizarPartidaAsync(idPartida);

    return Results.Ok(new
    {
        mensaje
    });
});

// Consulta el estado de una partida y su ganador.
app.MapGet("/api/partidas/{idPartida:int}/estado", async (
    int idPartida,
    IPartidaRepositorio partidaRepositorio,
    IJugadorPartidaRepositorio jugadorRepositorio) =>
{
    var partida = await partidaRepositorio.ObtenerPorIdAsync(idPartida);

    if (partida == null)
    {
        return Results.NotFound(new
        {
            mensaje = "La partida no existe."
        });
    }

    var jugadores = await jugadorRepositorio.ObtenerPorPartidaAsync(idPartida);
    var ganador = jugadores.FirstOrDefault(j => j.EsGanador);

    return Results.Ok(new
    {
        idPartida = partida.IdPartida,
        estado = partida.Estado,
        fechaFin = partida.FechaFin,
        idGanador = ganador?.IdJugadorPartida,
        idUsuarioGanador = ganador?.IdUsuario
    });
});

// Consulta las puntuaciones de todos los jugadores de una partida.
app.MapGet("/api/partidas/{idPartida:int}/puntuaciones", async (
    int idPartida,
    IPartidaRepositorio partidaRepositorio,
    IJugadorPartidaRepositorio jugadorRepositorio) =>
{
    var partida = await partidaRepositorio.ObtenerPorIdAsync(idPartida);

    if (partida == null)
    {
        return Results.NotFound(new
        {
            mensaje = "La partida no existe."
        });
    }

    var jugadores = await jugadorRepositorio.ObtenerPorPartidaAsync(idPartida);

    return Results.Ok(jugadores.Select(j => new
    {
        idJugadorPartida = j.IdJugadorPartida,
        idUsuario = j.IdUsuario,
        puntaje = j.Puntaje,
        esGanador = j.EsGanador
    }));
});

// Solicitar un código para recuperar la contraseña.
app.MapPost("/api/usuarios/solicitar-recuperacion", async (
    SolicitarRecuperacionDto dto,
    ServicioRecuperacionPassword servicio) =>
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
app.MapPost("/api/usuarios/verificar-codigo-recuperacion", async (
    VerificarCodigoRecuperacionDto dto,
    ServicioRecuperacionPassword servicio) =>
{
    var resultado = await servicio.VerificarCodigoAsync(
        dto.Correo, dto.Codigo);

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
app.MapPost("/api/usuarios/cambiar-password", async (
    CambiarPasswordDto dto,
    ServicioRecuperacionPassword servicio) =>
{
    var resultado = await servicio.CambiarPasswordAsync(
        dto.Correo, dto.Codigo, dto.NuevaPassword);

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


app.MapPut("/api/usuarios/avatar", async (
    ActualizarAvatarDto dto,
    ServicioAvatarUsuario servicio) =>
{
    var resultado = await servicio.ActualizarAvatarAsync(
        dto.IdUsuario,
        dto.AvatarSeleccionado
    );

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


if (app.Configuration.GetValue<bool>("HabilitarPruebaAlmacenamiento"))
{
    app.MapGet("/api/prueba-almacenamiento", () =>
    {
        try
        {
            string carpeta = "/home/FotosPerfil";

            Directory.CreateDirectory(carpeta);

            string archivo = Path.Combine(carpeta, "prueba-api.txt");

            File.WriteAllText(archivo, "Prueba desde Control del Reactor");

            return Results.Ok(new
            {
                mensaje = "La API puede escribir en /home.",
                contenido = File.ReadAllText(archivo)
            });
        }
        catch
        {
            return Results.Problem(
                "No se pudo escribir en /home.",
                statusCode: 500
            );
        }
    });
}


app.Run();