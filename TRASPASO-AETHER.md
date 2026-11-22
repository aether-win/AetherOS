# Traspaso de Aether OS — para seguir en otra cuenta de Claude

Este documento tiene dos partes:
- **Parte A — para ti (Daniel):** cómo conectar la otra cuenta de Claude a GitHub y qué escribirle.
- **Parte B — para Claude:** todo lo necesario para seguir exactamente donde quedó el trabajo.

---

## Parte A — Pasos para el usuario

### 1. Conecta GitHub en la otra cuenta de Claude
1. Inicia sesión en la **otra cuenta de Claude** (en la app de escritorio o en claude.ai).
2. Ve a **Configuración → Conectores → GitHub → Conectar**.
3. Se abre GitHub en tu navegador. Inicia sesión con la cuenta **aether-win** (la del repositorio).
4. Acepta el acceso. Si te pregunta a qué repositorios, elige **"Only select repositories"** y marca **AetherOS**.
5. Si GitHub dice que la app de Claude ya está instalada en aether-win, entra a
   **github.com → Settings → Applications → Installed GitHub Apps → Claude → Configure** y
   verifica que **AetherOS** esté marcado.

### 2. Abre un chat nuevo en esa cuenta y pega este mensaje

> Vamos a continuar un proyecto llamado **Aether OS**. Conecta el repositorio de GitHub
> **aether-win/AetherOS** con permiso para subir cambios (push), clónalo y lee primero
> **TRASPASO-AETHER.md** y después **AETHER-PLAN.md**. Sigue exactamente desde donde quedó,
> respeta todas las decisiones y reglas que están ahí, y háblame siempre en español.

### 3. Decisiones ya respondidas
- Apps de Windows: conserva Paint, Cámara, Notas rápidas, Alarmas y reloj, Grabadora de sonido,
  To Do y Outlook nuevo. Solo se borra OneDrive.

---

## Parte B — Para Claude

### B.1 Quién es el usuario y cómo trabajar con él
- **Daniel** ("Dharius"), Honduras (America/Tegucigalpa). Habla **español**; todo lo que se le
  muestre y todo Aether (instalador, app, accesos directos) va **en español**.
- Laptop **HP 15-fc0**, **Ryzen 7 7730U** (Zen 3, Vega 8 integrada, 15 W, cTDP 10–25 W),
  **16 GB DDR4-3200 en doble canal** (2×8), SSD 477 GB (≈319 usados), **Windows 11 Pro 26H2
  (build 26300)**, pantalla 1920×1080. Usa un ventilador grande debajo de la laptop.
- Usos: **trabajo** (Excel con macros VBA, Office 2024, Python, programación, carpetas
  compartidas en red como "SISTEMA EF RED") y **gaming** (Steam: GTA V Legacy/Enhanced y GTA Online
  de vez en cuando, BloodStrike, Left 4 Dead 1/2, Resident Evil 5/6, Bully, etc.).
- Usa **Edge** como navegador principal. Tiene cuenta Microsoft en Windows.
- Reglas de trabajo que pidió:
  - **Paso a paso.** Avisarle al terminar cada paso.
  - **No darle ningún .exe hasta que Aether esté completo.**
  - Los modos (Trabajo/Gaming) se cambian **siempre a mano**; única excepción: el Modo
    Batería se activa solo al 30%.
  - **No incluir software crackeado.** Mandó portables crackeados (IDM, WinRAR con clave del
    autor, IObit Uninstaller/Unlocker con parche, FileZilla Pro). Se rechazó integrarlos y él
    decidió: **de sus portables solo va KeyVault** (casilla). No agregar alternativas.
  - **KeyVault es su propio programa** (lanzador en Go firmado "Daniel Zelaya, 2026" que abre
    `keyvault-web.netlify.app` en Edge/Chrome/Brave; guarda contraseñas en Google Drive).
    **No preguntarle más sobre KeyVault.**
  - Pruebas de instalación **solo en máquina virtual (Hyper-V)**, nunca en su laptop real.

### B.2 Qué es el proyecto
Aether OS es una versión propia de **AtlasOS 0.6.0** (GPLv3): un conjunto de scripts
PowerShell (`playbook/`) que modifica Windows 11, y una app en Rust/GPUI (`app/`, ex Atlas
Manager) que hace de instalador. Aether sigue siendo GPLv3 y debe mencionar que deriva de Atlas.
**El diseño completo, las decisiones y el progreso están en `AETHER-PLAN.md`.** Ese archivo es
la fuente de verdad: léelo entero antes de cambiar nada y actualiza su sección de progreso
después de cada avance.

