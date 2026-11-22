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
- [x] Carpeta de accesos directos (AtlasDesktop) en español (20-nov-2026): 9 carpetas principales
      ("1. Programas" … "9. Solucion de problemas"), 93 carpetas y 209 archivos renombrados, rutas
      `Launcher` de los toggles, `Initialize-NewUser`, scripts, pruebas y catálogo. **Sin acentos ni ñ**:
      el validador de rutas de launchers y el paquete .apbx solo aceptan ASCII (con acentos el
      paquete guardaba "Mont�n"). Marca "(predeterminado)" en lugar de "(default)" (el motor la usa en
      `Atlas.Toggles/Domain/Engine.ps1` para el reinicio de servicios) y ya refleja los valores de
      Aether (uso compartido, Red en el Explorador, indexado normal, hibernación, apps en segundo
      plano, Vincular teléfono). Mapa usado: componente por componente (inglés → español).
      Se dejaron en inglés a propósito las rutas viejas de Atlas que usa el código de
      compatibilidad (WindowsTransition, Legacy*, fixtures, `legacy_choices.rs`) y los textos de la
      app Rust (se rehace en el paso 3). Pendiente: textos de consola de los scripts (en inglés).
- [x] Marca: **nada visible se llama Atlas** (pedido del usuario, 20-nov-2026). Cambiado a "Aether"/"Aether OS":
      mensajes de consola, avisos, títulos y preguntas de los scripts (`playbook/`, `app/resources/`),
      encabezado de consola (`$script:AtlasUiBrand = 'Aether OS'`), plan de energía "Aether OS", catálogos
      de la app (es, en-GB, en-US), título de ventana, datos del .exe (versión/propiedades), ícono y
      logo de la app (`assets/brand/atlas-mark.svg` ahora es la "A" con órbita), enlaces (repo de
      Aether; sin Discord ni reportes, que iban a servidores de Atlas). **Se quedan con nombre interno
      Atlas** (no los ve el usuario normal y cambiarlos rompería todo): módulos `Atlas.*`, funciones
      `*-Atlas*`, carpetas `C:\Windows\AtlasModules` y `C:\Windows\AtlasDesktop` (el acceso del
      escritorio se llama "Aether"), claves `HKLM\SOFTWARE\AtlasOS`, `%LOCALAPPDATA%\AtlasOS`, nombre
      interno del binario. Las herramientas de desarrollo (`tools/`) y sus pruebas siguen diciendo Atlas.
      La licencia GPL y "basado en AtlasOS" se mantienen (obligatorio).
- [x] Paso 3 (primera versión): la app ya muestra el asistente de `playbook.conf` (en español) sin cambios
      de código: las páginas nuevas (tema, apps, Wind-Aether) salen como "Elige una" / "Extras" genéricas.
      La app se fuerza a español (`i18n::activate`). Workflow nuevo **`.github/workflows/aether-app.yml`**
      (manual): arma `Aether OS.apbx`, lo mete dentro de la app (`embedded-playbook`, sin GitHub ni
      servidores de Atlas) y deja el artefacto `Aether-OS-<sha>` con `Aether.exe`. Compiló bien
      (run 44676763609). `app.yml` (clippy/pruebas de Rust) NO se corrió: sus pruebas de catálogo
      esperan el playbook.conf en inglés de Atlas; arreglarlo es pendiente.
      Pendientes del paso 3: textos fijos de la app sobre opciones de Atlas (navegador, Toolbox, quitar
      Edge…), avisos "versión de prueba (RC)" del modo embebido, páginas ISO/USB.
