namespace EscuelaInventario.Migracion.Modelos;

public class CambioEstadoHistoricoDetectado
{
    public List<string> Placas { get; set; }
        = new();

    public string TextoOriginal { get; set; }
        = string.Empty;

    public List<FilaHistoricaInventario> RegistrosLibro { get; set; }
        = new();

    public string EstadoAnteriorOriginal { get; set; }
        = string.Empty;

    public string EstadoNuevoOriginal { get; set; }
        = string.Empty;

    public string? EstadoAnteriorNormalizado { get; set; }

    public string? EstadoNuevoNormalizado { get; set; }

    public string FechaOriginal { get; set; }
        = string.Empty;

    public DateTime? FechaMovimiento { get; set; }

    public bool RequiereRevision { get; set; }

    public string MotivoRevision { get; set; }
        = string.Empty;
}