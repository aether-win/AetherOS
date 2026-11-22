using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CentroAether.Services;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using CentroAether.Controles;
using System.Windows.Media;
using Windows.Gaming.Input;

namespace CentroAether.Pages;

/// <summary>
/// Aether Controller (base): detecta mandos conectados vía Windows.Gaming.Input.
/// El motor de mapeo (emulación Xbox 360, perfiles) basado en DS4Windows llegará después.
/// </summary>
public sealed partial class ControllerPage : PaginaBase
{
    private bool _ocupado;
    private bool _cargando;
    private bool _listo;
    private readonly DispatcherTimer _diag = new() { Interval = TimeSpan.FromMilliseconds(60) };

    public ControllerPage()
    {
        this.InitializeComponent();
        _diag.Tick += (_, _) => DiagTick();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ServicioMandos.Cambio += AlCambiarMandos;
        MotorControlador.EstadoCambiado += AlCambiarMandos;
        PrepararAjustesMandos();
        Buscar();
        RefrescarMotor();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _diag.Stop();
        ServicioMandos.Cambio -= AlCambiarMandos;
        MotorControlador.EstadoCambiado -= AlCambiarMandos;
    }

    private void Diag_Toggled(object sender, RoutedEventArgs e)
    {
        if (TglDiag.IsOn)
        {
            CajaDiag.Visibility = Visibility.Visible;
            _diag.Start();
        }
        else
        {
            _diag.Stop();
            CajaDiag.Visibility = Visibility.Collapsed;
        }
    }

