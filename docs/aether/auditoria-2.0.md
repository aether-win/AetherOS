# Aether OS 2.0 — revisión antes de compilar (22-nov-2026)

Revisión de cada pedido de Daniel contra el código. "Código" = está hecho y CI en verde;
"Probar" = falta confirmarlo en la laptop/VM (no se puede probar aquí: no hay Windows).

| Pedido | Dónde está | Estado |
|---|---|---|
| Nombre "Aether Toolbox" en todo lo visible | centro/, aether-centro.ps1/.psd1, catálogo | Código |
| Acceso directo = ejecutable REAL de Aether Toolbox (Escritorio e Inicio) | aether-centro.ps1 (TargetPath = AetherToolbox.exe); el exe se eleva con la tarea \Aether\Toolbox sin UAC | Código · Probar |
| Toolbox siempre en segundo plano al iniciar sesión | tarea "Toolbox segundo plano" (siempre registrada) + la bandeja clásica la arranca si no corre | Código · Probar |
| Nunca se abre la versión vieja si está la nueva | Abrir-Centro.vbs decide al abrir; Centro-Aether.ps1 redirige al nuevo | Código |
| Aether Transfer (FTP) incluido | aether-app.yml lo compila y verifica dentro del .apbx; accesos en Escritorio e Inicio; opción en la bandeja | Código · Probar |
| Bienvenida oscura y nueva | VentanaBienvenida (Toolbox); la de PowerShell ya no se abre | Código · Probar |
| Nada blanco | menús/ventanas WPF oscuras; MessageBox reemplazados por aviso oscuro; RadioButton oscuro | Código |
| Uso compartido protegido con contraseña: desactivado | aether-red.ps1 paso 9 | Código · Probar |
| Red de trabajo (SMB, NetBIOS, Privada, invitado 24H2) | aether-red.ps1 | Código (probado antes por Daniel) |
| Modos Trabajo/Gaming/Batería a mano; Batería sola al 30 % | Aether.Centro.ps1 + bandeja clásica (vigilante) | Código |
| Animación al cambiar de modo | VentanaOverlayModo | Código |
| Gaming / Gaming Extremo / Máximo Rendimiento reversibles | Aether.Centro.ps1, ModosPage, docs/aether/optimizaciones-gaming.md | Código · Probar |
| Mandos DS4/Xbox, ocultar el real (HidHide), sin duplicados, reconexión | MotorControlador, HidHideServicio, ServicioMandos | Código · Probar |
| Luz del DS4 original | LuzDs4 (USB 0x05 / BT 0x11 con CRC) + mandos.log | Probar |
| Indicador 🟢 MANDO CONECTADO | bandeja + menú lateral + notificación | Código · Probar |
| Apagado del mando Bluetooth por inactividad | ServicioMandos (IOCTL_BTH_DISCONNECT_DEVICE) | Probar |
| Perfiles por juego, sonidos, restaurar al cerrar | PerfilesPage, InteligenciaFondo, ControlMandos | Código · Probar |
| Interruptor general de mandos | ControllerPage, ConfigCentro.MandosActivos | Código |
| ViGEmBus/HidHide se instalan solos y se verifican | Install-AetherApps.ps1 | Código · Probar |
| VC++/DirectX/.NET solo si faltan | Install-AetherApps.ps1 | Código · Probar |
| Office protegido (Windhawk no lo toca, tema fijo) | OficinaServicio + exclusión de Windhawk | Código · Probar |
| Word con colores invertidos | tema fijo + copias de la cuenta | Probar tras instalación limpia |
| Mica real | Tema.cs | Probar |
| Versión 2.0.0, propietario registrado "Aether" | playbook.conf, Set-OemInformation.ps1 | Código |
| Sin software crackeado; de sus portables solo KeyVault | Install-AetherApps.ps1 | Código |

## Pendiente que necesita decisión de Daniel
- **Wind-Aether (perfiles Aether Glass / Limpio):** se aplican con `windhawk-cli.exe`, que solo
  trae Windhawk 2.0 (todavía alfa). winget instala Windhawk 1.x estable → los perfiles no se
  activan solos. Opciones: instalar Windhawk 2.0 alfa, o importar los mods a mano en 1.x.
- IObit Driver Booster y DriverHub del Escritorio no los instala Aether.