- [x] Paso 4 (primera versión, 20-nov-2026): **Centro Aether** hecho en PowerShell + WPF (no hay que
      compilar; Rust queda para más adelante si hace falta). Archivos en
      `AtlasModules/Aether/Centro/`: `Aether.Centro.ps1` (funciones), `Centro-Aether.ps1` (ventana; con
      `-Bandeja` ícono de bandeja; con `-Modo X` cambio directo), `Respaldo-Aether.ps1`, `aether.ico`.
      Tweak `misc/aether-centro`: accesos "Centro Aether" en Inicio y Escritorio público, bandeja en
      `HKLM\...\Run\AetherBandeja`, `GlobalTimerResolutionRequests=1`. Prueba `tests/Aether.Centro.Tests.ps1`.
      * Modos (ajustes en HKCU\Software\AetherOS\Centro; guarda los valores originales del plan la
        primera vez y Trabajo los repone): Trabajo (equilibrado, notificaciones, segundo plano);
        Gaming (solo con cargador; sin estacionar núcleos, EPP 0, turbo agresivo o limitado, modo de
        energía "mejor rendimiento", temporizador 0,5 ms que mantiene la bandeja, no molestar, sin
        segundo plano, pausa `pausarEnModoGaming` de Wind-Aether, recordatorio del ventilador; si se
        desconecta el cargador pasa a Trabajo); Batería (ahorro de batería de Windows, sin turbo,
        EPP 70, eficiencia, Aether Limpio temporal). Automático al 30% (10–50, configurable) con aviso
        de 10 s y Cancelar; vuelve al modo anterior al conectar el cargador. Atajos Ctrl+Alt+T/G/B y
        menú de la bandeja.
      * Red: abrir el reparador, probar con una PC, copiar el reparador al Escritorio, uso compartido.
      * Actualizar: quitar pausa + abrir Windows Update, volver a pausar (lanzadores con /silent),
        `winget upgrade --all`, abrir edge://settings/searchEngines para Google.
      * Limpieza segura (temporales del usuario + papelera), limpieza de Windows (UAC), "¿Qué ocupa
        espacio?" en segundo plano. Batería: carga, salud (WMI) y reporte de Windows; consejo del 80%
        en el BIOS HP. Respaldo: carpetas/archivos, destino, versiones, hora diaria (schtasks como el
        usuario), respaldar ahora, abrir respaldos; aviso en la bandeja si falla. Wind-Aether:
        perfiles Glass/Limpio/ninguno con `windhawk-cli mod enable|disable`. Salud: revisión rápida y
        la completa (lanzador).
      * Unidades de red fijas (20-nov): pestaña Red del Centro: letra + \\IP\carpeta, `net use /persistent:yes`,
        guardadas en %APPDATA%\Aether\unidades.json; la IP/nombre va a Intranet local (ZoneMap) y la
        ruta y la letra a las ubicaciones de confianza de Excel 16.0 (AllowNetworkLocations=1); la
        bandeja reconecta cada minuto y avisa una vez si no puede.
      * App: sin avisos de "versión de prueba" ni "traducción preliminar" (`shell.rs app_notices`).
      * Instalador: las actualizaciones pendientes son solo un aviso (Verdict::Warn en la app; la
        preparación ya no las instala: `$script:AetherPrepararConActualizaciones = $false`).
      * 20-nov (tarde): casilla `experimental-cpu-power` **quitada** (playbook.conf, custom.yml, schema,
        install state; contadores de `AtlasYamlAction.ps1`: 31 acciones, 29 runs, 20 opciones).
        Instalación sin verificar Windows Update/Store (`Assert-PreparationCurrent` solo mira reinicio
        pendiente): quitó el error "Microsoft Store did not answer" y acelera el inicio.
        Asistente de bienvenida (`Centro-Aether.ps1 -Bienvenida`, se abre desde la bandeja la primera
        vez: modo, tema, Wind-Aether, carpeta de red Z:, respaldo). Colores de Wind-Aether por tema
        (`coloresPorTema` con `windhawk-cli mod settings set`; la bandeja los ajusta si cambia el
        tema; botones Tema claro/oscuro). Consola de los accesos en español (`Atlas.Core/Domain/Ui.ps1`,
        s/n).
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

### Estado de CI — TODO EN VERDE ✅ (commit ab8849a, accesos directos en español)
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

- [x] Instalación más rápida: se omiten la limpieza de componentes DISM (AETHER_LIMPIAR_COMPONENTES=1 la activa) y NGEN (AETHER_NGEN=1). Botón "Cancelar instalación" en la pantalla Instalando: se detiene entre pasos.

## 6. Sesión del 2026-11-20 (madrugada) — arreglo del error de instalación + lote de mejoras

### Error que detenía la instalación (RESUELTO)
Log del usuario: `No se puede enlazar el argumento al parámetro 'Path' porque es nulo` en
`Invoke-AtlasTweak`. Causa real: el companion `qol/aether-edge.ps1` pasaba
`${env:ProgramFiles(x86)}` (vacío bajo TrustedInstaller) directo a `Join-Path` con
`ErrorActionPreference=Stop`, y eso tumbaba TODA la instalación por un ajuste cosmético (buscador
de Edge). Arreglo: filtra raíces nulas antes de `Join-Path` y va en `try/catch`. Además se añadió
una red de seguridad `trap` a `aether-red.ps1`, `aether-centro.ps1` y `aether-apps.ps1` para que
ningún companion de Aether pueda abortar la instalación (regla: nada bloquea, solo avisa). También
se endureció `Install-AetherApps.ps1` (Windhawk cli) contra rutas nulas.

