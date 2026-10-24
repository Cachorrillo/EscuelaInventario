namespace EscuelaInventario.Migracion.Modelos;

public class ActivoIdentificacionIrregular
{
    public int NumeroFilaExcel { get; set; }

    public string IdentificacionOriginal { get; set; }
        = string.Empty;

    public string Descripcion { get; set; }
        = string.Empty;

    public string Observaciones { get; set; }
        = string.Empty;

    public int? Tomo { get; set; }

    public int? Folio { get; set; }

    public int? Asiento { get; set; }

    public string TipoIrregularidad { get; set; }
        = string.Empty;

    public string? PlacaSugerida { get; set; }

    public bool RequiereRevision { get; set; }
        = true;
}