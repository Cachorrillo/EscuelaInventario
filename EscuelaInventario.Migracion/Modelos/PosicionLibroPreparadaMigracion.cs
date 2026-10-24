namespace EscuelaInventario.Migracion.Modelos;

public class PosicionLibroPreparadaMigracion
{
    public int Tomo { get; set; }

    public int Folio { get; set; }

    public int Asiento { get; set; }

    public string? TextoOriginal { get; set; }

    public bool EsPrincipal { get; set; }
}