### Hecho en esta sesión
- [x] Aether SIEMPRE como administrador: manifiesto `requireAdministrator` (app/vendor gpui).
- [x] Cancelación más rápida y sólida: la marca `C:\ProgramData\Aether\cancelar-instalacion.flag`
      se revisa también entre cada tweak de una categoría (no solo entre pasos grandes).
- [x] Energía: `Initialize-AetherPlanEnergia` crea un paquete de energía propio de Aether
      (duplica "Rendimiento máximo"/Ultimate, sin tocar los planes del usuario) y lo deja activo.
      Gaming = máximo rendimiento (CPU 100% min/max, EPP 0, enfriamiento activo); Trabajo =
      optimizado pero fluido (CPU hasta 100% bajo demanda, EPP 50, turbo disponible); Batería =
      eficiente (con DC baja el techo de CPU y apaga turbo).
- [x] Prioridad alta a Office (tweak `misc/aether-office`, IFEO PerfOptions, CpuPriorityClass=3).
- [x] Prioridad total a la red + Enlace móvil (tweak `networking/aether-prioridad-red`:
      NetworkThrottlingIndex off, SystemResponsiveness=10, PhoneExperienceHost prioridad alta,
      EnableActiveProbing=1 para portales cautivos).
- [x] Office sin bloqueo de macros ni avisos de otra PC (tweak `misc/aether-office-macros`,
      HKCU Policies Office 16.0: VBAWarnings=1, blockcontentexecutionfrominternet=0, Vista protegida off).
- [x] Explorador más completo (tweak `qol/aether-explorer`: menú clásico completo, ruta en el
      título, panel expandido, procesos separados).
- [x] Wi-Fi a máximo rendimiento y portal cautivo estable: en `Reparar-Red-Aether.ps1` (powercfg
      SUB_WIFI=0 + PnPCapabilities=24 en los adaptadores inalámbricos).
- [x] Wind-Aether: el mod `local@translucent-windows-fork-fork` ahora excluye Word/Excel/PowerPoint/
      Outlook/OneNote/Access (el texto de Word ya no se vuelve invisible). Los 37 mods ya estaban
      todos desactivados por defecto; se pueden activar y editar desde la app y desde Windhawk.
- [x] Claude y ChatGPT con mejor prioridad (tweak `misc/aether-apps-ia`, CpuPriorityClass=6 AboveNormal).

### Pendiente / parcial (para la próxima)
- Botón en el Centro "Desbloquear archivo de otra PC" (Unblock-File) para complementar el tweak de
  macros en archivos ya descargados. El tweak ya evita el bloqueo de ahí en adelante.
- Subir volumen sin distorsión: no hay un ajuste de registro fiable (la "Ecualización de volumen" es
  por dispositivo y depende del driver). La vía sólida es instalar Equalizer APO; no se añadió para
  no meter software que el usuario no pidió. Pendiente de confirmar con él.
- Mejorar la cámara "notablemente": es sobre todo hardware/driver; los ajustes de software (p. ej.
  desactivar Frame Server) rompen apps que usan la cámara a la vez. No se tocó por seguridad.
- "Montar actualizaciones encima" a futuro: la instalación ya es idempotente (se puede reinstalar
  encima); documentar un flujo de actualización formal cuando se agreguen más cosas.

### NO se hará (y por qué)
- Saltarse/burlar firewall, DPI, filtrado DNS/contenido, proxy o puntos de acceso empresariales
  (Fortinet FortiGate, Cisco Meraki, Ubiquiti UniFi, SonicWall) de redes de colegio/empresa: eso es
  evadir los controles de seguridad de redes que no son del usuario. No se construye. Sí se mejoró lo
  legítimo y adyacente: estabilidad del Wi-Fi, detección fiable de portales cautivos, y Wi-Fi a
  máximo rendimiento. Para su propia privacidad puede usar un DNS propio (Cloudflare/Google) o una VPN.

## 7. Centro nativo en WinUI 3 (HISTÓRICO: ya migrado a WPF, ver sección 9) — sesión 2026-11-20 (tarde)

> **Actualización 2026-11-21 (noche): el Centro YA ESTÁ EN WPF** (sección 9). Lo de abajo queda
> como historia; las notas de build de WinUI ya no aplican.

