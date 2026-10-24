namespace EscuelaInventario.Migracion.Modelos;

public class MovimientoPreparadoMigracion
{
    public string Placa { get; set; } = string.Empty;

    public string TipoMovimientoCodigo { get; set; } = string.Empty;

    public DateTime? FechaMovimiento { get; set; }

    public int? AreaAnteriorNumero { get; set; }

    public int? AreaNuevaNumero { get; set; }

    public string? UbicacionAnteriorOriginal { get; set; }

    public string? UbicacionNuevaOriginal { get; set; }

    public string? EstadoAnteriorNombre { get; set; }

    public string? EstadoNuevoNombre { get; set; }

    public string? Motivo { get; set; }

    public string? Observacion { get; set; }

    public bool RequiereRevision { get; set; }

    public string? MotivoRevision { get; set; }

    public bool EsAnotacionCompartida { get; set; }

    public bool RecibeRegistroLibro { get; set; }

    public List<PosicionLibroPreparadaMigracion> PosicionesLibro { get; set; }
        = new();
}