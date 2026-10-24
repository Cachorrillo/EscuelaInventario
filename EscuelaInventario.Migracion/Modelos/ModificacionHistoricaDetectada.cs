namespace EscuelaInventario.Migracion.Modelos;

public class ModificacionHistoricaDetectada
{
    public List<string> Placas { get; set; }
        = new();

    public string TextoOriginal { get; set; }
        = string.Empty;

    public List<FilaHistoricaInventario> RegistrosLibro { get; set; }
        = new();

    // Tipo técnico de modificación detectada.
    //
    // Ejemplos posibles:
    // COLOR
    // MATERIAL
    // DESCRIPCION
    // CANTIDAD
    // ACCESORIO
    // UBICACION_DESCRIPTIVA
    // CORRECCION
    // OTRO
    public string TipoModificacion { get; set; }
        = string.Empty;

    // Valor anterior si puede extraerse con seguridad.
    public string ValorAnteriorOriginal { get; set; }
        = string.Empty;

    // Valor nuevo si puede extraerse con seguridad.
    public string ValorNuevoOriginal { get; set; }
        = string.Empty;

    public string FechaOriginal { get; set; }
        = string.Empty;

    public DateTime? FechaMovimiento { get; set; }

    public bool TieneMultiplesFechas { get; set; }

    public bool RequiereRevision { get; set; }

    public string MotivoRevision { get; set; }
        = string.Empty;
}