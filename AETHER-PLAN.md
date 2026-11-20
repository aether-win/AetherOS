# Aether OS — Plan maestro y progreso

> Si la sesión se corta: este archivo es la fuente de verdad. Leer "Progreso" y seguir
> desde la primera casilla sin marcar. Repositorio: **github.com/aether-win/AetherOS** (privado),
> clon de trabajo en `/home/claude/aetheros` (rama `main`). Commits con las líneas Co-Authored-By/Claude-Session.
> Recursos de Wind-Aether en `aether/wind-aether/`. Script de red probado en `aether/red/`.
> Para seguir en otra cuenta de Claude: ver `TRASPASO-AETHER.md`.

Base: Atlas 0.6.0 (GPLv3). Aether sigue siendo GPLv3, con aviso de que deriva de Atlas.
Usuario: laptop HP 15-fc0, Ryzen 7 7730U (Vega 8 integrada), 16 GB DDR4-3200 doble canal,
Windows 11 Pro 26H2 (26300), pantalla 1920x1080. Idioma: todo en español.

## 1. Decisiones de diseño

### Marca
- Nombre: **Aether OS**. Logo: "A" con órbita (recursos en `branding/`).
- Paleta: acento `#2F8CFF`, celeste `#4FC3F7`, oscuro `#0A0F1E`/`#1B1D23`, claro `#F3F5F8`.
- Acento de Windows = azul Aether. Fondo de escritorio y pantalla de bloqueo de Aether.
- Información OEM "Aether OS" en Configuración > Sistema > Información. Foto de perfil con logo.
- Casilla en el asistente: **tema de Windows Claro / Oscuro**.

### Instalación
- Sin AME Wizard. App **Aether** (derivada de Atlas Manager, Rust/GPUI) = instalador + centro de control.
- Flujo: Preparar (actualiza Windows y Store una última vez) → Asistente (casillas) → Instalar → reiniciar.
- Se conserva el script de PowerShell (front door) para modo avanzado y la creación de ISO/USB.

### Actualizaciones y seguridad
- Windows Update **pausado hasta el año 3000**, activado por defecto al instalar.
- Botón "Actualizar Windows ahora" (quita pausa, actualiza, vuelve a pausar).
- Botón "Actualizar todas mis apps" (winget upgrade).
- **Casilla "Eliminar Windows Defender y SmartScreen"** (desmarcada por defecto; reversible
  con "Elegir de nuevo"). SmartScreen (Windows y Edge) sigue la misma casilla.
- Firewall de Windows y UAC se quedan.
- Casilla experimental "Subir energía del procesador (no seguro)" con WinRing0: carga el driver
  solo en Modo Gaming, desactiva la lista de bloqueo de drivers vulnerables. Apagada por defecto.

### Red (obligatorio, automático)
- Todo lo de red encendido: uso compartido, NetBIOS/NetBT, detección, "Conceder acceso a",
  "Red" en el Explorador, LLDP, impresión (servicio + menú Imprimir + impresoras en red).
- Quitar los tweaks de Atlas `restrict-anonymous-access`, `restrict-anonymous-enumeration`,
  `disable-network-navigation-pane`, y el toggle FileSharing debe quedar en Enable.
- Script de reparación de red (`aether/red/Reparar-Red-Aether.ps1`, PROBADO y
  funcionó): se aplica al instalar, lo vigila la Revisión de salud y existe el botón
  "Diagnosticar y reparar" + script compañero para la otra PC.
- Unidades de red fijas por IP con reconexión; IPs de PCs de la red en Intranet local y
  ubicaciones de confianza de Excel (macros).
- Ahorro de energía del adaptador de red apagado con cargador.

### Modos
- **Trabajo** (manual): todo activo, perfil visual elegido, turbo CPU activo, ventiladores
  silenciosos, apps en segundo plano permitidas, indexado normal, notificaciones, reconecta red.