### B.3 Estado actual (19-nov-2026)
- **Paso 1 (base) casi completo:** marca Aether (logos, ícono, fondos, temas Aether
  Oscuro/Claro, OEM, acceso directo), `playbook.conf` en español con las opciones nuevas,
  limpieza de Atlas (reportes, Toolbox, eclean, navegadores, NanaZip, quitar Edge/Recortes),
  contrato de opciones igualado.
- **CI todo en verde:** Build Playbook ✅, Lint PowerShell ✅, Lint YAML ✅, Test (Pester PS7 +
  Windows PowerShell 5.1, más de 2500 pruebas) ✅.
- Las opciones nuevas `app-*`, `wind-aether`, `experimental-cpu-power` **existen en el
  asistente pero todavía no hacen nada** (se implementan en el paso 2).
- Hay código heredado de Atlas que no se puede elegir y no rompe nada (transición de Windows,
  quitar Edge usado por Widgets, LibreWolf, AME `custom.yml`); está listado en el plan.

### B.4 Qué sigue, en orden de prioridad
El usuario tiene poco límite de uso esta semana. Prioridad: **una primera versión que funcione**.
1. **Paso 2 — HECHO (20-nov, ver Progreso en AETHER-PLAN.md).** Lo que se pidió era:
   - **Red automática** (obligatorio): integrar `aether/red/Reparar-Red-Aether.ps1` (PROBADO en su
     laptop, resolvió el error 0x80070035) como tarea del instalador; FileSharing en Enable;
     quitar los tweaks `restrict-anonymous-access`, `restrict-anonymous-enumeration`,
     `disable-network-navigation-pane`; en `Entry/Initialize-NewUser.ps1` cambiar la llamada a
     "Disable Network Navigation Pane (default).cmd" por Enable; impresión y LLDP activos.
   - **Actualizaciones pausadas hasta el año 3000** cuando se elige `auto-updates-disable`
     (activar el toggle `General/PauseUpdates` en vez de apagar servicios, para que Store, Xbox
     y winget sigan funcionando).
   - **Defender + SmartScreen:** con `defender-disable` quitar Defender (ya lo hace Atlas) y
     **todo SmartScreen** (Windows y Edge); con `defender-enable` dejar ambos.
   - **Edge con Google** como buscador: archivo de preferencias iniciales + tarea que repone
     Google si vuelve Bing (las políticas de buscador se ignoran en PCs no administradas).
   - **Apps de las casillas** (instalar como el usuario, nunca como SYSTEM): Claude
     (`winget Anthropic.Claude`), ChatGPT (Store `9NT1R1C2HH7J`, si falla el instalador
     oficial), Python 3.13.16 (última con instalador de Windows; PATH, py, pip, rutas largas,
     sin alias de Store), KeyVault (exe del usuario: pedirle el archivo, acceso directo en
     Escritorio e Inicio), PC Manager (Store `9PM860492SZD`, si falla instalador oficial), Steam
     (`Valve.Steam`), drivers AMD.
   - **Wind-Aether:** instalar Windhawk 2.0 e importar `aether/wind-aether/Wind-Aether-mods.json`
     (37 mods, todos desactivados) con la CLI de Windhawk (`windhawk data import`).
   - **Valores de Atlas que cambian** (tabla en el plan): hibernación activa, actualizaciones
     de la Store activas, apps en segundo plano permitidas, corrector ortográfico activo, aviso de
     poco espacio activo, "Versiones anteriores" se queda, no desactivar dispositivos en laptops,
     indexado normal, Vincular teléfono activo, "Imprimir" en el menú, quitar Xbox/Game Bar
     pero **no** los drivers de mandos, quitar OneDrive/LinkedIn/Solitario.
2. ~~Traducir al español la carpeta de accesos directos (AtlasDesktop).~~ HECHO (sin acentos: ver plan).
3. Paso 3: asistente de la app (Rust) con las casillas nuevas.
4. Paso 4: app de control (modos, respaldo, red, limpieza, batería, Wind-Aether).

