namespace EscuelaInventario.Migracion.Modelos;

public class ResultadoDecisionAreaDiferente
{
    public string Placa { get; set; } = string.Empty;

    public int? FilaGeneral { get; set; }

    public int AreaGeneral { get; set; }

    public int AreaArchivo { get; set; }

    public DateTime? FechaLevantamiento { get; set; }

    public int? UltimaAreaHistoricaPrevia { get; set; }

    public string Clasificacion { get; set; } = string.Empty;

    public bool RequiereRevision { get; set; }

    public string Observacion { get; set; } = string.Empty;
}