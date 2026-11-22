using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CentroAether.Services;

/// <summary>
/// Perfiles automáticos (opt-in: ConfigCentro.InteligenciaActiva). Cada 3 segundos mira qué
/// procesos corren (funciona aunque el juego se abra desde Steam, Rockstar u otro launcher,
/// porque se busca el proceso real, p. ej. GTA5.exe):
///  - Al aparecer el proceso de un perfil ACTIVO: guarda el modo y el mando que había, aplica
///    el modo del perfil (si tiene) y su mando (verificado y con sonido).
///  - Si hay varios juegos con perfil abiertos a la vez, manda el que está más arriba en la lista.
///  - Al cerrarse el juego: vuelve al modo que había antes y deja el mando en el predeterminado.
/// </summary>
public static class InteligenciaFondo
{
    private static DispatcherTimer? _timer;
    private static PerfilApp? _perfilActivo;
    private static string? _modoAntes;
    private static bool _ocupado;

    public static void Iniciar()
    {
        if (_timer is not null)
        {
            return;
        }
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _timer.Tick += async (_, _) => await Revisar();
        _timer.Start();
    }

    public static void Detener()
    {
        _timer?.Stop();
        _timer = null;
        _perfilActivo = null;
        _modoAntes = null;
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

    /// <summary>Nombre del perfil que está aplicado ahora (para mostrarlo en Perfiles).</summary>
    public static string? PerfilAplicado => _perfilActivo?.App;

    private static string Limpio(string proceso)
    {
        string p = proceso.Trim();
        return p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? p[..^4] : p;
    }

    private static async Task Revisar()
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        try
        {
            List<PerfilApp> perfiles = PerfilesServicio.Cargar()
                .Where(p => p.Activo && !string.IsNullOrWhiteSpace(p.Proceso))
                .ToList();
            HashSet<string> procesos = await Task.Run(ProcesosEnEjecucion);

            // El primero de la lista que esté corriendo gana (evita conflictos entre perfiles).
            PerfilApp? enCurso = perfiles.FirstOrDefault(p => procesos.Contains(Limpio(p.Proceso)));

            if (enCurso is not null && !string.Equals(enCurso.App, _perfilActivo?.App, StringComparison.OrdinalIgnoreCase))
            {
                if (_perfilActivo is null)
                {
                    _modoAntes = Aether.Disponible ? Aether.ModoGuardado() : null;
                }
                _perfilActivo = enCurso;
                await Aplicar(enCurso);
            }
            else if (enCurso is null && _perfilActivo is not null)
            {
                await Restaurar(_perfilActivo);
                _perfilActivo = null;
                _modoAntes = null;
            }
        }
        catch (Exception ex)
        {
            ServicioMandos.Registrar("Perfiles automáticos: " + ex.Message);
        }
        finally
        {
            _ocupado = false;
        }
    }

    private static async Task Aplicar(PerfilApp p)
    {
        if (!string.IsNullOrEmpty(p.Modo) && Aether.Disponible)
        {
            (bool ok, string mensaje) = await Aether.CambiarModoDetallado(p.Modo);
            if (ok)
            {
                VentanaOverlayModo.Mostrar(p.Modo, p.App);
            }
            else
            {
                SegundoPlano.Notificar(mensaje);
            }
            SegundoPlano.ModoCambiado(Aether.ModoGuardado());
        }

        TipoVirtual? mando = ControlMandos.DesdeTexto(p.MandoVirtual);
        if (mando is not null && ConfigCentro.MandosActivos)
        {
            (bool ok, string mensaje) = await Task.Run(() => ControlMandos.Cambiar(mando));
            SegundoPlano.Notificar($"{p.App}: {mensaje}");
            ServicioMandos.Registrar($"Perfil «{p.App}» ({p.Proceso}): mando → {(ok ? "aplicado" : "no aplicado")}.");
            SegundoPlano.ActualizarIndicador();
        }
    }

    private static async Task Restaurar(PerfilApp p)
    {
        // Modo: volver al que había antes del juego.
        if (!string.IsNullOrEmpty(p.Modo) && !string.IsNullOrEmpty(_modoAntes) && Aether.Disponible
            && !string.Equals(_modoAntes, Aether.ModoGuardado(), StringComparison.OrdinalIgnoreCase))
        {
            (bool ok, _) = await Aether.CambiarModoDetallado(_modoAntes);
            if (ok)
            {
                VentanaOverlayModo.Mostrar(_modoAntes, $"{p.App} se cerró");
            }
            SegundoPlano.ModoCambiado(Aether.ModoGuardado());
        }

        // Mando: volver al predeterminado (si el perfil lo había cambiado).
        if (!string.IsNullOrEmpty(p.MandoVirtual) && ConfigCentro.MandosActivos)
        {
            TipoVirtual? predeterminado = ControlMandos.DesdeTexto(ConfigCentro.MandoPredeterminado);
            (_, string mensaje) = await Task.Run(() => ControlMandos.Cambiar(predeterminado));
            SegundoPlano.Notificar($"{p.App} se cerró: {mensaje}");
            SegundoPlano.ActualizarIndicador();
        }
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
