using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AetherTransfer.Views;

/// <summary>Pequeño cuadro de texto modal, construido en código (tema oscuro).</summary>
public static class InputBox
{
    public static string? Pedir(Window propietario, string titulo, string valor)
    {
        var ventana = new Window
        {
            Title = titulo,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = propietario,
            ResizeMode = ResizeMode.NoResize,
            Background = new SolidColorBrush(Color.FromRgb(0x0F, 0x20, 0x36)),
            WindowStyle = WindowStyle.ToolWindow,
        };

        var raiz = new StackPanel { Margin = new Thickness(18) };

        raiz.Children.Add(new TextBlock
        {
            Text = titulo,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xEE, 0xF8)),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12),
        });

        var caja = new TextBox { Text = valor };
        caja.SelectAll();
        raiz.Children.Add(caja);

        var fila = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0),
        };

        string? resultado = null;

        var btnOk = new Button { Content = "Aceptar", Width = 96, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        btnOk.Click += (_, _) => { resultado = caja.Text.Trim(); ventana.DialogResult = true; };

        var btnCancel = new Button
        {
            Content = "Cancelar",
            Width = 96,
            IsCancel = true,
            Background = new SolidColorBrush(Color.FromRgb(0x12, 0x28, 0x44)),
        };
        btnCancel.Click += (_, _) => { ventana.DialogResult = false; };

        fila.Children.Add(btnOk);
        fila.Children.Add(btnCancel);
        raiz.Children.Add(fila);

        ventana.Content = raiz;
        caja.Focus();

        return ventana.ShowDialog() == true ? resultado : null;
    }
}
