using System.Security.Cryptography;
using System.Text;
using ControlReactor.Api.Autenticacion;
using ControlReactor.Modelos.Entidades;
using ControlReactor.Negocio.Interfaces;
using ControlReactor.Negocio.Servicios;
using NSubstitute;

namespace ControlReactor.Tests;

// US-29 Autenticación mediante una red social (Google).
public class PruebasLoginGoogle
{
    // ---------- ServicioLoginGoogle ----------

    [Fact]
    public async Task US29_Positiva_CuentaGoogleVinculada_IniciaSesion()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        var usuario = new Usuario { IdUsuario = 7, NombreUsuario = "ana", Correo = "ana@gmail.com" };
        repositorio.ObtenerPorGoogleAsync("g-1").Returns(Task.FromResult<Usuario?>(usuario));
        var servicio = new ServicioLoginGoogle(repositorio);

        var (exito, _, resultado) = await servicio.LoginAsync("g-1", "ana@gmail.com", "Ana", true);

        Assert.True(exito);
        Assert.Same(usuario, resultado);
        await repositorio.DidNotReceive().CrearUsuarioGoogleAsync(Arg.Any<Usuario>(), Arg.Any<string>());
    }

    [Fact]
    public async Task US29_Positiva_CuentaNueva_SeRegistraConNombreUnico()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        repositorio.ObtenerPorGoogleAsync("g-2").Returns(Task.FromResult<Usuario?>(null));
        repositorio.ObtenerPorCorreoAsync("luis@gmail.com").Returns(Task.FromResult<Usuario?>(null));
        repositorio.ExisteNombreUsuarioAsync("luis").Returns(Task.FromResult(true));
        repositorio.ExisteNombreUsuarioAsync("luis1").Returns(Task.FromResult(false));
        repositorio.CrearUsuarioGoogleAsync(Arg.Any<Usuario>(), "g-2")
            .Returns(ci => Task.FromResult(ci.Arg<Usuario>()));
        var servicio = new ServicioLoginGoogle(repositorio);

        var (exito, _, usuario) = await servicio.LoginAsync("g-2", "Luis@Gmail.com", "Luis", true);

        Assert.True(exito);
        Assert.Equal("luis1", usuario!.NombreUsuario);
        Assert.Equal("luis@gmail.com", usuario.Correo);
        Assert.True(usuario.CorreoVerificado);
        Assert.Equal(string.Empty, usuario.PasswordHash); // CA-29.5: sin contraseña del proveedor
    }

    [Fact]
    public async Task US29_Negativa_CorreoNoVerificadoPorGoogle_SeRechaza()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        var servicio = new ServicioLoginGoogle(repositorio);

        var (exito, _, usuario) = await servicio.LoginAsync("g-3", "x@gmail.com", "X", false);

        Assert.False(exito);
        Assert.Null(usuario);
        await repositorio.DidNotReceive().CrearUsuarioGoogleAsync(Arg.Any<Usuario>(), Arg.Any<string>());
    }

    // CA-29.6: no se duplica una cuenta que ya existe con ese correo.
    [Fact]
    public async Task US29_Negativa_CorreoDeCuentaTradicional_NoCreaDuplicado()
    {
        var repositorio = Substitute.For<IUsuarioRepositorio>();
        repositorio.ObtenerPorGoogleAsync("g-4").Returns(Task.FromResult<Usuario?>(null));
        repositorio.ObtenerPorCorreoAsync("eva@gmail.com")
            .Returns(Task.FromResult<Usuario?>(new Usuario { IdUsuario = 3, Correo = "eva@gmail.com" }));
        var servicio = new ServicioLoginGoogle(repositorio);

        var (exito, _, usuario) = await servicio.LoginAsync("g-4", "eva@gmail.com", "Eva", true);

        Assert.False(exito);
        Assert.Null(usuario);
        await repositorio.DidNotReceive().CrearUsuarioGoogleAsync(Arg.Any<Usuario>(), Arg.Any<string>());
    }

    // ---------- ServicioCodigoLoginGoogle (PKCE) ----------

    private static string Desafio(string verificador)
    {
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(verificador));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    [Fact]
    public void US29_Positiva_CodigoConVerificadorCorrecto_DevuelveUsuario()
    {
        var servicio = new ServicioCodigoLoginGoogle();
        string codigo = servicio.GenerarCodigo(7, "sol-1", Desafio("verificador-secreto"));

        int? idUsuario = servicio.ConsumirCodigo(codigo, "sol-1", "verificador-secreto");

        Assert.Equal(7, idUsuario);
    }

    [Fact]
    public void US29_Negativa_VerificadorIncorrecto_NoDevuelveUsuario()
    {
        var servicio = new ServicioCodigoLoginGoogle();
        string codigo = servicio.GenerarCodigo(7, "sol-1", Desafio("verificador-secreto"));

        Assert.Null(servicio.ConsumirCodigo(codigo, "sol-1", "otro-verificador"));
    }

    [Fact]
    public void US29_Negativa_SolicitudDistinta_NoDevuelveUsuario()
    {
        var servicio = new ServicioCodigoLoginGoogle();
        string codigo = servicio.GenerarCodigo(7, "sol-1", Desafio("verificador-secreto"));

        Assert.Null(servicio.ConsumirCodigo(codigo, "sol-2", "verificador-secreto"));
    }

    [Fact]
    public void US29_Negativa_CodigoUsadoDosVeces_SoloFuncionaLaPrimera()
    {
        var servicio = new ServicioCodigoLoginGoogle();
        string codigo = servicio.GenerarCodigo(7, "sol-1", Desafio("verificador-secreto"));

        var primera = servicio.ConsumirCodigo(codigo, "sol-1", "verificador-secreto");
        var segunda = servicio.ConsumirCodigo(codigo, "sol-1", "verificador-secreto");

        Assert.Equal(7, primera);
        Assert.Null(segunda);
    }
}