### B.5 Cómo se trabaja (lecciones aprendidas — importante)
- **No hay Windows ni PowerShell en el entorno de Claude.** Todo se compila y prueba en
  **GitHub Actions** del repo. Ver resultados con
  `gh api "repos/aether-win/AetherOS/actions/runs?branch=main&per_page=8"` y fallos con
  `gh run view <id> --repo aether-win/AetherOS --log-failed`.
- Los **artefactos de Actions no se pueden descargar** desde el entorno de Claude (el proxy lo
  bloquea). Para traer archivos generados en CI se usa una rama:
  `.github/workflows/regenerate.yml` (manual: `gh workflow run regenerate.yml --repo
  aether-win/AetherOS --ref main`) regenera el catálogo de toggles y los lanzadores y los deja en
  la rama `regenerado`; traerlos con
  `git fetch --depth=2 origin regenerado && git cherry-pick -n FETCH_HEAD`.
- Al cambiar definiciones de toggles o tweaks, **regenerar el catálogo** (prueba "catalog drift").
- **BOM obligatorio:** todo `.ps1/.psm1/.psd1` con acentos o ñ debe guardarse en UTF-8 **con
  BOM** (si no, falla Lint y Windows PowerShell 5.1 lee mal los acentos).
- `playbook.conf` `<Title>` debe contener `v<Version>`.
- La app (`app.yml`) solo se compila **a mano** para ahorrar minutos; Dependabot está apagado.
  El repo es **privado**: los minutos de Actions son limitados, no lanzar compilaciones de más.
- Al empujar desde un clon superficial usar `git -c push.negotiate=true push origin main`.
- La conexión con la PC del usuario (app de escritorio de Claude) solo permite **leer y escribir
  archivos** en carpetas conectadas; sus comandos corren en una VM Linux, **no** en Windows.
- Al editar un archivo, conservar su codificación y saltos de línea (muchos son CRLF).
- **Al usuario no le gustan las ❌ ni los ❗ en GitHub Actions**: cuando una ejecución falle y ya esté
  corregida, borrarla (`gh api -X DELETE repos/aether-win/AetherOS/actions/runs/<id>`).
- El instalador se compila con `.github/workflows/aether-app.yml` (manual) → artefacto `Aether-OS-<sha>`.
- Pruebas reales: el usuario usa una **laptop aparte** recién instalada (no su laptop de trabajo).
- No cambiar nombres internos "Atlas" que comparten la app Rust y los scripts (p. ej. la carpeta
  `Atlas Setup Recovery`): romperlo causó el error "recovery staging".

### B.6 Mapa de archivos propios de Aether
| Ruta | Qué es |
|---|---|
| `AETHER-PLAN.md` | Diseño completo, decisiones, progreso, pendientes |
| `TRASPASO-AETHER.md` | Este documento |
| `branding/` | Logos, ícono `aether.ico`, fondos, foto de perfil (ver su README) |
| `aether/red/Reparar-Red-Aether.ps1` + `.cmd` | Script de reparación de red **probado** |
| `aether/wind-aether/Wind-Aether-mods.json` | Respaldo de Windhawk del usuario: 37 mods desactivados (incluye sus 11 mods propios) |
| `aether/wind-aether/Wind-Aether-perfiles.json` | Perfiles Aether Glass / Aether Limpio, pausa en Gaming, colores claro/oscuro, choques |
| `aether/wind-aether/aether-margen-escritorio.wh.cpp` | Mod nuevo: la barra de tareas auto-oculta ya no tapa la última fila de íconos |
| `aether/wind-aether/ventanas-translucidas-aether.wh.cpp` | Mod "Ventanas translúcidas" 1.8.2-es-aether: excluye Office, AMD, Steam, juegos, etc. |
| `playbook/playbook.conf` | Opciones del asistente (en español) |
| `playbook/Executables/Themes/aether-*.theme` | Temas Aether Oscuro / Claro |
| `.github/workflows/regenerate.yml` | Regenerar catálogo/lanzadores en CI |

Nota: los dos mods `.wh.cpp` no se pudieron compilar en Windows todavía; el usuario debe
probarlos en Windhawk.

---

# Parte C — Lo que se hizo en la segunda cuenta (sesión 2026-11-21)

> Esta sesión trabajó **solo en Aether Transfer** (el cliente FTP). No se tocó el playbook
> del sistema operativo. El gran resultado: **Aether Transfer se migró de WinUI 3 a WPF**
> porque WinUI 3 no arrancaba en la laptop de Daniel. Léelo entero antes de tocar Centro,
> porque Centro tiene el mismo problema.

