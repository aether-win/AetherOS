using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AetherTransfer.Views;

/// <summary>Cuadro de confirmación Sí/No con el tema oscuro de la app.</summary>
public static class Confirmacion
{
    public static bool Pedir(Window propietario, string mensaje, string titulo = "Confirmar")
    {
        var v = new Window
        {
            Title = titulo,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = propietario,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x1E, 0x34)),
        };

        var raiz = new StackPanel { Margin = new Thickness(20) };

        var cab = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 14) };
        cab.Children.Add(new TextBlock
        {
            Text = "",
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 20,
            Foreground = new SolidColorBrush(Color.FromRgb(0xF0, 0xB4, 0x29)),
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
        });
        cab.Children.Add(new TextBlock
        {
            Text = titulo,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xEE, 0xF8)),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        });
        raiz.Children.Add(cab);

        raiz.Children.Add(new TextBlock
        {
            Text = mensaje,
            Foreground = new SolidColorBrush(Color.FromRgb(0xC7, 0xD6, 0xEC)),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13.5,
        });

        var fila = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0),
        };

        bool resultado = false;

        var si = new Button { Content = "Sí", Width = 92, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        si.Click += (_, _) => { resultado = true; v.DialogResult = true; };

        var no = new Button
        {
            Content = "No",
            Width = 92,
            IsCancel = true,
            Background = new SolidColorBrush(Color.FromRgb(0x12, 0x28, 0x44)),
        };
        no.Click += (_, _) => { v.DialogResult = false; };

        fila.Children.Add(si);
        fila.Children.Add(no);
        raiz.Children.Add(fila);

        v.Content = raiz;
        Tema.BarraOscura(v);
        v.ShowDialog();
        return resultado;
    }
}