- **Gaming** (manual, **requiere cargador**: si no hay, mensaje "Conecta el cargador…" y no
  activa): máximo rendimiento sin estacionar núcleos, timer resolution alta, no molestar,
  pausa mods pesados, cierra segundo plano, recordatorio "¿Ventilador conectado?".
  Opción de limitar el turbo CPU para dar más energía a la gráfica integrada.
- **Batería**: se activa **solo al 30%** sin cargador (configurable 10–50%), aviso de 10 s con
  Cancelar, vuelve al modo anterior al conectar cargador; también manual. Usa Aether Limpio,
  CPU eficiente, ahorro de batería de Windows.
- Cambio también desde ícono de bandeja y atajo de teclado.

### Apps (casillas del asistente)
- IA: Claude (winget `Anthropic.Claude`), ChatGPT (Store `9NT1R1C2HH7J`; sin cuenta/Store →
  instalador oficial).
- Trabajo: Python **3.13.16** (PATH, py, pip, rutas largas, sin alias de Store), KeyVault
  (exe propio del usuario, acceso directo en Escritorio e Inicio).
- Utilidades: Microsoft PC Manager (Store `9PM860492SZD`; si falla → instalador oficial).
- Gaming: Steam (casilla), drivers AMD.
- Interfaz: Wind-Aether (Windhawk 2.0 + paquete de mods).
- Instalar apps de Store como el usuario, nunca como SYSTEM.
- NO incluir: IDM, WinRAR, IObit, FileZilla Pro (copias crackeadas), ni alternativas.

### Edge
- Se queda siempre (se quita la opción de eliminarlo). Buscador Google: preferencias iniciales
  + vigilancia que repone Google si vuelve Bing. Limpieza de Edge (compras, arranque en
  segundo plano, sugerencias); pestañas en suspensión activas.

### Wind-Aether (sección de la app)
- Windhawk 2.0 oficial por debajo; Aether lo maneja con su CLI (`data import`, `mod enable`, ...).
- Paquete: `aether/wind-aether/Wind-Aether-mods.json` (37 mods, todos desactivados,
  incluye `local@aether-margen-escritorio` y Ventanas translúcidas 1.8.2-es-aether).
- Perfiles: `aether/wind-aether/Wind-Aether-perfiles.json` (Aether Glass / Aether
  Limpio, pausa en Gaming, colores claro/oscuro, choques conocidos).
- Windhawk no se inyecta en juegos + exclusión de `steamapps\common\*`.
- Mod pendiente (experimental): "Bandeja con nombres" — NO pedido; descartado por ahora.

### Otros en la app
- Batería: salud (reporte de Windows) + recomendación de límite de carga 80% en BIOS HP.
- Espacio: limpieza segura (nunca Descargas/Escritorio/documentos) + "¿Qué ocupa espacio?".
- Respaldo: el usuario elige carpetas y archivos sueltos, destino, horario, versiones,
  restaurar, aviso si falla.
- Asistente de primer arranque. Revisión de salud.
- Fuera: botón SISTEMA EF, ordenar escritorio, seguridad mínima, administrador de inicio
  (lo cubre PC Manager).

### Cambios a los valores de Atlas
| Atlas | Aether |
|---|---|
| Uso compartido apagado | Encendido |
| Hibernación desactivada | Activa |
| Actualizaciones de apps de la Store apagadas | Activas |
| Apps en segundo plano desactivadas | Permitidas (limitadas en Gaming/Batería) |
| Corrector ortográfico desactivado | Activo |
| Aviso de poco espacio desactivado | Activo |
| Opción de quitar Recortes / atajo de captura desactivado | Recortes se queda, atajo activo |
| "Versiones anteriores" quitado | Se queda |
| Desactivar dispositivos (HPET, Base System Device…) | No en laptops |
| Indexado mínimo | Normal |
| Animaciones reducidas | Completas en Trabajo |
| Quitar Edge / otro navegador / eclean / Toolbox | Fuera |
| Toggle quitar Microsoft Store | Oculto |
| Vincular teléfono desactivado | Activo e instalado |
| Quitar "Imprimir" del menú | Se queda |
| Menú contextual antiguo (Atlas) | Lo hace el mod de Wind-Aether si está activo |

