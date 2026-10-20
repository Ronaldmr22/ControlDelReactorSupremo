using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using ControlReactor.Modelos.DTOs;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace ControlReactor.Tests;

public class PruebasRegistroUsuario
{
    [Fact]
    public async Task RegistrarAsync_DebeRegistrarUsuarioCuandoTodoEsValido()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        repositorio.ExisteNombreUsuarioAsync("Alice").Returns(false);
        repositorio.ExisteCorreoAsync("alice@example.com").Returns(false);
        repositorio.CrearAsync(Arg.Any<Usuario>())
            .Returns(ci =>
            {
                var usuario = ci.Arg<Usuario>();
                usuario.IdUsuario = 7;
                return usuario;
            });

        var validadorCorreo = CrearValidadorCorreo(entregable: true);
        var verificacion = CrearServicioVerificacion(repositorio, envioExitoso: true);
        var servicio = new ServicioRegistroUsuario(repositorio, validadorCorreo, verificacion);

        var dto = new RegistroUsuarioDto
        {
            NombreUsuario = " Alice ",
            Correo = "alice@example.com",
            Password = "12345678",
            ConfirmarPassword = "12345678"
        };

        var resultado = await servicio.RegistrarAsync(dto);

        Assert.True(resultado.Exito);
        Assert.Equal("Usuario registrado. Revisa tu correo para verificar tu cuenta.", resultado.Mensaje);
        await repositorio.Received(1).CrearAsync(Arg.Is<Usuario>(u =>
            u.NombreUsuario == "Alice" &&
            u.Correo == "alice@example.com" &&
            u.CorreoVerificado == false &&
            !string.IsNullOrWhiteSpace(u.PasswordHash)));
    }

    [Fact]
    public async Task RegistrarAsync_DebeRechazarCamposObligatorios()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        var validadorCorreo = CrearValidadorCorreo(entregable: true);
        var verificacion = CrearServicioVerificacion(repositorio, envioExitoso: true);
        var servicio = new ServicioRegistroUsuario(repositorio, validadorCorreo, verificacion);

        var dto = new RegistroUsuarioDto
        {
            NombreUsuario = "",
            Correo = "alice@example.com",
            Password = "12345678",
            ConfirmarPassword = "12345678"
        };

        var resultado = await servicio.RegistrarAsync(dto);

        Assert.False(resultado.Exito);
        Assert.Equal("Todos los campos son obligatorios.", resultado.Mensaje);
        await repositorio.DidNotReceive().CrearAsync(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task RegistrarAsync_DebeRechazarContrasenaCorta()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        var validadorCorreo = CrearValidadorCorreo(entregable: true);
        var verificacion = CrearServicioVerificacion(repositorio, envioExitoso: true);
        var servicio = new ServicioRegistroUsuario(repositorio, validadorCorreo, verificacion);

        var dto = new RegistroUsuarioDto
        {
            NombreUsuario = "Alice",
            Correo = "alice@example.com",
            Password = "1234567",
            ConfirmarPassword = "1234567"
        };

        var resultado = await servicio.RegistrarAsync(dto);

        Assert.False(resultado.Exito);
        Assert.Equal("La contraseña debe tener al menos 8 caracteres.", resultado.Mensaje);
        await repositorio.DidNotReceive().CrearAsync(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task RegistrarAsync_DebeRechazarSiLasContrasenasNoCoinciden()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        var validadorCorreo = CrearValidadorCorreo(entregable: true);
        var verificacion = CrearServicioVerificacion(repositorio, envioExitoso: true);
        var servicio = new ServicioRegistroUsuario(repositorio, validadorCorreo, verificacion);

        var dto = new RegistroUsuarioDto
        {
            NombreUsuario = "Alice",
            Correo = "alice@example.com",
            Password = "12345678",
            ConfirmarPassword = "87654321"
        };

        var resultado = await servicio.RegistrarAsync(dto);

        Assert.False(resultado.Exito);
        Assert.Equal("Las contraseñas no coinciden.", resultado.Mensaje);
        await repositorio.DidNotReceive().CrearAsync(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task RegistrarAsync_DebeRechazarCorreoConFormatoInvalido()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        var validadorCorreo = CrearValidadorCorreo(entregable: true);
        var verificacion = CrearServicioVerificacion(repositorio, envioExitoso: true);
        var servicio = new ServicioRegistroUsuario(repositorio, validadorCorreo, verificacion);

        var dto = new RegistroUsuarioDto
        {
            NombreUsuario = "Alice",
            Correo = "correo-invalido",
            Password = "12345678",
            ConfirmarPassword = "12345678"
        };

        var resultado = await servicio.RegistrarAsync(dto);

        Assert.False(resultado.Exito);
        Assert.Equal("El formato del correo no es válido.", resultado.Mensaje);
        await repositorio.DidNotReceive().CrearAsync(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task RegistrarAsync_DebeRechazarSiNombreUsuarioYaExiste()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        repositorio.ExisteNombreUsuarioAsync("Alice").Returns(true);
        var validadorCorreo = CrearValidadorCorreo(entregable: true);
        var verificacion = CrearServicioVerificacion(repositorio, envioExitoso: true);
        var servicio = new ServicioRegistroUsuario(repositorio, validadorCorreo, verificacion);

        var dto = new RegistroUsuarioDto
        {
            NombreUsuario = "Alice",
            Correo = "alice@example.com",
            Password = "12345678",
            ConfirmarPassword = "12345678"
        };

        var resultado = await servicio.RegistrarAsync(dto);

        Assert.False(resultado.Exito);
        Assert.Equal("El nombre de usuario ya está registrado.", resultado.Mensaje);
        await repositorio.DidNotReceive().CrearAsync(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task RegistrarAsync_DebeRechazarSiCorreoYaExiste()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        repositorio.ExisteNombreUsuarioAsync("Alice").Returns(false);
        repositorio.ExisteCorreoAsync("alice@example.com").Returns(true);
        var validadorCorreo = CrearValidadorCorreo(entregable: true);
        var verificacion = CrearServicioVerificacion(repositorio, envioExitoso: true);
        var servicio = new ServicioRegistroUsuario(repositorio, validadorCorreo, verificacion);

        var dto = new RegistroUsuarioDto
        {
            NombreUsuario = "Alice",
            Correo = "alice@example.com",
            Password = "12345678",
            ConfirmarPassword = "12345678"
        };

        var resultado = await servicio.RegistrarAsync(dto);

        Assert.False(resultado.Exito);
        Assert.Equal("El correo ya está registrado.", resultado.Mensaje);
        await repositorio.DidNotReceive().CrearAsync(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task RegistrarAsync_DebeRechazarSiCorreoNoEntregable()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        repositorio.ExisteNombreUsuarioAsync("Alice").Returns(false);
        repositorio.ExisteCorreoAsync("alice@example.com").Returns(false);
        var validadorCorreo = CrearValidadorCorreo(entregable: false);
        var verificacion = CrearServicioVerificacion(repositorio, envioExitoso: true);
        var servicio = new ServicioRegistroUsuario(repositorio, validadorCorreo, verificacion);

        var dto = new RegistroUsuarioDto
        {
            NombreUsuario = "Alice",
            Correo = "alice@example.com",
            Password = "12345678",
            ConfirmarPassword = "12345678"
        };

        var resultado = await servicio.RegistrarAsync(dto);

        Assert.False(resultado.Exito);
        Assert.Equal("No se pudo confirmar que el correo pueda recibir mensajes.", resultado.Mensaje);
        await repositorio.DidNotReceive().CrearAsync(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task RegistrarAsync_DebeIndicarseErrorSiNoSePuedeEnviarCodigo()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        repositorio.ExisteNombreUsuarioAsync("Alice").Returns(false);
        repositorio.ExisteCorreoAsync("alice@example.com").Returns(false);
        repositorio.CrearAsync(Arg.Any<Usuario>())
            .Returns(ci =>
            {
                var usuario = ci.Arg<Usuario>();
                usuario.IdUsuario = 9;
                return usuario;
            });

        var validadorCorreo = CrearValidadorCorreo(entregable: true);
        var verificacion = CrearServicioVerificacion(repositorio, envioExitoso: false);
        var servicio = new ServicioRegistroUsuario(repositorio, validadorCorreo, verificacion);

        var dto = new RegistroUsuarioDto
        {
            NombreUsuario = "Alice",
            Correo = "alice@example.com",
            Password = "12345678",
            ConfirmarPassword = "12345678"
        };

        var resultado = await servicio.RegistrarAsync(dto);

        Assert.False(resultado.Exito);
        Assert.Equal("La cuenta fue creada, pero no se pudo enviar el código. Intenta solicitar uno nuevo más tarde.", resultado.Mensaje);
        await repositorio.Received(1).CrearAsync(Arg.Any<Usuario>());
    }

    private static ServicioValidacionCorreo CrearValidadorCorreo(bool entregable)
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var payload = entregable
                ? new { email_deliverability = new { status = "deliverable" } }
                : new { email_deliverability = new { status = "undeliverable" } };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(payload)
            };
        });

        var httpClient = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AbstractApi:ApiKey"] = "test-key"
            })
            .Build();

        return new ServicioValidacionCorreo(httpClient, config);
    }

    private static ServicioVerificacionCorreo CrearServicioVerificacion(IUsuarioRepositorio repositorio, bool envioExitoso)
    {
        var codigoRepo = Substitute.For<ICodigoVerificacionRepositorio>();
        var servicioEnvioCorreo = new ServicioEnvioCorreo(
            new HttpClient(new StubHttpMessageHandler(_ =>
            {
                if (!envioExitoso)
                {
                    throw new HttpRequestException("Fallo simulado del envío");
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            })),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Brevo:ApiKey"] = "test-key",
                    ["Brevo:Remitente"] = "no-reply@example.com"
                })
                .Build());

        return new ServicioVerificacionCorreo(
            codigoRepo,
            new ServicioCodigoVerificacion(),
            servicioEnvioCorreo,
            repositorio);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respuesta;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respuesta)
        {
            _respuesta = respuesta;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_respuesta(request));
        }
    }
}
