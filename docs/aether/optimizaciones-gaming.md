# Aether OS 2.0 — Optimizaciones de los modos

Todo es **reversible**: Aether guarda el valor que había antes y lo repone al salir del modo.
Se aplica sobre el paquete de energía propio de Aether (no toca los planes del usuario).
**No se prometen FPS**: ningún valor se ha medido todavía en hardware real (pendiente de prueba en VM/laptop).

| Nivel | Optimización | Beneficio esperado | Justificación técnica | Riesgo | Restauración |
|---|---|---|---|---|---|
| Equilibrado (= Modo Trabajo) | CPU 10–100 % bajo demanda, EPP 50, turbo disponible, animaciones completas | Fluido y fresco en el día a día | Política de energía equilibrada de Windows | Ninguno | Es el estado base |
| Gaming | CPU 100 % mín/máx, sin estacionar núcleos, EPP 0, enfriamiento activo, turbo agresivo (o limitado si se elige) | Frecuencia estable al jugar | Evita bajadas de reloj y apagado de núcleos | Bajo (más consumo; exige cargador) | Trabajo repone los valores originales |
| Gaming | Modo de energía de Windows "Mejor rendimiento", temporizador 0,5 ms | Ritmo de fotogramas más parejo | Mayor precisión del planificador | Bajo | Al salir de Gaming |
| Gaming | Sin notificaciones, sin apps en segundo plano, mods pesados de Wind-Aether en pausa | Menos interrupciones y uso de CPU/RAM | Libera recursos | Ninguno | Al salir de Gaming |
| Gaming Extremo | Suspensión selectiva USB desactivada (AC) | Mandos/ratón sin retraso al despertar | El puerto USB no se duerme entre eventos | Bajo | Valor guardado (`ExtraUsb`) |
| Gaming Extremo | ASPM de PCI Express desactivado (AC) | Menos latencia de GPU/SSD | El enlace PCIe no entra en reposo | Bajo (más consumo/calor) | Valor guardado (`ExtraPcie`) |
| Gaming Extremo | Servicio de Búsqueda de Windows (WSearch) detenido mientras se juega | Sin picos de disco/CPU del indexado | El indexador compite por disco | Ninguno | Se vuelve a iniciar al salir |
| Gaming Extremo | `PowerThrottlingOff = 1` | El juego nunca queda "eficiente" | Windows no recorta energía a procesos | Bajo | Valor anterior o se borra |
| Máximo Rendimiento | CPU sin estados de reposo (IDLEDISABLE) + turbo agresivo siempre | Menor latencia posible | La CPU nunca entra en C-states profundos | **Medio: bastante más calor** (laptop 15 W: usar ventilador) | Valor guardado (`ExtraIdle`) |

Servicios críticos que Aether **nunca** desactiva: Windows Update (solo pausado), Microsoft Store, Defender
(salvo la casilla del instalador), audio, Bluetooth, USB, Plug and Play, Windows Installer, red, inicio de sesión.

Pendiente de medir en hardware real (no se inventan resultados): RAM/CPU en reposo, latencia DPC/ISR
(LatencyMon), FPS con y sin cada nivel.
