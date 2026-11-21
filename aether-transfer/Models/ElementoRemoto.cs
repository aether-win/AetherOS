using System;

namespace AetherTransfer.Models;

/// <summary>Un archivo o carpeta en el servidor remoto (o en el sitio local).</summary>
public sealed class ElementoRemoto
{
    public string Nombre { get; init; } = "";
    public bool EsCarpeta { get; init; }
    public long Tamano { get; init; }
    public DateTime Modificado { get; init; }

    public string TamanoTexto => EsCarpeta ? "" : FormatearTamano(Tamano);
    public string Tipo => EsCarpeta ? "Carpeta" : ExtensionTipo(Nombre);

    private static string FormatearTamano(long bytes)
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

    private static string ExtensionTipo(string nombre)
    {
        int p = nombre.LastIndexOf('.');
        return p > 0 && p < nombre.Length - 1 ? nombre[(p + 1)..].ToUpperInvariant() : "Archivo";
    }
}
