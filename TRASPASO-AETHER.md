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
