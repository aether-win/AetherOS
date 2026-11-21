using System;
using System.Collections.Generic;
using System.Diagnostics;
using CentroAether;
using Microsoft.UI.Xaml;

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
                    _ = Aether.CambiarModo(p.Modo);
                    // El overlay del modo correspondiente (Gaming, Trabajo o Batería).
                    VentanaOverlayModo.Mostrar(p.Modo, p.App);
                }
                return;   // un perfil activo a la vez
            }
        }

        _perfilActivo = null;   // ninguno corriendo; no revertimos el modo
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
