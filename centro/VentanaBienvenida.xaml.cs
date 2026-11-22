using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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

    private async void Listo_Click(object sender, RoutedEventArgs e)
    {
        BtnListo.IsEnabled = false;
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

            if (RbGlass.IsChecked == true || RbLimpio.IsChecked == true)
            {
                string perfil = RbGlass.IsChecked == true ? "aether-glass" : "aether-limpio";
                string r = (await Aether.Ejecutar($"Set-AetherPerfilWind -Perfil {perfil}")).Trim();
                if (!string.IsNullOrEmpty(r) && r != "Perfil aplicado.")
                {
                    avisos.Add("Wind-Aether: " + r);
                }
            }

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
            BtnSaltar.IsEnabled = true;
        }
        finally
        {
            Cursor = null;
        }
    }
}