## C.1 La saga del crash de WinUI 3 (y por qué se abandonó WinUI)
Al probar el Aether Transfer compilado (WinUI 3 unpackaged + self-contained) en la laptop real
de Daniel (HP-Dharius, Windows 11 build 26100), la app **crasheaba al arrancar** con
`combase.dll`, excepción `0x80004005` (E_FAIL), **hash de volcado idéntico** en cada intento.

Diagnóstico paso a paso (todos estos commits son de ESTA sesión):
- `f7d6fdf` — probar framework-dependent (depender del Windows App Runtime). **No lo arregló.**
- `5249b31` / `edf3da4` — self-contained completo del runtime WinUI con MSBuild `/t:Publish`.
  Lección: **`dotnet publish` NO puede generar los recursos PRI de WinUI** (falla en la tarea
  `ExpandPriContent`); por eso WinUI usa MSBuild. **No lo arregló** (mismo crash).
- `0248107`, `85a2f08`, `07747cc` — se le puso a la app un **`Main` propio** (constante
  `DISABLE_XAML_GENERATED_MAIN`) que registra la excepción real a un `aether-crash.log` (en el
  escritorio, junto al exe y en `%TEMP%`), con migas de pan por paso del arranque.
- `cd84c4c`, `9ef9d9c` — build en Debug + activar `DebugSettings` (tracing de recursos/bindings)
  para sacar el HRESULT. Resultado: **`XamlParseException` en `MainWindow.InitializeComponent()`**,
  HRESULT **`0x802B000A`** (facilidad XAML/DirectUI). WinUI 3 **no da número de línea ni en Debug**.
- `6b98cc5` (quitar MenuBar), `b6e2783` (quitar `x:Bind` sobre `x:String`), `8126d14` (convertir
  TODOS los `x:Bind` de plantillas a `Binding`), `0085099` (reescribir `MainWindow.xaml` con
  patrones "a prueba de balas"): **ninguno** arregló el crash. El XAML compila pero falla al
  cargarse en runtime de forma irreproducible y sin pista de qué elemento.

**Conclusión:** WinUI 3 unpackaged es inviable en la máquina de Daniel. Daniel se cansó y
preguntó si se podía usar otro framework. Se le ofreció **WPF** (vía pregunta de opción
múltiple) y lo aprobó.

## C.2 Migración a WPF (lo que funcionó)
- `3783e2b` — **migrar la capa visual de WinUI 3 a WPF (.NET 8).** La app **abrió perfecto** a la
  primera. Confirmado: el problema era 100% WinUI, no la máquina ni el código.
  - Se **conservó intacto todo el backend** (es agnóstico de UI): `Protocols/` (FluentFTP +
    SSH.NET), `TransferEngine/`, `Security/` (DPAPI), `Settings/`, `Connections/`, `Models/`.
  - Solo se reescribió: `.csproj` (UseWPF, net8.0-windows, self-contained single-file),
    `App.xaml/.cs` (tema oscuro global), `MainWindow.xaml/.cs`, y los diálogos (`Views/`:
    `DialogoSitios`, `DialogoConfig`, `DialogoSync`, `InputBox`).
  - CI nuevo: `.github/workflows/aether-transfer.yml` ahora usa
    `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`
    → **un solo `.exe`** como artefacto `Aether-Transfer-<sha>`. (WPF **sí** publica con
    `dotnet publish`, a diferencia de WinUI.)

## C.3 Mejoras pedidas por Daniel y aplicadas (en orden)
- `f24935e` — **Conexión anónima**: si el usuario va vacío, conecta como `anonymous` (el FTP de
  la PS4, puerto 2121, no pide usuario/contraseña). Antes daba "UserName can't be empty".
  + tema oscuro inicial de scrollbars/menús/combos.
- `c7e6dd5` — **Arrastrar y soltar**, **barra de título oscura** (API `DwmSetWindowAttribute`,
  archivo `Tema.cs`), **registro de operaciones** (borrar/renombrar/crear), y rediseño.
- `4ef4e92` — **rediseño grande**: un solo panel (ver C.4), casilla de conexiones guardadas,
  navegación con historial, un solo botón Conectar, sin datos predeterminados, Subir con
  selector, Descargar a la carpeta Descargas, motor FTP más sólido y rápido.
