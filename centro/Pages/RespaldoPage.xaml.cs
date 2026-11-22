using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using CentroAether.Controles;
using CentroAether.Services;
using Microsoft.Win32;

namespace CentroAether.Pages;

/// <summary>
/// Respaldo completo (igual que la versión clásica): carpetas y archivos, destino, versiones,
/// respaldo diario programado y "Respaldar ahora". Misma configuración que usa
/// Respaldo-Aether.ps1: %APPDATA%\Aether\respaldo.json.
/// </summary>
public sealed partial class RespaldoPage : PaginaBase
{
    private sealed class Config
    {
        public List<string> Origenes { get; set; } = new();
        public string Destino { get; set; } = "";
        public int Versiones { get; set; } = 5;
        public string Hora { get; set; } = "20:00";
        public bool Activo { get; set; }
    }

    private static readonly string Carpeta = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Aether");
    private static readonly string Archivo = Path.Combine(Carpeta, "respaldo.json");
    private static readonly string Script = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
        "AtlasModules", "Aether", "Centro", "Respaldo-Aether.ps1");

    private Config _config = new();
    private bool _cargando;

    public RespaldoPage()
    {
        InitializeComponent();
        foreach (int n in new[] { 1, 2, 3, 5, 7, 10, 14, 30 })
        {
            CmbVersiones.Items.Add(n);
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _cargando = true;
        _config = Leer();
        CmbVersiones.SelectedItem = CmbVersiones.Items.Contains(_config.Versiones) ? _config.Versiones : 5;
        TxtHora.Text = _config.Hora;
        TglDiario.IsOn = _config.Activo;
        _cargando = false;
        Pintar();
    }

    private static Config Leer()
    {
        try
        {
            if (File.Exists(Archivo))
            {
                return JsonSerializer.Deserialize<Config>(File.ReadAllText(Archivo),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new Config();
            }
        }
        catch { /* configuración dañada: se empieza de nuevo */ }
        return new Config();
    }

    private void Guardar()
    {
        Directory.CreateDirectory(Carpeta);
        File.WriteAllText(Archivo, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string? Ajuste(string nombre)
    {
        try
        {
            using RegistryKey? k = Registry.CurrentUser.OpenSubKey(@"Software\AetherOS\Centro");
            return k?.GetValue(nombre)?.ToString();
        }
        catch { return null; }
    }

    private void Pintar()
    {
        PanelOrigenes.Children.Clear();
        if (_config.Origenes.Count == 0)
        {
            PanelOrigenes.Children.Add(new TextBlock
            {
                Text = "Todavía no elegiste nada para respaldar.",
                Foreground = (System.Windows.Media.Brush)FindResource("TextFillColorSecondaryBrush"),
                Margin = new Thickness(4, 2, 0, 4),
            });
        }
        foreach (string origen in _config.Origenes)
        {
            var quitar = new Button { Content = "Quitar", Tag = origen };
            quitar.Click += (_, _) => { _config.Origenes.Remove(origen); Guardar(); Pintar(); };
            PanelOrigenes.Children.Add(new FilaAjuste
            {
                Glyph = Directory.Exists(origen) ? "" : "",
                Titulo = Path.GetFileName(origen.TrimEnd('\\')) is { Length: > 0 } n ? n : origen,
                Descripcion = origen,
                Accion = quitar,
            });
        }
        FilaDestino.Descripcion = string.IsNullOrEmpty(_config.Destino) ? "Sin carpeta elegida." : _config.Destino;
        string ultimo = Ajuste("RespaldoUltimo") ?? "";
        string error = Ajuste("RespaldoError") ?? "";
        FilaUltimo.Descripcion = (ultimo.Length > 0 ? $"Último respaldo: {ultimo}." : "Todavía no hay respaldos.")
                                 + (error.Length > 0 ? $" Problema: {error}" : "");
        BtnRespaldar.IsEnabled = _config.Origenes.Count > 0 && _config.Destino.Length > 0;
    }

    private void AgregarCarpeta_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFolderDialog { Title = "Elige carpetas para respaldar", Multiselect = true };
        if (d.ShowDialog(Window.GetWindow(this)) == true)
        {
            foreach (string f in d.FolderNames.Where(f => !_config.Origenes.Contains(f)))
            {
                _config.Origenes.Add(f);
            }
            Guardar();
            Pintar();
        }
    }

    private void AgregarArchivos_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFileDialog { Title = "Elige archivos para respaldar", Multiselect = true };
        if (d.ShowDialog(Window.GetWindow(this)) == true)
        {
            foreach (string f in d.FileNames.Where(f => !_config.Origenes.Contains(f)))
            {
                _config.Origenes.Add(f);
            }
            Guardar();
            Pintar();
        }
    }

    private void Destino_Click(object sender, RoutedEventArgs e)
    {
        var d = new OpenFolderDialog { Title = "Elige dónde guardar los respaldos (por ejemplo, un disco externo)" };
        if (d.ShowDialog(Window.GetWindow(this)) == true && !string.IsNullOrEmpty(d.FolderName))
        {
            _config.Destino = d.FolderName;
            Guardar();
            Pintar();
        }
    }

    private void Opcion_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || CmbVersiones.SelectedItem is not int n)
        {
            return;
        }
        _config.Versiones = n;
        Guardar();
    }

    private bool HoraValida(out string hora)
    {
        hora = TxtHora.Text.Trim();
        if (!Regex.IsMatch(hora, @"^([01]?\d|2[0-3]):[0-5]\d$"))
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = "La hora debe ser como 20:00.";
            Estado.IsOpen = true;
            return false;
        }
        if (hora.Length == 4)
        {
            hora = "0" + hora;
        }
        return true;
    }

    private void Hora_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_cargando || !HoraValida(out string hora))
        {
            return;
        }
        _config.Hora = hora;
        Guardar();
        if (_config.Activo)
        {
            Programar(true);
        }
    }

    private void Diario_Toggled(object sender, RoutedEventArgs e)
    {
        if (_cargando)
        {
            return;
        }
        if (TglDiario.IsOn && !HoraValida(out _))
        {
            _cargando = true;
            TglDiario.IsOn = false;
            _cargando = false;
            return;
        }
        _config.Hora = TxtHora.Text.Trim();
        _config.Activo = TglDiario.IsOn;
        Guardar();
        Programar(_config.Activo);
    }

    /// <summary>Tarea diaria "Aether Respaldo" como este usuario (igual que la versión clásica).</summary>
    private void Programar(bool activo)
    {
        Correr("schtasks.exe", "/Delete /TN \"Aether Respaldo\" /F");
        if (!activo)
        {
            Estado.Severity = InfoBarSeverity.Informational;
            Estado.Message = "Respaldo diario desactivado.";
            Estado.IsOpen = true;
            return;
        }
        string accion = $"powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \\\"{Script}\\\"";
        int codigo = Correr("schtasks.exe", $"/Create /TN \"Aether Respaldo\" /SC DAILY /ST {_config.Hora} /TR \"{accion}\" /F");
        Estado.Severity = codigo == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        Estado.Message = codigo == 0 ? $"Respaldo diario programado a las {_config.Hora}." : "No se pudo programar el respaldo diario.";
        Estado.IsOpen = true;
    }

    private static int Correr(string exe, string args)
    {
        try
        {
            using Process? p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = false, CreateNoWindow = true });
            p?.WaitForExit(15000);
            return p?.ExitCode ?? -1;
        }
        catch { return -1; }
    }

    private void Abrir_Click(object sender, RoutedEventArgs e)
    {
        string raiz = Path.Combine(_config.Destino, "Respaldos Aether");
        if (_config.Destino.Length > 0 && Directory.Exists(raiz))
        {
            Process.Start("explorer.exe", "\"" + raiz + "\"");
        }
        else
        {
            Estado.Severity = InfoBarSeverity.Informational;
            Estado.Message = "Todavía no hay respaldos en ese destino.";
            Estado.IsOpen = true;
        }
    }

    private async void Respaldar_Click(object sender, RoutedEventArgs e)
    {
        BtnRespaldar.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Haciendo el respaldo… puede tardar según cuántos archivos tengas.";
        Estado.IsOpen = true;
        await Aether.HacerRespaldo();
        string error = Ajuste("RespaldoError") ?? "";
        Estado.Severity = error.Length == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        Estado.Message = error.Length == 0 ? "Respaldo terminado." : "El respaldo terminó con problemas: " + error;
        Estado.IsOpen = true;
        Pintar();
    }
}
