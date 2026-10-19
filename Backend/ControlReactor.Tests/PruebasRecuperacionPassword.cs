using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace ControlReactor.Tests;

public class PruebasRecuperacionPassword
{
    [Fact]
    public async Task SolicitarRecuperacion_DebeEnviarCodigoCuandoElCorreoExiste()
    {
        var repositorioCodigo = Substitute.For<ICodigoRecuperacionRepositorio>();
        var repositorioUsuarios = Substitute.For<IUsuarioRepositorio>();
        var servicio = CrearServicio(repositorioCodigo, repositorioUsuarios, envioExitoso: true);

        var usuario = new Usuario
        {
            IdUsuario = 10,
            NombreUsuario = "Ana",
            Correo = "ana@example.com",
            CorreoVerificado = true
        };

        repositorioUsuarios.ObtenerPorCorreoAsync("ana@example.com").Returns(usuario);
        repositorioCodigo.ObtenerUltimoAsync(10).Returns((CodigoRecuperacion?)null);

        var resultado = await servicio.SolicitarRecuperacionAsync("ana@example.com");

        Assert.True(resultado.Exito);
        Assert.Equal("Si el correo está registrado, recibirás un código de recuperación.", resultado.Mensaje);
        await repositorioCodigo.Received(1).InvalidarCodigosAnterioresAsync(10);
        await repositorioCodigo.Received(1).GuardarAsync(Arg.Any<CodigoRecuperacion>());
    }

    [Fact]
    public async Task SolicitarRecuperacion_DebeDevolverMensajeGenericoCuandoElCorreoNoExiste()
    {
        var repositorioCodigo = Substitute.For<ICodigoRecuperacionRepositorio>();
        var repositorioUsuarios = Substitute.For<IUsuarioRepositorio>();
        var servicio = CrearServicio(repositorioCodigo, repositorioUsuarios, envioExitoso: true);

        repositorioUsuarios.ObtenerPorCorreoAsync("desconocido@example.com").Returns((Usuario?)null);

        var resultado = await servicio.SolicitarRecuperacionAsync("desconocido@example.com");

        Assert.True(resultado.Exito);
        Assert.Equal("Si el correo está registrado, recibirás un código de recuperación.", resultado.Mensaje);
        await repositorioCodigo.DidNotReceive().GuardarAsync(Arg.Any<CodigoRecuperacion>());
    }

    [Fact]
    public async Task SolicitarRecuperacion_DebeRechazarCorreoVacio()
    {
        var repositorioCodigo = Substitute.For<ICodigoRecuperacionRepositorio>();
        var repositorioUsuarios = Substitute.For<IUsuarioRepositorio>();
        var servicio = CrearServicio(repositorioCodigo, repositorioUsuarios, envioExitoso: true);

        var resultado = await servicio.SolicitarRecuperacionAsync(" ");

        Assert.False(resultado.Exito);
        Assert.Equal("Debes ingresar un correo electrónico.", resultado.Mensaje);
    }

    [Fact]
    public async Task VerificarCodigo_DebeAceptarCodigoValido()
    {
        var repositorioCodigo = Substitute.For<ICodigoRecuperacionRepositorio>();
        var repositorioUsuarios = Substitute.For<IUsuarioRepositorio>();
        var servicio = CrearServicio(repositorioCodigo, repositorioUsuarios, envioExitoso: true);

        var usuario = new Usuario
        {
            IdUsuario = 20,
            NombreUsuario = "Luis",
            Correo = "luis@example.com",
            CorreoVerificado = true
        };

        var codigo = "123456";
        var hasher = new PasswordHasher<CodigoRecuperacion>();
        var registro = new CodigoRecuperacion
        {
            IdCodigoRecuperacion = 5,
            IdUsuario = 20,
            FechaExpiracion = DateTime.UtcNow.AddMinutes(10),
            Utilizado = false,
            IntentosFallidos = 0,
            CodigoHash = hasher.HashPassword(new CodigoRecuperacion(), codigo)
        };

        repositorioUsuarios.ObtenerPorCorreoAsync("luis@example.com").Returns(usuario);
        repositorioCodigo.ObtenerUltimoAsync(20).Returns(registro);

        var resultado = await servicio.VerificarCodigoAsync("luis@example.com", codigo);

        Assert.True(resultado.Exito);
        Assert.Equal("Código de recuperación válido.", resultado.Mensaje);
    }

