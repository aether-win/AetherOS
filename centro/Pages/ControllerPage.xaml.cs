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
        Buscar();
        RefrescarMotor();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _diag.Stop();
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

        TglEmular.IsEnabled = vigem;
        CmbObjetivo.IsEnabled = vigem && !MotorControlador.Activo;   // no cambiar destino en caliente
        PanelRequisitos.Visibility = vigem ? Visibility.Collapsed : Visibility.Visible;

        if (!vigem)
        {
            FilaEmular.Descripcion = "Falta el driver ViGEmBus. Instálalo para activar la emulación.";
        }
        else
        {
            string sony = MotorControlador.HaySony() ? " Detecté un mando Sony (PS4/PS5)." : "";
            FilaEmular.Descripcion = MotorControlador.Activo
                ? "Activa: tu mando se refleja como un Xbox 360 virtual." + sony
                : "Lista para activar. Convierte tu mando en un Xbox 360 virtual." + sony;
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

        string salida = await Aether.InstalarWinget(wingetId);

        bool ok = !salida.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
        btn.IsEnabled = true;
        btn.Content = original;
        Estado.Severity = ok ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = ok
            ? $"{nombre} instalado. Puede que necesites reiniciar para que el driver cargue."
            : $"No se pudo instalar {nombre}.";
        Estado.IsOpen = true;
        _ocupado = false;
        RefrescarMotor();
    }

    private void Refrescar_Click(object sender, RoutedEventArgs e) => Buscar();

    private void Buscar()
    {
        Contenedor.Children.Clear();
        List<MandoInfo> mandos = MandosServicio.Detectar();
        Vacio.Visibility = mandos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        foreach (MandoInfo m in mandos)
        {
            Contenedor.Children.Add(CrearTarjeta(m));
        }
    }

    private FilaAjuste CrearTarjeta(MandoInfo m)
    {
        return new FilaAjuste
        {
            Glyph = "\uE7FC",
            Titulo = m.Nombre,
            Descripcion = $"{m.Marca} · {m.Protocolo}",
            Nota = m.EsGamepad ? "Conectado · compatible con emulación" : "Conectado",
            ColorNota = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"],
            Accion = Ui.Insignia(m.EsGamepad ? "Listo" : "Conectado", Ui.Hex("#3FBF7F")),
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
