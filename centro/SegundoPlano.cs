using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CentroAether.Services;
using Forms = System.Windows.Forms;

namespace CentroAether;

/// <summary>
/// Segundo plano de Aether Toolbox (siempre activo). Mientras la sesión esté abierta:
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
    private static readonly DispatcherTimer GuardiaOffice = new() { Interval = TimeSpan.FromMinutes(2) };
    private static System.Drawing.Icon? _iconoBase;
    private static IntPtr _hIconActual = IntPtr.Zero;
    private static string _ultimoModo = "";
    private static bool _ocupado;


    public static void Iniciar()
    {
        _ultimoModo = Aether.ModoGuardado();

        // Si la vez anterior se cerró de golpe con el mando real oculto, volver a mostrarlo.
        _ = Task.Run(HidHideServicio.RestaurarPendientes);

        // Servicio de mandos (por eventos): indicador, luz del DS4, apagado por inactividad.
        ServicioMandos.Cambio += () => Application.Current?.Dispatcher.BeginInvoke(new Action(ActualizarIndicador));
        ServicioMandos.Conexion += (m, conectado) => Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            Notificar(conectado
                ? $"🟢 MANDO CONECTADO — {m.Nombre} · {m.TextoConexion}"
                : $"⚪ Mando desconectado — {m.Nombre}")));
        ServicioMandos.Iniciar();
        ActualizarIndicador();

        // Automatización: al iniciar sesión, Aether se completa solo (drivers de mandos, Windhawk 2.0
        // y los mods de Wind-Aether). Espera un poco a que haya red. Avisa solo si hizo algo.
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(45));
            // Guardián (parte del usuario): repone los ajustes de Aether de esta cuenta si algo los quitó.
            await Aether.Ejecutar("$g = Join-Path ([Environment]::GetFolderPath('Windows')) 'AtlasModules\\Aether\\Guardian\\Guardian-Aether.ps1'; if (Test-Path -LiteralPath $g) { & $g -Usuario }");
            string r = (await Aether.Ejecutar("(@(Invoke-AetherAutoReparacion) -join '; ')")).Trim();
            if (r.Length > 0 && !r.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                Application.Current?.Dispatcher.BeginInvoke(new Action(() => Notificar("Aether se completó: " + r)));
            }
        });

        _iconoBase = CargarIcono();
        _icono = new Forms.NotifyIcon
        {
            Icon = _iconoBase,
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

        // Guardia de Office: al iniciar y cada 2 minutos comprueba que Office siga fuera del
        // alcance de Windhawk y con tema fijo (Office pisa el tema al cerrar o al sincronizar la cuenta).
        GuardiaOffice.Tick += (_, _) => _ = ProtegerOffice();
        GuardiaOffice.Start();
        _ = ProtegerOffice();
    }

    private static async Task ProtegerOffice()
    {
        bool reparado = await Task.Run(() =>
        {
            bool algo = false;
            // Tema fijo: si Office volvió a "usar el del sistema" o a Negro, se repone (Colorido).
            if (!OficinaServicio.TemaProtegido())
            {
                algo |= OficinaServicio.FijarTema(0);
            }
            if (OficinaServicio.WindhawkInstalado() && !OficinaServicio.OfficeBlindado())
            {
                algo |= OficinaServicio.BlindarOffice();
            }
            return algo;
        });
        if (reparado)
        {
            Notificar("Office quedó protegido: ni los mods ni el modo claro/oscuro de Windows lo alteran. Si Word o Excel estaban abiertos, ciérralos y ábrelos de nuevo.");
        }
    }

    public static void Detener()
    {
        Vigia.Stop();
        GuardiaOffice.Stop();
        ServicioMandos.Detener();
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
    public static async Task CambiarMando(TipoVirtual? destino)
    {
        // Fuera del hilo de la ventana: iniciar el mando virtual y HidHide tarda un momento.
        (bool _, string mensaje) = await Task.Run(() => ControlMandos.Cambiar(destino));
        Notificar(mensaje);
        ActualizarIndicador();
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
            : "Aether Toolbox";
        if (ServicioMandos.Activo)
        {
            MandoConectado? m = ServicioMandos.Conectados.FirstOrDefault();
            texto += m is null ? "\nMando: desconectado" : $"\nMando: {m.Nombre} ({m.TextoConexion})";
        }
        return texto.Length > 63 ? texto[..63] : texto;   // límite de Windows
    }

    /// <summary>
    /// Ícono de la bandeja con un punto: verde si hay un mando conectado, gris si no (solo con el
    /// interruptor de mandos encendido). Se dibuja sobre el ícono de Aether.
    /// </summary>
    public static void ActualizarIndicador()
    {
        if (_icono is null || _iconoBase is null)
        {
            return;
        }
        try
        {
            System.Drawing.Icon nuevo;
            IntPtr hNuevo = IntPtr.Zero;
            if (!ServicioMandos.Activo)
            {
                nuevo = _iconoBase;
            }
            else
            {
                bool hay = ServicioMandos.Conectados.Count > 0;
                int lado = Math.Max(_iconoBase.Width, 16);
                using var bmp = new System.Drawing.Bitmap(lado, lado);
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.DrawIcon(_iconoBase, new System.Drawing.Rectangle(0, 0, lado, lado));
                    float d = lado * 0.46f;
                    var zona = new System.Drawing.RectangleF(lado - d - 0.5f, lado - d - 0.5f, d, d);
                    using var borde = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 10, 21, 36));
                    using var punto = new System.Drawing.SolidBrush(hay
                        ? System.Drawing.Color.FromArgb(255, 63, 191, 127)
                        : System.Drawing.Color.FromArgb(255, 120, 135, 155));
                    g.FillEllipse(borde, zona);
                    float m = d * 0.18f;
                    g.FillEllipse(punto, zona.X + m, zona.Y + m, d - (2 * m), d - (2 * m));
                }
                hNuevo = bmp.GetHicon();
                nuevo = System.Drawing.Icon.FromHandle(hNuevo);
            }
            _icono.Icon = nuevo;
            _icono.Text = TextoIcono();
            if (_hIconActual != IntPtr.Zero)
            {
                DestroyIcon(_hIconActual);
            }
            _hIconActual = hNuevo;
        }
        catch { /* el indicador nunca debe tumbar la bandeja */ }

        if (Application.Current?.MainWindow is MainWindow v)
        {
            v.ActualizarMandoMenu();
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr h);

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
