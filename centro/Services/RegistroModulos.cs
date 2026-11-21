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
        new("biblioteca", "Biblioteca", "Tus juegos detectados en un solo lugar",
            "Juegos", "", null,
            "Detección de Steam, Epic y Xbox",
            "Portadas y lanzamiento rápido",
            "Aplica el modo Gaming al iniciar un juego"),
        new("control", "Control", "Aether Controller: soporte de mandos",
            "Juegos", "", null,
            "DualShock 4, DualSense, Switch Pro",
            "Emulación de mando Xbox 360",
            "Perfiles por juego",
            "Basado en DS4Windows (GPLv3, acreditado)"),
        new("perfiles", "Perfiles", "Ajustes automáticos por aplicación o juego",
            "Juegos", "", null,
            "Perfil que se aplica solo al abrir una app",
            "Modo y ajustes guardados por juego",
            "Vuelve al estado normal al cerrar"),

        // ── Trabajo ──────────────────────────────────────────────────────────
        new("oficina", "Oficina", "Protección de Office y modo trabajo",
            "Trabajo", "", null,
            "Macros de Office seguras por defecto",
            "Sin transparencias en Word, Excel y PowerPoint",
            "Modo enfocado para concentrarse"),

        // ── Software ─────────────────────────────────────────────────────────
        new("software", "Software", "Catálogo de aplicaciones (winget)",
            "Software", "", null,
            "Instala y actualiza apps con un clic",
            "Catálogo curado y confiable",
            "Estado de instalación en vivo"),
        new("runtimes", "Runtimes", "VC++, DirectX, .NET, WebView2 y Python",
            "Software", "", null,
            "Detecta qué runtimes faltan",
            "Instala binarios oficiales de Microsoft",
            "Modo sin internet opcional"),

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
            "Avanzado", "", null,
            "Cargar módulos de terceros",
            "API segura y con permisos",
            "Activar y desactivar sin reiniciar"),
        new("compatibilidad", "Compatibilidad", "Base de datos de compatibilidad",
            "Avanzado", "", null,
            "Qué apps funcionan tras los ajustes",
            "Advertencias conocidas",
            "Recomendaciones por equipo"),
    };

    public static Modulo? Buscar(string id) => Todos.FirstOrDefault(m => m.Id == id);
}
