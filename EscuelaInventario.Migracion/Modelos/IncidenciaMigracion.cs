namespace EscuelaInventario.Migracion.Modelos;

public class IncidenciaMigracion
{
    public string Tipo { get; set; } = string.Empty;

    public string Severidad { get; set; } = string.Empty;

    public string Placa { get; set; } = string.Empty;

    public int? NumeroFilaExcel { get; set; }

    public string Mensaje { get; set; } = string.Empty;
}