### Se borra de Atlas
Servicio de reportes (`services/`), AME Wizard (`Configuration/custom.yml` y textos), Toolbox,
eclean, selección de navegador, quitar Edge, enlaces de Atlas (Discord, web, ko-fi),
FUNDING, actualizaciones desde Atlas 0.4/0.5 (código de compatibilidad) — este último con
cuidado, en una pasada aparte.

## 2. Plan de construcción
1. Base: copiar Atlas, cambiar a Aether, borrar lo que no sirve, traducir al español.
2. Ajustes: red, modos, Defender/SmartScreen, Edge+Google, actualizaciones pausadas, valores.
3. Instalador y asistente con casillas (incluye tema claro/oscuro).
4. App de control: modos, Wind-Aether, respaldo, red, limpieza, salud, batería.
5. Pruebas (las hace el usuario): VM Hyper-V → imagen del disco → laptop.

## 3. Progreso
- [x] Proyecto creado desde Atlas con git (commit base).
- [x] Recursos de marca (logo, .ico, fondos) en `branding/`.
- [x] Borrado (parte 1): servicio de reportes (`services/`), Toolbox (carpeta, launcher,
      Install-Toolbox.ps1, Toolbox-Package.ps1, tests), instaladores de navegadores/Toolbox/eclean
      en Atlas.Software, NanaZip por defecto (Windows 11 abre .rar/.7z), toggle y launcher
      "Quitar Edge" + entrada en catalog.json, enlaces .url de Atlas, FUNDING.yml, refs de CI.
- [ ] Borrado (parte 2, pasada aparte con cuidado):
      * AME Wizard: `playbook/Configuration/custom.yml`, `Entry/Invoke-AtlasInstall.ps1` y los
        validadores de build que lo exigen (`tools/build/AtlasBuild/AtlasYamlAction.ps1`,
        `Test-Apbx.ps1`, `Set-AtlasVersion.ps1`).
      * Código muerto de opciones eliminadas (ya no se pueden elegir): quitar Edge
        (`Install/Tasks/Invoke-AtlasEdgeRemoval.ps1`, `Operations/Remove-Edge*.ps1`, ramas en
        Components/Choices phase, Initialize-NewUser, Test-EdgeState.cmd, Widgets.ps1 usa
        Remove-Edge.ps1 como helper — revisar), quitar Recortes (`Atlas.Appx` Removal,
        `disable-screen-capture-hotkey` tweak), LibreWolf (`Operations/Initialize-AtlasLibreWolfUser.ps1`,
        Initialize-NewUser), listas de opciones conocidas (`Atlas.Tweaks/Domain/Schema.ps1`,
        `Entry/Initialize-AtlasInstallState.ps1`, `Atlas.InstallState.psm1`,
        `Preparation/WindowsTransition.ps1`), `Repair-AtlasToolboxRecord`.
      * Actualizaciones desde Atlas 0.4.1/0.5.0 (Compat/, LegacyChoices, Rebase de Atlas).
      * `Scripts/Registry/README.md` habla de copias en Toolbox (ya no existe).
      * App (Rust): página de reportes, opciones viejas del asistente, 14 idiomas que no son
        español (se rehace en el paso 3).
      * Tests que referencian lo borrado (actualizar cuando haya pwsh para correrlos).
