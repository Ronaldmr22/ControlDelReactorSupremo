using ControlReactor.Datos.Contexto;
using Microsoft.EntityFrameworkCore;
using ControlReactor.Negocio.Servicios;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Datos.Repositorios;
using ControlReactor.Modelos.DTOs;
using ControlReactor.Api.Servicios;
using ControlReactor.Modelos.Entidades;


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
builder.Services.AddScoped<ServicioFotoPerfil>();

builder.Services.AddScoped<ServicioLogros>();

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
builder.Services.AddScoped<ISalaRepositorio, SalaRepositorio>();
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

app.MapPost("/api/usuarios/foto-perfil",
    async (HttpRequest request, ServicioFotoPerfil servicio) =>
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Debes enviar un formulario con una imagen.");
        }
        const long limiteSolicitud = 3 * 1024 * 1024; // 3 MB

        if (request.ContentLength > limiteSolicitud)
        {
            return Results.BadRequest(
                "La solicitud supera el tamaño permitido.");
        }

        var formulario = await request.ReadFormAsync();

        if (!int.TryParse(formulario["idUsuario"], out int idUsuario)
            || idUsuario <= 0)
        {
            return Results.BadRequest("El ID del usuario no es válido.");
        }

        var foto = formulario.Files.GetFile("foto");

        if (foto == null || foto.Length == 0)
        {
            return Results.BadRequest("Debes seleccionar una fotografía.");
        }
        const long tamañoMaximo = 2 * 1024 * 1024; // 2 MB

        if (foto.Length > tamañoMaximo)
        {
            return Results.BadRequest("La fotografía no puede superar los 2 MB.");
        }

        string extension = Path.GetExtension(foto.FileName).ToLowerInvariant();

        string[] extensionesPermitidas =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        if (!extensionesPermitidas.Contains(extension))
        {
            return Results.BadRequest("Solo se permiten imágenes JPG, JPEG, PNG o WebP.");
        }

        byte[] encabezado = new byte[12];

        using (var stream = foto.OpenReadStream())
        {
            int leidos = await stream.ReadAsync(encabezado);

            if (leidos < 12)
            {
                return Results.BadRequest("La imagen no es válida.");
            }
        }

        bool esJpg =
            encabezado[0] == 0xFF &&
            encabezado[1] == 0xD8 &&
            encabezado[2] == 0xFF;

        bool esPng =
            encabezado[0] == 0x89 &&
            encabezado[1] == 0x50 &&
            encabezado[2] == 0x4E &&
            encabezado[3] == 0x47 &&
            encabezado[4] == 0x0D &&
            encabezado[5] == 0x0A &&
            encabezado[6] == 0x1A &&
            encabezado[7] == 0x0A;

        bool esWebp =
            encabezado[0] == 'R' &&
            encabezado[1] == 'I' &&
            encabezado[2] == 'F' &&
            encabezado[3] == 'F' &&
            encabezado[8] == 'W' &&
            encabezado[9] == 'E' &&
            encabezado[10] == 'B' &&
            encabezado[11] == 'P';

        bool formatoValido = extension switch
        {
            ".jpg" or ".jpeg" => esJpg,
            ".png" => esPng,
            ".webp" => esWebp,
            _ => false
        };

        if (!formatoValido)
        {
            return Results.BadRequest("El contenido del archivo no coincide con un formato de imagen permitido.");
        }

        string carpeta = app.Environment.IsDevelopment()
            ? Path.Combine(app.Environment.ContentRootPath, "FotosPerfil")
            : "/home/FotosPerfil";

        Directory.CreateDirectory(carpeta);

        // Generamos un nombre único para evitar archivos repetidos.
        string nombreArchivo = $"{Guid.NewGuid():N}{extension}";

        string rutaArchivo = Path.Combine(carpeta, nombreArchivo);

        // Guardamos la fotografía.
        try
        {
            // Guardar la fotografía en el servidor.
            await using (var stream = new FileStream(
                rutaArchivo,
                FileMode.CreateNew,
                FileAccess.Write))
            {
                await foto.CopyToAsync(stream);
            }

            // Construir la URL de la fotografía.
            string urlFoto = app.Environment.IsDevelopment()
                ? $"http://localhost:5008/api/fotos-perfil/{nombreArchivo}"
                : $"https://control-reactor-api-a0e4atckgma2d0as.canadacentral-01.azurewebsites.net/api/fotos-perfil/{nombreArchivo}";

            // Guardar la URL en PostgreSQL.
            var resultado = await servicio.ActualizarFotoAsync(idUsuario, urlFoto);

            if (!resultado.Exito)
            {
                File.Delete(rutaArchivo);

                return Results.BadRequest(resultado.Mensaje);
            }
        }
        catch (Exception)
        {
            // Eliminar la fotografía si ocurrió algún error.
            if (File.Exists(rutaArchivo))
            {
                File.Delete(rutaArchivo);
            }

            return Results.Problem(
                "Ocurrió un error al guardar la fotografía.",
                statusCode: 500);
        }

        return Results.Ok(new
        {
            mensaje = "Fotografía guardada correctamente.",
            archivo = nombreArchivo,
            idUsuario
        });
});

