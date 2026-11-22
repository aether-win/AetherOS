using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CentroAether.Controles;
using CentroAether.Services;
using Microsoft.Win32;

namespace CentroAether;

/// <summary>
/// Asistente de bienvenida del primer inicio de sesión (reemplaza al de PowerShell): modo,
/// Modo Batería automático, tema, perfil Wind-Aether, carpeta de red y respaldo.
/// Oscuro por completo y con la barra de título oscura.
/// </summary>
public partial class VentanaBienvenida : Window
{
    private const string Clave = @"Software\AetherOS\Centro";
    private readonly DispatcherTimer _vigiaWindhawk = new() { Interval = TimeSpan.FromSeconds(3) };

    public VentanaBienvenida()
    {
        InitializeComponent();
        Tema.Mica(this);
        MostrarPaso(1);
        if (TemaWindowsClaro())
        {
            RbClaro.IsChecked = true;
        }
        ActualizarWindhawk();
        // Windhawk se instala al primer inicio de sesión, a veces después de abrir esta ventana:
        // se revisa cada pocos segundos para activar las opciones en cuanto esté listo.
        _vigiaWindhawk.Tick += (_, _) => ActualizarWindhawk();
        _vigiaWindhawk.Start();
        Closed += (_, _) => _vigiaWindhawk.Stop();
    }

    /// <summary>¿Ya se vio (o se saltó) la bienvenida?</summary>
    public static bool YaVista()
    {
        try
        {
            using RegistryKey? k = Registry.CurrentUser.OpenSubKey(Clave);
            return Convert.ToInt32(k?.GetValue("BienvenidaVista") ?? 0) == 1;
        }
        catch
        {
            return true; // ante la duda, no molestar
        }
    }

    private static void MarcarVista()
    {
        try
        {
            using RegistryKey k = Registry.CurrentUser.CreateSubKey(Clave);
            k.SetValue("BienvenidaVista", 1, RegistryValueKind.DWord);
        }
        catch { /* sin permisos: se volverá a mostrar */ }
    }

    private static bool TemaWindowsClaro()
    {
        try
        {
            using RegistryKey? k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(k?.GetValue("AppsUseLightTheme") ?? 0) == 1;
        }
        catch
        {
            return false;
        }
    }

    internal static string? RutaWindhawk()
    {
        foreach (string? b in new[] { Environment.GetEnvironmentVariable("ProgramFiles"), Environment.GetEnvironmentVariable("ProgramFiles(x86)") })
        {
            if (string.IsNullOrEmpty(b))
            {
                continue;
            }
            string exe = Path.Combine(b, "Windhawk", "windhawk.exe");
            if (File.Exists(exe))
            {
                return exe;
            }
        }
        return null;
    }

    private void ActualizarWindhawk()
    {
        bool cli = Aether.WindhawkInstalado();
        bool presente = RutaWindhawk() is not null;
        RbGlass.IsEnabled = cli;
        RbLimpio.IsEnabled = cli;
        BtnAbrirWindhawk.Visibility = presente && !cli ? Visibility.Visible : Visibility.Collapsed;
        if (cli)
        {
            TxtWindNota.Text = "Windhawk está listo. Elige un perfil (puedes cambiarlo después en Aether Toolbox › Wind-Aether).";
            _vigiaWindhawk.Stop();
        }
        else if (presente)
        {
            TxtWindNota.Text = "Windhawk está instalado, pero esta versión no permite aplicar los perfiles automáticamente " +
                               "(eso llega con Windhawk 2.0). Puedes importarlos a mano en Windhawk › Ajustes › Importar.";
        }
        else
        {
            TxtWindNota.Text = "Windhawk se está instalando en segundo plano; estas opciones se activan solas cuando termine.";
        }
    }

