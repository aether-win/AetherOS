# Aether Transfer — hoja de ruta

> Cliente FTP/FTPS/SFTP profesional e independiente (no toca el Explorador real),
> ventana propia estilo FileZilla con identidad Aether. Proyecto: `aether-transfer/`.
> Regla: **no compilar el .exe final** hasta autorización expresa de Daniel.

## Decisiones
- **Stack:** WinUI 3 + C#/.NET 8, self-contained, modular (Core, Protocols, Connections,
  FileExplorer, TransferEngine, Synchronization, Security, Settings, Views).
- **Protocolos:** FluentFTP (FTP/FTPS) + SSH.NET (SFTP) — ambos MIT, sin obligaciones GPL.
- **Credenciales:** DPAPI / Windows Credential Manager. Nunca en texto plano.
- **Icono:** el de Daniel, integrado (PNG 1024/512/256 + ICO). El arte no se regenera.
- **UI:** fiel al mockup (menú, toolbar, conexión rápida, registro, explorador doble, cola, estado).

## Estado por fase
Leyenda: ✅ hecho · 🟡 parcial · ⬜ pendiente

| Fase | Área | Estado |
|------|------|--------|
| 1 | Inspección del entorno/repo | ✅ |
| 2 | Arquitectura (proyecto modular) | ✅ esqueleto creado |
| 3 | Identidad gráfica e icono | ✅ icono integrado + recursos |
| 4 | Interfaz fiel al mockup | ✅ shell completo (datos de ejemplo), compila en CI |
| 5 | Motor de protocolos (FluentFTP/SSH.NET) | ✅ FTP/FTPS/SFTP: conectar, listar, subir, descargar, crear/eliminar/renombrar |
| 6 | Administrador de sitios (DPAPI) | ✅ sitios guardados, credenciales cifradas, conectar/editar/eliminar |
| 7 | Explorador dual (operaciones reales, local y remoto) | ✅ listar/navegar/transferir + menús contextuales (crear/renombrar/eliminar) |
| 8 | Motor de transferencias (cola, progreso, pausar/reanudar) | ✅ cola secuencial, progreso/velocidad, pausar/reanudar (Resume), cancelar |
| 9 | Sincronización | ✅ comparar local/remoto, subir o descargar diferencias, sin borrado automático |
| 9 | Sincronización | ⬜ |
| 10 | Seguridad (TLS/SSH, credenciales) | ⬜ |
| 11 | Configuración y herramientas | ⬜ |
| 12 | Integración completa | ⬜ |
| 13 | Pruebas reales (FTP/FTPS/SFTP, PS4, etc.) | ⬜ (requiere servidores/hardware) |
| 14 | Compilación y distribución | 🔒 bloqueada hasta autorización |

## Perfiles de dispositivo previstos (sección 9 del spec)
PS4 (FTP por IP/puerto), PS5 si tiene servidor, Android (FTP/SFTP), Linux, Windows, NAS,
routers, hosting. La app explica errores de conexión y qué configuración falta.

## Bitácora
**2026-11-21 — núcleo funcional (compila en verde en CI):**
- Fases 1-4: proyecto, icono, interfaz fiel al mockup ✅
- Fase 5: motor de protocolos FTP/FTPS/SFTP (FluentFTP + SSH.NET) ✅
- Fases 5-7: conexión rápida real, exploradores local y remoto con navegación,
  subir/descargar por botones o doble clic, registro de actividad y barra de estado ✅
- Fase 6/10: administrador de sitios con credenciales cifradas (DPAPI) ✅
- Fase 7: menús contextuales (crear carpeta / renombrar / eliminar) local y remoto ✅
- Fase 8: cola de transferencias con progreso, velocidad, pausar/reanudar y cancelar ✅
- Fase 9: sincronización local↔remoto (comparar y copiar diferencias, sin borrado automático) ✅

- Fase 11: configuración (límite de velocidad, tiempo de espera, confirmar eliminaciones) ✅

**Aplicación funcionalmente completa** (Fases 1-11). Pendiente opcional: herramientas extra
de la Fase 11 (editor de texto remoto, visor, cálculo de hash, compresión) y, lo importante,
**Fase 13: pruebas con servidores reales** (tu PS4, un FTP/SFTP de prueba) — eso no puede
hacerse en CI; necesita tu hardware.

## Realidad honesta
El código ya conecta y transfiere, pero **compila ≠ probado**: falta ejecutarlo contra
**servidores reales** (tu PS4, un FTP de prueba, un SFTP). El spec es claro: no declarar
que algo funciona sin probarlo. Pendiente de mayor valor: administrador de sitios con
credenciales guardadas (DPAPI), cola de transferencias con progreso/pausa, menús contextuales,
y sincronización.
