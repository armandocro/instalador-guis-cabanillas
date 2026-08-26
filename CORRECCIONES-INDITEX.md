# Correcciones para Cumplimiento Inditex

**Fecha:** 2026-08-26
**Proyecto:** InstaladorGuis v1.3.0 (WPF Desktop Application)
**Objetivo:** Detallar todas las correcciones necesarias para que otra IA las aplique de forma autonoma.

---

## Tabla de Contenidos

1. [Seguridad (CRITICO)](#1-seguridad-critico)
2. [Calidad de Codigo](#2-calidad-de-codigo)
3. [Testing y Cobertura](#3-testing-y-cobertura)
4. [CI/CD y Herramientas](#4-cicd-y-herramientas)
5. [Documentacion](#5-documentacion)
6. [Estructura del Repositorio](#6-estructura-del-repositorio)

---

## 1. Seguridad (CRITICO)

### CORRECCION S-01: Sanitizacion de URLs en command injection (PRIORIDAD: ALTA)

**Archivo:** `InstaladorGuis/Services/InstallerService.cs`
**Lineas:** 104, 106, 145, 188, 190
**Referencia:** Security White Paper - OWASP ASVS V.5.3.8: "Verify that the application protects against OS command injection"
**Fuentes Geppetto:** `security-white-paper.docs.inditex.dev/secudoc/stable/best-practices/security-requirements-files/v5.html`

**Problema:** Las URLs se interpolan directamente en comandos shell a traves de `cmd.exe /c`. Aunque se aplica `SanitizeUrl`, la sanitizacion actual es una lista negra (remueve caracteres especificos) en lugar de una lista blanca. Esto es insuficiente segun OWASP.

**Codigo actual (lineas 104, 106):**
```csharp
CommandService.Ejecutar("javaws -uninstall \"" + safeUrl + "\"", true);
// ...
var install = CommandService.Ejecutar(PathService.AmigaLauncherShortPath + " -install -silent \"" + safeUrl + "\"", true);
```

**Codigo actual (linea 145):**
```csharp
CommandService.Ejecutar(PathService.AmigaLauncherShortPath + " -uninstall -silent \"" + safeUrl + "\"", false);
```

**Codigo actual (lineas 188, 190):**
```csharp
CommandService.Ejecutar("javaws -uninstall \"" + safeUrl + "\"", true);
var install = CommandService.Ejecutar(PathService.AmigaLauncherShortPath + " -install -silent \"" + safeUrl + "\"", true);
```

**Correccion requerida:**

1. Modificar `CommandService.cs` para que `Ejecutar()` acepte argumentos como array separado en lugar de un solo string:

**Archivo:** `InstaladorGuis/Services/CommandService.cs`

Cambiar la signatura de `Ejecutar`:
```csharp
// ANTES:
public static CommandResult Ejecutar(string cmd, bool esperar, int timeoutMs = 900_000)

// DESPUES: agregar un nuevo metodo sobrecargado
public static CommandResult Ejecutar(string ejecutable, string[] argumentos, bool esperar, int timeoutMs = 900_000)
{
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = ejecutable,
            Arguments = string.Join(" ", argumentos.Select(a => "\"" + a.Replace("\"", "\\\"") + "\"")),
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        // ... resto de la implementacion igual a Ejecutar existente
    }
}
```

2. En `InstallerService.cs`, cambiar todas las llamadas para usar la nueva sobrecarga:

```csharp
// ANTES (linea 104):
CommandService.Ejecutar("javaws -uninstall \"" + safeUrl + "\"", true);

// DESPUES:
CommandService.Ejecutar("javaws", new[] { "-uninstall", safeUrl }, true);

// ANTES (linea 106):
var install = CommandService.Ejecutar(PathService.AmigaLauncherShortPath + " -install -silent \"" + safeUrl + "\"", true);

// DESPUES:
var install = CommandService.Ejecutar(PathService.AmigaLauncherShortPath, new[] { "-install", "-silent", safeUrl }, true);

// ANTES (linea 145):
CommandService.Ejecutar(PathService.AmigaLauncherShortPath + " -uninstall -silent \"" + safeUrl + "\"", false);

// DESPUES:
CommandService.Ejecutar(PathService.AmigaLauncherShortPath, new[] { "-uninstall", "-silent", safeUrl }, false);

// ANTES (linea 157):
CommandService.Ejecutar("taskkill /IM amglauncher.exe /F", true);

// DESPUES:
CommandService.Ejecutar("taskkill", new[] { "/IM", "amglauncher.exe", "/F" }, true);

// ANTES (linea 188):
CommandService.Ejecutar("javaws -uninstall \"" + safeUrl + "\"", true);

// DESPUES:
CommandService.Ejecutar("javaws", new[] { "-uninstall", safeUrl }, true);

// ANTES (linea 190):
var install = CommandService.Ejecutar(PathService.AmigaLauncherShortPath + " -install -silent \"" + safeUrl + "\"", true);

// DESPUES:
var install = CommandService.Ejecutar(PathService.AmigaLauncherShortPath, new[] { "-install", "-silent", safeUrl }, true);
```

3. Reforzar `SanitizeUrl` con validacion de allowlist mas estricta:

```csharp
// ANTES (lineas 25-26):
internal static string SanitizeUrl(string url) =>
    url.Replace("\"", "").Replace("'", "").Replace("`", "").Replace("\\", "").Replace("&", "");

// DESPUES: mantener isValidUrl con regex allowlist y agregar validacion adicional
internal static string SanitizeUrl(string url)
{
    if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("La URL no puede estar vacia.");
    var sanitized = url.Trim();
    // Remover caracteres potencialmente peligrosos
    sanitized = sanitized.Replace("\"", "").Replace("'", "").Replace("`", "")
                         .Replace("\\", "").Replace("&", "").Replace(";", "")
                        .Replace("|", "").Replace(">", "").Replace("<", "");
    return sanitized;
}
```

---

### CORRECCION S-02: Eliminar ruta de desarrollador hardcodeada (PRIORIDAD: ALTA)

**Archivo:** `InstaladorGuis/ErrorLog.cs`
**Linea:** 10 (via `RutasLog` en linea 7-11)

**Problema:** La ruta hardcodeada `C:\Temp\DESARROLLO\Instalador de GUI` fue eliminada en la version actual, pero hay un problema de diseno: el array `RutasLog` solo tiene 2 rutas fallback. Si ambas fallan, no hay tercera opcion.

**Codigo actual (lineas 7-11):**
```csharp
private static readonly string[] RutasLog =
[
    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InstaladorGuis", "instalador-error.log"),
    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "instalador-error.log")
];
```

**Estado:** Ya corregido parcialmente. No contiene rutas hardcodeadas. Sin embargo, se recomienda agregar una tercera ruta de fallback en `%APPDATA%`:

```csharp
private static readonly string[] RutasLog =
[
    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InstaladorGuis", "instalador-error.log"),
    System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "InstaladorGuis", "instalador-error.log"),
    System.IO.Path.Combine(System.IO.Path.GetTempPath(), "instalador-error.log")
];
```

---

### CORRECCION S-03: Rutas UNC corporativas en repositorio (PRIORIDAD: MEDIA)

**Archivos:** `InstaladorGuis/Brands/pb.json`, `InstaladorGuis/Brands/zh.json`
**Lineas:** 11-17 (rutasRedBase), 18-22 (rutaMetricasBase), 24-28 (rutaActualizadorBase)
**Referencia:** Security White Paper - Secrets management

**Problema:** Los archivos de configuracion de marca contienen rutas UNC corporativas (`\\cabfs\sys\Datos`) que exponen la topologia de red. Segun el Security White Paper de Inditex, no se debe exponer infraestructura interna en repositorios.

**Codigo actual en `pb.json` (lineas 11-17):**
```json
"rutasRedBase": [
    "F:\\cabfs\\sys\\Datos",
    "\\\\cabfs\\sys\\Datos",
    "F:/cabfs/sys/Datos",
    "Z:\\cabfs\\sys\\Datos",
    "Y:\\cabfs\\sys\\Datos"
]
```

**Correccion requerida:**
- Opcion 1 (recomendada): Mantener las rutas en el JSON pero agregar un comment en el README que indique que estas rutas son especificas de cada centro y deben ajustarse.
- Opcion 2: Mover las rutas a un archivo de configuracion local que no se commitee (`.gitignore`) y que se genere en la instalacion.

Dado que es una aplicacion desktop interna, la Opcion 1 es aceptable siempre que:
1. El `.gitignore` excluya archivos de configuracion local (*.local.json, appsettings.Local.json)
2. Se documente en el README que las rutas son center-specific

---

### CORRECCION S-04: Deshabilitar BinaryFormatter inseguro (PRIORIDAD: MEDIA)

**Archivo:** `InstaladorGuis/InstaladorGuis.csproj` o `runtimeconfig.json`
**Referencia:** OWASP - Deserialization prevention

**Problema:** Si el proyecto habilita `EnableUnsafeBinaryFormatterSerialization`, esto permite deserializacion insegura.

**Correccion:** Verificar si hay alguna referencia a `BinaryFormatter` en el proyecto. Si la hay, eliminarla. Si no la hay, asegurar que el runtimeconfig NO contenga `EnableUnsafeBinaryFormatterSerialization: true`.

Buscar en el proyecto:
```
grep -r "BinaryFormatter" InstaladorGuis/
grep -r "EnableUnsafeBinaryFormatterSerialization" .
```

Si se encuentra, eliminar la referencia. En `app.runtimeconfig.json` o `runtimeconfig.template.json`, si existe, establecer:
```json
{
  "configProperties": {
    "System.Text.Json.JsonSerializerOptions.EnableUnsafeBinaryFormatterSerialization": false
  }
}
```

O en el `.csproj`:
```xml
<PropertyGroup>
  <EnableUnsafeBinaryFormatterSerialization>false</EnableUnsafeBinaryFormatterSerialization>
</PropertyGroup>
```

---

### CORRECCION S-05: Validacion de entrada robusta en RegistroLibre (PRIORIDAD: MEDIA)

**Archivo:** `InstaladorGuis/MainWindow.xaml.cs`
**Lineas:** 280-284
**Referencia:** OWASP ASVS V.5.1 - Input validation requirements

**Problema:** La validacion de URL en el flow de "Registro Libre" solo verifica que empiece con `http://` o `https://` pero no valida longitud maxima ni caracteres peligrosos.

**Codigo actual (lineas 280-284):**
```csharp
if (!Regex.IsMatch(url, @"^https?://.+", RegexOptions.IgnoreCase))
{
    await MostrarAvisoAsync("La URL debe comenzar con http:// o https://");
    return;
}
```

**Correccion:** Usar la misma validacion `InstallerService.IsValidUrl()` que ya tiene regex allowlist y limite de longitud:

```csharp
// ANTES (linea 280):
if (!Regex.IsMatch(url, @"^https?://.+", RegexOptions.IgnoreCase))

// DESPUES:
if (!InstallerService.IsValidUrl(url))
```

Y eliminar la lineas 282-283 (el mensaje de error duplicado) o ajustar el mensaje:
```csharp
if (!InstallerService.IsValidUrl(url))
{
    await MostrarAvisoAsync("La URL no es valida. Debe ser HTTP/HTTPS, maximo 2048 caracteres, y contener solo caracteres seguros.");
    return;
}
```

---

## 2. Calidad de Codigo

### CORRECCION C-01: Process disposal correcto en CommandService (PRIORIDAD: ALTA)

**Archivo:** `InstaladorGuis/Services/CommandService.cs`
**Lineas:** 32-54
**Referencia:** Quality 360 - Resource management

**Problema:** Cuando `esperar=false`, el `Process` se crea pero nunca se dispone correctamente. El `using var proceso` en linea 32 solo se ejecuta al final del scope, pero el `return` en linea 35 sale antes.

**Codigo actual (lineas 32-36):**
```csharp
using var proceso = Process.Start(psi);
if (proceso == null) return new CommandResult { Ok = false, Error = "No se pudo iniciar el proceso." };
if (!esperar)
    return new CommandResult { Ok = true };
```

**Correccion:** Dispone el proceso explicitamente cuando no se espera:

```csharp
var proceso = Process.Start(psi);
if (proceso == null) return new CommandResult { Ok = false, Error = "No se pudo iniciar el proceso." };
if (!esperar)
{
    proceso.Dispose();
    return new CommandResult { Ok = true };
}
try
{
    // ... logica existente con WaitForExit ...
}
finally
{
    proceso.Dispose();
}
```

---

### CORRECCION C-02: Eliminar codigo muerto (PRIORIDAD: MEDIA)

**Archivos:**
- `InstaladorGuis/Windows/RegistroLibreWindow.xaml` (70 lineas)
- `InstaladorGuis/Windows/RegistroLibreWindow.xaml.cs` (18 lineas)

**Problema:** Estos archivos no se usan. El flow de "Registro Libre" ahora usa `AppDialogHost.ShowRegistroLibreAsync()`. El window standalone esta muerto.

**Correccion:** Eliminar ambos archivos y cualquier referencia en `.csproj` si la hay.

---

### CORRECCION C-03: Duplicacion de BrushFrom() (PRIORIDAD: BAJA)

**Archivos:**
- `InstaladorGuis/MainWindow.xaml.cs` (usa `BrushHelper.FromHex` - ya esta delegado)
- `InstaladorGuis/ViewModels/GuiVm.cs` (usa `BrushHelper.FromHex` - ya esta delegado)

**Estado:** Ya esta resuelto con `BrushHelper.cs`. Ambos archivos usan `BrushHelper.FromHex()`. No hay duplicacion activa.

**Verificar:** Confirmar que no hay llamadas directas a `new BrushConverter().ConvertFromString()` en ningun archivo:

```
grep -r "BrushConverter" InstaladorGuis/
```

Si se encuentra alguna, reemplazarla por `BrushHelper.FromHex()`.

---

### CORRECCION C-04: Thread safety en MetricsService (PRIORIDAD: MEDIA)

**Archivo:** `InstaladorGuis/Services/MetricsService.cs`
**Lineas:** 173-182 (metodo `Guardar`)

**Problema:** El metodo `Guardar()` escribe a disco sin proteccion contra escritura concurrente. Si dos threads llaman a metodos de registro simultaneamente, el archivo JSON puede corromperse.

**Codigo actual (lineas 173-182):**
```csharp
private void Guardar()
{
    if (_metricas == null || string.IsNullOrEmpty(_ruta)) return;
    try
    {
        _metricas.UltimaActualizacion = DateTime.UtcNow.ToString("o");
        File.WriteAllText(_ruta, JsonSerializer.Serialize(_metricas, JsonOptions));
    }
    catch { }
}
```

**Correccion:** Agregar un lock o usar `SemaphoreSlim`:

```csharp
private readonly SemaphoreSlim _lock = new(1, 1);

private async Task GuardarAsync()
{
    if (_metricas == null || string.IsNullOrEmpty(_ruta)) return;
    await _lock.WaitAsync();
    try
    {
        _metricas.UltimaActualizacion = DateTime.UtcNow.ToString("o");
        var json = JsonSerializer.Serialize(_metricas, JsonOptions);
        await File.WriteAllTextAsync(_ruta, json);
    }
    catch { }
    finally
    {
        _lock.Release();
    }
}
```

**Nota:** Como los metodos `Registrar*` son sincronos y se llaman desde el UI thread via `Progress<T>`, es aceptable usar un `lock` sincrono en su lugar:

```csharp
private readonly object _lock = new();

private void Guardar()
{
    if (_metricas == null || string.IsNullOrEmpty(_ruta)) return;
    lock (_lock)
    {
        try
        {
            _metricas.UltimaActualizacion = DateTime.UtcNow.ToString("o");
            File.WriteAllText(_ruta, JsonSerializer.Serialize(_metricas, JsonOptions));
        }
        catch { }
    }
}
```

---

### CORRECCION C-05: Fire-and-forget Task.Run sin manejo de excepciones (PRIORIDAD: MEDIA)

**Archivo:** `InstaladorGuis/MainWindow.xaml.cs`
**Lineas:** 242, 291

**Problema:** Las llamadas `_ = Task.Run(async () => { ... })` descartan el Task. Si ocurre una excepcion no capturada dentro del lambda, sera silenciosamente tragada.

**Codigo actual (lineas 242-260):**
```csharp
_ = Task.Run(async () =>
{
    try
    {
        // ...
    }
    catch (Exception ex)
    {
        await Dispatcher.InvokeAsync(() => DialogHost.MostrarMensaje("Error", ex.Message));
    }
});
```

**Estado:** Ya tiene try/catch interno, lo cual es correcto. Sin embargo, se recomienda agregar un `TaskScheduler.UnobservedTaskException` handler o verificar que todos los paths estan cubiertos.

**Correccion:** Verificar que el `try/catch` en linea 244 cubre toda la lambda. Si esta bien, solo agregar un comment indicando que es intencional:

```csharp
_ = Task.Run(async () => // fire-and-forget: excepciones manejadas internamente
{
    try { ... }
    catch (Exception ex) { ... }
});
```

---

### CORRECCION C-06: Eliminar strings magicos "PB" y "ZH" (PRIORIDAD: BAJA)

**Archivos:** Multiples (`BrandService.cs`, `MainWindow.xaml.cs`)

**Problema:** Los identificadores de marca "PB" y "ZH" aparecen como string literals sin constantes.

**Correccion:** Definir constantes en un lugar central:

**Archivo:** `InstaladorGuis/Models/BrandConfig.cs` (agregar):
```csharp
public static class BrandIds
{
    public const string PullBear = "PB";
    public const string ZaraHome = "ZH";
}
```

Luego reemplazar en `BrandService.cs` (lineas donde se compara `"PB"`, `"ZH"`, etc.):
```
grep -rn "\"PB\"\|\"ZH\"\|'PB'\|'ZH'" InstaladorGuis/Services/
```

---

## 3. Testing y Cobertura

### CORRECCION T-01: Agregar tests unitarios (PRIORIDAD: ALTA)

**Archivo:** Proyecto `InstaladorGuis.Tests`
**Referencia:** Quality 360 - "Unit testing involves creating small, isolated tests for individual components"

**Problema:** Solo hay 24 tests basicos. Faltan tests para:
- `InstallerService` (logica de instalacion/desinstalacion)
- `MetricsService` (CRUD de metricas)
- `CommandService` (ejecucion de procesos)
- `MainWindow` (logica de UI via ViewModels)
- `ChatbotControl` (decision tree)

**Correccion:** Crear los siguientes archivos de test:

1. **`InstaladorGuis.Tests/Services/CommandServiceTests.cs`** - Testear `Ejecutar()` con procesos mock, timeouts, y `AbrirRuta()`.
2. **`InstaladorGuis.Tests/Services/MetricsServiceTests.cs`** - Testear `RegistrarAperturaApp()`, `RegistrarInstalacionGUI()`, `Inicializar()`.
3. **`InstaladorGuis.Tests/Services/BrandServiceTests.cs`** - Testear deteccion de marca por argumentos, variables de entorno, nombre de executable.
4. **`InstaladorGuis.Tests/ViewModels/GuiVmTests.cs`** - Testear propiedades computadas, `INotifyPropertyChanged`, estados de status.

---

### CORRECCION T-02: Configurar cobertura de codigo (PRIORIDAD: ALTA)

**Archivo:** `InstaladorGuis.Tests/InstaladorGuis.Tests.csproj`
**Referencia:** Quality 360 - JaCoCo/dotnet-coverage para SonarCloud

**Problema:** El proyecto de tests usa `coverlet.collector` pero no hay configuracion explicita para generar reportes de cobertura en formato que SonarCloud pueda consumir.

**Correccion:** Agregar al `.csproj` del test project:

```xml
<PropertyGroup>
    <CollectCoverage>true</CollectCoverage>
    <CoverletOutputFormat>opencover</CoverletOutputFormat>
    <CoverletOutput>$(MSBuildThisFileDirectory)\..\TestResults\opencover.xml</CoverletOutput>
    <ExcludeByFile>**/obj/**,**/bin/**</ExcludeByFile>
</PropertyGroup>
```

Y asegurar que `sonar-project.properties` apunte al reporte correcto:

```properties
sonar.cs.opencover.reportsPaths=InstaladorGuis.Tests/TestResults/opencover.xml
```

---

### CORRECCION T-03: Configurar mutation testing (PRIORIDAD: BAJA)

**Referencia:** Quality 360 - "Mutation testing takes unit testing to the next level"

**Correccion:** Agregar Stryker.NET como dependencia de desarrollo:

En `InstaladorGuis.Tests/InstaladorGuis.Tests.csproj`:
```xml
<ItemGroup>
    <PackageReference Include="Stryker.NET" Version="3.*" />
</ItemGroup>
```

Y crear archivo `stryker-config.json` en la raiz:
```json
{
  "stryker-config": {
    "project": "InstaladorGuis/InstaladorGuis.csproj",
    "test-projects": ["InstaladorGuis.Tests/InstaladorGuis.Tests.csproj"],
    "target-framework": "net8.0"
  }
}
```

---

## 4. CI/CD y Herramientas

### CORRECCION CI-01: Configurar SonarCloud correctamente (PRIORIDAD: ALTA)

**Archivo:** `sonar-project.properties`
**Referencia:** Quality 360 - "complete your project configuration on SonarCloud"

**Codigo actual:**
```properties
sonar.projectKey=instalador-guis
sonar.sources=InstaladorGuis/
sonar.exclusions=**/obj/**,**/bin/**,**/Generated/**
sonar.tests=InstaladorGuis.Tests/
sonar.testExecutionReportPaths=
sonar.coverage.opencover.xmlReportPaths=TestResults/opencover.xml
```

**Problemas:**
1. `sonar.testExecutionReportPaths` esta vacio
2. Faltan `sonar.cs.opencover.reportsPaths`
3. No hay exclusiones para archivos generados por WPF

**Correccion completa:**
```properties
sonar.projectKey=instalador-guis
sonar.projectName=Instalador de GUIs
sonar.organization=inditex
sonar.sources=InstaladorGuis/
sonar.tests=InstaladorGuis.Tests/
sonar.exclusions=**/obj/**,**/bin/**,**/Generated/**,**/*.g.cs,**/*.g.i.cs,**/Themes/Theme.xaml.cs
sonar.cs.opencover.reportsPaths=InstaladorGuis.Tests/TestResults/opencover.xml
sonar.sourceEncoding=UTF-8
sonar.core.codeCoveragePlugin=opencover
```

---

### CORRECCION CI-02: Agregar GitHub Actions workflow (PRIORIDAD: ALTA)

**Archivo:** `.github/workflows/ci.yml` (nuevo)

**Correccion:** Crear workflow de CI con build + test + Sonar:

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test with coverage
        run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage" --results-directory TestResults

      - name: Generate coverage report
        run: |
          dotnet tool install --global dotnet-reportgenerator-globaltool
          reportgenerator "-reports:TestResults/**/coverage.cobertura.xml" "-targetdir:TestResults/coverage" "-reporttypes:OpenCover"

      - name: SonarCloud Scan
        uses: SonarSource/sonarcloud-github-action@master
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```

---

### CORRECCION CI-03: Configurar Detect Secrets (PRIORIDAD: ALTA)

**Referencia:** Detect Secrets docs - "Detect Secrets automated scan provides an effective solution to identify and prevent the inclusion of unwanted secrets"

**Problema:** No hay archivo `.secrets.baseline` ni workflow de Detect Secrets.

**Correccion:**
1. Ejecutar localmente `detect-secrets scan` para generar `.secrets.baseline`
2. Auditar los findings con `detect-secrets audit`
3. Agregar el workflow en `.github/workflows/security-detect-secrets.yml`:

```yaml
name: security-detect-secrets

on:
  pull_request:
    branches: [main, develop]
  push:
    branches: [main]

jobs:
  detect-secrets:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0
      - name: Run Detect Secrets
        uses: inditex/detect-secrets-action@v1
```

---

### CORRECCION CI-04: Configurar Snyk (PRIORIDAD: MEDIA)

**Referencia:** Quality 360 - "Snyk: software composition analysis tool"

**Problema:** No hay analisis de dependencias de terceros.

**Correccion:** Agregar workflow de Snyk:

```yaml
name: security-snyk

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  snyk:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Run Snyk
        uses: snyk/actions/dotnet@master
        env:
          SNYK_TOKEN: ${{ secrets.SNYK_TOKEN }}
```

---

## 5. Documentacion

### CORRECCION D-01: Agregar CONTRIBUTING.md (PRIORIDAD: MEDIA)

**Archivo:** `CONTRIBUTING.md` (nuevo)
**Referencia:** DevPortal - Contributing process

**Correccion:** Crear archivo con guia de contribucion:

```markdown
# Contributing to Instalador de GUIs

## Development Setup
1. Install .NET 8 SDK
2. Clone the repository
3. Open `InstaladorGuis.sln` in Visual Studio 2022 or later

## Building
```bash
dotnet build
```

## Testing
```bash
dotnet test
```

## Code Style
- Follow `.editorconfig` rules
- Use file-scoped namespaces
- Prefer `var` when type is evident
- Use `BrushHelper.FromHex()` for brush creation

## Pull Request Process
1. Create a feature branch from `develop`
2. Make your changes
3. Ensure all tests pass
4. Update `CHANGELOG.md` if applicable
5. Request review from `@inditex/logistica-dev`

## Security
- Do not hardcode secrets or credentials
- Do not commit network paths that expose infrastructure
- Run `detect-secrets scan` before committing
```

---

### CORRECCION D-02: Agregar CHANGELOG.md (PRIORIDAD: BAJA)

**Archivo:** `CHANGELOG.md` (nuevo)
**Referencia:** DevPortal - "Changelog guidelines: How to keep a consistent, well-structured changelog"

**Correccion:**
```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## [1.3.0] - 2026-08-24

### Added
- PULL&BEAR brand configuration (24 GUIs)
- ZARA HOME brand configuration (17 GUIs)
- Update checking and auto-update flow
- Free registration flow for custom JNLP URLs
- Decision-tree chatbot for user assistance
- Local telemetry/metrics system
- Custom dialog system (AppDialogHost)

### Changed
- Migrated to .NET 8
- Improved error handling with 3-level global exception handlers

### Security
- Added URL validation with allowlist regex
- Added URL sanitization for command execution
```

---

### CORRECCION D-03: Actualizar README.md con referencias a herramientas Inditex (PRIORIDAD: BAJA)

**Archivo:** `README.md`

**Correccion:** Agregar seccion sobre herramientas Inditex usadas:

```markdown
## Herramientas Corporativas

- **SonarCloud:** Analisis estatico - [Quality 360](https://quality360.docs.inditex.dev/)
- **Detect Secrets:** Deteccion de secretos - [Detect Secrets](https://detect-secrets.docs.inditex.dev/)
- **Snyk:** Analisis de dependencias - [Snyk](https://snyk.docs.inditex.dev/)
- **Defect Dojo:** Gestion de vulnerabilidades - [Defect Dojo](https://defectdojo.docs.inditex.dev/)
```

---

## 6. Estructura del Repositorio

### CORRECCION R-01: Agregar LICENSE (PRIORIDAD: BAJA)

**Archivo:** `LICENSE` (nuevo)

**Correccion:** Para uso interno Inditex:
```
Internal Use Only - Inditex S.A.
This software is proprietary and confidential.
Unauthorized copying, modification, distribution, or use of this software is strictly prohibited.
```

---

### CORRECCION R-02: Verificar .gitignore completo (PRIORIDAD: MEDIA)

**Archivo:** `.gitignore`

**Codigo actual:** Parece completo pero verificar que incluye:

```gitignore
# Build results
[Bb]in/
[Oo]bj/
[Dd]ebug/
[Rr]elease/

# IDE
.vs/
.vscode/
.idea/
*.user
*.suo

# Logs
*.log

# OS
Thumbs.db
Desktop.ini
.DS_Store

# Test results
TestResults/

# Secrets
*.pfx
*.key
*.pem
.env
.env.local

# Local config
*.local.json
appsettings.Local.json
```

---

## Resumen de Prioridades

### CRITICO (antes de primer commit a repositorio corporativo):
| ID | Correccion | Archivos | Esfuerzo |
|----|-----------|----------|----------|
| S-01 | Sanitizacion de URLs - command injection | InstallerService.cs, CommandService.cs | Alto |
| S-02 | Eliminar ruta hardcodeada | ErrorLog.cs | Bajo (ya corregido) |
| C-01 | Process disposal correcto | CommandService.cs | Bajo |
| CI-01 | Configurar SonarCloud | sonar-project.properties | Bajo |
| CI-03 | Configurar Detect Secrets | .secrets.baseline, workflow | Medio |

### ALTO (corto plazo - primer PR):
| ID | Correccion | Archivos | Esfuerzo |
|----|-----------|----------|----------|
| S-03 | Rutas UNC en repositorio | pb.json, zh.json | Medio |
| S-04 | BinaryFormatter inseguro | .csproj / runtimeconfig | Bajo |
| S-05 | Validacion URL en RegistroLibre | MainWindow.xaml.cs | Bajo |
| C-02 | Eliminar codigo muerto | Windows/RegistroLibreWindow.* | Bajo |
| C-04 | Thread safety en MetricsService | MetricsService.cs | Bajo |
| T-01 | Agregar tests unitarios | Tests/*.cs | Alto |
| T-02 | Configurar cobertura | .csproj, sonar-project.properties | Bajo |
| CI-02 | GitHub Actions workflow | .github/workflows/ci.yml | Medio |

### MEDIO (medio plazo):
| ID | Correccion | Archivos | Esfuerzo |
|----|-----------|----------|----------|
| C-05 | Fire-and-forget Task.Run | MainWindow.xaml.cs | Bajo |
| CI-04 | Configurar Snyk | .github/workflows/security-snyk.yml | Bajo |
| D-01 | CONTRIBUTING.md | CONTRIBUTING.md | Bajo |
| R-01 | LICENSE | LICENSE | Bajo |
| R-02 | Verificar .gitignore | .gitignore | Bajo |

### BAJO (mejoras incrementales):
| ID | Correccion | Archivos | Esfuerzo |
|----|-----------|----------|----------|
| C-06 | Eliminar strings magicos | BrandService.cs, BrandConfig.cs | Bajo |
| T-03 | Mutation testing | .csproj, stryker-config.json | Medio |
| D-02 | CHANGELOG.md | CHANGELOG.md | Bajo |
| D-03 | Actualizar README | README.md | Bajo |

---

## Referencias Inditex

- Security White Paper: https://security-white-paper.docs.inditex.dev/secudoc/stable/
- Quality 360: https://quality360.docs.inditex.dev/qualitydoc/latest/
- Detect Secrets: https://detect-secrets.docs.inditex.dev/clrdsecret/stable/
- SonarCloud: https://quality360.docs.inditex.dev/qualitydoc/latest/configuration/technologies.html
- DevPortal: https://devportal.docs.inditex.dev/devportal/latest/
- Reference Architecture: https://referencearchitecture.docs.inditex.dev/reference-architecture/latest/
- OWASP ASVS: https://owasp.org/www-project-application-security-verification-standard/