    private void AbrirWindhawk_Click(object sender, RoutedEventArgs e)
    {
        string? exe = RutaWindhawk();
        if (exe is not null)
        {
            try { Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true }); } catch { /* no se pudo abrir */ }
        }
    }

    private void Saltar_Click(object sender, RoutedEventArgs e)
    {
        MarcarVista();
        Close();
    }

    private static string Esc(string s) => s.Replace("'", "''");

    private int _paso = 1;
    private static readonly string[] NombresPasos = { "Modo de uso", "Apariencia", "Red de trabajo", "Listo" };

    private void MostrarPaso(int paso)
    {
        _paso = paso;
        Paso1.Visibility = paso == 1 ? Visibility.Visible : Visibility.Collapsed;
        Paso2.Visibility = paso == 2 ? Visibility.Visible : Visibility.Collapsed;
        Paso3.Visibility = paso == 3 ? Visibility.Visible : Visibility.Collapsed;
        Paso4.Visibility = paso == 4 ? Visibility.Visible : Visibility.Collapsed;
        BtnAtras.Visibility = paso > 1 ? Visibility.Visible : Visibility.Collapsed;
        BtnListo.Content = paso == 4 ? "Aplicar" : "Siguiente";

        ListaPasos.Children.Clear();
        for (int i = 0; i < NombresPasos.Length; i++)
        {
            bool actual = i + 1 == paso, hecho = i + 1 < paso;
            var fila = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 16) };
            var punto = new System.Windows.Controls.Border
            {
                Width = 28, Height = 28, CornerRadius = new CornerRadius(14),
                Background = new System.Windows.Media.SolidColorBrush(actual ? Ui.Hex("#2F8CFF") : hecho ? Ui.Hex("#22A06B") : Ui.Hex("#1A2C48")),
                Child = new System.Windows.Controls.TextBlock
                {
                    Text = hecho ? "" : (i + 1).ToString(),
                    FontFamily = new System.Windows.Media.FontFamily(hecho ? "Segoe Fluent Icons, Segoe MDL2 Assets" : "Segoe UI"),
                    FontSize = hecho ? 12 : 13, FontWeight = FontWeights.SemiBold,
                    Foreground = System.Windows.Media.Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                },
            };
            fila.Children.Add(punto);
            fila.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = NombresPasos[i], Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center,
                FontWeight = actual ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = actual ? System.Windows.Media.Brushes.White : new System.Windows.Media.SolidColorBrush(Ui.Hex("#8FA6C1")),
            });
            ListaPasos.Children.Add(fila);
        }

        if (paso == 4)
        {
            string modo = RbGaming.IsChecked == true ? "Gaming" : RbBateria.IsChecked == true ? "Batería" : "Trabajo";
            string wind = RbGlass.IsChecked == true ? "Aether Glass" : RbLimpio.IsChecked == true ? "Aether Limpio" : "Ninguno";
            string red = TxtRuta.Text.Trim().Length > 0 ? TxtRuta.Text.Trim() + "  →  Z:" : "Sin carpeta de red";
            TxtResumen.Text =
                $"•  Modo: {modo}{(ChkAuto.IsChecked == true ? "  (Batería sola al 30 %)" : "")}\n" +
                $"•  Tema de Windows: {(RbClaro.IsChecked == true ? "Claro" : "Oscuro")}\n" +
                $"•  Wind-Aether: {wind}\n" +
                $"•  Red: {red}" +
                (ChkRespaldo.IsChecked == true ? "\n•  Abrir Respaldo al terminar" : "");
        }
    }

    private void Atras_Click(object sender, RoutedEventArgs e)
    {
        if (_paso > 1)
        {
            MostrarPaso(_paso - 1);
        }
    }

    private void Siguiente_Click(object sender, RoutedEventArgs e)
    {
        if (_paso < 4)
        {
            MostrarPaso(_paso + 1);
            return;
        }
        Aplicar();
    }

    private async void Aplicar()
    {
        BtnListo.IsEnabled = false;
        BtnAtras.IsEnabled = false;
        BtnSaltar.IsEnabled = false;
        Cursor = Cursors.Wait;
        TxtResultado.Text = "Aplicando…";
        var avisos = new List<string>();
        try
        {
            await Aether.GuardarAjuste("BateriaAuto", ChkAuto.IsChecked == true ? 1 : 0);

            string modo = RbGaming.IsChecked == true ? "Gaming" : RbBateria.IsChecked == true ? "Bateria" : "Trabajo";
            (bool ok, string mensaje) = await Aether.CambiarModoDetallado(modo);
            if (!ok)
            {
                avisos.Add(mensaje);
            }

            await Aether.AplicarTema(RbClaro.IsChecked == true ? "claro" : "oscuro");

            string ruta = TxtRuta.Text.Trim();
            if (ruta.Length > 0)
            {
                string r = await Aether.Ejecutar(
                    $"try {{ if (Add-AetherUnidadRed -Letra 'Z' -Ruta '{Esc(ruta)}') {{ 'RED|OK' }} else {{ 'RED|PENDIENTE' }} }} catch {{ 'RED|ERROR|' + $_.Exception.Message }}");
                if (r.Contains("RED|PENDIENTE"))
                {
                    avisos.Add("La carpeta de red quedó guardada; se conectará como Z: cuando la otra PC esté encendida.");
                }
                else if (r.Contains("RED|ERROR|"))
                {
                    avisos.Add("Carpeta de red: " + r[(r.IndexOf("RED|ERROR|", StringComparison.Ordinal) + 10)..].Trim());
                }
            }

            // Wind-Aether: Windhawk prepara cada mod (los propios se compilan) y eso tarda; se hace
            // en segundo plano y se avisa al terminar, sin dejar esta ventana esperando.
            if (RbGlass.IsChecked == true || RbLimpio.IsChecked == true)
            {
                string perfil = RbGlass.IsChecked == true ? "aether-glass" : "aether-limpio";
                string nombre = RbGlass.IsChecked == true ? "Aether Glass" : "Aether Limpio";
                _ = Task.Run(async () =>
                {
                    string r = (await Aether.Ejecutar($"Set-AetherPerfilWind -Perfil {perfil}")).Trim();
                    Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                        SegundoPlano.Notificar(r == "Perfil aplicado." ? $"Wind-Aether: {nombre} aplicado." : $"Wind-Aether: {r}")));
                });
            }

            MarcarVista();
            if (ChkRespaldo.IsChecked == true && Application.Current is App app)
            {
                app.MostrarCentro();
                (Application.Current.MainWindow as MainWindow)?.IrA("respaldo");
            }

            if (avisos.Count == 0)
            {
                Close();
                return;
            }
            TxtResultado.Text = "Listo, con avisos:\n• " + string.Join("\n• ", avisos);
            BtnSaltar.Content = "Cerrar";
            BtnSaltar.IsEnabled = true;
        }
        catch (Exception ex)
        {
            TxtResultado.Text = "Error: " + ex.Message;
            BtnListo.IsEnabled = true;
            BtnAtras.IsEnabled = true;
            BtnSaltar.IsEnabled = true;
        }
        finally
        {
            Cursor = null;
        }
    }
}
