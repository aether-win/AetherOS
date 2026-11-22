using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CentroAether.Controles;

/// <summary>
/// Cuadrícula de datos en vivo (etiqueta pequeña + valor grande), para Batería, Salud y Monitor.
/// Se crea una vez y solo se actualizan los textos (sin parpadeo).
/// </summary>
public sealed class Estadisticas : WrapPanel
{
    private readonly Dictionary<string, TextBlock> _valores = new();

    public void Poner(string etiqueta, string valor)
    {
        if (!_valores.TryGetValue(etiqueta, out TextBlock? t))
        {
            t = new TextBlock { FontSize = 20, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 0) };
            var caja = new Border
            {
                Width = 210,
                Margin = new Thickness(0, 0, 12, 12),
                Padding = new Thickness(16, 12, 16, 12),
                CornerRadius = new CornerRadius(10),
                Background = (Brush)Application.Current.FindResource("TarjetaFondoBrush"),
                BorderBrush = (Brush)Application.Current.FindResource("CardStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1),
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = etiqueta,
                            FontSize = 12.5,
                            Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush"),
                        },
                        t,
                    },
                },
            };
            Children.Add(caja);
            _valores[etiqueta] = t;
        }
        t.Text = valor;
    }
}