    [Fact]
    public async Task VerificarCodigo_DebeRechazarCodigoIncorrecto()
    {
        var repositorioCodigo = Substitute.For<ICodigoRecuperacionRepositorio>();
        var repositorioUsuarios = Substitute.For<IUsuarioRepositorio>();
        var servicio = CrearServicio(repositorioCodigo, repositorioUsuarios, envioExitoso: true);

        var usuario = new Usuario
        {
            IdUsuario = 21,
            NombreUsuario = "Marta",
            Correo = "marta@example.com",
            CorreoVerificado = true
        };

        var codigoCorrecto = "123456";
        var codigoIncorrecto = "654321";
        var hasher = new PasswordHasher<CodigoRecuperacion>();
        var registro = new CodigoRecuperacion
        {
            IdCodigoRecuperacion = 6,
            IdUsuario = 21,
            FechaExpiracion = DateTime.UtcNow.AddMinutes(10),
            Utilizado = false,
            IntentosFallidos = 0,
            CodigoHash = hasher.HashPassword(new CodigoRecuperacion(), codigoCorrecto)
        };

        repositorioUsuarios.ObtenerPorCorreoAsync("marta@example.com").Returns(usuario);
        repositorioCodigo.ObtenerUltimoAsync(21).Returns(registro);

        var resultado = await servicio.VerificarCodigoAsync("marta@example.com", codigoIncorrecto);

        Assert.False(resultado.Exito);
        Assert.Equal("El código no es válido o ha expirado.", resultado.Mensaje);
        await repositorioCodigo.Received(1).ActualizarAsync(Arg.Is<CodigoRecuperacion>(r => r.IntentosFallidos == 1));
    }

    [Fact]
    public async Task VerificarCodigo_DebeRechazarCodigoExpirado()
    {
        var repositorioCodigo = Substitute.For<ICodigoRecuperacionRepositorio>();
        var repositorioUsuarios = Substitute.For<IUsuarioRepositorio>();
        var servicio = CrearServicio(repositorioCodigo, repositorioUsuarios, envioExitoso: true);

        var usuario = new Usuario
        {
            IdUsuario = 22,
            NombreUsuario = "Nora",
            Correo = "nora@example.com",
            CorreoVerificado = true
        };

        var hasher = new PasswordHasher<CodigoRecuperacion>();
        var registro = new CodigoRecuperacion
        {
            IdCodigoRecuperacion = 7,
            IdUsuario = 22,
            FechaExpiracion = DateTime.UtcNow.AddMinutes(-1),
            Utilizado = false,
            IntentosFallidos = 0,
            CodigoHash = hasher.HashPassword(new CodigoRecuperacion(), "123456")
        };

        repositorioUsuarios.ObtenerPorCorreoAsync("nora@example.com").Returns(usuario);
        repositorioCodigo.ObtenerUltimoAsync(22).Returns(registro);

        var resultado = await servicio.VerificarCodigoAsync("nora@example.com", "123456");

        Assert.False(resultado.Exito);
        Assert.Equal("El código no es válido o ha expirado.", resultado.Mensaje);
    }

    [Fact]
    public async Task CambiarPassword_DebeActualizarLaContrasenaCuandoElCodigoEsValido()
    {
        var repositorioCodigo = Substitute.For<ICodigoRecuperacionRepositorio>();
        var repositorioUsuarios = Substitute.For<IUsuarioRepositorio>();
        var servicio = CrearServicio(repositorioCodigo, repositorioUsuarios, envioExitoso: true);

        var usuario = new Usuario
        {
            IdUsuario = 30,
            NombreUsuario = "Pablo",
            Correo = "pablo@example.com",
            CorreoVerificado = true
        };

        repositorioUsuarios.ObtenerPorCorreoAsync("pablo@example.com").Returns(usuario);
        repositorioCodigo.ObtenerUltimoAsync(30).Returns(new CodigoRecuperacion
        {
            IdCodigoRecuperacion = 9,
            IdUsuario = 30,
            FechaExpiracion = DateTime.UtcNow.AddMinutes(10),
            Utilizado = false,
            IntentosFallidos = 0
        });
        repositorioCodigo.RestablecerPasswordAsync(30, 9, "123456", "nuevaPassword123").Returns(true);

        var resultado = await servicio.CambiarPasswordAsync("pablo@example.com", "123456", "nuevaPassword123");

        Assert.True(resultado.Exito);
        Assert.Equal("Contraseña actualizada correctamente.", resultado.Mensaje);
    }

