namespace EscuelaInventario.Migracion.Modelos;

public class IncidenciaArchivoArea
{
    public int NumeroAreaArchivo { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;

    public string NombreHoja { get; set; } = string.Empty;

    public int NumeroFilaExcel { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string ValorOriginal { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;
}