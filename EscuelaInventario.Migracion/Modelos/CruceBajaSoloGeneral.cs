namespace EscuelaInventario.Migracion.Modelos;

public class CruceBajaSoloGeneral
{
    public string Placa { get; set; } = string.Empty;

    public int? FilaGeneral { get; set; }

    public string UbicacionGeneralOriginal { get; set; } = string.Empty;

    public bool TieneBajaHistorica { get; set; }

    public string MotivoBaja { get; set; } = string.Empty;

    public string FechaBajaOriginal { get; set; } = string.Empty;

    public DateTime? FechaBaja { get; set; }

    public int CantidadBajasEncontradas { get; set; }

    public bool RequiereRevision { get; set; }

    public string Observacion { get; set; } = string.Empty;
}

public class ResumenCruceBajasSoloGeneral
{
    public List<CruceBajaSoloGeneral> Resultados { get; set; } = new();

    public int TotalSoloGeneral =>
        Resultados.Count;

    public int ExplicadosPorBaja =>
        Resultados.Count(
            x => x.TieneBajaHistorica
        );

    public int SinBajaHistorica =>
        Resultados.Count(
            x => !x.TieneBajaHistorica
        );

    public int ConMultiplesBajas =>
        Resultados.Count(
            x => x.CantidadBajasEncontradas > 1
        );

    public int RequierenRevision =>
        Resultados.Count(
            x => x.RequiereRevision
        );
}