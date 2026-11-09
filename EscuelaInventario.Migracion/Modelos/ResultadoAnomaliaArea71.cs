namespace EscuelaInventario.Migracion.Modelos;

public class ResultadoAnomaliaArea71
{
    public string Placa { get; set; } = string.Empty;

    public string Clasificacion { get; set; } = string.Empty;

    public int? AreaActualNumero { get; set; }

    public string SituacionActual { get; set; } = string.Empty;

    public List<string> HojasOrigen { get; set; } = new();

    public List<int> FilasOrigen { get; set; } = new();

    public int CantidadAparicionesExcel { get; set; }

    public string Observacion { get; set; } = string.Empty;

    public DateTime? FechaUltimoRegistro { get; set; }

    public string OrigenFechaUltimoRegistro { get; set; }
        = string.Empty;
}