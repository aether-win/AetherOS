# Aether Transfer — hoja de ruta

> Cliente FTP/FTPS/SFTP profesional e independiente (no toca el Explorador real),
> ventana propia con identidad Aether. Proyecto: `aether-transfer/`.
> Regla 38: **no compilar el .exe final** hasta autorización expresa de Daniel.

## Decisiones (ACTUALIZADO)
- **Stack:** **WPF + C#/.NET 8** (migrado desde WinUI 3 el 2026-11-21; ver abajo), self-contained
  **single-file** (un solo `.exe` que abre sin instalar nada). Backend agnóstico de UI.
- **Por qué WPF y no WinUI 3:** WinUI 3 unpackaged **crasheaba al arrancar** en la laptop de
  Daniel (`combase.dll` 0x802B000A, `XamlParseException` irreproducible). WPF abrió a la primera.
  **No volver a WinUI para estas apps.**
- **Protocolos:** FluentFTP (FTP/FTPS) + SSH.NET (SFTP) — ambos MIT, sin obligaciones GPL.
- **Credenciales:** DPAPI (usuario actual). Nunca en texto plano. Sitios en
  `%APPDATA%\Aether Transfer\sitios.json`.
- **Icono:** el de Daniel (PNG 1024/512/256 + ICO). El arte no se regenera.
- **UI (decisión de Daniel, 2026-11-21):** **un solo panel** (solo carpetas FTP, a lo ancho; se
  quitó el panel local), porque se puede **arrastrar desde/hacia el Explorador nativo**. Tema
  **oscuro total** (nada blanco: barra de título, menús, combos, tooltips, confirmaciones).

## Estructura del proyecto
```
aether-transfer/
  AetherTransfer.csproj      (WPF, net8.0-windows, self-contained single-file)
  App.xaml / App.xaml.cs     (tema oscuro GLOBAL: estilos de todos los controles)
  Tema.cs                    (barra de título oscura vía DwmSetWindowAttribute)
  MainWindow.xaml / .cs      (un solo panel FTP + cola + registro)
  Models/                    (ConexionInfo, ElementoRemoto, ProgresoTransfer)
  Protocols/                 (IServidorRemoto, ServidorFtp [FluentFTP], ServidorSftp [SSH.NET])
  TransferEngine/            (TransferItem, GestorTransferencias: cola secuencial)
  Connections/AlmacenSitios  (SitioGuardado + guardar/cargar, DPAPI)
  Security/Credenciales      (DPAPI proteger/desproteger)
  Settings/ConfigApp         (config.json: velocidad, timeout, confirmar eliminar)
  Views/                     (DialogoSitios, DialogoConfig, DialogoSync, InputBox, Confirmacion)
```
CI: `.github/workflows/aether-transfer.yml` → `dotnet publish -c Release -r win-x64
--self-contained true -p:PublishSingleFile=true` → artefacto `Aether-Transfer-<sha>`.

## Estado por fase
Leyenda: ✅ hecho · 🟡 parcial · ⬜ pendiente

| Fase | Área | Estado |
|------|------|--------|
| 1-4 | Entorno, arquitectura, icono, interfaz | ✅ (ahora en WPF) |
| 5 | Motor de protocolos (FluentFTP/SSH.NET) | ✅ FTP/FTPS/SFTP: conectar, listar, subir, descargar, crear/eliminar/renombrar |
| 6 | Administrador de sitios (DPAPI) | ✅ + casilla "Guardadas" (reconexión 1 clic, auto-guardado) |
| 7 | Explorador FTP (un panel, operaciones reales) | ✅ navegar, menú contextual, atrás/adelante con historial |
| 8 | Cola de transferencias (progreso, pausar/reanudar) | ✅ |
| 9 | Sincronización | 🟡 (existe `DialogoSync` pero el botón se quitó al pasar a un panel; revisar si se reintroduce) |
| 10 | Seguridad (TLS/SSH, credenciales) | ✅ DPAPI + FTPS/SFTP |
| 11 | Configuración | ✅ velocidad, timeout, confirmar eliminar |
| — | **Arrastrar y soltar** (Explorador↔FTP) | ✅ subir (Explorador→panel); descargar (panel→Explorador, a temporal) |
| — | **Tema oscuro total + barra de título** | ✅ |
| 13 | Pruebas reales | 🟡 **PS4 probado** (conectar, listar, subir, borrar, navegar). Falta SFTP real + archivos grandes/velocidad |
| 14 | Compilación/distribución final | 🔒 bloqueada hasta autorización (Regla 38) |

## Pendiente
- **Probar SFTP** real y archivos grandes (velocidad sostenida).
- **Subir carpetas** por arrastre (hoy solo archivos sueltos).
- Arrastrar-hacia-afuera de archivos grandes descarga a temporal primero (pausa breve); mejorable
  con streaming de archivo virtual (COM `IDataObject`).
- Decidir si se reintroduce el botón de **Sincronizar** (el motor `DialogoSync` sigue en el repo).

## Bitácora
**2026-11-21 (segunda cuenta) — migración a WPF + cliente FTP funcional probado con PS4:**
- Diagnóstico y abandono de WinUI 3 (crash `combase` 0x802B000A irreproducible).
- Migración completa de la UI a **WPF .NET 8** (backend conservado). La app **abre bien**.
- Conexión anónima (PS4, puerto 2121, sin usuario). Arrastrar/soltar Explorador↔FTP.
- Un solo panel; casilla de conexiones guardadas; navegación `‹ ›` con historial; registro con
  auto-scroll; Subir (selector) / Descargar (a carpeta Descargas); tema oscuro total (incluida
  barra de título y tooltips); motor FTP más sólido/rápido (timeouts de datos, keep-alive,
  reintentos, chunk 256 KB).
- **Probado en la PS4 real de Daniel**: conectar, listar `/data`, subir, borrar, navegar. ✅
- Detalle completo paso a paso y decisiones: **TRASPASO-AETHER.md, Parte C.**

**2026-11-21 (primera cuenta) — núcleo funcional en WinUI 3 (luego reemplazado por WPF):**
- Fases 1-11 implementadas en WinUI 3. Compilaba en verde pero **no abría** en la laptop de
  Daniel → se migró a WPF (ver arriba).
