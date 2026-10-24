using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class ClasificadorAreaDiferenteService
{
    public List<ResultadoDecisionAreaDiferente> Clasificar(
        ResumenCruceAreas cruceAreas,
        List<TrasladoHistoricoDetectado> trasladosHistoricos,
        List<FechaLevantamientoArea> fechasLevantamientoAreas)
    {
        var resultados =
            new List<ResultadoDecisionAreaDiferente>();

        var diferencias =
            cruceAreas.Resultados
                .Where(
                    x =>
                    x.Clasificacion == "AREA_DIFERENTE"
                )
                .ToList();

        foreach (var diferencia in diferencias)
        {
            if (
                !diferencia.AreaGeneral.HasValue ||
                diferencia.AreasArchivos.Count != 1
            )
            {
                resultados.Add(
                    new ResultadoDecisionAreaDiferente
                    {
                        Placa = diferencia.Placa,
                        FilaGeneral = diferencia.FilaGeneral,
                        AreaGeneral =
                            diferencia.AreaGeneral ?? 0,
                        AreaArchivo =
                            diferencia.AreasArchivos.FirstOrDefault(),
                        Clasificacion =
                            "DATOS_INSUFICIENTES",
                        RequiereRevision = true,
                        Observacion =
                            "No existe una única combinación de área general y archivo de área que permita analizar el caso."
                    }
                );

                continue;
            }

            int areaGeneral =
                diferencia.AreaGeneral.Value;

            int areaArchivo =
                diferencia.AreasArchivos.Single();

            var fechaArea =
                fechasLevantamientoAreas
                    .FirstOrDefault(
                        x =>
                        x.NumeroArea == areaArchivo
                    );

            if (
                fechaArea == null ||
                !fechaArea.FechaLevantamiento.HasValue
            )
            {
                resultados.Add(
                    new ResultadoDecisionAreaDiferente
                    {
                        Placa = diferencia.Placa,
                        FilaGeneral = diferencia.FilaGeneral,
                        AreaGeneral = areaGeneral,
                        AreaArchivo = areaArchivo,
                        Clasificacion =
                            "SIN_FECHA_LEVANTAMIENTO",
                        RequiereRevision = true,
                        Observacion =
                            "No existe una fecha de levantamiento utilizable para el archivo del área."
                    }
                );

                continue;
            }

            DateTime fechaLevantamiento =
                fechaArea.FechaLevantamiento.Value.Date;

            var trasladosNumericos =
                trasladosHistoricos
                    .Where(
                        traslado =>
                        traslado.Placas.Any(
                            placa =>
                            placa.Equals(
                                diferencia.Placa,
                                StringComparison.OrdinalIgnoreCase
                            )
                        )
                    )
                    .Where(
                        traslado =>
                        traslado.FechaMovimiento.HasValue &&
                        traslado.AreaAnteriorNumero.HasValue &&
                        traslado.AreaNuevaNumero.HasValue
                    )
                    .OrderBy(
                        traslado =>
                        traslado.FechaMovimiento
                    )
                    .ToList();

            // ============================================
            // TRASLADOS DEL MISMO DÍA
            // ============================================

            var trasladosMismoDia =
                trasladosNumericos
                    .Where(
                        traslado =>
                        traslado.FechaMovimiento!.Value.Date ==
                        fechaLevantamiento
                    )
                    .ToList();

            if (trasladosMismoDia.Count > 0)
            {
                resultados.Add(
                    new ResultadoDecisionAreaDiferente
                    {
                        Placa = diferencia.Placa,
                        FilaGeneral = diferencia.FilaGeneral,
                        AreaGeneral = areaGeneral,
                        AreaArchivo = areaArchivo,
                        FechaLevantamiento =
                            fechaLevantamiento,
                        Clasificacion =
                            "TRASLADO_MISMO_DIA",
                        RequiereRevision = true,
                        Observacion =
                            "Existe al menos un traslado el mismo día del levantamiento. " +
                            "No puede determinarse automáticamente si ocurrió antes o después del conteo físico."
                    }
                );

                continue;
            }

            // ============================================
            // ÚLTIMO TRASLADO ESTRICTAMENTE ANTERIOR
            // ============================================

            var ultimoTrasladoPrevio =
                trasladosNumericos
                    .Where(
                        traslado =>
                        traslado.FechaMovimiento!.Value.Date <
                        fechaLevantamiento
                    )
                    .LastOrDefault();

            if (ultimoTrasladoPrevio == null)
            {
                resultados.Add(
                    new ResultadoDecisionAreaDiferente
                    {
                        Placa = diferencia.Placa,
                        FilaGeneral = diferencia.FilaGeneral,
                        AreaGeneral = areaGeneral,
                        AreaArchivo = areaArchivo,
                        FechaLevantamiento =
                            fechaLevantamiento,
                        Clasificacion =
                            "SIN_HISTORIAL_PREVIO_NUMERICO",
                        RequiereRevision = true,
                        Observacion =
                            "No existe un traslado numérico anterior al levantamiento que permita respaldar alguna de las dos áreas."
                    }
                );

                continue;
            }

            int destinoPrevio =
                ultimoTrasladoPrevio
                    .AreaNuevaNumero!
                    .Value;

            // ============================================
            // HISTORIAL RESPALDA ARCHIVO DE ÁREA
            // ============================================

            if (destinoPrevio == areaArchivo)
            {
                resultados.Add(
                    new ResultadoDecisionAreaDiferente
                    {
                        Placa = diferencia.Placa,
                        FilaGeneral = diferencia.FilaGeneral,
                        AreaGeneral = areaGeneral,
                        AreaArchivo = areaArchivo,
                        FechaLevantamiento =
                            fechaLevantamiento,
                        UltimaAreaHistoricaPrevia =
                            destinoPrevio,
                        Clasificacion =
                            "AREA_RESPALDADA_POR_HISTORIAL_PREVIO",
                        RequiereRevision = false,
                        Observacion =
                            "El último traslado anterior al levantamiento y el archivo físico del área coinciden."
                    }
                );

                continue;
            }

            // ============================================
            // HISTORIAL RESPALDA INVENTARIO GENERAL
            // ============================================

            if (destinoPrevio == areaGeneral)
            {
                resultados.Add(
                    new ResultadoDecisionAreaDiferente
                    {
                        Placa = diferencia.Placa,
                        FilaGeneral = diferencia.FilaGeneral,
                        AreaGeneral = areaGeneral,
                        AreaArchivo = areaArchivo,
                        FechaLevantamiento =
                            fechaLevantamiento,
                        UltimaAreaHistoricaPrevia =
                            destinoPrevio,
                        Clasificacion =
                            "GENERAL_RESPALDADO_POR_HISTORIAL_PREVIO",
                        RequiereRevision = true,
                        Observacion =
                            "El último traslado anterior al levantamiento respalda el inventario general, " +
                            "pero el levantamiento físico posterior muestra otra área."
                    }
                );

                continue;
            }

            // ============================================
            // HISTORIAL APUNTA A UNA TERCERA ÁREA
            // ============================================

            resultados.Add(
                new ResultadoDecisionAreaDiferente
                {
                    Placa = diferencia.Placa,
                    FilaGeneral = diferencia.FilaGeneral,
                    AreaGeneral = areaGeneral,
                    AreaArchivo = areaArchivo,
                    FechaLevantamiento =
                        fechaLevantamiento,
                    UltimaAreaHistoricaPrevia =
                        destinoPrevio,
                    Clasificacion =
                        "HISTORIAL_PREVIO_APUNTA_OTRA_AREA",
                    RequiereRevision = true,
                    Observacion =
                        "El último traslado anterior al levantamiento apunta a un área distinta " +
                        "tanto del inventario general como del archivo de área."
                }
            );
        }

        return resultados;
    }
}