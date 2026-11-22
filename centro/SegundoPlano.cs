using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CentroAether.Services;
using Forms = System.Windows.Forms;

namespace CentroAether;

/// <summary>
/// Segundo plano del Centro Aether (siempre activo). Mientras la sesión esté abierta:
///  - Ícono en la bandeja: clic abre el Centro; clic derecho abre el menú oscuro
///    (modo de uso y mando) — nada blanco.
///  - Atajos Ctrl+Alt+T / G / B para Trabajo, Gaming y Batería, con su animación.
///  - Vigila el modo (registro, cada 2 s): si cambia por otra vía (Batería automática,
///    perfiles…), actualiza todo y muestra la animación.
/// Cerrar la ventana del Centro solo la oculta: el segundo plano sigue funcionando.
/// </summary>
public static class SegundoPlano
{
    private static Forms.NotifyIcon? _icono;
    private static MenuBandeja? _menu;
    private static HwndSource? _atajos;
    private static readonly DispatcherTimer Vigia = new() { Interval = TimeSpan.FromSeconds(2) };
    private static string _ultimoModo = "";
    private static bool _ocupado;

    public static void Iniciar()
    {
        _ultimoModo = Aether.ModoGuardado();

        _icono = new Forms.NotifyIcon
        {
            Icon = CargarIcono(),
            Visible = true,
            Text = TextoIcono(),
        };
        _icono.MouseUp += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Right)
            {
                AbrirMenu();
            }
            else if (e.Button == Forms.MouseButtons.Left)
            {
                ((App)Application.Current).MostrarCentro();
            }
        };

        RegistrarAtajos();

        Vigia.Tick += (_, _) => RevisarModo();
        Vigia.Start();
    }

    public static void Detener()
    {
        Vigia.Stop();
        _atajos?.Dispose();
        if (_icono is not null)
        {
            _icono.Visible = false;
            _icono.Dispose();
            _icono = null;
        }
    }

    // ── Modo de uso ──────────────────────────────────────────────────────────────

    /// <summary>Cambia el modo desde la bandeja o los atajos, con su animación.</summary>
    public static async Task CambiarModo(string modo)
    {
        if (_ocupado)
        {
            return;
        }
        if (!Aether.Disponible)
        {
            Notificar("Aether OS no está instalado en este equipo, así que no se puede cambiar de modo.");
            return;
        }
        _ocupado = true;
        try
        {
            (bool ok, string mensaje) = await Aether.CambiarModoDetallado(modo);
            if (ok)
            {
                VentanaOverlayModo.Mostrar(modo);
            }
            else
            {
                // Lo rechazó la lógica de Aether (p. ej. Gaming sin cargador): explicar por qué.
                Notificar(mensaje);
            }
            ModoCambiado(Aether.ModoGuardado());
        }
        finally
        {
            _ocupado = false;
        }
    }

    /// <summary>Lo llaman las páginas tras cambiar de modo, para no repetir la animación.</summary>
    public static void ModoCambiado(string modo)
    {
        _ultimoModo = modo;
        if (_icono is not null)
        {
            _icono.Text = TextoIcono();
        }
        if (Application.Current.MainWindow is MainWindow v)
        {
            _ = v.ActualizarModoMenu();
        }
    }

    private static void RevisarModo()
    {
        string actual = Aether.ModoGuardado();
        if (!Aether.Disponible || string.Equals(actual, _ultimoModo, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        // Cambió por otra vía (Batería automática, perfiles, otra ventana): avisar con la animación.
        VentanaOverlayModo.Mostrar(actual);
        ModoCambiado(actual);
    }

    // ── Mando ────────────────────────────────────────────────────────────────────

    /// <summary>null = apagar la emulación; si no, emular ese mando (Xbox 360 o PS4).</summary>
    public static void CambiarMando(TipoVirtual? destino)
    {
        if (destino is null)
        {
            if (MotorControlador.Activo)
            {
                MotorControlador.Detener();
                Notificar("Emulación de mando apagada.");
            }
            return;
        }

        if (!MotorControlador.ViGEmDisponible())
        {
            Notificar("Falta el driver ViGEmBus. Instálalo en Centro Aether › Control.");
            return;
        }
        if (MotorControlador.Activo && MotorControlador.Objetivo == destino)
        {
            return;
        }
        string? conflicto = MotorControlador.ConflictoActivo();
        if (conflicto is not null)
        {
            Notificar($"{conflicto} está abierto. Ciérralo para evitar entrada doble.");
            return;
        }

        if (MotorControlador.Activo)
        {
            MotorControlador.Detener();
        }
        MotorControlador.Objetivo = destino.Value;
        bool ok = MotorControlador.Iniciar();
        string nombre = destino == TipoVirtual.DualShock4 ? "mando de PS4 (DualShock 4)" : "mando de Xbox 360";
        Notificar(ok ? $"Tu mando ahora funciona como {nombre}." : "No se pudo iniciar la emulación del mando.");
    }

    // ── Menú y avisos ────────────────────────────────────────────────────────────

    private static void AbrirMenu()
    {
        _menu ??= new MenuBandeja();
        Forms.Screen pantalla = Forms.Screen.FromPoint(Forms.Cursor.Position);
        _menu.MostrarEn(Forms.Cursor.Position, pantalla.WorkingArea);
    }

    /// <summary>Notificación de Windows desde el ícono de la bandeja (sigue el tema del sistema).</summary>
    public static void Notificar(string texto)
    {
        try
        {
            _icono?.ShowBalloonTip(4000, "Aether OS", texto, Forms.ToolTipIcon.None);
        }
        catch { /* sin bandeja */ }
    }

    private static string TextoIcono()
    {
        string texto = Aether.Disponible
            ? $"Aether OS · Modo {Controles.Ui.NombreModo(_ultimoModo)}"
            : "Centro Aether";
        return texto.Length > 63 ? texto[..63] : texto;   // límite de Windows
    }

    private static System.Drawing.Icon CargarIcono()
    {
        try
        {
            var recurso = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/aether.ico"));
            if (recurso is not null)
            {
                return new System.Drawing.Icon(recurso.Stream, Forms.SystemInformation.SmallIconSize);
            }
        }
        catch { /* uso el del exe */ }
        return System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? "") ?? System.Drawing.SystemIcons.Application;
    }

    // ── Atajos globales ──────────────────────────────────────────────────────────

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modificadores, uint tecla);

    private const int WmHotkey = 0x0312;
    private const uint CtrlAlt = 0x0001 | 0x0002 | 0x4000;   // Alt + Ctrl, sin repetición

    private static void RegistrarAtajos()
    {
        // Ventana invisible solo para recibir los atajos (HWND_MESSAGE).
        var p = new HwndSourceParameters("AetherAtajos") { ParentWindow = new IntPtr(-3), WindowStyle = 0 };
        _atajos = new HwndSource(p);
        _atajos.AddHook(AlRecibir);
        RegisterHotKey(_atajos.Handle, 1, CtrlAlt, 0x54);   // T
        RegisterHotKey(_atajos.Handle, 2, CtrlAlt, 0x47);   // G
        RegisterHotKey(_atajos.Handle, 3, CtrlAlt, 0x42);   // B
    }

    private static IntPtr AlRecibir(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool manejado)
    {
        if (msg == WmHotkey)
        {
            string? modo = wParam.ToInt32() switch { 1 => "Trabajo", 2 => "Gaming", 3 => "Bateria", _ => null };
            if (modo is not null)
            {
                _ = CambiarModo(modo);
                manejado = true;
            }
        }
        return IntPtr.Zero;
    }
}
