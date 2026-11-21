# Aether Universal Controller Engine — hoja de ruta

> Motor propio de mandos de Aether, con DS4Windows (GPLv3) como referencia técnica.
> **No** es un cambio de nombre de DS4Windows: identidad y arquitectura propias, en español.
> Regla 15/38 vigente: **no compilar el .exe final** hasta autorización expresa.

## Alcance real (decidido por Daniel, 2026-11-20)
Lo único que importa: **DualShock 4 (PS4) → mando Xbox** funcionando, y **mando Xbox**
cuando se quiera. **Fuera de alcance:** Nintendo/Joy-Con y los extras de PlayStation
(giroscopio, touchpad, barra luminosa, gatillos adaptativos, vibración). No se desarrollan
salvo que Daniel lo pida.

## Principios y límites (del spec, obligatorios)
- No afirmar compatibilidad de un mando/función sin **probarlo en hardware real**.
- No instalar drivers en silencio. ViGEmBus/HidHide solo con acción explícita del usuario.
- No aplicar **dos capas de emulación** sobre el mismo mando (detectar Steam Input / DS4Windows).
- Crear dispositivos virtuales **solo cuando se necesiten**; liberarlos al cerrar el juego.
- No reducir capacidades originales del mando; no modificar drivers de forma irreversible.
- Mantener avisos de licencia de DS4Windows; evitar segunda instancia del motor.

## Estado por sección

| # | Área | Estado |
|---|------|--------|
| 1 | DS4 (PS4) + Xbox (lo que importa a Daniel) | ✅ DS4 por HID crudo (USB+BT) + Xbox por gamepad; a probar en hardware |
| 2 | Motor de conversión (virtual Xbox 360 / DualShock 4) | ✅ ambos destinos, entrada neutral, PS→Xbox y Xbox→PS |
| 3 | Perfiles de mando (crear/editar; mando virtual por juego) | 🟡 perfil por juego fija el mando; import/export + editor visual pendientes |
| 4 | Editor visual de botones + prueba en vivo | 🟡 **diagnóstico en vivo ✅**; editor gráfico de reasignación pendiente |
| 5 | Config avanzada (deadzones, sensibilidad, curvas, vibración, gyro, macros) | 🟡 **zona muerta + sensibilidad ✅**; curvas/vibración/gyro/macros pendientes |
| 6 | Sistema de dispositivos virtuales (ciclo de vida, anti-duplicado) | ✅ crear solo si hace falta, liberar al cerrar el juego, anti-conflicto |
| 7 | Joy-Con engine (individual/combinado, gyro, batería) | ⬜ (requiere HID dedicado) |
| 8 | Funciones avanzadas PS (touchpad, barra luminosa, gatillos adaptativos, gyro) | ⬜ (capa HID profunda) |
| 9 | Integración con Game Library (perfil por juego, auto-switch) | ✅ perfil fija modo+mando; auto-activar/liberar con Background Intelligence |
| 10 | Nueva interfaz (dashboard, selector de conversión, diagnóstico) | ✅ dashboard (fabricante/protocolo) + selector + diagnóstico en vivo |
| 11 | Integración/auditoría DS4Windows (reutilizar con licencia) | 🟡 motor propio ViGEm; HID crudo pendiente |
| 12 | Compatibilidad Steam/otros (detección de conflictos) | 🟡 detecta DS4Windows/Steam en ejecución |
| 13 | Pruebas obligatorias en hardware | ⬜ (requiere los mandos físicos de Daniel) |
| 14 | Seguridad y rendimiento | 🟡 bajo consumo, liberar al cerrar, sin auto-instalar |
| 15 | Regla de desarrollo (no compilar) | ✅ respetada |

## Realidad técnica
- **Emulación Xbox 360 / DualShock 4 virtual**: factible nativo con ViGEm (hecho / en progreso).
- **Soporte profundo DS4/DualSense/Joy-Con** (gyro, touchpad, luces, gatillos adaptativos,
  lectura fiable de botones por USB y Bluetooth): requiere **parseo HID crudo** por dispositivo,
  como hace DS4Windows. Es la capa más grande y se hará por modelo, con pruebas reales.
- Lo que no se pueda probar aquí (no hay mandos en el CI) se marca **experimental** en la UI.

## Bitácora
**2026-11-20 (noche) — núcleo del motor terminado, todo verde en CI:**
- Clasificación de fabricante (PS/Xbox/Nintendo/Genérico) y protocolo (XInput/HID)
- Conversión con destino Xbox 360 o DualShock 4 virtual (ViGEm), entrada neutral
- Entrada DS4/DualSense en crudo (RawGameController) + gamepad estándar
- Detección de conflictos (DS4Windows/InputMapper/x360ce) para no doble-emular
- Perfil por juego fija modo + mando virtual; Background Intelligence lo activa y libera
- Diagnóstico de mando en vivo (índices/ejes) para afinar el mapeo
- Zona muerta y sensibilidad de sticks, ajustables en caliente

**Pendiente (requiere capa HID profunda y/o hardware de Daniel):** editor visual de
reasignación, curvas de respuesta, vibración, giroscopio, touchpad, barra luminosa,
gatillos adaptativos, Joy-Con, calibración, macros, y las **pruebas obligatorias** por modelo.
