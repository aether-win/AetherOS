using System.Windows;
using AetherTransfer.Settings;

namespace AetherTransfer.Views;

public partial class DialogoConfig : Window
{
    public DialogoConfig()
    {
        InitializeComponent();
        Tema.BarraOscura(this);
        ConfigApp.Datos d = ConfigApp.Actual;
        CLimite.Text = d.LimiteVelocidadKBps.ToString();
        CTimeout.Text = d.TimeoutSegundos.ToString();
        CConfirmar.IsChecked = d.ConfirmarEliminar;
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        ConfigApp.Datos d = ConfigApp.Actual;
        if (int.TryParse(CLimite.Text.Trim(), out int lim) && lim >= 0)
        {
            d.LimiteVelocidadKBps = lim;
        }
        if (int.TryParse(CTimeout.Text.Trim(), out int to) && to >= 5)
        {
            d.TimeoutSegundos = to;
        }
        d.ConfirmarEliminar = CConfirmar.IsChecked == true;
        ConfigApp.Guardar(d);
        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
