namespace EscuelaInventario.Migracion.Modelos;

public class TrasladoHistoricoDetectado
{
    public List<string> Placas { get; set; }
        = new();

    public string TextoOriginal { get; set; }
        = string.Empty;

    public List<FilaHistoricaInventario> RegistrosLibro { get; set; }
        = new();

    // Área anterior numérica, cuando el texto
    // realmente contiene un número de área.
    public int? AreaAnteriorNumero { get; set; }

    // Área nueva numérica, cuando el texto
    // realmente contiene un número de área.
    public int? AreaNuevaNumero { get; set; }

    // Texto original de ubicación anterior.
    // Ejemplos:
    // "Antigua Aula de TO"
    // "Aula #8"
    // "Área 20"
    public string UbicacionAnteriorOriginal { get; set; }
        = string.Empty;

    // Texto original de ubicación nueva.
    public string UbicacionNuevaOriginal { get; set; }
        = string.Empty;

    public DateTime? FechaMovimiento { get; set; }

    public string FechaOriginal { get; set; }
        = string.Empty;

    public bool RequiereRevision { get; set; }

    public string MotivoRevision { get; set; }
        = string.Empty;
}