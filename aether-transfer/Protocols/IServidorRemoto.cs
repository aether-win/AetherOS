using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AetherTransfer.Models;

namespace AetherTransfer.Protocols;

/// <summary>Contrato común para cualquier servidor remoto (FTP, FTPS, SFTP).</summary>
public interface IServidorRemoto : IAsyncDisposable
{
    bool Conectado { get; }

    Task ConectarAsync(ConexionInfo info, CancellationToken ct = default);
    Task DesconectarAsync();

    Task<IReadOnlyList<ElementoRemoto>> ListarAsync(string ruta, CancellationToken ct = default);
    Task DescargarAsync(string rutaRemota, string rutaLocal, IProgress<ProgresoTransfer>? progreso = null, bool reanudar = false, CancellationToken ct = default);
    Task SubirAsync(string rutaLocal, string rutaRemota, IProgress<ProgresoTransfer>? progreso = null, bool reanudar = false, CancellationToken ct = default);
    Task CrearCarpetaAsync(string ruta, CancellationToken ct = default);
    Task EliminarAsync(string ruta, bool esCarpeta, CancellationToken ct = default);
    Task RenombrarAsync(string origen, string destino, CancellationToken ct = default);
}
