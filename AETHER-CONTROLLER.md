# Aether Universal Controller Engine — hoja de ruta

> Motor propio de mandos de Aether, con DS4Windows (GPLv3) como referencia técnica.
> **No** es un cambio de nombre de DS4Windows: identidad y arquitectura propias, en español.
> Regla 15/38 vigente: **no compilar el .exe final** hasta autorización expresa.

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
| 1 | Compatibilidad universal (PS/Xbox/Nintendo/genéricos) | 🟡 clasificación por fabricante hecha; por-modelo a probar |
| 2 | Motor de conversión (virtual Xbox 360 / DualShock 4) | 🟡 Xbox360 ✅; DS4 virtual en progreso |
| 3 | Perfiles de mando (crear/duplicar/editar/importar/exportar) | 🟡 almacenamiento + CRUD base |
| 4 | Editor visual de botones + prueba en vivo | ⬜ |
| 5 | Config avanzada (deadzones, curvas, vibración, gyro, calibración, macros) | ⬜ |
| 6 | Sistema de dispositivos virtuales (ciclo de vida, anti-duplicado) | 🟡 crear/liberar; anti-conflicto base |
| 7 | Joy-Con engine (individual/combinado, gyro, batería) | ⬜ (requiere HID dedicado) |
| 8 | Funciones avanzadas PS (touchpad, barra luminosa, gatillos adaptativos, gyro) | ⬜ (capa HID profunda) |
| 9 | Integración con Game Library (perfil por juego, auto-switch) | 🟡 base de perfiles; auto-switch con Background Intelligence |
| 10 | Nueva interfaz (dashboard, selector de conversión, diagnóstico) | 🟡 dashboard + selector base |
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