> ### ⚠️ AVISO CRÍTICO (2026-11-21): WinUI 3 no arranca en la laptop de Daniel
> En la sesión del 2026-11-21 se descubrió que una app **WinUI 3 unpackaged + self-contained**
> (la misma arquitectura de este Centro) **crashea al arrancar** en la laptop de Daniel
> (`combase.dll` 0x802B000A, `XamlParseException` irreproducible, compila pero no abre). Se
> intentó de todo (framework-dependent, self-contained, reescribir el XAML) y **no se pudo
> arreglar**; se migró esa app (Aether Transfer) a **WPF** y abrió a la primera.
> **Es casi seguro que este Centro WinUI 3 tampoco abra en su laptop.**
> **Acción recomendada: migrar el Centro a WPF** (como se hizo Aether Transfer: conservar la
> lógica/servicios, reescribir solo la UI). Ver **TRASPASO-AETHER.md, Parte C** (todo el
> diagnóstico y el método de migración). El alcance de los mandos sigue siendo solo
> **PS4 (DualShock 4) → mando Xbox** + que funcionen los mandos Xbox.

El usuario pidió rehacer el Centro en **WinUI 3 / Windows App SDK** (los mockups) porque la
versión en PowerShell se ve mal, abre lento, deja una ventana de PowerShell, pide permisos y
cambia de modo lento. Arquitectura elegida: app WinUI 3 **unpackaged + self-contained** (.exe
directo, sin instalar runtime aparte), **requireAdministrator** en el manifiesto (siempre admin,
sin UAC por acción), y la interfaz nativa **llama a la lógica PowerShell ya probada**
(Aether.Centro.ps1) por debajo — así no se reescribe toda la lógica y se reutiliza lo que funciona.

- Carpeta: `centro/` (CentroAether.csproj, app.manifest, App, MainWindow con NavigationView +
  Mica, Pages/ModosPage, Pages/PlaceholderPage, Services/Aether.cs = puente a PowerShell).
- Workflow `centro.yml`: compila con **MSBuild de Visual Studio** (el `dotnet` CLI no trae las
  tareas de PRI de WinUI 3) y deja `CentroAether.exe` como artefacto. **Compila en verde.**
- Hecho: armazón con look Windows 11, página **Modos** funcional (llama a Set-AetherModo con
  animación de "cargando modo"). Resto de secciones como marcador.

### Pendiente del Centro nativo (tareas #13 y #14)
- Páginas: Red, Actualizar (un solo interruptor de Windows Update, no 2 botones), Limpieza,
  Batería, Respaldo, Wind-Aether (**casillas por mod** + botón agregar), Salud, inicio,
  asistente de bienvenida, tema claro/oscuro.
- Integrar `CentroAether.exe` en el payload del instalador y lanzarlo al iniciar sesión con una
  tarea programada (privilegios altos, sin UAC); reemplazar el lanzador del Centro WPF.

### Notas de build WinUI 3 (para no repetir errores)
- Usar **MSBuild** (`microsoft/setup-msbuild`), no `dotnet publish`: el SDK .NET por sí solo no
  tiene `Microsoft.Build.Packaging.Pri.Tasks.dll` (MrtCore.PriGen) → error MSB4062.
