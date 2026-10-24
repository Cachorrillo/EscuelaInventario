namespace EscuelaInventario.Migracion.Modelos;

public class BajaHistoricaDetectada
{
    public List<string> Placas { get; set; }
        = new();

    public string TextoOriginal { get; set; }
        = string.Empty;

    public List<FilaHistoricaInventario> RegistrosLibro { get; set; }
        = new();

    // Fecha encontrada exactamente como aparece
    // en el registro histórico.
    public string FechaOriginal { get; set; }
        = string.Empty;

    // Fecha normalizada solo cuando puede
    // determinarse con seguridad.
    public DateTime? FechaMovimiento { get; set; }

    // Texto relacionado con la causa de la baja.
    // Se conserva para no perder información histórica.
    public string MotivoOriginal { get; set; }
        = string.Empty;

    // Clasificación técnica que obtendremos después.
    //
    // Posibles ejemplos:
    // INSERVIBILIDAD
    // ROBO
    // ERROR_PLAQUEO
    // OTRO
    // NO_DETERMINADO
    public string MotivoNormalizado { get; set; }
        = string.Empty;

    public bool RequiereRevision { get; set; }

    public string MotivoRevision { get; set; }
        = string.Empty;
}