app.MapGet("/api/fotos-perfil/{nombreArchivo}", (string nombreArchivo) =>
{
    // Evitar rutas como ../ y nombres inesperados
    if (!System.Text.RegularExpressions.Regex.IsMatch(
        nombreArchivo,
        @"^[a-fA-F0-9]{32}\.(jpg|jpeg|png|webp)$"))
    {
        return Results.BadRequest("Nombre de archivo no válido.");
    }

    string carpeta = app.Environment.IsDevelopment()
        ? Path.Combine(app.Environment.ContentRootPath, "FotosPerfil")
        : "/home/FotosPerfil";

    string rutaArchivo = Path.Combine(carpeta, nombreArchivo);

    if (!File.Exists(rutaArchivo))
    {
        return Results.NotFound("Fotografía no encontrada.");
    }

    string tipoContenido = Path.GetExtension(nombreArchivo).ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    return Results.File(rutaArchivo, tipoContenido);
});

// Consulta la información pública del perfil de un usuario.
app.MapGet("/api/usuarios/{idUsuario:int}/perfil", async (
    int idUsuario,
    ControlReactorDbContext db) =>
{

    if (idUsuario <= 0)
    {
        return Results.BadRequest(new
        {
            mensaje = "El ID del usuario no es válido."
        });
    } 

    var usuario = await db.Usuarios
        .AsNoTracking()
        .Where(u => u.IdUsuario == idUsuario)
        .Select(u => new
        {
            idUsuario = u.IdUsuario,
            nombreUsuario = u.NombreUsuario,
            correo = u.Correo,
            correoVerificado = u.CorreoVerificado,
            avatarSeleccionado = u.AvatarSeleccionado,
            urlFotoPerfil = u.UrlFotoPerfil     
        })
        .FirstOrDefaultAsync();

    if (usuario == null)
    {
        return Results.NotFound(new
        {
            mensaje = "El usuario no existe."
        });
      
        }

    return Results.Ok(usuario);
});

// Consulta las estadísticas históricas del usuario.
app.MapGet("/api/usuarios/{idUsuario:int}/estadisticas", async (
    int idUsuario,
    ControlReactorDbContext db,
    IJugadorPartidaRepositorio jugadorRepositorio,
    ServicioLogros servicioLogros) =>
{
    if (idUsuario <= 0)
    {
        return Results.BadRequest(new
        {
            mensaje = "ID de usuario inválido."
        });
    }

    var existeUsuario = await db.Usuarios
        .AnyAsync(u => u.IdUsuario == idUsuario);

    if (!existeUsuario)
    {
        return Results.NotFound(new
        {
            mensaje = "El usuario no existe."
        });
    }

    var participaciones = await jugadorRepositorio
        .ObtenerPorUsuarioAsync(idUsuario);

    // Solo contabilizamos partidas finalizadas.
    var finalizadas = participaciones
        .Where(j => j.Partida.FechaFin != null)
        .ToList();

    int partidasJugadas = finalizadas.Count;
    int partidasGanadas = finalizadas.Count(j => j.EsGanador);
    int partidasPerdidas = partidasJugadas - partidasGanadas;
    int puntosTotales = finalizadas.Sum(j => j.Puntaje);

    var historial = finalizadas.Select(j => new
    {
        idPartida = j.IdPartida,
        fechaInicio = j.Partida.FechaInicio,
        fechaFin = j.Partida.FechaFin,
        puntaje = j.Puntaje,
        esGanador = j.EsGanador
    });

    // Calcular la posición del usuario en el ranking global.
    var rankingGlobal = await db.JugadoresPartida
        .AsNoTracking()
        .Where(j => j.Partida.FechaFin != null)
        .GroupBy(j => j.IdUsuario)
        .Select(g => new
        {
            idUsuario = g.Key,
            puntosTotales = g.Sum(j => j.Puntaje),
            partidasGanadas = g.Count(j => j.EsGanador)
        })
        .OrderByDescending(j => j.puntosTotales)
        .ThenByDescending(j => j.partidasGanadas)
        .ThenBy(j => j.idUsuario)
        .ToListAsync();

    int? posicionRanking = null;

    for (int i = 0; i < rankingGlobal.Count; i++)
    {
        if (rankingGlobal[i].idUsuario == idUsuario)
        {
            posicionRanking = i + 1;
            break;
        }
    }

    var logros = servicioLogros.ObtenerLogros(
    partidasJugadas,
    partidasGanadas);

    return Results.Ok(new
    {
        idUsuario,
        partidasJugadas,
        partidasGanadas,
        partidasPerdidas,
        puntosTotales,
        posicionRanking,
        logros,
        historialPartidas = historial
    });
});