- `centro/global.json` fija .NET 8 (el runner trae .NET 11 preview, que falla distinto).
- Publicar: `msbuild /t:Publish /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:SelfContained=true
  /p:WindowsPackageType=None /p:WindowsAppSDKSelfContained=true /p:PublishDir=publish\`.

### Avance Centro nativo (continuación) — integrado al instalador
- Páginas hechas: Modos, Red, Actualizar (un interruptor), Wind-Aether (casillas por mod).
  Faltan: Limpieza, Batería, Respaldo, Salud, inicio, asistente de bienvenida, tema.
- **Integración lista**: `aether-app.yml` compila el Centro (MSBuild) y lo copia al payload
  (`AtlasModules\Aether\Centro\app\CentroAether.exe`) antes de armar el apbx. El instalador
  completo compila en verde con el Centro adentro (artefacto ~150 MB).
- Lanzado sin UAC: `aether-centro.ps1` registra la tarea `\Aether\Centro` (privilegios altos)
  y los accesos directos la disparan con un `.vbs` (sin consola). La bandeja (automatización en
  segundo plano) sigue oculta; su "Abrir" usa el mismo lanzador (Show-AetherCentro). Respaldo a
  la ventana WPF si el exe nativo no está.

## 8. Aether Transfer — migrado a WPF y funcional (sesión 2026-11-21, segunda cuenta)

**Hecho:** El cliente FTP **Aether Transfer** (`aether-transfer/`) se **migró de WinUI 3 a WPF
.NET 8** y **ya funciona probado con la PS4 real de Daniel** (conectar, listar, subir, borrar,
navegar). Compila en verde: workflow **"Aether Transfer (WPF)"** → un solo `.exe` self-contained.

- [x] Migración WinUI 3 → WPF (la app abre; WinUI no abría).
- [x] Un solo panel FTP, arrastrar/soltar Explorador↔FTP, conexiones guardadas (1 clic),
      navegación `‹ ›`, tema oscuro total (incl. barra de título y tooltips), motor FTP
      más sólido/rápido, conexión anónima para PS4.
- [ ] Pendiente: probar SFTP real + archivos grandes; subir carpetas por arrastre; (opcional)
      reintroducir Sincronizar; **compilar el .exe final (Regla 38, requiere autorización).**

Detalle completo, decisiones y lecciones: **AETHER-TRANSFER.md** y **TRASPASO-AETHER.md Parte C**.

**Estado 2026-11-21 (noche): CONGELADO** por decisión de Daniel. Pendientes anotados arriba para
más adelante. El siguiente trabajo pasó al Centro (sección 9).

## 9. Centro Aether migrado a WPF (sesión 2026-11-21 noche, vuelta a la primera cuenta)

Decisiones de Daniel (21-nov, 21:40): congelar Aether Transfer; migrar Centro; mandos solo
**DualShock 4 → Xbox** + mandos Xbox; probar el Centro en su **laptop de pruebas** bajando el
artefacto de Actions (no es el .exe final; Regla 38 vigente); borrar las ❌ ya corregidas (hecho).

- [x] Migración WinUI 3 → WPF (.NET 8), commit `c06f888`. **Compila en verde a la primera, sin
      advertencias** (workflow "Centro Aether (WPF)", artefacto `Centro-Aether-<sha>`, un solo .exe).
      * Se conservó intacta la lógica: `Services/` (puente a PowerShell, motor ViGEm, DS4 por HID,
        perfiles, Background Intelligence, etc.) y `Models/`. Solo cambió `InteligenciaFondo.cs`
        (DispatcherTimer de WPF).
      * `centro/Controles/`: sustitutos propios de WinUI con el mismo nombre/uso para no reescribir
        la lógica de las páginas: `FontIcon` (Segoe Fluent Icons/MDL2), `Pila` (StackPanel con
        Spacing y Padding), `InfoBar`, `ToggleSwitch` (IsOn, Toggled **solo al cambiarlo el usuario**:
        cargar el estado inicial nunca ejecuta acciones), `PaginaBase` (OnNavigatedTo/From) y
        `Sistema.Abrir` (en vez de Windows.System.Launcher).
      * `App.xaml`: tema oscuro Aether global (colores de Aether Transfer) con los **mismos nombres de
        recursos de WinUI** (`TextFillColorSecondaryBrush`, `AccentButtonStyle`, `TitleTextBlockStyle`…).
        Barra de título oscura (`Tema.cs`). Ventana 1160×780, menú lateral por categorías.
      * Overlay de modos en WPF: sin bordes, siempre al frente, fuera de la barra de tareas, sin robar
        el foco, fundido y auto-cierre a 3 s; barra "cargando" animada propia.
      * Respaldo: selector de carpeta `OpenFolderDialog` de .NET 8 (antes FolderPicker de WinRT).
      * Errores: se escriben en `CentroAether-aether-crash.log` (Escritorio, %TEMP% y junto al .exe) y
        se muestra un aviso **sin cerrar el Centro**.
      * `centro.yml` y `aether-app.yml`: `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true`
        (ya no MSBuild ni Windows App SDK). El instalador sigue copiando a
        `AtlasModules\Aether\Centro\app\CentroAether.exe` (misma ruta, el tweak `aether-centro` no cambia).
- [ ] **Prueba de Daniel en la laptop de pruebas**: bajar `Centro-Aether-<sha>` de Actions, abrir
      `CentroAether.exe` (pide administrador), recorrer las secciones. Si algo falla, mandar el
      `CentroAether-aether-crash.log` del Escritorio.
- [ ] Pendiente de decidir con Daniel: la sección **Oficina** tiene un botón "Proteger macros"
      (VBAWarnings=2, macros bloqueadas con aviso) que **contradice** el tweak del instalador
      `misc/aether-office-macros` (macros sin bloqueo, VBAWarnings=1 por directiva). Ver TRASPASO D.3.

### 👉 SIGUIENTE PASO EXACTO (al retomar)
1. Esperar la prueba de Daniel del Centro WPF en su laptop de pruebas y corregir lo que reporte.
2. Resolver la contradicción de macros de la sección Oficina (pregunta en TRASPASO D.3).
3. Después: pendientes del Centro (sección 7: inicio/bienvenida/tema) y la pasada de limpieza
   (Borrado parte 2) del plan.
