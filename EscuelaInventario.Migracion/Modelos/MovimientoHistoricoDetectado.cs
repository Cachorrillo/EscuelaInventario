namespace EscuelaInventario.Migracion.Modelos;

public class MovimientoHistoricoDetectado
{
    // Puede haber una o varias placas asociadas
    // a una misma anotación histórica.
    public List<string> Placas { get; set; }
        = new();

    // Se mantiene temporalmente para facilitar
    // reportes y compatibilidad con el código actual.
    // Representa la primera placa detectada.
    public string PlacaPrincipal =>
        Placas.FirstOrDefault() ?? string.Empty;

    public string TipoMovimiento { get; set; }
        = string.Empty;

    public string TextoCompleto { get; set; }
        = string.Empty;

    public List<FilaHistoricaInventario> RegistrosLibro { get; set; }
        = new();

    public int? AreaAnterior { get; set; }

    public int? AreaNueva { get; set; }

    public DateTime? FechaMovimiento { get; set; }

    public bool RequiereRevision { get; set; }

    public string MotivoRevision { get; set; }
        = string.Empty;
}