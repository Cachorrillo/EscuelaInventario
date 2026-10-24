namespace EscuelaInventario.Migracion.Modelos;

public class FilaAreaInventario
{
    public int NumeroAreaArchivo { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;

    public string NombreHoja { get; set; } = string.Empty;

    public int NumeroFilaExcel { get; set; }

    public string RegistroLibroOriginal { get; set; } = string.Empty;

    public string PlacaOriginal { get; set; } = string.Empty;

    public string PlacaNormalizada { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string Serie { get; set; } = string.Empty;

    public string EstadoOriginal { get; set; } = string.Empty;

    public string UbicacionOriginal { get; set; } = string.Empty;

    public string ModoAdquisicionOriginal { get; set; } = string.Empty;

    public string PrecioOriginal { get; set; } = string.Empty;

    public string Observaciones { get; set; } = string.Empty;

    public string FechaAdquisicionOriginal { get; set; } = string.Empty;
}