- `ac85a21` — fix de compilación (faltaba `using AetherTransfer.TransferEngine`).
- `d629dcd` — **tooltips oscuros** (eran lo blanco que seguía saliendo) y quitar el botón de
  subir-nivel (redundante con atrás/adelante y la fila `..`).

## C.4 TODAS las decisiones que dio Daniel en esta sesión (con el motivo)
- **Solo le importa que el FTP de la PS4 funcione** (IP, puerto 2121, sin usuario) → se hizo la
  conexión anónima el default cuando el usuario va vacío.
- **Cambiar de WinUI a WPF** (aprobado explícitamente) → porque WinUI no abría y se cansó de los
  parches. WPF es el framework de escritorio más estable de .NET.
- **Un solo panel, solo carpetas FTP** → quitó el panel local ("almacenamiento interno") porque
  *"todo se puede arrastrar directamente desde el explorador nativo y viceversa"*. Las dos
  flechas del medio (subir/bajar entre paneles) se eliminaron porque "no hacen nada".
- **Nada blanco**: todo lo que abra (confirmar eliminar, combos, menús, barra de título,
  tooltips) debe ir con el tema oscuro de la app. (El **selector de archivos de "Subir" es el
  cuadro nativo de Windows** y NO se puede tematizar; se le explicó.)
- **El registro debe bajar solo** (auto-scroll) cuando pasa algo, para no arrastrar el mouse.
- **Casilla de conexiones guardadas** para reconectar con un clic → se añadió el combo
  "Guardadas"; además **cada conexión exitosa se guarda sola** (dedup por host+puerto+usuario).
- **Botones atrás/adelante tipo `‹ ›`** con historial; **quitar** el de subir-nivel.
- **Sin datos predeterminados** al abrir (antes había `192.168.1.10` y puerto `21` de ejemplo).
- **Un solo botón de conexión** (había "Conectar" en la barra y "Conexión rápida" duplicados).
- **Buena velocidad** de transferencia y **conexión más sólida**; y que **no diga "Error de
  conexión" si ya está conectado**.
- Arrastrar **desde** el FTP **hacia afuera**: se acepta que primero descarga a temporal (en
  archivos grandes se pausa un momento); se le dijo y lo aceptó, con la cola/Descargar como
  alternativa para archivos grandes.

Decisiones de sesiones anteriores que siguen vigentes (recordatorio):
- **Centro**: a Daniel solo le importa que el **mando de PS4 (DualShock 4) funcione como mando de
  Xbox** y que los mandos de Xbox funcionen. Nintendo, giroscopio, touchpad, luces y vibración
  están **fuera**.
- **FortiGate / evasión de firewalls: NO** (se acordó no agregarlo).
- **Regla 38**: **no compilar el .exe final** hasta autorización expresa de Daniel. Los builds de
  CI (validación) sí están permitidos. Aún **no** se ha compilado ningún ejecutable final.

## C.5 Problemas encontrados y cómo se resolvieron
- *WinUI combase 0x802B000A* → migrar a WPF (único arreglo real).
- *`dotnet publish` no genera PRI de WinUI* → en WinUI había que usar MSBuild; en WPF ya no
  importa (WPF publica bien con `dotnet publish`).
- *WPF no tiene `Spacing` en `StackPanel` ni `Padding` en `Grid`* → se usan `Margin`.
- *Controles WPF salen claros por defecto* → **tema oscuro completo en `App.xaml`** con
  `ControlTemplate` propios: TextBox, PasswordBox, Button (+ `BtnPlano`), ComboBox (plantilla
  completa con Popup), ComboBoxItem, ListView/ListViewItem, ListBox, **ScrollBar**,
  **ContextMenu/MenuItem/Separator**, **ToolTip**. Barra de título por `Tema.cs` (DWM).
- *Crash de build por `using` faltante* → agregado.

## C.6 Lecciones técnicas nuevas (que la próxima cuenta DEBE saber)
1. **Aether Transfer ahora es WPF (.NET 8), NO WinUI 3.** No volver a WinUI: crashea unpackaged
   en la máquina de Daniel (`combase` 0x802B000A, irreproducible, sin línea).
2. **⚠️ Centro sigue en WinUI 3** (`centro/`, ver sección 7 del plan). **Es casi seguro que
   crashea igual** en la laptop de Daniel. **Siguiente trabajo recomendado: migrar Centro a WPF**
   exactamente como se hizo Aether Transfer (conservar la lógica, reescribir solo la UI).
