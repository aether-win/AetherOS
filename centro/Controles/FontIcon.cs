using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CentroAether.Controles;

/// <summary>
/// Ícono de la fuente de símbolos de Windows (Segoe Fluent Icons en Windows 11,
/// Segoe MDL2 Assets en Windows 10). Equivale al FontIcon de WinUI: se usa con
/// <c>Glyph="&amp;#xE7FC;"</c> en XAML o <c>Glyph = ""</c> en código.
/// </summary>
public class FontIcon : TextBlock
{
    private static readonly FontFamily FuenteIconos =
        new("Segoe Fluent Icons, Segoe MDL2 Assets");

    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(FontIcon),
        new PropertyMetadata("", (d, e) => ((FontIcon)d).Text = e.NewValue as string ?? ""));

    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public FontIcon()
    {
        FontFamily = FuenteIconos;
        FontSize = 16;
        FontWeight = FontWeights.Normal;
        VerticalAlignment = VerticalAlignment.Center;
        TextAlignment = TextAlignment.Center;
    }
}
