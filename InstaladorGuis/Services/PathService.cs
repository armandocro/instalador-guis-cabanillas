using System.IO;

namespace InstaladorGuis.Services;

internal static class PathService
{
    public static string UserPath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static string DesktopPath
    {
        get
        {
            var escritorios = EscritoriosCandidatos();
            return escritorios.Count > 0
                ? escritorios[0]
                : System.IO.Path.Combine(UserPath, "Desktop");
        }
    }

    public static IReadOnlyList<string> EscritoriosCandidatos()
    {
        var list = new List<string>();
        void Add(string? ruta)
        {
            if (string.IsNullOrWhiteSpace(ruta)) return;
            try
            {
                var full = System.IO.Path.GetFullPath(ruta);
                if (!Directory.Exists(full)) return;
                if (list.Exists(x => x.Equals(full, StringComparison.OrdinalIgnoreCase))) return;
                list.Add(full);
            }
            catch { /* ignorar rutas inválidas */ }
        }

        Add(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        Add(System.IO.Path.Combine(UserPath, "Desktop"));
        Add(System.IO.Path.Combine(UserPath, "OneDrive", "Desktop"));
        Add(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));
        return list;
    }

    public static string AmigaAppsPath => System.IO.Path.Combine(UserPath, "AppData", "Local", "Inditex", "ijlp", "apps");
    public static string AmigaLauncherPath => @"C:\Program Files\AmigaLauncher\amglauncher.exe";
    public static string AmigaLauncherShortPath => @"C:\PROGRA~1\AmigaLauncher\amglauncher.exe";

    public static string? ResolverArchivo(IEnumerable<string> bases, string archivo)
    {
        foreach (var baseDir in bases)
        {
            try
            {
                var candidato = System.IO.Path.Combine(baseDir, archivo);
                if (File.Exists(candidato)) return candidato;
            }
            catch { }
        }
        return null;
    }

    public static string DecodeSpecialCharacters(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text
            .Replace("Ã³n", "ón").Replace("Ã³", "ó").Replace("Ã¡", "á")
            .Replace("Ã©", "é").Replace("Ã­", "í").Replace("Ãº", "ú")
            .Replace("Ã±", "ñ").Replace("Ã", "í").Replace("âœ", "✅");
    }

    public static int CompareVersions(string version1, string version2)
    {
        try
        {
            var v1 = ParseParts(version1);
            var v2 = ParseParts(version2);
            var max = Math.Max(v1.Count, v2.Count);
            while (v1.Count < max) v1.Add(0);
            while (v2.Count < max) v2.Add(0);
            for (var i = 0; i < max; i++)
            {
                if (v1[i] < v2[i]) return -1;
                if (v1[i] > v2[i]) return 1;
            }
            return 0;
        }
        catch
        {
            return string.Compare(version1, version2, StringComparison.Ordinal);
        }
    }

    private static List<int> ParseParts(string version) =>
        version.Split('.').Select(part => int.TryParse(part, out var n) ? n : 0).ToList();
}
