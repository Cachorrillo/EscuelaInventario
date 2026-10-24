using ClosedXML.Excel;
using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class ReconstruidorActivosHistoricosService
{
    public List<ActivoHistoricoReconstruido> Reconstruir(
        string rutaArchivo,
        List<FilaHistoricaInventario> registrosHistoricos)
    {
        var resultados =
            new List<ActivoHistoricoReconstruido>();

        using var workbook =
            new XLWorkbook(rutaArchivo);

        var hoja =
            workbook.Worksheets.First();

        // ========================================================
        // 1. ACTIVOS HISTÓRICOS CUYA ÚLTIMA SITUACIÓN ES BAJA
        // ========================================================

        AgregarSiExiste(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-3318",
            identificacionEsperada: "4615- 3318",
            tomo: 5,
            folio: 334,
            asiento: 23,
            situacionPreparada: "DADO_DE_BAJA",
            clasificacionSituacion:
                "HISTORICO_RECONSTRUIDO_CON_BAJA",
            areaActualNumero: null,
            ubicacionHistorica: null,
            fuenteAreaSeleccionada:
                "SIN_AREA_ACTUAL_POR_BAJA",
            requiereRevision: false,
            motivoRevision: string.Empty
        );

        AgregarSiExiste(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-3698",
            identificacionEsperada: "4615- 3698",
            tomo: 5,
            folio: 435,
            asiento: 31,
            situacionPreparada: "DADO_DE_BAJA",
            clasificacionSituacion:
                "HISTORICO_RECONSTRUIDO_CON_BAJA",
            areaActualNumero: null,
            ubicacionHistorica: null,
            fuenteAreaSeleccionada:
                "SIN_AREA_ACTUAL_POR_BAJA",
            requiereRevision: false,
            motivoRevision: string.Empty
        );

        AgregarSiExiste(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-3934",
            identificacionEsperada: "4616-3934",
            tomo: 5,
            folio: 497,
            asiento: 14,
            situacionPreparada: "DADO_DE_BAJA",
            clasificacionSituacion:
                "HISTORICO_RECONSTRUIDO_CON_BAJA",
            areaActualNumero: null,
            ubicacionHistorica: null,
            fuenteAreaSeleccionada:
                "SIN_AREA_ACTUAL_POR_BAJA",
            requiereRevision: true,
            motivoRevision:
                "El asiento original contiene la identificación " +
                "4616-3934, mientras que los movimientos históricos " +
                "posteriores identifican el mismo activo como " +
                "4615-3934. La vinculación se realiza por coincidencia " +
                "de consecutivo, descripción y referencia al asiento original."
        );

        AgregarSiExiste(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4082",
            identificacionEsperada: "4616-4082",
            tomo: 5,
            folio: 546,
            asiento: 23,
            situacionPreparada: "DADO_DE_BAJA",
            clasificacionSituacion:
                "HISTORICO_RECONSTRUIDO_CON_BAJA",
            areaActualNumero: null,
            ubicacionHistorica: null,
            fuenteAreaSeleccionada:
                "SIN_AREA_ACTUAL_POR_BAJA",
            requiereRevision: true,
            motivoRevision:
                "El asiento original contiene la identificación " +
                "4616-4082, mientras que los movimientos históricos " +
                "posteriores identifican el mismo activo como " +
                "4615-4082. La vinculación se realiza por coincidencia " +
                "de consecutivo, descripción y referencia al asiento original."
        );

        // ========================================================
        // 2. ACTIVOS RESPALDADOS POR LEVANTAMIENTO DE ÁREA 2026
        // ========================================================

        AgregarSiExiste(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-3314",
            identificacionEsperada: "4615- 3314",
            tomo: 5,
            folio: 334,
            asiento: 11,
            situacionPreparada: "ACTIVO",
            clasificacionSituacion:
                "ACTIVO_RESPALDADO_POR_ARCHIVO_AREA",
            areaActualNumero: 31,
            ubicacionHistorica: null,
            fuenteAreaSeleccionada:
                "ARCHIVO_AREA_31_2026",
            requiereRevision: false,
            motivoRevision: string.Empty
        );

        AgregarSiExiste(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-3316",
            identificacionEsperada: "4615- 3316",
            tomo: 5,
            folio: 334,
            asiento: 13,
            situacionPreparada: "ACTIVO",
            clasificacionSituacion:
                "ACTIVO_RESPALDADO_POR_ARCHIVO_AREA",
            areaActualNumero: 38,
            ubicacionHistorica: null,
            fuenteAreaSeleccionada:
                "ARCHIVO_AREA_38_2026",
            requiereRevision: false,
            motivoRevision: string.Empty
        );

        // ========================================================
        // 3. ACTIVOS SIN BAJA Y SIN CONFIRMACIÓN FÍSICA ACTUAL
        //
        // Se conservan como ACTIVO porque no existe evidencia
        // de baja, pero NO se afirma que su último destino
        // histórico sea todavía su área actual.
        // ========================================================

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-3312",
            identificacionEsperada: "4615- 3312",
            tomo: 5,
            folio: 334,
            asiento: 1,
            ultimaUbicacionHistorica: "pasillo A",
            tienePrefijoOriginalDistinto: false
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-3317",
            identificacionEsperada: "4615- 3317",
            tomo: 5,
            folio: 334,
            asiento: 14,
            ultimaUbicacionHistorica: "pasillo A",
            tienePrefijoOriginalDistinto: false
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4060",
            identificacionEsperada: "4616-4060",
            tomo: 5,
            folio: 546,
            asiento: 1,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4061",
            identificacionEsperada: "4616-4061",
            tomo: 5,
            folio: 546,
            asiento: 2,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4062",
            identificacionEsperada: "4616-4062",
            tomo: 5,
            folio: 546,
            asiento: 3,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4063",
            identificacionEsperada: "4616-4063",
            tomo: 5,
            folio: 546,
            asiento: 4,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4064",
            identificacionEsperada: "4616-4064",
            tomo: 5,
            folio: 546,
            asiento: 5,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4065",
            identificacionEsperada: "4616-4065",
            tomo: 5,
            folio: 546,
            asiento: 6,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4066",
            identificacionEsperada: "4616-4066",
            tomo: 5,
            folio: 546,
            asiento: 7,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4068",
            identificacionEsperada: "4616-4068",
            tomo: 5,
            folio: 546,
            asiento: 9,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4069",
            identificacionEsperada: "4616-4069",
            tomo: 5,
            folio: 546,
            asiento: 10,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4070",
            identificacionEsperada: "4616-4070",
            tomo: 5,
            folio: 546,
            asiento: 11,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4071",
            identificacionEsperada: "4616-4071",
            tomo: 5,
            folio: 546,
            asiento: 12,
            ultimaUbicacionHistorica: "Área 52",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4072",
            identificacionEsperada: "4616-4072",
            tomo: 5,
            folio: 546,
            asiento: 13,
            ultimaUbicacionHistorica: "Área 14",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4073",
            identificacionEsperada: "4616-4073",
            tomo: 5,
            folio: 546,
            asiento: 14,
            ultimaUbicacionHistorica: "Área 14",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4075",
            identificacionEsperada: "4616-4075",
            tomo: 5,
            folio: 546,
            asiento: 16,
            ultimaUbicacionHistorica: "Área 14",
            tienePrefijoOriginalDistinto: true
        );

        AgregarActivoHistoricoSinConfirmacionActual(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada: "4615-4083",
            identificacionEsperada: "4616-4083",
            tomo: 5,
            folio: 546,
            asiento: 24,
            ultimaUbicacionHistorica: "Área 51",
            tienePrefijoOriginalDistinto: true
        );

        return resultados;
    }

    private static void AgregarActivoHistoricoSinConfirmacionActual(
        List<ActivoHistoricoReconstruido> resultados,
        List<FilaHistoricaInventario> registrosHistoricos,
        IXLWorksheet hoja,
        string placaMigrada,
        string identificacionEsperada,
        int tomo,
        int folio,
        int asiento,
        string ultimaUbicacionHistorica,
        bool tienePrefijoOriginalDistinto)
    {
        string motivo =
            "El activo no tiene una baja histórica registrada. " +
            "La última evidencia conocida es un traslado histórico, " +
            "pero no aparece en un levantamiento físico de área posterior. " +
            "Se conserva como ACTIVO sin área oficial actual y se mantiene " +
            $"'{ultimaUbicacionHistorica}' únicamente como última ubicación histórica conocida.";

        if (tienePrefijoOriginalDistinto)
        {
            motivo +=
                $" El asiento original utiliza la identificación " +
                $"{identificacionEsperada}, mientras que los movimientos " +
                $"históricos posteriores utilizan {placaMigrada}. " +
                "La vinculación se conserva documentada y no se modifica " +
                "silenciosamente la fuente original.";
        }

        AgregarSiExiste(
            resultados,
            registrosHistoricos,
            hoja,
            placaMigrada,
            identificacionEsperada,
            tomo,
            folio,
            asiento,
            situacionPreparada: "ACTIVO",
            clasificacionSituacion:
                "ACTIVO_SOLO_EVIDENCIA_HISTORICA_SIN_CONFIRMACION_ACTUAL",
            areaActualNumero: null,
            ubicacionHistorica:
                ultimaUbicacionHistorica,
            fuenteAreaSeleccionada:
                "ULTIMO_TRASLADO_HISTORICO_NO_CONFIRMADO",
            requiereRevision: true,
            motivoRevision: motivo
        );
    }

    private static void AgregarSiExiste(
        List<ActivoHistoricoReconstruido> resultados,
        List<FilaHistoricaInventario> registrosHistoricos,
        IXLWorksheet hoja,
        string placaMigrada,
        string identificacionEsperada,
        int tomo,
        int folio,
        int asiento,
        string situacionPreparada,
        string clasificacionSituacion,
        int? areaActualNumero,
        string? ubicacionHistorica,
        string fuenteAreaSeleccionada,
        bool requiereRevision,
        string motivoRevision)
    {
        var registro =
            registrosHistoricos
                .FirstOrDefault(
                    x =>
                    x.Tomo == tomo &&
                    x.Folio == folio &&
                    x.Asiento == asiento
                );

        if (registro == null)
        {
            return;
        }

        int filaExcel =
            registro.NumeroFilaExcel;

        string identificacionOriginal =
            ObtenerTexto(
                hoja,
                filaExcel,
                2
            );

        string descripcion =
            ObtenerTexto(
                hoja,
                filaExcel,
                3
            );

        string marca =
            ObtenerTexto(
                hoja,
                filaExcel,
                4
            );

        string modelo =
            ObtenerTexto(
                hoja,
                filaExcel,
                5
            );

        string serie =
            ObtenerTexto(
                hoja,
                filaExcel,
                6
            );

        string estadoOriginal =
            ObtenerTexto(
                hoja,
                filaExcel,
                7
            );

        string ubicacionOriginal =
            ObtenerTexto(
                hoja,
                filaExcel,
                8
            );

        string modoOriginal =
            ObtenerTexto(
                hoja,
                filaExcel,
                9
            );

        string precioOriginal =
            ObtenerTexto(
                hoja,
                filaExcel,
                10
            );

        string observacionesOriginales =
            ObtenerTexto(
                hoja,
                filaExcel,
                11
            );

        string fechaAdquisicionOriginal =
            ObtenerTexto(
                hoja,
                filaExcel,
                12
            );

        int? areaOriginalNumero =
            ExtraerAreaNumero(
                ubicacionOriginal
            );

        string? estadoNormalizado =
            NormalizarEstado(
                estadoOriginal
            );

        string? modoNormalizado =
            NormalizarModoAdquisicion(
                modoOriginal
            );

        decimal? precioNormalizado =
            NormalizarPrecio(
                precioOriginal
            );

        DateTime? fechaNormalizada =
            NormalizarFecha(
                hoja,
                filaExcel,
                12
            );

        bool revisionFinal =
            requiereRevision;

        string motivoFinal =
            motivoRevision;

        bool identificacionCoincide =
            identificacionOriginal.Equals(
                identificacionEsperada,
                StringComparison.OrdinalIgnoreCase
            );

        if (!identificacionCoincide)
        {
            revisionFinal =
                true;

            motivoFinal =
                AgregarMotivo(
                    motivoFinal,
                    $"La identificación encontrada en el asiento original " +
                    $"es '{identificacionOriginal}', mientras que se esperaba " +
                    $"'{identificacionEsperada}'."
                );
        }

        if (
            string.IsNullOrWhiteSpace(
                estadoNormalizado
            )
        )
        {
            revisionFinal =
                true;

            motivoFinal =
                AgregarMotivo(
                    motivoFinal,
                    $"El estado histórico '{estadoOriginal}' " +
                    "no pudo normalizarse de forma segura."
                );
        }

        if (
            string.IsNullOrWhiteSpace(
                modoNormalizado
            )
        )
        {
            revisionFinal =
                true;

            motivoFinal =
                AgregarMotivo(
                    motivoFinal,
                    $"El modo de adquisición histórico '{modoOriginal}' " +
                    "no pudo normalizarse de forma segura."
                );
        }

        resultados.Add(
            new ActivoHistoricoReconstruido
            {
                Placa =
                    placaMigrada,

                IdentificacionOriginal =
                    identificacionOriginal,

                NumeroFilaExcel =
                    filaExcel,

                Descripcion =
                    descripcion,

                Marca =
                    marca,

                Modelo =
                    modelo,

                Serie =
                    serie,

                EstadoOriginal =
                    estadoOriginal,

                EstadoNormalizado =
                    estadoNormalizado,

                UbicacionOriginal =
                    ubicacionOriginal,

                AreaOriginalNumero =
                    areaOriginalNumero,

                AreaActualNumero =
                    areaActualNumero,

                UbicacionHistorica =
                    ubicacionHistorica,

                FuenteAreaSeleccionada =
                    fuenteAreaSeleccionada,

                ModoAdquisicionOriginal =
                    modoOriginal,

                ModoAdquisicionNormalizado =
                    modoNormalizado,

                PrecioOriginal =
                    precioOriginal,

                PrecioNormalizado =
                    precioNormalizado,

                FechaAdquisicionOriginal =
                    fechaAdquisicionOriginal,

                FechaAdquisicionNormalizada =
                    fechaNormalizada,

                ObservacionesOriginales =
                    observacionesOriginales,

                TomoOriginal =
                    tomo,

                FolioOriginal =
                    folio,

                AsientoOriginal =
                    asiento,

                SituacionPreparada =
                    situacionPreparada,

                ClasificacionSituacion =
                    clasificacionSituacion,

                RequiereRevision =
                    revisionFinal,

                MotivoRevision =
                    motivoFinal,

                OrigenReconstruccion =
                    "REGISTRO_HISTORICO_RECONSTRUIDO"
            }
        );
    }

    private static string ObtenerTexto(
        IXLWorksheet hoja,
        int fila,
        int columna)
    {
        return hoja
            .Cell(
                fila,
                columna
            )
            .GetFormattedString()
            .Trim();
    }

    private static int? ExtraerAreaNumero(
        string ubicacionOriginal)
    {
        if (
            string.IsNullOrWhiteSpace(
                ubicacionOriginal
            )
        )
        {
            return null;
        }

        string limpio =
            ubicacionOriginal
                .ToUpperInvariant()
                .Replace(
                    "ÁREA",
                    ""
                )
                .Replace(
                    "AREA",
                    ""
                )
                .Trim();

        if (
            int.TryParse(
                limpio,
                out int numero
            ) &&
            numero >= 1 &&
            numero <= 70
        )
        {
            return numero;
        }

        return null;
    }

    private static string? NormalizarEstado(
        string estadoOriginal)
    {
        string valor =
            estadoOriginal
                .Trim()
                .ToUpperInvariant();

        return valor switch
        {
            "EXCELENTE" =>
                "Excelente",

            "BUENO" =>
                "Bueno",

            "BIEN" =>
                "Bueno",

            "BUENA" =>
                "Bueno",

            "NUEVA" =>
                "Bueno",

            "REGULAR" =>
                "Regular",

            "REGULULAR" =>
                "Regular",

            "MALO" =>
                "Malo",

            "MALA" =>
                "Malo",

            _ =>
                null
        };
    }

    private static string? NormalizarModoAdquisicion(
        string modoOriginal)
    {
        string valor =
            modoOriginal
                .Trim()
                .ToUpperInvariant();

        return valor switch
        {
            "JA" =>
                "Junta Administrativa",

            "JPS" =>
                "Junta Protección Social",

            _ =>
                null
        };
    }

    private static decimal? NormalizarPrecio(
        string precioOriginal)
    {
        string valor =
            precioOriginal.Trim();

        if (
            string.IsNullOrWhiteSpace(
                valor
            ) ||
            valor.Equals(
                "NI",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return null;
        }

        if (
            decimal.TryParse(
                valor,
                out decimal precio
            )
        )
        {
            return precio;
        }

        return null;
    }

    private static DateTime? NormalizarFecha(
        IXLWorksheet hoja,
        int fila,
        int columna)
    {
        var celda =
            hoja.Cell(
                fila,
                columna
            );

        if (
            celda.TryGetValue<DateTime>(
                out DateTime fecha
            )
        )
        {
            return fecha.Date;
        }

        string texto =
            celda
                .GetFormattedString()
                .Trim();

        if (
            DateTime.TryParse(
                texto,
                out DateTime fechaTexto
            )
        )
        {
            return fechaTexto.Date;
        }

        return null;
    }

    private static string AgregarMotivo(
        string existente,
        string nuevo)
    {
        if (
            string.IsNullOrWhiteSpace(
                existente
            )
        )
        {
            return nuevo;
        }

        if (
            existente.Contains(
                nuevo,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return existente;
        }

        return existente +
            " | " +
            nuevo;
    }
}