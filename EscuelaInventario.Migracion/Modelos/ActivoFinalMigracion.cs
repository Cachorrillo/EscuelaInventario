namespace EscuelaInventario.Migracion.Modelos;

public class ActivoFinalMigracion
{
    public string Placa { get; set; } = string.Empty;

    public List<int> FilasFuente { get; set; } = new();

    public string Descripcion { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;

    public string EstadoOriginal { get; set; } = string.Empty;
    public string? EstadoNormalizado { get; set; }

    public string SituacionPreparada { get; set; } = string.Empty;
    public string ClasificacionSituacion { get; set; } = string.Empty;

    public string UbicacionOriginal { get; set; } = string.Empty;

    public int? AreaActualNumero { get; set; }

    public string? UbicacionHistorica { get; set; }

    public string FuenteAreaSeleccionada { get; set; } = string.Empty;

    public string ModoAdquisicionOriginal { get; set; } = string.Empty;

    public string? ModoAdquisicionNormalizado { get; set; }

    public string PrecioOriginal { get; set; } = string.Empty;

    public decimal? PrecioNormalizado { get; set; }

    public string FechaAdquisicionOriginal { get; set; } = string.Empty;

    public DateTime? FechaAdquisicionNormalizada { get; set; }

    public string ObservacionesOriginales { get; set; } = string.Empty;

    public int? TomoOriginal { get; set; }

    public int? FolioOriginal { get; set; }

    public int? AsientoOriginal { get; set; }

    // Si es false, el activo sí se migra,
    // pero NO se crea un RegistroLibro de inclusión
    // usando la posición original conflictiva.
    public bool RegistrarPosicionLibroInclusion { get; set; } = true;

    // Conserva las posiciones que aparecieron en la fuente,
    // aunque no puedan insertarse como posiciones estructuradas.
    public List<string> PosicionesLibroConflictivas { get; set; }
        = new();

    public bool RequiereRevision { get; set; }

    public List<string> MotivosRevision { get; set; }
        = new();

    public string OrigenMigracion { get; set; } = string.Empty;
}