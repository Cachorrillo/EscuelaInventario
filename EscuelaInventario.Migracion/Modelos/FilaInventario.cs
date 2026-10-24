namespace EscuelaInventario.Migracion.Modelos;

public class FilaInventario
{
    public int NumeroFilaExcel { get; set; }

    public string RegistradoEn { get; set; } = string.Empty;

    public string Placa { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string Serie { get; set; } = string.Empty;

    public string EstadoOriginal { get; set; } = string.Empty;

    public string? EstadoNormalizado { get; set; }

    public string UbicacionOriginal { get; set; } = string.Empty;

    public string ModoAdquisicionOriginal { get; set; } = string.Empty;

    public string? ModoAdquisicionNormalizado { get; set; }

    public string PrecioOriginal { get; set; } = string.Empty;

    public decimal? PrecioNormalizado { get; set; }

    public string ObservacionesOriginales { get; set; } = string.Empty;

    public string FechaAdquisicionOriginal { get; set; } = string.Empty;

    public DateTime? FechaAdquisicionNormalizada { get; set; }

    public int? Tomo { get; set; }
    public int? Folio { get; set; }
    public int? Asiento { get; set; }
}