using System;
using System.Collections.Generic;

namespace CentroAether.Models;

/// <summary>
/// Describe un módulo del Centro Aether (Toolbox). Un módulo "disponible" tiene
/// una página real; uno sin página se muestra como "Próximamente" con su lista
/// de funciones planeadas. Así el shell refleja toda la visión y agregar un
/// módulo nuevo es solo registrarlo.
/// </summary>
public sealed class Modulo
{
    public string Id { get; }
    public string Nombre { get; }
    public string Descripcion { get; }
    public string Categoria { get; }
    public string Glyph { get; }
    public Type? Pagina { get; }
    public IReadOnlyList<string> Caracteristicas { get; }

    /// <summary>Un módulo está disponible cuando ya tiene página real.</summary>
    public bool Disponible => Pagina is not null;

    public Modulo(
        string id,
        string nombre,
        string descripcion,
        string categoria,
        string glyph,
        Type? pagina,
        params string[] caracteristicas)
    {
        Id = id;
        Nombre = nombre;
        Descripcion = descripcion;
        Categoria = categoria;
        Glyph = glyph;
        Pagina = pagina;
        Caracteristicas = caracteristicas ?? Array.Empty<string>();
    }
}
