namespace EscuelaInventario.Migracion.Modelos;

public class PlanMigracion
{
    public int ActivosActuales { get; set; }

    public int ActivosHistoricosReconstruidos { get; set; }

    public int TotalActivos { get; set; }

    public int ActivosSituacionActivo { get; set; }

    public int ActivosSituacionBaja { get; set; }

    public int PlacasDuplicadasFinales { get; set; }

    public List<string> DetallePlacasDuplicadas { get; set; }
        = new();

    public int PosicionesActualesDuplicadas { get; set; }

    public List<string> DetallePosicionesActualesDuplicadas { get; set; }
        = new();

    public int PosicionesHistoricasDuplicadas { get; set; }

    public List<string> DetallePosicionesHistoricasDuplicadas { get; set; }
        = new();

    public int ConflictosActualHistorico { get; set; }

    public List<string> DetalleConflictosActualHistorico { get; set; }
        = new();

    public int MovimientosHistoricosDetectados { get; set; }

    public int MovimientosSinPlaca { get; set; }

    public int ReferenciasPlacaSinActivoFinal { get; set; }

    public List<string> PlacasReferenciadasSinActivo { get; set; }
        = new();

    public int ActivosQueRequierenRevision { get; set; }

    public int HistoricosReconstruidosQueRequierenRevision { get; set; }

    public bool TieneIncidenciasBloqueantes { get; set; }
}