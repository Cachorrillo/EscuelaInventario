namespace EscuelaInventario.Migracion.Modelos;

public class ActivoHistoricoReconstruido
{
    public string Placa { get; set; } = string.Empty;

    public string IdentificacionOriginal { get; set; }
        = string.Empty;

    public int NumeroFilaExcel { get; set; }

    public string Descripcion { get; set; }
        = string.Empty;

    public string Marca { get; set; }
        = string.Empty;

    public string Modelo { get; set; }
        = string.Empty;

    public string Serie { get; set; }
        = string.Empty;

    public string EstadoOriginal { get; set; }
        = string.Empty;

    public string? EstadoNormalizado { get; set; }

    public string UbicacionOriginal { get; set; }
        = string.Empty;

    public int? AreaOriginalNumero { get; set; }

    public int? AreaActualNumero { get; set; }

    public string? UbicacionHistorica { get; set; }

    public string FuenteAreaSeleccionada { get; set; }
        = string.Empty;

    public string ModoAdquisicionOriginal { get; set; }
        = string.Empty;

    public string? ModoAdquisicionNormalizado { get; set; }

    public string PrecioOriginal { get; set; }
        = string.Empty;

    public decimal? PrecioNormalizado { get; set; }

    public string FechaAdquisicionOriginal { get; set; }
        = string.Empty;

    public DateTime? FechaAdquisicionNormalizada { get; set; }

    public string ObservacionesOriginales { get; set; }
        = string.Empty;

    public int TomoOriginal { get; set; }

    public int FolioOriginal { get; set; }

    public int AsientoOriginal { get; set; }

    public string SituacionPreparada { get; set; }
        = string.Empty;

    public string ClasificacionSituacion { get; set; }
        = string.Empty;

    public bool RequiereRevision { get; set; }

    public string MotivoRevision { get; set; }
        = string.Empty;

    public string OrigenReconstruccion { get; set; }
        = "REGISTRO_HISTORICO_RECONSTRUIDO";
}