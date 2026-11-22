using System;
using System.Collections.Generic;
using System.Diagnostics;
using CentroAether;
using System.Windows.Threading;

namespace CentroAether.Services;

/// <summary>
/// Background Intelligence: vigila los procesos en ejecución y, cuando aparece uno
/// que tiene un perfil, aplica su modo una sola vez (y muestra el overlay si es
/// Gaming). No revierte el modo al cerrar: respeta la elección del usuario.
/// Opt-in: solo corre si ConfigCentro.InteligenciaActiva está activa.
/// </summary>
public static class InteligenciaFondo
{
    private static DispatcherTimer? _timer;
    private static string? _perfilActivo;
    private static bool _mandoAuto;

    public static void Iniciar()
    {
        if (_timer is not null)
        {
            return;
        }
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _timer.Tick += (_, _) => Revisar();
        _timer.Start();
    }

    public static void Detener()
    {
        _timer?.Stop();
        _timer = null;
        _perfilActivo = null;
    }

    /// <summary>Arranca o detiene según la configuración actual.</summary>
    public static void Refrescar()
    {
        if (ConfigCentro.InteligenciaActiva)
        {
            Iniciar();
        }
        else
        {
            Detener();
        }
    }

    private static void Revisar()
    {
        List<PerfilApp> perfiles = PerfilesServicio.Cargar();
        if (perfiles.Count == 0)
        {
            _perfilActivo = null;
            return;
        }

        HashSet<string> procesos = ProcesosEnEjecucion();

        foreach (PerfilApp p in perfiles)
        {
            string proc = p.Proceso.Trim();
            if (proc.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                proc = proc[..^4];
            }
            if (string.IsNullOrEmpty(proc))
            {
                continue;
            }

            if (procesos.Contains(proc))
            {
                if (_perfilActivo != p.App)
                {
                    _perfilActivo = p.App;
                    _ = AplicarModo(p.Modo, p.App);

                    // Mando virtual del perfil: crear el dispositivo solo si hace falta.
                    if (!string.IsNullOrEmpty(p.MandoVirtual) && !MotorControlador.Activo)
                    {
                        MotorControlador.Objetivo = p.MandoVirtual == "DualShock4"
                            ? TipoVirtual.DualShock4
                            : TipoVirtual.Xbox360;
                        if (MotorControlador.ConflictoActivo() is null && MotorControlador.Iniciar())
                        {
                            _mandoAuto = true;
                        }
                    }
                }
                return;   // un perfil activo a la vez
            }
        }

        // Ningún juego con perfil corriendo: liberar el mando virtual si lo iniciamos nosotros.
        if (_mandoAuto && MotorControlador.Activo)
        {
            MotorControlador.Detener();
            _mandoAuto = false;
        }
        _perfilActivo = null;   // no revertimos el modo
    }

    private static async System.Threading.Tasks.Task AplicarModo(string modo, string app)
    {
        (bool ok, string mensaje) = await Aether.CambiarModoDetallado(modo);
        if (ok)
        {
            // El overlay del modo correspondiente (Gaming, Trabajo o Batería).
            VentanaOverlayModo.Mostrar(modo, app);
        }
        else
        {
            SegundoPlano.Notificar(mensaje);
        }
        SegundoPlano.ModoCambiado(Aether.ModoGuardado());
    }

    private static HashSet<string> ProcesosEnEjecucion()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (Process pr in Process.GetProcesses())
            {
                try
                {
                    set.Add(pr.ProcessName);
                }
                catch { /* proceso sin acceso */ }
                finally
                {
                    pr.Dispose();
                }
            }
        }
        catch { /* sin permisos */ }
        return set;
    }
}
