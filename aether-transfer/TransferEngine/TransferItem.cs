using System.ComponentModel;
using System.Threading;

namespace AetherTransfer.TransferEngine;

public enum EstadoTransfer
{
    EnCola,
    Transfiriendo,
    Pausado,
    Completado,
    Fallido,
    Cancelado,
}

/// <summary>Un elemento de la cola de transferencias, observable para la UI.</summary>
public sealed class TransferItem : INotifyPropertyChanged
{
    public string Nombre { get; init; } = "";
    public string RutaLocal { get; init; } = "";
    public string RutaRemota { get; init; } = "";
    public bool Subida { get; init; }         // true = local → remoto
    public long Tamano { get; init; }

    internal CancellationTokenSource? Cts;
    internal bool Reanudar;
    internal bool PausaSolicitada;
    public string Error { get; set; } = "";

    public string DireccionIcono => Subida ? "" : "";   // subir / descargar
    public string TamanoTexto => FormatBytes(Tamano);

    private double _progreso;
    public double Progreso
    {
        get => _progreso;
        set { _progreso = value; Raise(nameof(Progreso)); Raise(nameof(ProgresoTexto)); }
    }
    public string ProgresoTexto => $"{_progreso:0}%";

    private double _velBps;
    public double VelBps
    {
        set { _velBps = value; Raise(nameof(VelocidadTexto)); }
    }
    public string VelocidadTexto => _velBps > 1 ? FormatBytes((long)_velBps) + "/s" : "";

    private EstadoTransfer _estado = EstadoTransfer.EnCola;
    public EstadoTransfer Estado
    {
        get => _estado;
        set { _estado = value; Raise(nameof(Estado)); Raise(nameof(EstadoTexto)); }
    }

    public string EstadoTexto => _estado switch
    {
        EstadoTransfer.EnCola => "En cola",
        EstadoTransfer.Transfiriendo => "Transfiriendo…",
        EstadoTransfer.Pausado => "Pausado",
        EstadoTransfer.Completado => "Completado",
        EstadoTransfer.Cancelado => "Cancelado",
        EstadoTransfer.Fallido => string.IsNullOrEmpty(Error) ? "Fallido" : "Fallido: " + Error,
        _ => "",
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    public static string FormatBytes(long bytes)
    {
        string[] u = { "B", "KB", "MB", "GB", "TB" };
        double n = bytes;
        int i = 0;
        while (n >= 1024 && i < u.Length - 1)
        {
            n /= 1024;
            i++;
        }
        return i == 0 ? $"{bytes} B" : $"{n:0.0} {u[i]}";
    }
}