3. CI de Aether Transfer: `dotnet publish ... --self-contained true -p:PublishSingleFile=true`
   → un `.exe` que **abre sin instalar nada** (ni Windows App Runtime). Artefacto
   `Aether-Transfer-<sha>`, ~68 MB comprimido.
4. **Los artefactos de Actions no se descargan desde el entorno de Claude** (el proxy bloquea el
   host de blobs). Daniel los baja desde la página de Actions → "Artifacts".
5. Para **ver el error real de un XAML de WinUI** en Release (si algún día hace falta): WinUI no
   da línea; hay que poner `Main` propio con try/catch que escriba la excepción a archivo, y aun
   así el HRESULT `0x802B000A` no dice el elemento. (En WPF esto ya no es problema: da excepciones
   claras.)
6. Tema oscuro WPF: definir estilos **globales en `App.xaml`** (aplican a toda la app y a los
   diálogos). La barra de título se oscurece con `DwmSetWindowAttribute` (ver `Tema.cs`,
   `Tema.BarraOscura(window)` enganchado en `SourceInitialized`).
7. El **selector "Abrir archivo" de Windows (OpenFileDialog) es nativo y claro**; no se puede
   tematizar. No es un bug de la app.

## C.7 Qué quedó a medias / sin terminar
- **Centro NO migrado** a WPF todavía (sigue en WinUI 3 → crasheará). Es el siguiente paso.
- Aether Transfer: **subir carpetas por arrastre** no está (solo archivos sueltos).
- Arrastrar-hacia-afuera de archivos grandes **descarga a temporal primero** (pausa la UI un
  momento); mejorable con streaming de archivo virtual (COM `IDataObject`/`CFSTR_FILECONTENTS`),
  pendiente y de riesgo medio.
- **Pruebas**: probado con la PS4 de Daniel (conectar, listar, subir, borrar, navegar). Falta
  probar **SFTP** real y archivos grandes/velocidad sostenida.
- El **.exe final** sigue bloqueado por la Regla 38 (no compilado).

---

## Estado de entrega (al cierre de esta sesión, 2026-11-21)

**Último commit:** `d629dcd` — *fix(aether-transfer): tooltips oscuros y quitar boton subir-nivel*
(rama `main`, repo `aether-win/AetherOS`, subido y confirmado).

**GitHub Actions:**
- **Aether Transfer (WPF):** ✅ verde en `d629dcd` (artefacto `Aether-Transfer-d629dcd…`, un `.exe`).
- **Test** (Pester): ✅ verde (último en `3783e2b`).
- **Lint YAML:** ✅ verde (último en `3783e2b`).
- **Build Playbook** y **Lint PowerShell:** no se re-ejecutaron esta sesión (solo se tocó
  `aether-transfer/`, que tiene su propio workflow con filtro de rutas). **Último estado conocido:
  TODO EN VERDE** (ver Parte B.3 / plan sección 4). No se tocó ningún `.ps1` ni el playbook.
- El workflow viejo **"Aether Transfer (WinUI 3)"** quedó obsoleto; ahora el activo es
  **"Aether Transfer (WPF)"**.

---

# Parte D — Tercera sesión (vuelta a la primera cuenta, 2026-11-21 noche)

## D.1 Respuestas de Daniel a las preguntas de la Parte C (21-nov, 21:40)
1. **Aether Transfer se congela** tal como está. Sus pendientes (SFTP real, subir carpetas por
   arrastre, arrastre de archivos grandes sin pausa) quedan anotados para después.
2. **Centro:** se mantiene el alcance de mandos: **solo DualShock 4 (PS4) → mando Xbox** y que
   funcionen los mandos Xbox. Nada más.
3. **Prueba del Centro WPF:** Daniel lo prueba en su **laptop de pruebas** (la aparte, no la de
   trabajo), bajando el artefacto de Actions, igual que hizo con Aether Transfer. Esto **no** es
   el .exe final (Regla 38 sigue vigente).
4. Se **borraron las 7 ejecuciones con ❌** ya corregidas de GitHub Actions (autorizado).
- Preguntas 3–5 de abajo (archivos grandes, carpetas, SFTP): quedan **en pausa** con Transfer congelado.

