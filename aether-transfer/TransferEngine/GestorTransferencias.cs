using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AetherTransfer.Models;
using AetherTransfer.Protocols;

namespace AetherTransfer.TransferEngine;

/// <summary>
/// Cola de transferencias secuencial con progreso, velocidad, pausar/reanudar y
/// cancelar. La reanudación usa el modo Resume del servidor cuando es posible.
/// </summary>
public sealed class GestorTransferencias
{
    public ObservableCollection<TransferItem> Items { get; } = new();

    private readonly Func<IServidorRemoto?> _servidor;
    private readonly Action _alCompletar;
    private bool _procesando;

    public GestorTransferencias(Func<IServidorRemoto?> servidor, Action alCompletar)
    {
        _servidor = servidor;
        _alCompletar = alCompletar;
    }

    public void Encolar(TransferItem item)
    {
        Items.Add(item);
        _ = Procesar();
    }

    private async Task Procesar()
    {
        if (_procesando)
        {
            return;
        }
        _procesando = true;
        try
        {
            TransferItem? siguiente;
            while ((siguiente = Items.FirstOrDefault(i => i.Estado == EstadoTransfer.EnCola)) is not null)
            {
                await Ejecutar(siguiente);
            }
        }
        finally
        {
            _procesando = false;
        }
    }

    private async Task Ejecutar(TransferItem item)
    {
        IServidorRemoto? srv = _servidor();
        if (srv is null || !srv.Conectado)
        {
            item.Error = "Sin conexión";
            item.Estado = EstadoTransfer.Fallido;
            return;
        }

        item.Cts = new CancellationTokenSource();
        item.Estado = EstadoTransfer.Transfiriendo;
        var progreso = new Progress<ProgresoTransfer>(p =>
        {
            item.Progreso = p.Porcentaje;
            item.VelBps = p.VelocidadBps;
        });

        try
        {
            if (item.Subida)
            {
                await srv.SubirAsync(item.RutaLocal, item.RutaRemota, progreso, item.Reanudar, item.Cts.Token);
            }
            else
            {
                await srv.DescargarAsync(item.RutaRemota, item.RutaLocal, progreso, item.Reanudar, item.Cts.Token);
            }
            item.Progreso = 100;
            item.VelBps = 0;
            item.Estado = EstadoTransfer.Completado;
            _alCompletar();
        }
        catch (OperationCanceledException)
        {
            item.VelBps = 0;
            item.Estado = item.PausaSolicitada ? EstadoTransfer.Pausado : EstadoTransfer.Cancelado;
        }
        catch (Exception ex)
        {
            item.VelBps = 0;
            item.Error = ex.Message;
            item.Estado = EstadoTransfer.Fallido;
        }
    }

    public void Pausar(TransferItem item)
    {
        if (item.Estado == EstadoTransfer.Transfiriendo)
        {
            item.PausaSolicitada = true;
            item.Reanudar = true;       // al reanudar, continuar desde lo descargado
            item.Cts?.Cancel();
        }
    }

    public void Reanudar(TransferItem item)
    {
        if (item.Estado is EstadoTransfer.Pausado or EstadoTransfer.Fallido or EstadoTransfer.Cancelado)
        {
            item.PausaSolicitada = false;
            item.Estado = EstadoTransfer.EnCola;
            _ = Procesar();
        }
    }

    public void Cancelar(TransferItem item)
    {
        item.PausaSolicitada = false;
        item.Reanudar = false;
        if (item.Estado == EstadoTransfer.Transfiriendo)
        {
            item.Cts?.Cancel();
        }
        else if (item.Estado is EstadoTransfer.EnCola or EstadoTransfer.Pausado)
        {
            item.Estado = EstadoTransfer.Cancelado;
        }
    }

    public void Quitar(TransferItem item)
    {
        Cancelar(item);
        Items.Remove(item);
    }
}
