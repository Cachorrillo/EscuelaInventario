namespace EscuelaInventario.Migracion.Modelos;

public class ActivoPreparadoMigracion
{
    // ============================================
    // IDENTIFICACIÓN Y ORIGEN
    // ============================================

    public string Placa { get; set; } = string.Empty;

    public int NumeroFilaExcel { get; set; }

    // ============================================
    // DATOS GENERALES DEL ACTIVO
    // ============================================

    public string Descripcion { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string Serie { get; set; } = string.Empty;

    // ============================================
    // ESTADO FÍSICO
    // ============================================

    public string EstadoOriginal { get; set; } = string.Empty;

    public string? EstadoNormalizado { get; set; }

    // ============================================
    // SITUACIÓN ADMINISTRATIVA
    // ============================================

    public string SituacionPreparada { get; set; }
        = string.Empty;

    public string ClasificacionSituacion { get; set; }
        = string.Empty;

    // ============================================
    // UBICACIÓN / ÁREA
    // ============================================

    public string UbicacionOriginal { get; set; } = string.Empty;

    public int? AreaActualNumero { get; set; }

    public string? UbicacionHistorica { get; set; }

    public string FuenteAreaSeleccionada { get; set; }
        = string.Empty;

    // ============================================
    // MODO DE ADQUISICIÓN
    // ============================================

    public string ModoAdquisicionOriginal { get; set; }
        = string.Empty;

    public string? ModoAdquisicionNormalizado { get; set; }

    // ============================================
    // PRECIO
    // ============================================

    public string PrecioOriginal { get; set; }
        = string.Empty;

    public decimal? PrecioNormalizado { get; set; }

    public string ClasificacionPrecio { get; set; }
        = string.Empty;

    // ============================================
    // FECHA DE ADQUISICIÓN
    // ============================================

    public string FechaAdquisicionOriginal { get; set; }
        = string.Empty;

    public DateTime? FechaAdquisicionNormalizada { get; set; }

    public string ClasificacionFechaAdquisicion { get; set; }
        = string.Empty;

    // ============================================
    // OBSERVACIONES
    // ============================================

    public string ObservacionesOriginales { get; set; }
        = string.Empty;

    // ============================================
    // REGISTRO DE LIBRO
    // ============================================

    public int? Tomo { get; set; }

    public int? Folio { get; set; }

    public int? Asiento { get; set; }

    // ============================================
    // CONTROL DE MIGRACIÓN
    // ============================================

    public bool RequiereRevision { get; set; }

    public string MotivoRevision { get; set; }
        = string.Empty;

    public string ClasificacionOrigen { get; set; }
        = string.Empty;

    public List<string> MotivosRevision { get; set; }
        = new();
}