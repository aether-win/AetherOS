using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CentroAether.Controles;

public enum InfoBarSeverity
{
    Informational,
    Success,
    Warning,
    Error,
}

/// <summary>
/// Barra de aviso (equivalente al InfoBar de WinUI) con el tema oscuro de Aether:
/// ícono y franja de color según la gravedad, mensaje, botón de acción opcional y
/// botón de cerrar. Se oculta por completo cuando IsOpen es false.
/// </summary>
public class InfoBar : Border
{
    private readonly FontIcon _icono = new() { FontSize = 16, Margin = new Thickness(0, 1, 12, 0), VerticalAlignment = VerticalAlignment.Top };
    private readonly TextBlock _titulo = new() { FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 2), TextWrapping = TextWrapping.Wrap };
    private readonly TextBlock _mensaje = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ContentControl _accion = new() { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
    private readonly Button _cerrar;

    private bool _isOpen;
    private bool _isClosable = true;
    private InfoBarSeverity _severity = InfoBarSeverity.Informational;
    private string _message = "";
    private string _title = "";
    private UIElement? _actionButton;

    public InfoBar()
    {
        CornerRadius = new CornerRadius(8);
        BorderThickness = new Thickness(4, 1, 1, 1);
        Padding = new Thickness(14, 10, 8, 10);

        _cerrar = new Button
        {
            Content = new FontIcon { Glyph = "", FontSize = 11 },
            Style = (Style)Application.Current.Resources["BotonPlanoStyle"],
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            ToolTip = "Cerrar",
        };
        _cerrar.Click += (_, _) => IsOpen = false;

        var textos = new Pila { VerticalAlignment = VerticalAlignment.Center };
        textos.Children.Add(_titulo);
        textos.Children.Add(_mensaje);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(_icono, 0);
        Grid.SetColumn(textos, 1);
        Grid.SetColumn(_accion, 2);
        Grid.SetColumn(_cerrar, 3);
        grid.Children.Add(_icono);
        grid.Children.Add(textos);
        grid.Children.Add(_accion);
        grid.Children.Add(_cerrar);
        Child = grid;

        Actualizar();
    }

    public bool IsOpen
    {
        get => _isOpen;
        set { _isOpen = value; Actualizar(); }
    }

    public bool IsClosable
    {
        get => _isClosable;
        set { _isClosable = value; Actualizar(); }
    }

    public InfoBarSeverity Severity
    {
        get => _severity;
        set { _severity = value; Actualizar(); }
    }

    public string Message
    {
        get => _message;
        set { _message = value ?? ""; Actualizar(); }
    }

    public string Title
    {
        get => _title;
        set { _title = value ?? ""; Actualizar(); }
    }

    /// <summary>Botón opcional a la derecha del mensaje (p. ej. "Instalar Windhawk").</summary>
    public UIElement? ActionButton
    {
        get => _actionButton;
        set { _actionButton = value; _accion.Content = value; Actualizar(); }
    }

    private void Actualizar()
    {
        Visibility = _isOpen ? Visibility.Visible : Visibility.Collapsed;
        _cerrar.Visibility = _isClosable ? Visibility.Visible : Visibility.Collapsed;
        _accion.Visibility = _actionButton is null ? Visibility.Collapsed : Visibility.Visible;
        _titulo.Text = _title;
        _titulo.Visibility = string.IsNullOrEmpty(_title) ? Visibility.Collapsed : Visibility.Visible;
        _mensaje.Text = _message;

        (string glyph, string clave) = _severity switch
        {
            InfoBarSeverity.Success => ("", "SystemFillColorSuccessBrush"),
            InfoBarSeverity.Warning => ("", "SystemFillColorCautionBrush"),
            InfoBarSeverity.Error => ("", "SystemFillColorCriticalBrush"),
            _ => ("", "AccentFillColorDefaultBrush"),
        };
        var color = Application.Current?.TryFindResource(clave) as Brush ?? Brushes.DodgerBlue;
        _icono.Glyph = glyph;
        _icono.Foreground = color;
        BorderBrush = color;
        Background = Application.Current?.TryFindResource("InfoBarFondoBrush") as Brush
            ?? new SolidColorBrush(Color.FromRgb(0x10, 0x22, 0x3A));
    }
}