## D.2 Trabajo de esta sesión
- **Centro (`centro/`) migrado de WinUI 3 a WPF** — commit `c06f888`, compila en verde a la primera.
  Detalle completo en **AETHER-PLAN.md sección 9**. Método: igual que Aether Transfer (lógica
  intacta, solo UI), más una capa de controles propios en `centro/Controles/` con los mismos
  nombres que WinUI (FontIcon, InfoBar, ToggleSwitch, Pila=StackPanel con Spacing, PaginaBase).
- **Lección:** el `ToggleSwitch` propio dispara `Toggled` solo cuando lo cambia el usuario (no al
  cargar datos o enlazar), para que abrir una sección nunca ejecute acciones (p. ej. activar mods).
- **Lección:** en WPF el `ScrollViewer` por defecto deja desplazamiento horizontal y el texto no se
  ajusta: todas las páginas usan `HorizontalScrollBarVisibility="Disabled"`.
- WPF de .NET 8 trae `Microsoft.Win32.OpenFolderDialog` (selector de carpetas nativo).

- **Rediseño de la interfaz del Centro** (pedido de Daniel, 21:57): commit `833c5f1`. Componentes
  `Encabezado` / `FilaAjuste` / mosaicos en `centro/Controles/Diseno.cs`; detalle en el plan, sección 9.
  Para nuevas secciones: usar siempre esos componentes para que todo se vea igual.
- Los artefactos de Actions **no se pueden descargar** desde el entorno de Claude (probado otra vez
  hoy: el proxy bloquea `*.blob.core.windows.net`). Daniel los baja él mismo desde la página de la
  ejecución → Artifacts.

## D.4 Estado de entrega (2026-11-22)
- **Directiva 2.0, pasos 1–5 hechos y en verde**: `a22bf87` (servicio de mandos, indicador, temporizador BT,
  interruptor general), `38fb088` (perfiles inteligentes), `faebeef` (Mica), `b8e84bf` (niveles Gaming).
  Paso 6: `72dfff3` + `91fe4b4` versión 2.0.0 y RegisteredOwner "Aether" (la plantilla de errores debe decir
  "Atlas vX for Windows …": lo exige un test). Instalador candidato 2.0 lanzado con `aether-app.yml`.
- Pendiente: prueba de Daniel en la VM (lista en `docs/aether/prueba-vm-2.0.md`) → compilación final.
  Word con colores invertidos: Daniel lo revisará tras instalar Aether limpio.

