# Auditoría Fase 1 — Aether OS (Master Development Prompt)

> Fecha: 2026-11-20 · Estado: **Auditoría completa, sin compilar** (Regla 38 respetada)
> Este documento mapea la visión del "Master Development Prompt" (23 módulos)
> contra el código que ya existe en el repositorio.

---

## 1. Realidad del alcance (honesto)

El prompt describe un **ecosistema profesional nativo** equivalente a una suite
comercial (tipo Razer Cortex + DS4Windows + un gestor de runtimes + un centro de
control, todo integrado). Eso es, de forma realista, **trabajo de varias sesiones**,
no de una sola. No se puede "terminar al 100%" en un turno sin que salga código
superficial o falso. El plan de abajo es incremental: cada fase deja piezas
**reales y funcionales** en el código fuente, y **no se compila el .exe final**
hasta que Daniel lo autorice expresamente.

Dos reglas marco que se cumplen en todo el proyecto:
- **Regla 38:** no se genera el ejecutable de distribución hasta autorización expresa.
- **Sección 28:** solo diagnóstico de red legítimo (latencia, DNS, señal, portal
  cautivo). **Nada** de evasión de firewalls/DPI/autenticación.

---

## 2. Licencias (verificado)

| Componente | Licencia | Implicación |
|---|---|---|
| Aether OS (fork de AtlasOS) | **GPLv3** | El proyecto ya es copyleft. |
| DS4Windows (fuente aportada) | **GPLv3** | ✅ Compatible: se puede reutilizar su código para el Aether Controller **siempre que** Aether se mantenga abierto GPLv3 y dé crédito a DS4Windows / Ryochan7 / Jays2Kings. |

**Conclusión legal:** el Aether Controller basado en DS4Windows es viable. Requiere
conservar avisos de licencia y acreditar a los autores originales.

---

## 3. Mapa de módulos: visión vs. estado real

Leyenda: ✅ hecho · 🟡 parcial (base existe) · ⬜ no empezado · ⛔ descartado por Daniel

| # | Módulo (prompt) | Estado | Dónde vive hoy |
|---|---|---|---|
| 1 | Aether Toolbox (Centro de Control) | 🟡 | `centro/` WinUI 3 (9 páginas) |
| 2 | System Performance Engine | 🟡 | `Aether.Centro.ps1` (planes de energía) + Centro·Modos |
| 3 | Gaming Mode (+ overlay 470×260) | 🟡 | Centro·Modos + overlay nativo 470×260 ✅ |
| 4 | Work Mode | 🟡 | Centro·Modos |
| 5 | Office Protection | 🟡 | tweaks `aether-office*`, Wind-Aether excluye apps Office |
| 6 | Aether Controller (DS4Windows) | ⬜ | fuente disponible (GPLv3 OK) |
| 7 | Game Library | 🟡 | Centro·Biblioteca: detección de Steam + lanzar ✅ |
| 8 | Aether Profiles | ✅ | Centro·Perfiles: app→modo con almacenamiento propio ✅ |
| 9 | Software Center | ✅ | Centro·Software: catálogo winget + instalar/actualizar ✅ |
| 10 | Runtime Manager (VC++/DX/.NET/WebView2/Python) | ✅ | Centro·Runtimes: detección nativa + instalar oficial ✅ |
| 11 | Network Engine (solo diagnóstico) | 🟡 | Centro·Red (`RepararRed`, `HacerRedPrivada`) |
| 12 | Audio Engine | ⛔/⬜ | Daniel pidió **no** control de volumen |
| 13 | Camera Engine | ⛔ | Daniel pidió **no** agregar cámara |
| 14 | Live Monitor | ✅ | Centro·Monitor: CPU/RAM/uptime/procesos + modo activo ✅ |
| 15 | Smart Boost | 🟡 | Centro·Limpieza |
| 16 | Plugin System | ⬜ | — |
| 17 | Update Engine | 🟡 | Centro·Actualizar (pausa Windows Update) |
| 18 | Smart Restore | 🟡 | Centro·Respaldo |
| 19 | Background Intelligence | ✅ | Centro: vigila procesos y aplica perfiles (opt-in) ✅ |
| 20 | Compatibility Database | ✅ | Centro·Compatibilidad: base curada de comportamiento ✅ |
| 21 | WindAether | ✅ | `Wind-Aether-mods.json` (37 mods, sincronizado con backup) |
| 22 | Fotos de perfil (restaurar) | ✅ | `Foto-Perfil.ps1` |
| 23 | Instalador "Configurar Aether" | ✅ | `app/` (Rust/GPUI) |

