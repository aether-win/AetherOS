using CentroAether.Services;
using System;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using CentroAether.Controles;

namespace CentroAether.Pages;

public sealed partial class RedPage : PaginaBase
{
    public RedPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        TxtEquipo.Text = Aether.NombreEquipo;
        if (CmbLetra.Items.Count == 0)
        {
            foreach (char l in "ZYXWVUTSRQPONMLKJIHG")
            {
                CmbLetra.Items.Add(l + ":");
            }
            CmbLetra.SelectedIndex = 0;
        }
        _ = CargarUnidades();
    }

    private sealed record Unidad(string Letra, string Ruta);

    private async System.Threading.Tasks.Task CargarUnidades()
    {
        string json = await Aether.Ejecutar("ConvertTo-Json -Compress -InputObject @(Get-AetherUnidades)");
        PanelUnidades.Children.Clear();
        Unidad[] lista = Array.Empty<Unidad>();
        try
        {
            string limpio = json.Trim();
            int i = limpio.IndexOf('[');
            if (i >= 0)
            {
                lista = JsonSerializer.Deserialize<Unidad[]>(limpio[i..], new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? lista;
            }
        }
        catch { /* sin unidades */ }

        if (lista.Length == 0)
        {
            PanelUnidades.Children.Add(new TextBlock
            {
                Text = "Todavía no hay unidades de red fijas.",
                Foreground = (Brush)FindResource("TextFillColorSecondaryBrush"),
                Margin = new Thickness(4, 4, 0, 0),
            });
            return;
        }
        foreach (Unidad u in lista.OrderBy(x => x.Letra))
        {
            var quitar = new Button { Content = "Quitar", Tag = u.Letra };
            quitar.Click += QuitarUnidad_Click;
            PanelUnidades.Children.Add(new FilaAjuste
            {
                Glyph = "\uE8CE",
                Titulo = u.Letra + ":  " + u.Ruta,
                Descripcion = "Se reconecta sola al iniciar sesión y cada minuto si se cae.",
                Accion = quitar,
            });
        }
    }

    private async void AgregarUnidad_Click(object sender, RoutedEventArgs e)
    {
        string ruta = TxtRutaRed.Text.Trim().Replace("'", "''");
        string letra = (CmbLetra.SelectedItem as string ?? "Z:").TrimEnd(':');
        if (ruta.Length == 0)
        {
            return;
        }
        BtnAgregarUnidad.IsEnabled = false;
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Conectando la unidad...";
        Estado.IsOpen = true;
        string r = await Aether.Ejecutar(
            $"try {{ if (Add-AetherUnidadRed -Letra '{letra}' -Ruta '{ruta}') {{ 'RED|OK' }} else {{ 'RED|PENDIENTE' }} }} catch {{ 'RED|ERROR|' + $_.Exception.Message }}");
        BtnAgregarUnidad.IsEnabled = true;
        if (r.Contains("RED|OK"))
        {
            Estado.Severity = InfoBarSeverity.Success;
            Estado.Message = $"Unidad {letra}: conectada.";
            TxtRutaRed.Text = "";
        }
        else if (r.Contains("RED|PENDIENTE"))
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = $"Unidad {letra}: guardada. Se conectará cuando la otra PC esté encendida y compartiendo.";
            TxtRutaRed.Text = "";
        }
        else
        {
            int i = r.IndexOf("RED|ERROR|", StringComparison.Ordinal);
            Estado.Severity = InfoBarSeverity.Error;
            Estado.Message = i >= 0 ? r[(i + 10)..].Trim() : "No se pudo agregar la unidad.";
        }
        Estado.IsOpen = true;
        await CargarUnidades();
    }

    private async void QuitarUnidad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string letra)
        {
            await Aether.Ejecutar($"Remove-AetherUnidadRed -Letra '{letra}'");
            await CargarUnidades();
        }
    }

    private void Reparar_Click(object sender, RoutedEventArgs e)
    {
        // El reparador hace preguntas (otra PC, reiniciar): se abre en su propia consola.
        bool ok = Herramientas.AbrirReparador();
        Estado.Severity = ok ? InfoBarSeverity.Informational : InfoBarSeverity.Error;
        Estado.Message = ok ? "Se abrió el reparador de red en su ventana. Sigue lo que te pregunta." : "No se encontró el reparador de red.";
        Estado.IsOpen = true;
    }

    private void Probar_Click(object sender, RoutedEventArgs e)
    {
        string equipo = TxtProbarEquipo.Text.Trim();
        if (equipo.Length == 0)
        {
            Estado.Severity = InfoBarSeverity.Warning;
            Estado.Message = "Escribe el nombre o la IP de la otra PC.";
        }
        else
        {
            bool ok = Herramientas.AbrirReparador(equipo);
            Estado.Severity = ok ? InfoBarSeverity.Informational : InfoBarSeverity.Error;
            Estado.Message = ok ? $"Probando la conexión con {equipo} en la ventana del reparador." : "No se encontró el reparador de red.";
        }
        Estado.IsOpen = true;
    }

    private void GuardarReparador_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string carpeta = Herramientas.GuardarReparadorEnEscritorio();
            Estado.Severity = InfoBarSeverity.Success;
            Estado.Message = $"Listo: copia la carpeta '{carpeta}' a la otra PC y abre Reparar-Red-Aether.cmd.";
        }
        catch (Exception ex)
        {
            Estado.Severity = InfoBarSeverity.Error;
            Estado.Message = "No se pudo copiar: " + ex.Message;
        }
        Estado.IsOpen = true;
    }

    private async void Reconectar_Click(object sender, RoutedEventArgs e)
    {
        string r = (await Herramientas.ReconectarUnidades()).Trim();
        Estado.Severity = r.Contains("FALTAN|") ? InfoBarSeverity.Warning : InfoBarSeverity.Success;
        Estado.Message = r.Contains("FALTAN|") ? "Sin conectar: " + r[(r.IndexOf("FALTAN|", StringComparison.Ordinal) + 7)..] : "Unidades conectadas.";
        Estado.IsOpen = true;
    }

    private async void Privada_Click(object sender, RoutedEventArgs e)
    {
        Estado.Severity = InfoBarSeverity.Informational;
        Estado.Message = "Haciendo privada la red...";
        Estado.IsOpen = true;

        await Aether.HacerRedPrivada();

        Estado.Severity = InfoBarSeverity.Success;
        Estado.Message = "Las redes conectadas quedaron como privadas.";
        Estado.IsOpen = true;
    }

    private async void Compartir_Click(object sender, RoutedEventArgs e)
    {
        await Aether.AbrirUsoCompartido();
    }
}
