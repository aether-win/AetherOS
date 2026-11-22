using System;
using System.Windows;
using System.Windows.Controls;

namespace CentroAether.Controles;

/// <summary>
/// Panel apilado con separación entre elementos (Spacing) y relleno (Padding),
/// como el StackPanel de WinUI. El StackPanel de WPF no tiene ninguna de las dos.
/// Los elementos ocultos (Collapsed) no suman separación.
/// </summary>
public class Pila : Panel
{
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(Pila),
        new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing), typeof(double), typeof(Pila),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(
        nameof(Padding), typeof(Thickness), typeof(Pila),
        new FrameworkPropertyMetadata(new Thickness(0), FrameworkPropertyMetadataOptions.AffectsMeasure));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    private bool Horizontal => Orientation == Orientation.Horizontal;

    protected override Size MeasureOverride(Size disponible)
    {
        Thickness p = Padding;
        double anchoLibre = Math.Max(0, disponible.Width - p.Left - p.Right);
        double altoLibre = Math.Max(0, disponible.Height - p.Top - p.Bottom);
        Size paraHijo = Horizontal
            ? new Size(double.PositiveInfinity, altoLibre)
            : new Size(anchoLibre, double.PositiveInfinity);

        double principal = 0, cruzado = 0;
        int visibles = 0;
        foreach (UIElement hijo in InternalChildren)
        {
            if (hijo is null)
            {
                continue;
            }
            hijo.Measure(paraHijo);
            if (hijo.Visibility == Visibility.Collapsed)
            {
                continue;
            }
            Size d = hijo.DesiredSize;
            principal += Horizontal ? d.Width : d.Height;
            cruzado = Math.Max(cruzado, Horizontal ? d.Height : d.Width);
            visibles++;
        }
        if (visibles > 1)
        {
            principal += Spacing * (visibles - 1);
        }

        return Horizontal
            ? new Size(principal + p.Left + p.Right, cruzado + p.Top + p.Bottom)
            : new Size(cruzado + p.Left + p.Right, principal + p.Top + p.Bottom);
    }

    protected override Size ArrangeOverride(Size final)
    {
        Thickness p = Padding;
        double ancho = Math.Max(0, final.Width - p.Left - p.Right);
        double alto = Math.Max(0, final.Height - p.Top - p.Bottom);
        double pos = Horizontal ? p.Left : p.Top;
        bool primero = true;

        foreach (UIElement hijo in InternalChildren)
        {
            if (hijo is null || hijo.Visibility == Visibility.Collapsed)
            {
                continue;
            }
            if (!primero)
            {
                pos += Spacing;
            }
            primero = false;

            Size d = hijo.DesiredSize;
            if (Horizontal)
            {
                hijo.Arrange(new Rect(pos, p.Top, d.Width, alto));
                pos += d.Width;
            }
            else
            {
                hijo.Arrange(new Rect(p.Left, pos, ancho, d.Height));
                pos += d.Height;
            }
        }
        return final;
    }
}