    private void DiagTick()
    {
        try
        {
            RawGameController? rc = RawGameController.RawGameControllers
                .FirstOrDefault(c => c.HardwareVendorId == 0x054C)
                ?? RawGameController.RawGameControllers.FirstOrDefault();

            if (rc is null)
            {
                TxtDiag.Text = "No hay ningún mando conectado.";
                return;
            }

            var botones = new bool[rc.ButtonCount];
            var switches = new GameControllerSwitchPosition[rc.SwitchCount];
            var ejes = new double[rc.AxisCount];
            rc.GetCurrentReading(botones, switches, ejes);

            var pulsados = Enumerable.Range(0, botones.Length).Where(i => botones[i]).ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"Mando: {rc.DisplayName}");
            sb.AppendLine($"Botones pulsados (índice): {(pulsados.Count > 0 ? string.Join(", ", pulsados) : "—")}");
            sb.AppendLine($"Cruceta: {(switches.Length > 0 ? switches[0].ToString() : "—")}");
            for (int i = 0; i < ejes.Length; i++)
            {
                sb.Append($"Eje{i}:{ejes[i]:0.00}  ");
            }
            TxtDiag.Text = sb.ToString();
        }
        catch
        {
            TxtDiag.Text = "No se pudo leer el mando.";
        }
    }

    private void RefrescarMotor()
    {
        bool vigem = MotorControlador.ViGEmDisponible();

        _cargando = true;
        TglEmular.IsOn = MotorControlador.Activo;
        CmbObjetivo.SelectedIndex = MotorControlador.Objetivo == TipoVirtual.DualShock4 ? 1 : 0;
        SldDeadzone.Value = Math.Clamp(ConfigCentro.Deadzone * 100.0, 0, 40);
        SldSens.Value = Math.Clamp(ConfigCentro.Sensibilidad * 100.0, 50, 200);
        TxtDeadzone.Text = $"{SldDeadzone.Value:0}%";
        TxtSens.Text = $"{SldSens.Value:0}%";
        _cargando = false;
        _listo = true;

        TglEmular.IsEnabled = vigem && ConfigCentro.MandosActivos;
        CmbObjetivo.IsEnabled = vigem && !MotorControlador.Activo;   // no cambiar destino en caliente
        bool hidhide = HidHideServicio.Disponible;
        PanelRequisitos.Visibility = vigem && hidhide ? Visibility.Collapsed : Visibility.Visible;
        BtnVigem.Visibility = vigem ? Visibility.Collapsed : Visibility.Visible;
        BtnHidHide.Visibility = hidhide ? Visibility.Collapsed : Visibility.Visible;
        PanelRequisitos.Titulo = !vigem ? "Faltan drivers" : "Falta HidHide (recomendado)";

        if (!vigem)
        {
            FilaEmular.Descripcion = "Falta el driver ViGEmBus. Instálalo para activar la emulación.";
        }
        else
        {
            string destino = MotorControlador.Objetivo == TipoVirtual.DualShock4 ? "un DualShock 4 (PS4)" : "un Xbox 360";
            string oculto = MotorControlador.MandoRealOculto ? " El original está oculto: los juegos solo ven el emulado." : "";
            FilaEmular.Descripcion = MotorControlador.Activo
                ? $"Activa: tu mando funciona como {destino} virtual.{oculto}"
                : "Lista para activar. Tu mando se presenta a los juegos como un Xbox 360 o un PS4 virtual, con la luz encendida y el original oculto.";
        }
    }

    private void Ajuste_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_listo || _cargando)
        {
            return;
        }
        double dz = SldDeadzone.Value / 100.0;
        double s = SldSens.Value / 100.0;
        ConfigCentro.Deadzone = dz;
        ConfigCentro.Sensibilidad = s;
        MotorControlador.ActualizarAjustes(dz, s);
        TxtDeadzone.Text = $"{SldDeadzone.Value:0}%";
        TxtSens.Text = $"{SldSens.Value:0}%";
    }

    private void Objetivo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando)
        {
            return;
        }
        MotorControlador.Objetivo = CmbObjetivo.SelectedIndex == 1
            ? TipoVirtual.DualShock4
            : TipoVirtual.Xbox360;
    }

    private void Emular_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando)
        {
            return;
        }

        if (TglEmular.IsOn)
        {
            if (!MotorControlador.ViGEmDisponible())
            {
                _cargando = true;
                TglEmular.IsOn = false;
                _cargando = false;
                RefrescarMotor();
                return;
            }

            // No aplicar doble emulación: si hay otro emulador activo, avisar y no iniciar.
            string? conflicto = MotorControlador.ConflictoActivo();
            if (conflicto is not null)
            {
                _cargando = true;
                TglEmular.IsOn = false;
                _cargando = false;
                Estado.Severity = InfoBarSeverity.Warning;
                Estado.Message = $"{conflicto} está en ejecución. Ciérralo para evitar entrada doble y vuelve a intentar.";
                Estado.IsOpen = true;
                return;
            }

            bool ok = MotorControlador.Iniciar();
            Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
            Estado.Message = ok ? "Emulación Xbox 360 activada." : "No se pudo iniciar la emulación.";
            Estado.IsOpen = true;
        }
        else
        {
            MotorControlador.Detener();
            Estado.Severity = InfoBarSeverity.Informational;
            Estado.Message = "Emulación detenida.";
            Estado.IsOpen = true;
        }
        RefrescarMotor();
    }

    private async void Vigem_Click(object sender, RoutedEventArgs e)
        => await InstalarRequisito(BtnVigem, "Nefarius.ViGEmBus", "ViGEmBus");

    private async void HidHide_Click(object sender, RoutedEventArgs e)
        => await InstalarRequisito(BtnHidHide, "Nefarius.HidHide", "HidHide");

    private async System.Threading.Tasks.Task InstalarRequisito(Button btn, string wingetId, string nombre)
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        btn.IsEnabled = false;
        string original = btn.Content?.ToString() ?? "Instalar";
        btn.Content = "Instalando…";
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = $"Instalando {nombre}…";
        Estado.IsOpen = true;

        // Descarga oficial de Nefarius (firma verificada) y, si falla, winget. Se comprueba el servicio.
        await Aether.Ejecutar("[void](Install-AetherDriversMando)");
        bool ok;
        using (var clave = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + nombre))
        {
            ok = clave is not null;
        }
        btn.IsEnabled = true;
        btn.Content = original;
        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = ok
            ? $"{nombre} instalado. Puede que necesites reiniciar para que el driver cargue."
            : $"No se pudo instalar {nombre}. Mira el registro: C:\\ProgramData\\Aether\\autoreparacion.log";
        Estado.IsOpen = true;
        _ocupado = false;
        RefrescarMotor();
    }

    private void Refrescar_Click(object sender, RoutedEventArgs e)
    {
        ServicioMandos.Programar();
        Buscar();
    }

    private static readonly (int Min, string Texto)[] OpcionesApagado =
    {
        (0, "Nunca"), (5, "5 minutos"), (10, "10 minutos"), (15, "15 minutos"), (20, "20 minutos"),
        (30, "30 minutos"), (45, "45 minutos"), (60, "60 minutos"), (-1, "Personalizado…"),
    };

    private void PrepararAjustesMandos()
    {
        _cargando = true;
        TglMandos.IsOn = ConfigCentro.MandosActivos;
        PanelMandos.Visibility = TglMandos.IsOn ? Visibility.Visible : Visibility.Collapsed;
        if (CmbApagado.Items.Count == 0)
        {
            foreach (var o in OpcionesApagado)
            {
                CmbApagado.Items.Add(o.Texto);
            }
        }
        int min = ConfigCentro.ApagadoMandoMin;
        int idx = Array.FindIndex(OpcionesApagado, o => o.Min == min);
        CmbApagado.SelectedIndex = idx >= 0 ? idx : OpcionesApagado.Length - 1;
        TxtMinutos.Visibility = idx >= 0 ? Visibility.Collapsed : Visibility.Visible;
        TxtMinutos.Text = idx >= 0 ? "" : min.ToString();
        _cargando = false;
    }

    private void Mandos_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando)
        {
            return;
        }
        ConfigCentro.MandosActivos = TglMandos.IsOn;
        if (TglMandos.IsOn)
        {
            ServicioMandos.Iniciar();
            Aviso("Mandos de Aether activados.", InfoBarSeverity.Success);
        }
        else
        {
            MotorControlador.Detener();
            ServicioMandos.Detener();
            Aviso("Mandos de Aether desactivados: no corre nada de mandos.", InfoBarSeverity.Informational);
        }
        SegundoPlano.ActualizarIndicador();
        PanelMandos.Visibility = TglMandos.IsOn ? Visibility.Visible : Visibility.Collapsed;
        RefrescarMotor();
        Buscar();
    }

    private void Apagado_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || CmbApagado.SelectedIndex < 0)
        {
            return;
        }
        int min = OpcionesApagado[CmbApagado.SelectedIndex].Min;
        if (min < 0)
        {
            TxtMinutos.Visibility = Visibility.Visible;
            TxtMinutos.Focus();
            return;
        }
        TxtMinutos.Visibility = Visibility.Collapsed;
        GuardarApagado(min);
    }

    private void Minutos_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtMinutos.Text.Trim(), out int min) && min > 0)
        {
            GuardarApagado(Math.Min(min, 600));
        }
    }

    private void GuardarApagado(int min)
    {
        ConfigCentro.ApagadoMandoMin = min;
        ServicioMandos.AplicarTemporizador();
        Aviso(min == 0 ? "El mando Bluetooth no se apagará solo." : $"El mando Bluetooth se apagará tras {min} min sin uso.",
            InfoBarSeverity.Success);
    }

    private void Registro_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string log = System.IO.Path.Combine(ServicioMandos.CarpetaLog, "mandos.log");
            Sistema.Abrir(System.IO.File.Exists(log) ? log : ServicioMandos.CarpetaLog);
        }
        catch
        {
            Aviso("Todavía no hay registro de mandos.", InfoBarSeverity.Informational);
        }
    }

    private void Aviso(string texto, InfoBarSeverity tipo)
    {
        Estado.Severity = tipo;
        Estado.Message = texto;
        Estado.IsOpen = true;
    }

    private void AlCambiarMandos() => Dispatcher.BeginInvoke(new Action(Buscar));

    private void Buscar()
    {
        Contenedor.Children.Clear();
        IReadOnlyList<MandoConectado> mandos = ServicioMandos.Conectados;
        Vacio.Visibility = ServicioMandos.Activo && mandos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (MandoConectado m in mandos)
        {
            Contenedor.Children.Add(CrearTarjeta(m));
        }
    }

    private FilaAjuste CrearTarjeta(MandoConectado m)
    {
        string tipo = m.Tipo switch
        {
            TipoMando.DualShock4 => "PlayStation 4",
            TipoMando.DualSense => "PlayStation 5",
            TipoMando.Xbox => "Xbox",
            _ => "Genérico",
        };
        string emulando = MotorControlador.Activo
            ? $" · funcionando como {(MotorControlador.Objetivo == TipoVirtual.DualShock4 ? "PS4" : "Xbox 360")}"
              + (MotorControlador.MandoRealOculto ? " (original oculto)" : "")
            : "";
        var punto = new System.Windows.Shapes.Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = Ui.Recurso("SystemFillColorSuccessBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };
        var insignia = new Pila { Orientation = Orientation.Horizontal };
        insignia.Children.Add(punto);
        insignia.Children.Add(Ui.Insignia("MANDO CONECTADO", Ui.Hex("#3FBF7F")));
        return new FilaAjuste
        {
            Glyph = "\uE7FC",
            Titulo = m.Nombre,
            Descripcion = $"{tipo} · {m.TextoConexion} · conectado desde las {m.Desde:HH:mm}",
            Nota = "Funcionando" + emulando,
            ColorNota = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"],
            Accion = insignia,
        };
    }

    private async void Ds4_Click(object sender, RoutedEventArgs e)
    {
        if (_ocupado)
        {
            return;
        }
        _ocupado = true;
        BtnDs4.IsEnabled = false;
        BtnDs4.Content = "Instalando…";
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Instalando DS4Windows…";
        Estado.IsOpen = true;

        string salida = await Aether.InstalarWinget("Ryochan7.DS4Windows");

        bool ok = !salida.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
        BtnDs4.IsEnabled = true;
        BtnDs4.Content = ok ? "Instalado" : "Reintentar";
        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = ok ? "DS4Windows instalado o ya estaba al día." : "No se pudo instalar DS4Windows.";
        Estado.IsOpen = true;
        _ocupado = false;
    }
}
