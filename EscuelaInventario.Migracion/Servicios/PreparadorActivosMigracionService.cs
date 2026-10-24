using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class PreparadorActivosMigracionService
{
    public List<ActivoPreparadoMigracion> Preparar(
        List<FilaInventario> filasInventario,
        ResumenCruceAreas cruceAreas,
        List<ResultadoDecisionAreaDiferente> decisionesAreaDiferente,
        HashSet<string> placasConBajaHistorica,
        HashSet<string> placasConEvidenciaPosterior)
    {
        var resultados =
            new List<ActivoPreparadoMigracion>();

        foreach (var fila in filasInventario)
        {
            var preparado =
                new ActivoPreparadoMigracion
                {
                    // ============================================
                    // IDENTIFICACIÓN Y ORIGEN
                    // ============================================

                    Placa =
                        fila.Placa,

                    NumeroFilaExcel =
                        fila.NumeroFilaExcel,

                    // ============================================
                    // DATOS GENERALES
                    // ============================================

                    Descripcion =
                        fila.Descripcion,

                    Marca =
                        fila.Marca,

                    Modelo =
                        fila.Modelo,

                    Serie =
                        fila.Serie,

                    // ============================================
                    // ESTADO
                    // ============================================

                    EstadoOriginal =
                        fila.EstadoOriginal,

                    EstadoNormalizado =
                        fila.EstadoNormalizado,

                    // ============================================
                    // UBICACIÓN
                    // ============================================

                    UbicacionOriginal =
                        fila.UbicacionOriginal
                        ?? string.Empty,

                    // ============================================
                    // MODO DE ADQUISICIÓN
                    // ============================================

                    ModoAdquisicionOriginal =
                        fila.ModoAdquisicionOriginal,

                    ModoAdquisicionNormalizado =
                        fila.ModoAdquisicionNormalizado,

                    // ============================================
                    // PRECIO
                    // ============================================

                    PrecioOriginal =
                        fila.PrecioOriginal,

                    PrecioNormalizado =
                        fila.PrecioNormalizado,

                    // ============================================
                    // FECHA
                    // ============================================

                    FechaAdquisicionOriginal =
                        fila.FechaAdquisicionOriginal,

                    FechaAdquisicionNormalizada =
                        fila.FechaAdquisicionNormalizada,

                    // ============================================
                    // OBSERVACIONES
                    // ============================================

                    ObservacionesOriginales =
                        fila.ObservacionesOriginales,

                    // ============================================
                    // LIBRO
                    // ============================================

                    Tomo =
                        fila.Tomo,

                    Folio =
                        fila.Folio,

                    Asiento =
                        fila.Asiento
                };

            // ============================================
            // UBICACIÓN / ÁREA
            // ============================================

            var cruce =
                cruceAreas.Resultados
                    .FirstOrDefault(
                        x =>
                        x.Placa.Equals(
                            fila.Placa,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

            if (cruce == null)
            {
                preparado.ClasificacionOrigen =
                    "SIN_CRUCE";

                preparado.AreaActualNumero =
                    null;

                preparado.UbicacionHistorica =
                    string.IsNullOrWhiteSpace(
                        fila.UbicacionOriginal
                    )
                        ? null
                        : fila.UbicacionOriginal.Trim();

                preparado.FuenteAreaSeleccionada =
                    "SIN_AREA_OFICIAL";

                AgregarMotivo(
                    preparado,
                    "No se encontró resultado de cruce de áreas para la placa."
                );
            }
            else
            {
                preparado.ClasificacionOrigen =
                    cruce.Clasificacion;

                string ubicacionOriginal =
                    fila.UbicacionOriginal
                        ?.Trim()
                    ?? string.Empty;

                // ========================================
                // PLACA DUPLICADA EN GENERAL
                // ========================================

                if (
                    cruce.Clasificacion ==
                    "GENERAL_DUPLICADO"
                )
                {
                    preparado.AreaActualNumero =
                        null;

                    preparado.UbicacionHistorica =
                        null;

                    preparado.FuenteAreaSeleccionada =
                        "SIN_AREA_OFICIAL";

                    AgregarMotivo(
                        preparado,
                        "La placa aparece más de una vez en el inventario general. " +
                        "No se puede decidir automáticamente cuál fila debe migrarse."
                    );
                }

                // ========================================
                // ÁREA DIFERENTE
                // ========================================

                else if (
                    cruce.Clasificacion ==
                    "AREA_DIFERENTE"
                )
                {
                    var decision =
                        decisionesAreaDiferente
                            .FirstOrDefault(
                                x =>
                                x.Placa.Equals(
                                    fila.Placa,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            );

                    if (decision == null)
                    {
                        preparado.AreaActualNumero =
                            cruce.AreaGeneral;

                        preparado.UbicacionHistorica =
                            null;

                        preparado.FuenteAreaSeleccionada =
                            "INVENTARIO_GENERAL";

                        AgregarMotivo(
                            preparado,
                            "Existe una diferencia de área, pero no se encontró " +
                            "una decisión temporal para el caso."
                        );
                    }
                    else if (
                        decision.Clasificacion ==
                        "AREA_RESPALDADA_POR_HISTORIAL_PREVIO"
                    )
                    {
                        preparado.AreaActualNumero =
                            decision.AreaArchivo;

                        preparado.UbicacionHistorica =
                            null;

                        preparado.FuenteAreaSeleccionada =
                            "ARCHIVO_AREA_RESPALDADO_POR_HISTORIAL";
                    }
                    else
                    {
                        preparado.AreaActualNumero =
                            decision.AreaGeneral;

                        preparado.UbicacionHistorica =
                            null;

                        preparado.FuenteAreaSeleccionada =
                            "INVENTARIO_GENERAL";

                        AgregarMotivo(
                            preparado,
                            decision.Observacion
                        );
                    }
                }

                // ========================================
                // ÁREA NUMÉRICA VÁLIDA
                // ========================================

                else if (
                    cruce.AreaGeneral.HasValue &&
                    cruce.AreaGeneral.Value >= 1 &&
                    cruce.AreaGeneral.Value <= 70
                )
                {
                    preparado.AreaActualNumero =
                        cruce.AreaGeneral.Value;

                    preparado.UbicacionHistorica =
                        null;

                    preparado.FuenteAreaSeleccionada =
                        "INVENTARIO_GENERAL";
                }

                // ========================================
                // UBICACIÓN VACÍA
                // ========================================

                else if (
                    string.IsNullOrWhiteSpace(
                        ubicacionOriginal
                    )
                )
                {
                    preparado.AreaActualNumero =
                        null;

                    preparado.UbicacionHistorica =
                        null;

                    preparado.FuenteAreaSeleccionada =
                        "SIN_AREA_OFICIAL";

                    AgregarMotivo(
                        preparado,
                        "La ubicación original está vacía."
                    );
                }

                // ========================================
                // UBICACIÓN 0
                // ========================================

                else if (
                    ubicacionOriginal == "0"
                )
                {
                    preparado.AreaActualNumero =
                        null;

                    preparado.UbicacionHistorica =
                        ubicacionOriginal;

                    preparado.FuenteAreaSeleccionada =
                        "SIN_AREA_OFICIAL";

                    AgregarMotivo(
                        preparado,
                        "La ubicación original contiene el valor 0, " +
                        "que no corresponde a un área oficial."
                    );
                }

                // ========================================
                // UBICACIÓN DESCRIPTIVA
                // ========================================

                else
                {
                    preparado.AreaActualNumero =
                        null;

                    preparado.UbicacionHistorica =
                        ubicacionOriginal;

                    preparado.FuenteAreaSeleccionada =
                        "SIN_AREA_OFICIAL";
                }
            }

            // ============================================
            // ESTADO
            // ============================================

            if (
                string.IsNullOrWhiteSpace(
                    preparado.EstadoNormalizado
                )
            )
            {
                if (
                    string.IsNullOrWhiteSpace(
                        preparado.EstadoOriginal
                    )
                )
                {
                    AgregarMotivo(
                        preparado,
                        "El estado original está vacío."
                    );
                }
                else
                {
                    AgregarMotivo(
                        preparado,
                        $"El estado original " +
                        $"'{preparado.EstadoOriginal.Trim()}' " +
                        "no pudo normalizarse a uno de los estados oficiales."
                    );
                }
            }

            // ============================================
            // MODO DE ADQUISICIÓN
            // ============================================

            if (
                string.IsNullOrWhiteSpace(
                    preparado.ModoAdquisicionNormalizado
                )
            )
            {
                if (
                    string.IsNullOrWhiteSpace(
                        preparado.ModoAdquisicionOriginal
                    )
                )
                {
                    AgregarMotivo(
                        preparado,
                        "El modo de adquisición original está vacío."
                    );
                }
                else
                {
                    AgregarMotivo(
                        preparado,
                        $"El modo de adquisición original " +
                        $"'{preparado.ModoAdquisicionOriginal.Trim()}' " +
                        "no puede asignarse de forma segura " +
                        "a uno de los modos oficiales."
                    );
                }
            }

            // ============================================
            // PRECIO
            // ============================================

            ClasificarPrecio(
                preparado
            );

            // ============================================
            // FECHA
            // ============================================

            ClasificarFechaAdquisicion(
                preparado
            );

            // ============================================
            // SITUACIÓN ADMINISTRATIVA
            // ============================================

            ClasificarSituacion(
                preparado,
                placasConBajaHistorica,
                placasConEvidenciaPosterior
            );

            // ============================================
            // CONSOLIDAR REVISIÓN
            // ============================================

            preparado.RequiereRevision =
                preparado.MotivosRevision.Count > 0;

            preparado.MotivoRevision =
                string.Join(
                    " | ",
                    preparado.MotivosRevision
                );

            resultados.Add(
                preparado
            );
        }

        // ============================================
        // POSICIONES DUPLICADAS DEL LIBRO
        // ============================================

        var posicionesDuplicadasLibro =
            resultados
                .Where(
                    x =>
                    x.Tomo.HasValue &&
                    x.Folio.HasValue &&
                    x.Asiento.HasValue
                )
                .GroupBy(
                    x => new
                    {
                        Tomo = x.Tomo!.Value,
                        Folio = x.Folio!.Value,
                        Asiento = x.Asiento!.Value
                    }
                )
                .Where(
                    grupo => grupo.Count() > 1
                )
                .ToList();

        foreach (
            var grupo
            in posicionesDuplicadasLibro
        )
        {
            string placas =
                string.Join(
                    ", ",
                    grupo.Select(
                        x => x.Placa
                    )
                );

            foreach (
                var activo
                in grupo
            )
            {
                AgregarMotivo(
                    activo,
                    $"La posición de libro " +
                    $"T{grupo.Key.Tomo}/F{grupo.Key.Folio}/A{grupo.Key.Asiento} " +
                    $"está repetida en el inventario general. " +
                    $"Placas involucradas: {placas}."
                );
            }
        }

        // ============================================
        // RECONSOLIDAR REVISIONES
        // ============================================

        foreach (
            var activo
            in resultados
        )
        {
            activo.RequiereRevision =
                activo.MotivosRevision.Count > 0;

            activo.MotivoRevision =
                string.Join(
                    " | ",
                    activo.MotivosRevision
                );
        }

        return resultados;
    }

    // ============================================
    // SITUACIÓN ADMINISTRATIVA
    // ============================================

    private static void ClasificarSituacion(
        ActivoPreparadoMigracion preparado,
        HashSet<string> placasConBajaHistorica,
        HashSet<string> placasConEvidenciaPosterior)
    {
        bool tieneBaja =
            placasConBajaHistorica.Contains(
                preparado.Placa
            );

        if (!tieneBaja)
        {
            preparado.SituacionPreparada =
                "ACTIVO";

            preparado.ClasificacionSituacion =
                "ACTIVO_SIN_BAJA_HISTORICA";

            return;
        }

        bool tieneEvidenciaPosterior =
            placasConEvidenciaPosterior.Contains(
                preparado.Placa
            );

        if (tieneEvidenciaPosterior)
        {
            preparado.SituacionPreparada =
                "ACTIVO";

            preparado.ClasificacionSituacion =
                "ACTIVO_CON_BAJA_HISTORICA_Y_EVIDENCIA_POSTERIOR";

            AgregarMotivo(
                preparado,
                "Existe una baja histórica, pero también se encontró evidencia " +
                "posterior que indica que el activo continuó siendo trasladado " +
                "o fue registrado posteriormente en un levantamiento de área. " +
                "Se prepara como ACTIVO para operación y se conserva la baja " +
                "histórica para revisión."
            );

            return;
        }

        preparado.SituacionPreparada =
            "DADO_DE_BAJA";

        preparado.ClasificacionSituacion =
            "BAJA_SIN_EVIDENCIA_POSTERIOR";
    }

    // ============================================
    // PRECIO
    // ============================================

    private static void ClasificarPrecio(
        ActivoPreparadoMigracion preparado)
    {
        if (
            preparado.PrecioNormalizado.HasValue
        )
        {
            preparado.ClasificacionPrecio =
                "NUMERICO";

            return;
        }

        string precioOriginal =
            preparado.PrecioOriginal
                ?.Trim()
            ?? string.Empty;

        if (
            string.IsNullOrWhiteSpace(
                precioOriginal
            )
        )
        {
            preparado.ClasificacionPrecio =
                "SIN_DATO";

            return;
        }

        string precioMayuscula =
            precioOriginal.ToUpperInvariant();

        if (
            precioMayuscula == "NI" ||
            precioMayuscula == ",NI" ||
            precioMayuscula == "N" ||
            precioMayuscula == "N>I"
        )
        {
            preparado.ClasificacionPrecio =
                "SIN_DATO";

            return;
        }

        if (
            precioMayuscula.StartsWith(
                "VER "
            )
        )
        {
            preparado.ClasificacionPrecio =
                "REFERENCIA_HISTORICA";

            return;
        }

        if (
            precioMayuscula == "DONACIÓN" ||
            precioMayuscula == "DONACION"
        )
        {
            preparado.ClasificacionPrecio =
                "NO_NUMERICO_INFORMATIVO";

            return;
        }

        preparado.ClasificacionPrecio =
            "AMBIGUO";

        AgregarMotivo(
            preparado,
            $"El precio original '{precioOriginal}' " +
            "no puede interpretarse de forma segura como un valor numérico."
        );
    }

    // ============================================
    // FECHA
    // ============================================

    private static void ClasificarFechaAdquisicion(
        ActivoPreparadoMigracion preparado)
    {
        if (
            preparado.FechaAdquisicionNormalizada.HasValue
        )
        {
            preparado.ClasificacionFechaAdquisicion =
                "FECHA_COMPLETA";

            return;
        }

        string fechaOriginal =
            preparado.FechaAdquisicionOriginal
                ?.Trim()
            ?? string.Empty;

        if (
            string.IsNullOrWhiteSpace(
                fechaOriginal
            )
        )
        {
            preparado.ClasificacionFechaAdquisicion =
                "SIN_DATO";

            return;
        }

        string fechaMayuscula =
            fechaOriginal.ToUpperInvariant();

        if (
            fechaMayuscula == "NI"
        )
        {
            preparado.ClasificacionFechaAdquisicion =
                "SIN_DATO";

            return;
        }

        if (
            fechaOriginal.Length == 4 &&
            int.TryParse(
                fechaOriginal,
                out int anio
            ) &&
            anio >= 1900 &&
            anio <= 2100
        )
        {
            preparado.ClasificacionFechaAdquisicion =
                "ANIO_PARCIAL";

            return;
        }

        if (
            fechaMayuscula.StartsWith(
                "VER "
            )
        )
        {
            preparado.ClasificacionFechaAdquisicion =
                "REFERENCIA_HISTORICA";

            return;
        }

        preparado.ClasificacionFechaAdquisicion =
            "INCOMPLETA_AMBIGUA";

        AgregarMotivo(
            preparado,
            $"La fecha de adquisición original '{fechaOriginal}' " +
            "está incompleta o no puede interpretarse de forma segura."
        );
    }

    // ============================================
    // AGREGAR MOTIVO
    // ============================================

    private static void AgregarMotivo(
        ActivoPreparadoMigracion preparado,
        string motivo)
    {
        if (
            string.IsNullOrWhiteSpace(
                motivo
            )
        )
        {
            return;
        }

        if (
            preparado.MotivosRevision.Any(
                x =>
                x.Equals(
                    motivo,
                    StringComparison.OrdinalIgnoreCase
                )
            )
        )
        {
            return;
        }

        preparado.MotivosRevision.Add(
            motivo
        );
    }
}