**Resumen:** 4 ✅ · 9 🟡 · 8 ⬜ · 2 ⛔. Gran parte de la base ya existe; el trabajo
pendiente es (a) convertir los 🟡 en módulos de verdad dentro del Toolbox y
(b) construir los ⬜ de mayor valor.

### Conflicto a resolver con Daniel
El prompt pide **Audio Engine** y **Camera Engine**, pero Daniel dijo antes
explícitamente *"no agregues volumen ni cámara"*. **Por defecto respeto su
instrucción directa** (quedan descartados) hasta que diga lo contrario.

---

## 4. Hoja de ruta realista (por fases, sin compilar)

Orden pensado por **valor / dependencia**, no por el número del prompt.

**Fase 1 — Auditoría** ✅ (este documento).

**Fase 2 — Arquitectura del Toolbox.** Reestructurar `centro/` como shell real de
módulos: registro de módulos, navegación por categorías (Rendimiento / Juegos /
Trabajo / Sistema / Red / Mods), base común de servicios. Todo lo 🟡 se reubica aquí.

**Fase 3 — System Performance Engine + Gaming/Work Mode completos.** Consolidar los
modos, añadir el **overlay de Gaming (470×260)** como ventana nativa, métricas en vivo.

**Fase 4 — Live Monitor + Smart Boost.** CPU/GPU/RAM/red en tiempo real + limpieza
inteligente unificada.

**Fase 5 — Runtime Manager.** Detección e instalación de VC++/DirectX/.NET/WebView2/
Python usando **binarios oficiales de Microsoft** (no el paquete de terceros).

**Fase 6 — Software Center + Game Library + Aether Profiles.** Catálogo winget con UI,
detección de juegos instalados, perfiles por app.

**Fase 7 — Aether Controller.** Integración del código DS4Windows (GPLv3), acreditado.

**Fase 8 — Plugin System + Compatibility Database + Background Intelligence.** Lo más
abstracto; al final.

**Fase 9 — Pulido, pruebas en el equipo de pruebas, documentación.**

**Fase 10 — Compilación final** → solo cuando Daniel autorice expresamente con un "sí"
a la pregunta: *"¿Deseas compilar Aether OS en un ejecutable .exe?"*

---

## 5. Qué NO se toca / se mantiene
- No romper Windows Update / Defender / Firewall / Store / Office / Audio / Bluetooth /
  Red / drivers AMD.
- Mods de Windhawk **desactivados por defecto**.
- Nada de software pirata o de terceros no confiable (incluye el paquete
  `GamingLibrary.exe` / `Bin.zip` aportado — se descarta a favor de redistribuibles
  oficiales de Microsoft).
- No modificar configuración personal sin autorización.

---

## 6. Bitácora de avance

**2026-11-20 (noche) — Fases 1-6 en marcha, todo validado en CI (centro.yml verde):**
- F1 Auditoría completa ✅
- F2 Toolbox como shell de módulos (registro central + navegación por categorías) ✅
- F3 Monitor en vivo (CPU/RAM/uptime/procesos + modo activo) ✅
- F3 Overlay de Gaming 470×260 (ventana nativa, fundido, auto-cierre) ✅
- F5 Runtime Manager (detección nativa + instalación oficial winget) ✅
- F6 Software Center (catálogo winget + actualizar todo) ✅
- F6 Biblioteca de juegos (detección de Steam + lanzar, opción Modo Gaming al jugar) ✅

**2026-11-20 (continuación autónoma) — más módulos, todos verdes en CI:**
- F6 Aether Profiles (perfiles app→modo, almacenamiento propio) ✅
- F8 Background Intelligence (vigilante de procesos que aplica perfiles, opt-in, apagado por defecto) ✅
- F8 Compatibility Database (base curada de comportamiento tras ajustes) ✅

Pendiente de mayor valor: **Aether Controller** (DS4Windows, GPLv3 — merece sesión propia),
**Oficina** (Office Protection) y **Plugin System**. Lo demás de la visión ya tiene módulo real
o base funcional. Sigue vigente la Regla 38: **no compilar el .exe final** hasta autorización expresa.