- **22-nov 02:53 — Daniel instaló el candidato 2.0 en su laptop y reportó**: (1) faltaba Aether Transfer
  (FTP): nunca se había metido en el instalador → `e63e12d` lo compila y lo incluye, con accesos directos;
  (2) la bienvenida era la vieja de PowerShell con barra y cuadro blancos → `e63e12d` bienvenida nativa
  oscura en Aether Toolbox; (3) "Uso compartido protegido con contraseña" seguía activado → `1e8cc6a` lo
  desactiva en `aether-red.ps1`. **GitHub Actions bloqueado por facturación** ("recent account payments
  have failed or your spending limit needs to be increased"): esos commits NO están compilados ni probados
  en CI. Wind-Aether: `windhawk-cli.exe` solo existe en Windhawk 2.0 (alfa); winget instala la 1.x estable,
  así que los perfiles no se aplican solos — decisión pendiente de Daniel.

- **22-nov 03:30 — repo hecho PÚBLICO por Daniel** (para que Actions no se bloquee por minutos).
  Revisión completa antes de compilar en `docs/aether/auditoria-2.0.md`. `63a0bae`: el acceso directo
  apunta al AetherToolbox.exe real (manifiesto asInvoker + autoelevación por la tarea \Aether\Toolbox),
  Aether Transfer en la bandeja. `a211567`: sin MessageBox blancos. CI todo verde.

- **22-nov 04:10 — DECISIÓN de Daniel: el Aether Toolbox nuevo NO lleva ninguna función de la versión de
  PowerShell** (unidades de red, respaldo ampliado, perfiles Wind-Aether, herramientas). `9726758` revierte
  `23d6abe`. No volver a portarlas. La versión de PowerShell solo queda como respaldo oculto (bandeja:
  Batería al 30 %). Compilación final: artefacto **AETHER-OS-2.0** con `AETHER OS 2.0.exe` — run 45011729121, artefacto 14750297449 (296 MB), verificado que lleva AetherToolbox.exe y AetherTransfer.exe. CI todo verde.

## D.4b Estado anterior (2026-11-21, noche)
- Último commit de código: `87aca2a` (instalador: componentes y drivers de mandos solo si faltan) y `61da075` (tema de Office también en la copia de la cuenta). Antes: `f808dea` (Office con tema fijo, no sigue a Windows; CI todo verde). Antes: `8e382fe` (mando DS4 original: luz, reconexión, ocultar el real). Antes: `0ac3698` (la versión nueva reemplaza a la vieja abierta). Antes: `7151fdb` (Office blindado; CI todo verde). Antes: `45fa36b` (nombre Aether Toolbox + botón Cerrar), `504dab3` (segundo plano siempre activo + bandeja oscura). Antes: `7970b64` (animación de modos centrada y rediseñada). Antes: `833c5f1` (rediseño) y `c06f888` (migración a WPF).
- "Centro Aether (WPF)" en `7970b64`: ✅ verde.
- **Confirmado por Daniel con captura: el Centro WPF abre en su equipo.**
- GitHub Actions en `c06f888`: **TODO EN VERDE** ✅ — Centro Aether (WPF), Build Playbook, Lint
  PowerShell, Lint YAML y Test (Pester PS7 + Windows PowerShell 5.1). Sin ejecuciones con ❌.
- El workflow viejo "Centro Aether (WinUI 3)" ahora se llama **"Centro Aether (WPF)"**.
- Ningún .exe final compilado (Regla 38). El artefacto de CI es solo para la prueba de Daniel.

- **Decisión de Daniel (21-nov 22:32): el Centro debe estar SIEMPRE activo en segundo plano**, con menú de
  bandeja para cambiar el modo de uso y el mando (Xbox / PS4 real). **Nada blanco** en nada que abra.
  Hecho en `504dab3` (ver plan sección 9). Lección: el menú de WinForms (ContextMenuStrip) sale blanco →
  el menú es una ventana WPF propia (`MenuBandeja`); el NotifyIcon de WinForms solo pone el ícono.

- **Nombre oficial de la app de control: "Aether Toolbox"** (antes "Centro Aether"). Daniel lo recalcó el
  21-nov 22:57: usar SIEMPRE "Aether Toolbox" en todo lo visible. Internamente sigue `centro/`/`CentroAether`.
- **Office blindado** (21-nov 22:55): Office no debe ser alterado por ningún mod de Windhawk ni nada más.
  Hecho con la exclusión global de Windhawk + guardia en Aether Toolbox (ver plan sección 9).
- La pregunta de "Proteger macros" quedó resuelta: se quitó y la sección Oficina se rehízo.

- **Directiva Aether OS 2.0** (21-nov 23:39): lista de trabajo en AETHER-PLAN.md sección 10. Daniel autorizó la
  compilación final al terminar todo ("ahora sí cuando acabes todo esto"); igual se compila solo tras su prueba en VM.

## D.3 Preguntas abiertas para Daniel
0. (Hecho en `504dab3`: animación en bandeja y atajos.) ¿Aviso general + botones desactivados cuando Aether
   OS no está instalado?
1. ~~Botón "Proteger macros"~~ — resuelto: quitado; Oficina rehecha con blindaje y desbloqueo.

## Preguntas pendientes para Daniel (sesión anterior; 1 y 2 ya respondidas en D.1)
1. ¿**Aether Transfer** ya está lo bastante bien para **congelarlo** y pasar a **migrar Centro a
   WPF**? (Centro en WinUI 3 casi seguro no abrirá en tu laptop, igual que pasó aquí.)
2. **Centro**: ¿se mantiene el alcance de solo **PS4 (DualShock 4) → mando Xbox** + que funcionen
   los mandos Xbox? (Nintendo, giroscopio, touchpad, vibración fuera.)
3. Arrastrar archivos **grandes** desde el FTP hacia el Explorador: ¿lo dejamos como está (se
   descarga primero, con la guía de usar "Descargar" para los grandes) o invierto tiempo en el
   streaming de archivo virtual para que no se pause?
4. ¿Necesitas **subir carpetas enteras** por arrastre (hoy solo archivos sueltos)?
5. ¿Quieres probar **SFTP** (p. ej. un servidor SSH) además del FTP de la PS4, para validar esa
   ruta antes de dar por cerrado Aether Transfer?
