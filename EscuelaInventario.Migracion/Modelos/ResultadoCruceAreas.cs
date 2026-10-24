namespace EscuelaInventario.Migracion.Modelos;

public class ResultadoCruceArea
{
    public string Placa { get; set; } = string.Empty;

    public string Clasificacion { get; set; } = string.Empty;

    public int? AreaGeneral { get; set; }

    public List<int> AreasArchivos { get; set; } = new();

    public string UbicacionGeneralOriginal { get; set; } = string.Empty;

    public int? FilaGeneral { get; set; }

    public List<string> DetallesArchivosArea { get; set; } = new();

    public string Observacion { get; set; } = string.Empty;
}

public class ResumenCruceAreas
{
    public List<ResultadoCruceArea> Resultados { get; set; } = new();

    public int Coinciden =>
        Resultados.Count(
            x => x.Clasificacion == "COINCIDE"
        );

    public int AreaDiferente =>
        Resultados.Count(
            x => x.Clasificacion == "AREA_DIFERENTE"
        );

    public int SoloEnGeneral =>
        Resultados.Count(
            x => x.Clasificacion == "SOLO_EN_GENERAL"
        );

    public int SoloEnArchivosArea =>
        Resultados.Count(
            x => x.Clasificacion == "SOLO_EN_ARCHIVOS_AREA"
        );

    public int PresenteEnVariasAreas =>
        Resultados.Count(
            x => x.Clasificacion == "PRESENTE_EN_VARIAS_AREAS"
        );

    public int GeneralDuplicado =>
        Resultados.Count(
            x => x.Clasificacion == "GENERAL_DUPLICADO"
        );

    public int AreaGeneralNoNumerica =>
        Resultados.Count(
            x => x.Clasificacion == "AREA_GENERAL_NO_NUMERICA"
        );
}