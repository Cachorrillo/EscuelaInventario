namespace EscuelaInventario.Migracion.Modelos;

public class FilaHistoricaInventario
{
    public int NumeroFilaExcel { get; set; }

    public string RegistradoEnOriginal { get; set; } = string.Empty;

    public string IdentificacionOriginal { get; set; } = string.Empty;

    public string DescripcionOriginal { get; set; } = string.Empty;

    public string ObservacionesOriginales { get; set; } = string.Empty;

    public int? Tomo { get; set; }

    public int? Folio { get; set; }

    public int? Asiento { get; set; }

    // Indica si la fila tiene información real
    // además de la numeración Tomo/Folio/Asiento.
    //
    // Se revisan todas las columnas 2 a 13.
    public bool TieneContenidoReal { get; set; }
}