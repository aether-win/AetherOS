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
| 5 | Motor de protocolos (FluentFTP/SSH.NET) | ⬜ |
| 6 | Administrador de sitios (DPAPI) | ⬜ |
| 7 | Explorador dual (operaciones reales, local y remoto) | ⬜ |
| 8 | Motor de transferencias (cola, progreso, pausar/reanudar) | ⬜ |
| 9 | Sincronización | ⬜ |
| 10 | Seguridad (TLS/SSH, credenciales) | ⬜ |
| 11 | Configuración y herramientas | ⬜ |
| 12 | Integración completa | ⬜ |
| 13 | Pruebas reales (FTP/FTPS/SFTP, PS4, etc.) | ⬜ (requiere servidores/hardware) |
| 14 | Compilación y distribución | 🔒 bloqueada hasta autorización |

## Perfiles de dispositivo previstos (sección 9 del spec)
PS4 (FTP por IP/puerto), PS5 si tiene servidor, Android (FTP/SFTP), Linux, Windows, NAS,
routers, hosting. La app explica errores de conexión y qué configuración falta.

## Realidad honesta
La interfaz ya está y compila; **lo que sigue es la lógica real** (conectar, listar,
transferir), que es el grueso del trabajo y necesita **pruebas con servidores de verdad**.
El spec es claro: no declarar que algo funciona sin probarlo.
