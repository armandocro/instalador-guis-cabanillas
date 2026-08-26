using System.Diagnostics;

namespace InstaladorGuis.Services;

internal sealed class CommandResult
{
    public bool Ok { get; init; }
    public string? Error { get; init; }
    public bool TimedOut { get; init; }
}

internal static class CommandService
{
    /// <summary>
    /// Ejecuta un proceso con argumentos separados (sin shell) para evitar command injection.
    /// </summary>
    public static CommandResult Ejecutar(string ejecutable, string[] argumentos, bool esperar, int timeoutMs = 900_000)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = ejecutable,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            foreach (var arg in argumentos)
                psi.ArgumentList.Add(arg);

            if (esperar)
            {
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
            }

            var proceso = Process.Start(psi);
            if (proceso == null) return new CommandResult { Ok = false, Error = "No se pudo iniciar el proceso." };
            if (!esperar)
            {
                proceso.Dispose();
                return new CommandResult { Ok = true };
            }

            try
            {
                var stdoutTask = proceso.StandardOutput.ReadToEndAsync();
                var stderrTask = proceso.StandardError.ReadToEndAsync();
                if (!proceso.WaitForExit(timeoutMs))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "taskkill",
                            ArgumentList = { "/PID", proceso.Id.ToString(), "/T", "/F" },
                            CreateNoWindow = true,
                            UseShellExecute = false
                        })?.Dispose();
                    }
                    catch { /* best effort */ }

                    return new CommandResult
                    {
                        Ok = false,
                        TimedOut = true,
                        Error = "El proceso no termino dentro del tiempo limite y ha sido finalizado"
                    };
                }

                var exitCode = proceso.ExitCode;
                var ok = exitCode == 0;
                var error = ok
                    ? null
                    : (stderrTask.GetAwaiter().GetResult() + " " + stdoutTask.GetAwaiter().GetResult()).Trim();
                return new CommandResult
                {
                    Ok = ok,
                    Error = string.IsNullOrWhiteSpace(error) ? (ok ? null : "Código de salida " + exitCode) : error
                };
            }
            finally
            {
                proceso.Dispose();
            }
        }
        catch (Exception ex)
        {
            return new CommandResult { Ok = false, Error = ex.Message };
        }
    }

    public static void AbrirRuta(string ruta, string? argumentos = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ruta,
            UseShellExecute = true
        };
        if (!string.IsNullOrEmpty(argumentos))
            psi.Arguments = argumentos;
        Process.Start(psi);
    }
}
