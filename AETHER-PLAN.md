# Aether OS — Plan maestro y progreso

> Si la sesión se corta: este archivo es la fuente de verdad. Leer "Progreso" y seguir
> desde la primera casilla sin marcar. Proyecto en `/home/claude/AetherOS` (git).
> Recursos de Wind-Aether en `/home/claude/aether-assets`. Script de red probado en
> `/home/claude/aether-red`.

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
- Script de reparación de red (`/home/claude/aether-red/Reparar-Red-Aether.ps1`, PROBADO y
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
- Paquete: `/home/claude/aether-assets/Wind-Aether-mods.json` (37 mods, todos desactivados,
  incluye `local@aether-margen-escritorio` y Ventanas translúcidas 1.8.2-es-aether).
- Perfiles: `/home/claude/aether-assets/Wind-Aether-perfiles.json` (Aether Glass / Aether
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
- [ ] Traducir al español la carpeta de accesos directos (AtlasDesktop: 93 carpetas, 209
      launchers), sus rutas en las definiciones de toggles (`Launcher`), `catalog.json`,
      `Initialize-NewUser.ps1` (rutas fijas) y los textos de consola. Hacerlo con un mapa
      inglés→español aplicado de forma consistente.
- [x] Revisión (parte 1): sin referencias a archivos borrados; `ToolboxLauncher` quitado de 11
      toggles, del generador `tools/dev/New-ToggleLaunchers.ps1` y de `catalog.json`;
      `atlas-theme-upgrade` usa el tema Aether.
- [ ] NOTA paso 2 (red): `Initialize-NewUser.ps1` ejecuta "Disable Network Navigation Pane
      (default).cmd" por usuario → cambiar a Enable. FileSharing por defecto → Enable.

## 4. Limitación del entorno
Aquí no hay PowerShell ni Windows: no se puede compilar el .apbx, la app ni correr las pruebas
Pester. Para probar: GitHub Actions (los workflows de Atlas ya existen) o la VM del usuario.
