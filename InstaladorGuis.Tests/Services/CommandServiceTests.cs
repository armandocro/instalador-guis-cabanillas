using InstaladorGuis.Services;

namespace InstaladorGuis.Tests.Services;

public class CommandServiceTests
{
    [Fact]
    public void Ejecutar_ReturnsOkForSuccessfulProcess()
    {
        var result = CommandService.Ejecutar("cmd.exe", ["/c", "exit", "0"], true, 30_000);
        Assert.True(result.Ok);
        Assert.False(result.TimedOut);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Ejecutar_ReturnsErrorForNonZeroExit()
    {
        var result = CommandService.Ejecutar("cmd.exe", ["/c", "exit", "1"], true, 30_000);
        Assert.False(result.Ok);
        Assert.False(result.TimedOut);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Ejecutar_WithoutWait_ReturnsOkImmediately()
    {
        var result = CommandService.Ejecutar("cmd.exe", ["/c", "echo", "ok"], false);
        Assert.True(result.Ok);
    }

    [Fact]
    public void Ejecutar_InvalidExecutable_ReturnsError()
    {
        var result = CommandService.Ejecutar("este-ejecutable-no-existe-xyz.exe", ["--help"], true, 5_000);
        Assert.False(result.Ok);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }
}