    [Fact]
    public async Task CambiarPassword_DebeRechazarContrasenaCorta()
    {
        var repositorioCodigo = Substitute.For<ICodigoRecuperacionRepositorio>();
        var repositorioUsuarios = Substitute.For<IUsuarioRepositorio>();
        var servicio = CrearServicio(repositorioCodigo, repositorioUsuarios, envioExitoso: true);

        var resultado = await servicio.CambiarPasswordAsync("pablo@example.com", "123456", "1234567");

        Assert.False(resultado.Exito);
        Assert.Equal("La contraseña debe tener al menos 8 caracteres.", resultado.Mensaje);
    }

    [Fact]
    public async Task CambiarPassword_DebeRechazarSiElUsuarioNoExiste()
    {
        var repositorioCodigo = Substitute.For<ICodigoRecuperacionRepositorio>();
        var repositorioUsuarios = Substitute.For<IUsuarioRepositorio>();
        var servicio = CrearServicio(repositorioCodigo, repositorioUsuarios, envioExitoso: true);

        repositorioUsuarios.ObtenerPorCorreoAsync("inexistente@example.com").Returns((Usuario?)null);

        var resultado = await servicio.CambiarPasswordAsync("inexistente@example.com", "123456", "nuevaPassword123");

        Assert.False(resultado.Exito);
        Assert.Equal("No se pudo restablecer la contraseña.", resultado.Mensaje);
    }

    [Fact]
    public async Task CambiarPassword_DebeRechazarSiElCodigoNoEsValido()
    {
        var repositorioCodigo = Substitute.For<ICodigoRecuperacionRepositorio>();
        var repositorioUsuarios = Substitute.For<IUsuarioRepositorio>();
        var servicio = CrearServicio(repositorioCodigo, repositorioUsuarios, envioExitoso: true);

        var usuario = new Usuario
        {
            IdUsuario = 31,
            NombreUsuario = "Quino",
            Correo = "quino@example.com",
            CorreoVerificado = true
        };

        repositorioUsuarios.ObtenerPorCorreoAsync("quino@example.com").Returns(usuario);
        repositorioCodigo.ObtenerUltimoAsync(31).Returns(new CodigoRecuperacion
        {
            IdCodigoRecuperacion = 10,
            IdUsuario = 31,
            FechaExpiracion = DateTime.UtcNow.AddMinutes(10),
            Utilizado = false,
            IntentosFallidos = 0
        });
        repositorioCodigo.RestablecerPasswordAsync(31, 10, "codigoIncorrecto", "nuevaPassword123").Returns(false);

        var resultado = await servicio.CambiarPasswordAsync("quino@example.com", "codigoIncorrecto", "nuevaPassword123");

        Assert.False(resultado.Exito);
        Assert.Equal("El código no es válido o ha expirado.", resultado.Mensaje);
    }

    private static ServicioRecuperacionPassword CrearServicio(
        ICodigoRecuperacionRepositorio repositorioCodigo,
        IUsuarioRepositorio repositorioUsuarios,
        bool envioExitoso)
    {
        var servicioEnvioCorreo = new ServicioEnvioCorreo(
            new HttpClient(new StubHttpMessageHandler(_ =>
            {
                if (!envioExitoso)
                {
                    throw new HttpRequestException("Fallo simulado del envío");
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { ok = true })
                };
            })),
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Brevo:ApiKey"] = "test-key",
                    ["Brevo:Remitente"] = "noreply@example.com"
                })
                .Build());

        return new ServicioRecuperacionPassword(
            repositorioCodigo,
            new ServicioCodigoVerificacion(),
            servicioEnvioCorreo,
            repositorioUsuarios);
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
