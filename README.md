# Instalador de GUIs (WPF)

Aplicacion nativa de Windows en **C# / WPF** (.NET 8) para instalar y desinstalar las GUIs de almacenes de Inditex: **PULL&BEAR** y **ZARA HOME**.

La aplicacion gestiona la instalacion/desinstalacion de aplicaciones Java (JNLP) a traves de [Amiga Java Launcher](https://amiga-java.docs.inditex.dev/) en dispositivos de almacen.

## Arquitectura

```
InstaladorGuis/
├── App.xaml(.cs)              # Punto de entrada, DI manual, manejo global de errores
├── MainWindow.xaml(.cs)       # Ventana principal con logica de seleccion e instalacion
├── Brands/                    # Configuraciones JSON por marca (PB, ZH)
├── Controls/                  # UserControls reutilizables (dialogos, chatbot de ayuda)
├── Models/                    # Modelos de datos (BrandConfig, MetricsData, BannerKind)
├── Services/                  # Logica de negocio
│   ├── InstallerService.cs    # Instalacion/desinstalacion via Amiga Java Launcher
│   ├── BrandService.cs        # Carga de configuracion de marcas
│   ├── CommandService.cs      # Ejecucion de procesos sin shell (ArgumentList)
│   ├── MetricsService.cs      # Telemetria de uso
│   └── PathService.cs         # Resolucion de rutas y actualizadores
├── Themes/                    # ResourceDictionary con estilos y colores
└── ViewModels/                # ViewModels (GuiVm)
```

**Patron:** MVVM parcial con code-behind, inyeccion de dependencias manual.

## Requisitos

- .NET 8 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- Windows 10/11
- Amiga Java Launcher en `C:\Program Files\AmigaLauncher\amglauncher.exe` (para operaciones de instalacion)

## Compilar y ejecutar

```bash
# Compilar
dotnet build InstaladorGuis.sln

# Ejecutar (PULL&BEAR)
dotnet run --project InstaladorGuis -- --brand=PB

# Ejecutar (ZARA HOME)
dotnet run --project InstaladorGuis -- --brand=ZH
```

Tambien se puede definir la variable de entorno `GUIS_BRAND=PB` o `GUIS_BRAND=ZH`.

## Configuracion de marcas

Cada marca se configura mediante un archivo JSON en `InstaladorGuis/Brands/`:

| Archivo | Marca |
|---------|-------|
| `pb.json` | PULL&BEAR |
| `zh.json` | ZARA HOME |

Los archivos definen: nombre de marca, lista de GUIs (PAQ/PRC), rutas de red y actualizador.

**Importante:** las rutas UNC/unidad (`rutasRedBase`, `rutaMetricasBase`, `rutaActualizadorBase`) son **especificas de cada centro**. Ajusta `Brands/*.json` (o un override `*.local.json` no versionado) segun el mapeo de red del sitio. No commits overrides locales.

## Dependencias

| Paquete | Version | Proposito |
|---------|---------|-----------|
| .NET 8.0 | 8.0 | Runtime y SDK |
| FluentIcons.Wpf | 2.1.337 | Iconografia Fluent para WPF |

## Herramientas Corporativas

- **SonarCloud:** Analisis estatico - [Quality 360](https://quality360.docs.inditex.dev/)
- **Detect Secrets:** Deteccion de secretos - [Detect Secrets](https://detect-secrets.docs.inditex.dev/)
- **Snyk:** Analisis de dependencias - [Snyk](https://snyk.docs.inditex.dev/)
- **Defect Dojo:** Gestion de vulnerabilidades - [Defect Dojo](https://defectdojo.docs.inditex.dev/)

## Testing

```bash
dotnet test
# Cobertura OpenCover (coverlet.msbuild):
dotnet test /p:CollectCoverage=true
```

Mutation testing (opcional): instalar `dotnet-stryker` y usar `stryker-config.json`.

## Notas tecnicas

- Los procesos se lanzan con `ProcessStartInfo.ArgumentList` (sin `cmd.exe`) para evitar command injection.
- Los logs de error se escriben en `%LOCALAPPDATA%`, `%APPDATA%` y `%TEMP%`.
- La telemetria se almacena localmente en formato JSON.
- El chatbot integrado es un arbol de decision predefinido (no IA).

## Acceso directo (compatibilidad HTA)

El paquete de instalación incluye un HTA launcher con el **mismo nombre** que el instalador clásico:

| Marca | HTA (junto al .exe) | Lanza |
|-------|---------------------|--------|
| PB | `Instalador de GUIS P&B.hta` | `InstaladorGuis-PB.exe --brand=PB` |
| ZH | `Instalador de GUIS ZH.hta` | `InstaladorGuis-ZH.exe --brand=ZH` |

Así los accesos directos existentes no cambian: siguen apuntando al HTA, y el HTA solo arranca el exe.

Fuentes: `InstaladorGuis/Launchers/`. `compilar-wpf.bat` los copia a `publish\*\instalador\`.

## Licencia

Uso interno de Inditex. Ver `LICENSE`.
