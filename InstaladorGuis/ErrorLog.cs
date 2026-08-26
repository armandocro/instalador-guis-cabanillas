using System.IO;

namespace InstaladorGuis;

internal static class ErrorLog
{
    private static readonly string[] RutasLog =
    [
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InstaladorGuis", "instalador-error.log"),
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "InstaladorGuis", "instalador-error.log"),
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "instalador-error.log")
    ];

    public static void MostrarError(string titulo, Exception ex)
    {
        var texto = titulo + Environment.NewLine + Environment.NewLine +
                    ex.GetType().Name + ": " + ex.Message + Environment.NewLine + Environment.NewLine +
                    ex.StackTrace;
        if (ex.InnerException != null)
        {
            texto += Environment.NewLine + Environment.NewLine + "Interno: " + ex.InnerException.Message +
                     Environment.NewLine + ex.InnerException.StackTrace;
        }

        var rutas = Escribir(texto);
        texto += Environment.NewLine + Environment.NewLine + "Log guardado en:" + Environment.NewLine +
                 string.Join(Environment.NewLine, rutas);

        try
        {
            System.Windows.MessageBox.Show(texto, "Instalador de GUIs — error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        catch
        {
            // último recurso
        }
    }

    public static List<string> Escribir(string texto)
    {
        var escritos = new List<string>();
        var bloque = DateTime.Now + Environment.NewLine + texto + Environment.NewLine + Environment.NewLine;
        var extra = System.IO.Path.Combine(AppContext.BaseDirectory, "instalador-error.log");
        foreach (var ruta in RutasLog.Append(extra).Distinct())
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ruta)) continue;
                var dir = System.IO.Path.GetDirectoryName(ruta);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(ruta, bloque);
                escritos.Add(ruta);
            }
            catch
            {
                // probar la siguiente
            }
        }

        return escritos;
    }
}
