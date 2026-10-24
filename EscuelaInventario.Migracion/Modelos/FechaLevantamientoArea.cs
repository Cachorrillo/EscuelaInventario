namespace EscuelaInventario.Migracion.Modelos;

public class FechaLevantamientoArea
{
    public int NumeroArea { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;

    public string NombreHoja { get; set; } = string.Empty;

    public string TituloOriginal { get; set; } = string.Empty;

    public string FechaOriginal { get; set; } = string.Empty;

    public DateTime? FechaLevantamiento { get; set; }

    public bool RequiereRevision { get; set; }

    public string MotivoRevision { get; set; } = string.Empty;
}