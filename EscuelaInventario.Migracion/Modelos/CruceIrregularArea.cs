namespace EscuelaInventario.Migracion.Modelos;

public class CruceIrregularArea
{
    public string IdentificacionGeneralOriginal { get; set; } = string.Empty;

    public string PlacaEquivalenteComparada { get; set; } = string.Empty;

    public int FilaGeneral { get; set; }

    public string DescripcionGeneral { get; set; } = string.Empty;

    public string TipoIrregularidad { get; set; } = string.Empty;

    public bool EncontradoEnArchivosArea { get; set; }

    public List<int> AreasEncontradas { get; set; } = new();

    public List<string> DetallesArchivosArea { get; set; } = new();

    public bool EraSoloEnArchivosArea { get; set; }

    public bool RequiereRevision { get; set; }

    public string Observacion { get; set; } = string.Empty;
}

public class ResumenCruceIrregularesAreas
{
    public List<CruceIrregularArea> Resultados { get; set; } = new();

    public int TotalIrregulares =>
        Resultados.Count;

    public int ConEquivalenteComparable =>
        Resultados.Count(
            x =>
            !string.IsNullOrWhiteSpace(
                x.PlacaEquivalenteComparada
            )
        );

    public int EncontradosEnAreas =>
        Resultados.Count(
            x =>
            x.EncontradoEnArchivosArea
        );

    public int NoEncontradosEnAreas =>
        Resultados.Count(
            x =>
            !x.EncontradoEnArchivosArea
        );

    public int ExplicanSoloEnArchivosArea =>
        Resultados.Count(
            x =>
            x.EraSoloEnArchivosArea
        );

    public int EncontradosEnVariasAreas =>
        Resultados.Count(
            x =>
            x.AreasEncontradas.Count > 1
        );
}