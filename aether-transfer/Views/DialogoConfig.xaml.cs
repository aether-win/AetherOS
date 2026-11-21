using AetherTransfer.Settings;
using Microsoft.UI.Xaml.Controls;

namespace AetherTransfer.Views;

public sealed partial class DialogoConfig : ContentDialog
{
    public DialogoConfig()
    {
        this.InitializeComponent();
        ConfigApp.Datos d = ConfigApp.Actual;
        CLimite.Text = d.LimiteVelocidadKBps.ToString();
        CTimeout.Text = d.TimeoutSegundos.ToString();
        CConfirmar.IsOn = d.ConfirmarEliminar;
        this.PrimaryButtonClick += (_, _) => Guardar();
    }

    private void Guardar()
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
        d.ConfirmarEliminar = CConfirmar.IsOn;
        ConfigApp.Guardar(d);
    }
}
