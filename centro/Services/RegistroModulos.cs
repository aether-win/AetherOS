using System.Collections.Generic;
using System.Linq;
using CentroAether.Models;
using CentroAether.Pages;

namespace CentroAether.Services;

/// <summary>
/// Registro central de todos los módulos del Centro Aether. El orden de esta
/// lista es el orden del menú; la propiedad Categoria agrupa los módulos bajo
/// encabezados. Los módulos con página = null aparecen como "Próximamente".
///
/// Esta es la columna vertebral de la Fase 2: toda la navegación y la visión de
/// los 23 módulos se declara aquí en un solo lugar.
/// </summary>
public static class RegistroModulos
{
    public static IReadOnlyList<Modulo> Todos { get; } = new List<Modulo>
    {
        // ── Inicio (sin categoría, va primero) ───────────────────────────────
        new("inicio", "Aether", "Resumen del sistema y tema", "", "", typeof(InicioPage)),

        // ── Rendimiento ──────────────────────────────────────────────────────
        new("modos", "Modos", "Motor de rendimiento: Gaming, Trabajo y Batería",
            "Rendimiento", "", typeof(ModosPage)),
        new("monitor", "Monitor", "CPU, RAM y uso del equipo en tiempo real",
            "Rendimiento", "", typeof(MonitorPage)),
        new("boost", "Smart Boost", "Limpieza y liberación de recursos",
            "Rendimiento", "", typeof(LimpiezaPage)),

        // ── Juegos ───────────────────────────────────────────────────────────
        new("biblioteca", "Biblioteca", "Tus juegos de Steam detectados",
            "Juegos", "", typeof(BibliotecaPage)),
        new("control", "Control", "Aether Controller: mandos conectados",
            "Juegos", "", typeof(ControllerPage)),
        new("perfiles", "Perfiles", "Modo por aplicación o juego",
            "Juegos", "", typeof(PerfilesPage)),

        // ── Trabajo ──────────────────────────────────────────────────────────
        new("oficina", "Oficina", "Protección de Office y modo trabajo",
            "Trabajo", "", typeof(OficinaPage)),

        // ── Software ─────────────────────────────────────────────────────────
        new("software", "Software", "Catálogo de aplicaciones (winget)",
            "Software", "", typeof(SoftwarePage)),
        new("runtimes", "Runtimes", "VC++, DirectX, .NET, WebView2 y Python",
            "Software", "", typeof(RuntimesPage)),

        // ── Sistema ──────────────────────────────────────────────────────────
        new("actualizar", "Actualizar", "Motor de actualizaciones de Windows",
            "Sistema", "", typeof(ActualizarPage)),
        new("respaldo", "Respaldo", "Smart Restore: puntos y copias de seguridad",
            "Sistema", "", typeof(RespaldoPage)),
        new("bateria", "Batería", "Energía y salud de la batería",
            "Sistema", "", typeof(BateriaPage)),
        new("salud", "Salud", "Estado general del sistema",
            "Sistema", "", typeof(SaludPage)),

        // ── Red ──────────────────────────────────────────────────────────────
        new("red", "Red", "Diagnóstico de red (sin evasión de controles)",
            "Red", "", typeof(RedPage)),

        // ── Personalización ──────────────────────────────────────────────────
        new("wind", "Wind-Aether", "Mods de Windhawk (desactivados por defecto)",
            "Personalización", "", typeof(WindPage)),

        // ── Avanzado ─────────────────────────────────────────────────────────
        new("plugins", "Plugins", "Sistema de extensiones de Aether",
            "Avanzado", "", typeof(PluginsPage)),
        new("compatibilidad", "Compatibilidad", "Cómo se comportan las apps con Aether",
            "Avanzado", "", typeof(CompatibilidadPage)),
    };

    public static Modulo? Buscar(string id) => Todos.FirstOrDefault(m => m.Id == id);
}