- [x] Marca Aether y opciones del instalador:
      * `playbook.conf` en español con opciones nuevas: tema (`theme-dark`/`theme-light`),
        `experimental-cpu-power`, `app-claude`, `app-chatgpt`, `app-python`, `app-keyvault`,
        `app-pcmanager`, `app-steam`, `app-amd-drivers`, `wind-aether`. Sin hibernación,
        ahorro de energía, quitar Recortes/Edge, navegadores, Toolbox ni eclean. Teclado por
        defecto: un solo teclado. Nuevo UniqueId. (Version sigue 0.6.0 hasta la pasada de versión.)
      * Opciones nuevas en `Atlas.Tweaks/Domain/Schema.ps1` (AtlasKnownOptions).
      * Temas `Themes/aether-oscuro.theme` y `aether-claro.theme` (acento #2F8CFF); MRU actualizado.
      * `config-pins.ps1`: ancla Edge y guarda `HKLM\SOFTWARE\AtlasOS\SetupOptions\theme`.
      * `Initialize-NewUser.ps1`: aplica el tema elegido, fondo `aether-escritorio.jpg`,
        acceso directo "Aether.lnk". Pantalla de bloqueo `aether-bloqueo.jpg`.
      * OEM: "Aether OS <versión>" (Model, RegisteredOrganization, arranque), fabricante Aether OS.
      * `Other/atlas-folder.ico` y `Other/user.png` ahora con la marca Aether (mismo nombre interno).
      * PENDIENTE de implementar (paso 2/3): las opciones `app-*`, `wind-aether`,
        `experimental-cpu-power` todavía no hacen nada; fondo claro propio (hoy ambos temas usan
        el mismo fondo oscuro).
- [ ] (Siguiente) Traducir al español la carpeta de accesos directos (AtlasDesktop: 93 carpetas, 209
      launchers), sus rutas en las definiciones de toggles (`Launcher`), `catalog.json`,
      `Initialize-NewUser.ps1` (rutas fijas) y los textos de consola. Hacerlo con un mapa
      inglés→español aplicado de forma consistente.
- [x] Revisión (parte 1): sin referencias a archivos borrados; `ToolboxLauncher` quitado de 11
      toggles, del generador `tools/dev/New-ToggleLaunchers.ps1` y de `catalog.json`;
      `atlas-theme-upgrade` usa el tema Aether.
- [x] Paso 2 (ajustes de los scripts), primera versión (20-nov-2026):
      * Red: FileSharing → Enable e Indexing → Enable en `Invoke-ServicesPhase.ps1`; tweak nuevo
        `networking/aether-red` (versión sin preguntas de Reparar-Red-Aether: drivers/servicios SMB,
        impresión, proveedor de red, enlaces + LLDP, NetBIOS, redes Privadas, firewall, firma SMB e
        invitado); desactivados `restrict-anonymous-access`, `restrict-anonymous-enumeration`,
        `disable-network-navigation-pane`, quitar "Imprimir" (2) y `disable-pnp` (laptops/LLDP);
        `Initialize-NewUser` usa "Enable Network Navigation Pane.cmd". Script interactivo en el
        payload: `AtlasModules/Aether/Red/` (para el botón "Diagnosticar y reparar" del paso 4).
      * Actualizaciones: `disable-auto-updates-option` ahora aplica el toggle PauseUpdates (año 3000).
        Pendiente menor: "Elegir de nuevo" (Choices) todavía mapea la opción al toggle AutomaticUpdates.
      * Defender + SmartScreen: tweak `security/aether-disable-smartscreen` (Option defender-disable):
        Explorador, apps de la Store y Edge. Con defender-enable no se toca.
      * Edge: tweak `qol/aether-edge`: `initial_preferences` con Google (solo perfiles NUEVOS) +
        directivas de limpieza (primer arranque, Startup Boost, segundo plano, compras,
        recomendaciones, pestañas en suspensión). La "tarea que repone Google" NO se hizo: Edge
        protege el buscador de un perfil existente con firma (Secure Preferences) y lo revierte;
        en su lugar la app (paso 4) tendrá un botón que abre edge://settings/searchEngines.
      * Apps: tweak `misc/aether-apps` guarda las casillas en `HKLM\SOFTWARE\AtlasOS\SetupOptions\Apps`;
        `Initialize-NewUser` registra en RunOnce `AtlasModules/Aether/Install-AetherApps.ps1`, que corre
        como el usuario (con UAC, nunca SYSTEM) al primer inicio tras el reinicio: Claude (winget),
        ChatGPT y PC Manager (Store; si falla abre la página oficial), Python 3.13.16 (python.org,
        si falla winget; PATH, py, pip, rutas largas, sin alias), KeyVault (exe del usuario en
        `Aether/KeyVault/`, copia a Program Files + accesos en Escritorio e Inicio), Steam (winget),
        Wind-Aether (winget Windhawk + `windhawk-cli data import --yes`; si no hay CLI deja el json en
        Documentos), drivers AMD (no hay en winget: abre la página de AMD). Marca
        `HKLM\SOFTWARE\AtlasOS\Aether\AppsInstaladas=1`. Registro en `%ProgramData%\Aether\apps.log`.
      * Valores: desactivados disable-background-apps, disable-spell-checking,
        disable-store-auto-updates, remove-previous-versions, disable-low-disk-warning; activado
        disable-game-bar; Vincular teléfono se queda (sin Remove-AtlasPhoneLinkAppx ni toggle
        PhoneLink Disable); se quitan GamingApp, XboxGamingOverlay, SpeechToTextOverlay y LinkedIn.
        Hibernación ya estaba activa (set-power-settings).
      * DECISIÓN del usuario (19-nov): conserva Paint, Cámara, Notas rápidas, Alarmas y reloj,
        Grabadora de sonido, To Do y Outlook nuevo (quitados de `Atlas.Appx/Domain/Removal.ps1`);
        de esas solo se borra OneDrive (ya lo quita la fase Components).
      * Sin hacer todavía: `experimental-cpu-power` (va con los modos, paso 4).

## 4. Compilación y pruebas
GitHub Actions en el repo: `test.yml` (Pester), `lint.yml` (PSScriptAnalyzer), `yaml.yml`, `build.yml`
(paquete .apbx) corren en cada push. `app.yml` (app Rust, pesada) solo manual por ahora.
`integration.yml` necesita un runner propio con Hyper-V (no se usa). Ver resultados con
`gh run list --repo aether-win/AetherOS`.

### Estado de CI — TODO EN VERDE ✅ (commit 303b0f6, paso 2)
Build Playbook ✅ · Lint PowerShell ✅ · Lint YAML ✅ · Test (Pester PS7 + Windows PowerShell 5.1) ✅.
Hecho en la limpieza: contrato de opciones igualado (playbook.conf = state = schema = custom.yml);
sin tweak del atajo de captura; set-power-settings sin opciones viejas; Components y Choices
sin quitar Edge ni apps; AppX sin Edge ni Recortes; catálogo regenerado en CI
(`regenerate.yml` → rama `regenerado`; traer con `git fetch --depth=2 origin regenerado &&
git cherry-pick -n FETCH_HEAD`). Los artefactos de Actions NO se pueden descargar desde aquí.

Código heredado que sigue (no se puede elegir, no rompe nada; quitar en la pasada de
compatibilidad): transición de Windows/Rebase con opciones de Atlas (`WindowsTransition.ps1`,
fixtures `tests/fixtures/windows-transition/*.json`, app Rust `legacy_choices.rs`/`rebase.rs`),
`Invoke-AtlasEdgeRemoval.ps1`/`Remove-Edge*.ps1` (usados aún por Widgets y Test-EdgeState),
`Initialize-AtlasLibreWolfUser.ps1`, AME `custom.yml` y su validador de build.

Resuelto: el usuario conserva Paint, Cámara, Notas rápidas, Alarmas y reloj, Grabadora de sonido,
To Do y Outlook nuevo; solo se borra OneDrive.

### Reglas que exige CI
- Todo `.ps1/.psm1/.psd1` con caracteres no ASCII (acentos, ñ) debe llevar BOM UTF-8
  (PSScriptAnalyzer `PSUseBOMForUnicodeEncodedFile`; Windows PowerShell 5.1 lee mal sin BOM).
- `playbook.conf` `<Title>` debe contener `v<Version>` (Test-Apbx). Set-AtlasVersion ya escribe "Aether OS v...".

## 5. Limitación del entorno
Aquí no hay PowerShell ni Windows: no se puede compilar el .apbx, la app ni correr las pruebas
Pester. Para probar: GitHub Actions (los workflows de Atlas ya existen) o la VM del usuario.