// Ranking global de jugadores según puntos históricos.
app.MapGet("/api/usuarios/ranking", async (
    ControlReactorDbContext db) =>
{
    var ranking = await db.JugadoresPartida
        .AsNoTracking()
        .Where(j => j.Partida.FechaFin != null)
        .GroupBy(j => j.IdUsuario)
        .Select(g => new
        {
            idUsuario = g.Key,
            puntosTotales = g.Sum(j => j.Puntaje),
            partidasJugadas = g.Count(),
            partidasGanadas = g.Count(j => j.EsGanador)
        })
        .OrderByDescending(j => j.puntosTotales)
        .ThenByDescending(j => j.partidasGanadas)
        .ThenBy(j => j.idUsuario)
        .ToListAsync();

    var resultado = ranking.Select((j, indice) => new
    {
        posicion = indice + 1,
        j.idUsuario,
        j.puntosTotales,
        j.partidasJugadas,
        j.partidasGanadas
    });

    return Results.Ok(resultado);
});
app.MapPost("/api/salas", async (
    CrearSalaDto datos,
    ISalaRepositorio repositorio) =>
{
    var resultado = await repositorio.CrearAsync(datos.IdUsuario);

    if (resultado.Sala == null)
        return Results.BadRequest(new { mensaje = resultado.Mensaje });

    return Results.Ok(resultado.Sala);
});

// Incorpora un usuario mediante el código de sala.
app.MapPost("/api/salas/unirse", async (
    UnirseSalaDto datos,
    ISalaRepositorio repositorio) =>
{
    var resultado = await repositorio.UnirseAsync(
        datos.IdUsuario, datos.CodigoSala);

    if (resultado.Sala == null)
        return Results.BadRequest(new { mensaje = resultado.Mensaje });

    return Results.Ok(resultado.Sala);
});

// Consulta el estado y los participantes de una sala.
app.MapGet("/api/salas/{codigoSala}", async (
    string codigoSala,
    ISalaRepositorio repositorio) =>
{
    var sala = await repositorio.ObtenerPorCodigoAsync(codigoSala);

    if (sala == null)
        return Results.NotFound(new { mensaje = "La sala no existe." });

    return Results.Ok(sala);
});

// Confirma que el participante sigue conectado.
app.MapPost("/api/salas/actividad", async (
    UnirseSalaDto datos,
    ISalaRepositorio repositorio) =>
{
    var resultado = await repositorio.RegistrarActividadAsync(
        datos.IdUsuario, datos.CodigoSala);

    if (!resultado.Exito)
        return Results.BadRequest(new { mensaje = resultado.Mensaje });

    return Results.Ok(new { mensaje = resultado.Mensaje });
});

// Libera el espacio del participante.
app.MapPost("/api/salas/salir", async (
    UnirseSalaDto datos,
    ISalaRepositorio repositorio) =>
{
    var resultado = await repositorio.SalirAsync(
        datos.IdUsuario, datos.CodigoSala);

    if (!resultado.Exito)
        return Results.BadRequest(new { mensaje = resultado.Mensaje });

    return Results.Ok(new { mensaje = resultado.Mensaje });
});

// Inicia la partida cuando lo solicita el creador de la sala.
app.MapPost("/api/salas/iniciar", async (
    UnirseSalaDto datos,
    ISalaRepositorio repositorio) =>
{
    var resultado = await repositorio.IniciarPartidaAsync(
        datos.IdUsuario,
        datos.CodigoSala);

    if (!resultado.Exito)
        return Results.BadRequest(new
        {
            mensaje = resultado.Mensaje
        });

    return Results.Ok(new
    {
        mensaje = resultado.Mensaje
    });
});

app.Run();