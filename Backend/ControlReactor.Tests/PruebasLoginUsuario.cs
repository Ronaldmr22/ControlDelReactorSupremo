using ControlReactor.Modelos.DTOs;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;
using Microsoft.AspNetCore.Identity;
using NSubstitute;

namespace ControlReactor.Tests;

public class PruebasLoginUsuario
{
    [Fact]
    public async Task LoginAsync_DebeIniciarSesionCorrectamenteCuandoCredencialesSonValidas()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();

        var usuario = new Usuario
        {
            IdUsuario = 1,
            NombreUsuario = "Alice",
            Correo = "alice@example.com",
            CorreoVerificado = true
        };

        var hasher = new PasswordHasher<Usuario>();
        usuario.PasswordHash = hasher.HashPassword(usuario, "12345678");

        repositorio.ObtenerPorCorreoAsync("alice@example.com").Returns(usuario);

        var servicio = new ServicioLoginUsuario(repositorio);

        var resultado = await servicio.LoginAsync(new LoginUsuarioDto
        {
            Correo = "alice@example.com",
            Password = "12345678"
        });

        Assert.True(resultado.Exito);
        Assert.Equal("Credenciales verificadas correctamente.", resultado.Mensaje);
        Assert.NotNull(resultado.Usuario);
        Assert.Equal(1, resultado.Usuario!.IdUsuario);
    }

    [Fact]
    public async Task LoginAsync_DebeRechazarSiElUsuarioNoExiste()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        repositorio.ObtenerPorCorreoAsync("noexiste@example.com").Returns((Usuario?)null);

        var servicio = new ServicioLoginUsuario(repositorio);

        var resultado = await servicio.LoginAsync(new LoginUsuarioDto
        {
            Correo = "noexiste@example.com",
            Password = "12345678"
        });

        Assert.False(resultado.Exito);
        Assert.Equal("Correo o contraseña incorrectos.", resultado.Mensaje);
        Assert.Null(resultado.Usuario);
    }

    [Fact]
    public async Task LoginAsync_DebeRechazarSiLaContrasenaEsIncorrecta()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();

        var usuario = new Usuario
        {
            IdUsuario = 2,
            NombreUsuario = "Bob",
            Correo = "bob@example.com",
            CorreoVerificado = true
        };

        var hasher = new PasswordHasher<Usuario>();
        usuario.PasswordHash = hasher.HashPassword(usuario, "passwordCorrecta");

        repositorio.ObtenerPorCorreoAsync("bob@example.com").Returns(usuario);

        var servicio = new ServicioLoginUsuario(repositorio);

        var resultado = await servicio.LoginAsync(new LoginUsuarioDto
        {
            Correo = "bob@example.com",
            Password = "passwordIncorrecta"
        });

        Assert.False(resultado.Exito);
        Assert.Equal("Correo o contraseña incorrectos.", resultado.Mensaje);
        Assert.Null(resultado.Usuario);
    }

    [Fact]
    public async Task LoginAsync_DebeRechazarSiElCorreoNoHaSidoVerificado()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();

        var usuario = new Usuario
        {
            IdUsuario = 3,
            NombreUsuario = "Carol",
            Correo = "carol@example.com",
            CorreoVerificado = false
        };

        var hasher = new PasswordHasher<Usuario>();
        usuario.PasswordHash = hasher.HashPassword(usuario, "12345678");

        repositorio.ObtenerPorCorreoAsync("carol@example.com").Returns(usuario);

        var servicio = new ServicioLoginUsuario(repositorio);

        var resultado = await servicio.LoginAsync(new LoginUsuarioDto
        {
            Correo = "carol@example.com",
            Password = "12345678"
        });

        Assert.False(resultado.Exito);
        Assert.Equal("Debes verificar tu correo electrónico.", resultado.Mensaje);
        Assert.Null(resultado.Usuario);
    }
}
