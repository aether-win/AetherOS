using System.Windows;
using System.Windows.Controls.Primitives;

namespace CentroAether.Controles;

/// <summary>
/// Interruptor de encendido/apagado (equivalente al ToggleSwitch de WinUI): propiedad
/// IsOn (enlazable en dos sentidos), textos OnContent/OffContent y evento Toggled.
/// Toggled se dispara solo cuando el USUARIO lo cambia (clic o teclado); si IsOn se
/// cambia desde código o por un enlace de datos no se dispara, así cargar el estado
/// inicial nunca ejecuta acciones. La apariencia está en "ToggleSwitchStyle" (App.xaml).
/// </summary>
public class ToggleSwitch : ToggleButton
{
    public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
        nameof(IsOn), typeof(bool), typeof(ToggleSwitch),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, AlCambiarIsOn));

    public static readonly DependencyProperty OnContentProperty = DependencyProperty.Register(
        nameof(OnContent), typeof(object), typeof(ToggleSwitch),
        new PropertyMetadata(null, (d, _) => ((ToggleSwitch)d).ActualizarTexto()));

    public static readonly DependencyProperty OffContentProperty = DependencyProperty.Register(
        nameof(OffContent), typeof(object), typeof(ToggleSwitch),
        new PropertyMetadata(null, (d, _) => ((ToggleSwitch)d).ActualizarTexto()));

    public static readonly RoutedEvent ToggledEvent = EventManager.RegisterRoutedEvent(
        nameof(Toggled), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ToggleSwitch));

    private bool _sincronizando;

    public ToggleSwitch()
    {
        SetResourceReference(StyleProperty, "ToggleSwitchStyle");
        IsChecked = false;
    }

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    public object? OnContent
    {
        get => GetValue(OnContentProperty);
        set => SetValue(OnContentProperty, value);
    }

    public object? OffContent
    {
        get => GetValue(OffContentProperty);
        set => SetValue(OffContentProperty, value);
    }

    public event RoutedEventHandler Toggled
    {
        add => AddHandler(ToggledEvent, value);
        remove => RemoveHandler(ToggledEvent, value);
    }

    private static void AlCambiarIsOn(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var t = (ToggleSwitch)d;
        bool valor = (bool)e.NewValue;
        if (t.IsChecked != valor)
        {
            t._sincronizando = true;
            t.IsChecked = valor;
            t._sincronizando = false;
        }
        t.ActualizarTexto();
    }

    protected override void OnChecked(RoutedEventArgs e)
    {
        base.OnChecked(e);
        if (!_sincronizando)
        {
            IsOn = true;
            RaiseEvent(new RoutedEventArgs(ToggledEvent, this));
        }
    }

    protected override void OnUnchecked(RoutedEventArgs e)
    {
        base.OnUnchecked(e);
        if (!_sincronizando)
        {
            IsOn = false;
            RaiseEvent(new RoutedEventArgs(ToggledEvent, this));
        }
    }

    private void ActualizarTexto()
    {
        Content = IsOn ? OnContent : OffContent;
    }
}
