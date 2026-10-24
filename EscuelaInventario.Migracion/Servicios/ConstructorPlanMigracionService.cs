using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class ConstructorPlanMigracionService
{
    public PlanMigracion Construir(
        List<ActivoPreparadoMigracion> activosPreparados,
        List<ActivoHistoricoReconstruido> activosHistoricosReconstruidos,
        List<FilaHistoricaInventario> registrosHistoricos,
        List<MovimientoHistoricoDetectado> movimientosHistoricos)
    {
        var plan =
            new PlanMigracion();

        // ============================================
        // ACTIVOS
        // ============================================

        plan.ActivosActuales =
            activosPreparados.Count;

        plan.ActivosHistoricosReconstruidos =
            activosHistoricosReconstruidos.Count;

        plan.TotalActivos =
            plan.ActivosActuales +
            plan.ActivosHistoricosReconstruidos;

        plan.ActivosSituacionActivo =
            activosPreparados.Count(
                x =>
                x.SituacionPreparada.Equals(
                    "ACTIVO",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            +
            activosHistoricosReconstruidos.Count(
                x =>
                x.SituacionPreparada.Equals(
                    "ACTIVO",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        plan.ActivosSituacionBaja =
            activosPreparados.Count(
                x =>
                x.SituacionPreparada.Equals(
                    "DADO_DE_BAJA",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            +
            activosHistoricosReconstruidos.Count(
                x =>
                x.SituacionPreparada.Equals(
                    "DADO_DE_BAJA",
                    StringComparison.OrdinalIgnoreCase
                )
            );

        plan.ActivosQueRequierenRevision =
            activosPreparados.Count(
                x => x.RequiereRevision
            );

        plan.HistoricosReconstruidosQueRequierenRevision =
            activosHistoricosReconstruidos.Count(
                x => x.RequiereRevision
            );

        // ============================================
        // PLACAS FINALES
        // ============================================

        var placasFinales =
            activosPreparados
                .Select(
                    x => new
                    {
                        x.Placa,
                        Origen = "INVENTARIO_GENERAL"
                    }
                )
                .Concat(
                    activosHistoricosReconstruidos
                        .Select(
                            x => new
                            {
                                x.Placa,
                                Origen =
                                    "HISTORICO_RECONSTRUIDO"
                            }
                        )
                )
                .ToList();

        var placasDuplicadas =
            placasFinales
                .GroupBy(
                    x => x.Placa,
                    StringComparer.OrdinalIgnoreCase
                )
                .Where(
                    grupo =>
                    grupo.Count() > 1
                )
                .ToList();

        plan.PlacasDuplicadasFinales =
            placasDuplicadas.Count;

        foreach (
            var grupo
            in placasDuplicadas
        )
        {
            string origenes =
                string.Join(
                    ", ",
                    grupo.Select(
                        x => x.Origen
                    )
                );

            plan.DetallePlacasDuplicadas.Add(
                $"{grupo.Key} -> {origenes}"
            );
        }

        var conjuntoPlacasFinales =
            placasFinales
                .Select(
                    x => x.Placa
                )
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase
                );

        // ============================================
        // POSICIONES DE ACTIVOS ACTUALES
        // ============================================

        var posicionesActuales =
            activosPreparados
                .Where(
                    x =>
                    x.Tomo.HasValue &&
                    x.Folio.HasValue &&
                    x.Asiento.HasValue
                )
                .Select(
                    x => new
                    {
                        x.Placa,
                        Tomo =
                            x.Tomo!.Value,
                        Folio =
                            x.Folio!.Value,
                        Asiento =
                            x.Asiento!.Value
                    }
                )
                .ToList();

        var posicionesActualesDuplicadas =
            posicionesActuales
                .GroupBy(
                    x => new
                    {
                        x.Tomo,
                        x.Folio,
                        x.Asiento
                    }
                )
                .Where(
                    grupo =>
                    grupo.Count() > 1
                )
                .ToList();

        plan.PosicionesActualesDuplicadas =
            posicionesActualesDuplicadas.Count;

        foreach (
            var grupo
            in posicionesActualesDuplicadas
        )
        {
            string placas =
                string.Join(
                    ", ",
                    grupo.Select(
                        x => x.Placa
                    )
                );

            plan.DetallePosicionesActualesDuplicadas.Add(
                $"T{grupo.Key.Tomo}/" +
                $"F{grupo.Key.Folio}/" +
                $"A{grupo.Key.Asiento} -> " +
                placas
            );
        }

        // ============================================
        // POSICIONES HISTÓRICAS OCUPADAS
        // ============================================

        var posicionesHistoricas =
            registrosHistoricos
                .Where(
                    x =>
                    x.TieneContenidoReal &&
                    x.Tomo.HasValue &&
                    x.Folio.HasValue &&
                    x.Asiento.HasValue
                )
                .Select(
                    x => new
                    {
                        Tomo =
                            x.Tomo!.Value,
                        Folio =
                            x.Folio!.Value,
                        Asiento =
                            x.Asiento!.Value,
                        x.NumeroFilaExcel
                    }
                )
                .ToList();

        var posicionesHistoricasDuplicadas =
            posicionesHistoricas
                .GroupBy(
                    x => new
                    {
                        x.Tomo,
                        x.Folio,
                        x.Asiento
                    }
                )
                .Where(
                    grupo =>
                    grupo.Count() > 1
                )
                .ToList();

        plan.PosicionesHistoricasDuplicadas =
            posicionesHistoricasDuplicadas.Count;

        foreach (
            var grupo
            in posicionesHistoricasDuplicadas
        )
        {
            string filas =
                string.Join(
                    ", ",
                    grupo.Select(
                        x => x.NumeroFilaExcel
                    )
                );

            plan.DetallePosicionesHistoricasDuplicadas.Add(
                $"T{grupo.Key.Tomo}/" +
                $"F{grupo.Key.Folio}/" +
                $"A{grupo.Key.Asiento} -> " +
                $"filas {filas}"
            );
        }

        // ============================================
        // CONFLICTOS ACTUAL ↔ HISTÓRICO
        // ============================================

        var conjuntoPosicionesHistoricas =
            posicionesHistoricas
                .Select(
                    x => (
                        x.Tomo,
                        x.Folio,
                        x.Asiento
                    )
                )
                .ToHashSet();

        var conflictosActualHistorico =
            posicionesActuales
                .Where(
                    x =>
                    conjuntoPosicionesHistoricas.Contains(
                        (
                            x.Tomo,
                            x.Folio,
                            x.Asiento
                        )
                    )
                )
                .ToList();

        plan.ConflictosActualHistorico =
            conflictosActualHistorico.Count;

        foreach (
            var conflicto
            in conflictosActualHistorico
        )
        {
            plan.DetalleConflictosActualHistorico.Add(
                $"{conflicto.Placa} -> " +
                $"T{conflicto.Tomo}/" +
                $"F{conflicto.Folio}/" +
                $"A{conflicto.Asiento}"
            );
        }

        // ============================================
        // MOVIMIENTOS HISTÓRICOS
        // ============================================

        plan.MovimientosHistoricosDetectados =
            movimientosHistoricos.Count;

        plan.MovimientosSinPlaca =
            movimientosHistoricos.Count(
                x =>
                x.Placas.Count == 0
            );

        var placasHistoricasReferenciadas =
            movimientosHistoricos
                .SelectMany(
                    x => x.Placas
                )
                .Where(
                    placa =>
                    !string.IsNullOrWhiteSpace(
                        placa
                    )
                )
                .Select(
                    placa =>
                    placa.Trim()
                )
                .Distinct(
                    StringComparer.OrdinalIgnoreCase
                )
                .ToList();

        var placasSinActivoFinal =
            placasHistoricasReferenciadas
                .Where(
                    placa =>
                    !conjuntoPlacasFinales.Contains(
                        placa
                    )
                )
                .OrderBy(
                    placa => placa
                )
                .ToList();

        plan.ReferenciasPlacaSinActivoFinal =
            placasSinActivoFinal.Count;

        plan.PlacasReferenciadasSinActivo =
            placasSinActivoFinal;

        // ============================================
        // BLOQUEOS
        // ============================================

        plan.TieneIncidenciasBloqueantes =
            plan.PlacasDuplicadasFinales > 0 ||
            plan.PosicionesHistoricasDuplicadas > 0 ||
            plan.ConflictosActualHistorico > 0 ||
            plan.MovimientosSinPlaca > 0 ||
            plan.ReferenciasPlacaSinActivoFinal > 0;

        // Las posiciones duplicadas entre activos
        // actuales ya son incidencias conocidas.
        //
        // No se corrigen automáticamente aquí.
        // Se reportan aparte para decidir cómo
        // insertarlas cuando construyamos el importador.

        return plan;
    }
}