# Aether OS 2.0 — lista de prueba en la máquina virtual

Solo en la VM de Hyper-V, nunca en la laptop real. Marca cada punto con ✅ / ❌ y, si falla, manda captura
y el archivo `C:\ProgramData\Aether\mandos.log` (y `AetherToolbox-error.log` si existe).

## 1. Instalación
- [ ] Windows 11 limpio en la VM, con Internet, Defender desactivado como pide el instalador.
- [ ] El instalador muestra "Aether OS v2.0.0" y termina sin errores.
- [ ] Tras reiniciar: fondo y temas Aether, nada blanco en lo que abre Aether.
- [ ] `winver` / Configuración > Sistema > Información: propietario registrado **Aether**; el nombre del equipo
      NO cambió.
- [ ] Al primer inicio de sesión se instalan solo las dependencias que faltaban (VC++, DirectX, .NET 8, .NET 3.5);
      ViGEmBus y HidHide quedan instalados (Panel de control > Programas).
- [ ] Windows Update, Microsoft Store, sonido y Bluetooth funcionan.

## 2. Aether Toolbox
- [ ] Aparece el ícono en la bandeja al iniciar sesión (segundo plano).
- [ ] Clic izquierdo abre Aether Toolbox; clic derecho abre el menú oscuro con modos, mando y "Cerrar".
- [ ] En Windows 11 22H2 o posterior la ventana tiene efecto Mica (fondo translúcido).
- [ ] Ctrl+Alt+T / G / B cambian a Trabajo / Gaming / Batería y aparece la animación centrada.

## 3. Mandos (en la VM pasar el mando por USB si Hyper-V lo permite; si no, probar en la laptop tras formatear)
- [ ] Conectar DS4 original por USB: en 1–2 s aparece 🟢 MANDO CONECTADO (bandeja y Toolbox) con "USB".
- [ ] La luz del mando se enciende azul (jugador 1). Si no: mandar `mandos.log` (dice qué respondió Windows).
- [ ] Desconectar: el punto pasa a gris.
- [ ] Mismo con Bluetooth: dice "Bluetooth"; temporizador de apagado (p. ej. 5 min) apaga el mando sin tocarlo.
- [ ] Cambiar a "Xbox 360" y "PS4" desde la bandeja: se oye el sonido distinto de cada uno; en
      `joy.cpl` aparece solo el mando emulado (el real queda oculto por HidHide), sin duplicados.
- [ ] Apagar el interruptor general: no se crea ningún mando virtual.

## 4. Perfiles inteligentes
- [ ] Crear un perfil eligiendo un juego (selector de procesos) con modo Gaming y mando Xbox.
- [ ] Abrir el juego (también desde Steam): cambia solo el modo y el mando.
- [ ] Cerrar el juego: vuelven el modo anterior y el mando predeterminado.
- [ ] Pausar / editar / borrar el perfil funciona.

## 5. Niveles de Gaming
- [ ] En Modos, elegir Gaming Extremo y luego Máximo Rendimiento: cada ajuste muestra beneficio, porqué y riesgo.
- [ ] Volver a Trabajo: `powercfg /q` muestra la suspensión selectiva USB y ASPM como estaban; Windows Search
      vuelve a estar en ejecución.

## 6. Office
- [ ] Instalar Office, abrir Word: texto negro sobre fondo blanco con Windows en oscuro y en claro.
- [ ] El tema elegido en Aether Toolbox > Office se mantiene después de cerrar y reabrir Word.

## 7. Rendimiento en reposo
- [ ] Administrador de tareas tras 5 min sin uso: anotar RAM usada y CPU (AetherToolbox.exe < 1 % de CPU).
- [ ] Opcional: LatencyMon 5 min sin errores rojos.
