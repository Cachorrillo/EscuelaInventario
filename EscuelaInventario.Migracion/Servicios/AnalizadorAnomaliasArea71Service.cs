using ClosedXML.Excel;
using EscuelaInventario.Migracion.Modelos;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class AnalizadorAnomaliasArea71Service
{
    private static readonly Regex PatronPlaca =
        new(
            @"4615-\d{4}",
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled
        );

    public List<ResultadoAnomaliaArea71> Analizar(
        string rutaArchivo,
        List<ActivoFinalMigracion> activosFinales,
        List<MovimientoPreparadoMigracion> movimientosPreparados)
    {
        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException(
                "No se encontró el archivo de anomalías.",
                rutaArchivo
            );
        }

        var apariciones =
            new List<AparicionAnomalia>();

        using var workbook =
            new XLWorkbook(rutaArchivo);

        // ========================================================
        // 1. LEER TODAS LAS HOJAS DEL ARCHIVO
        // ========================================================

        foreach (
            var worksheet
            in workbook.Worksheets
        )
        {
            var ultimaFila =
                worksheet.LastRowUsed()?.RowNumber() ?? 0;

            var ultimaColumna =
                worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

            for (
                int fila = 1;
                fila <= ultimaFila;
                fila++
            )
            {
                for (
                    int columna = 1;
                    columna <= ultimaColumna;
                    columna++
                )
                {
                    string texto =
                        worksheet
                            .Cell(fila, columna)
                            .GetFormattedString()
                            .Trim();

                    if (string.IsNullOrWhiteSpace(texto))
                    {
                        continue;
                    }

                    var coincidencias =
                        PatronPlaca.Matches(texto);

                    foreach (
                        Match coincidencia
                        in coincidencias
                    )
                    {
                        string placa =
                            coincidencia
                                .Value
                                .ToUpperInvariant();

                        apariciones.Add(
                            new AparicionAnomalia
                            {
                                Placa = placa,
                                Hoja = worksheet.Name,
                                Fila = fila
                            }
                        );
                    }
                }
            }
        }

        // ========================================================
        // 2. AGRUPAR POR PLACA
        // ========================================================

        var grupos =
            apariciones
                .GroupBy(
                    x => x.Placa,
                    StringComparer.OrdinalIgnoreCase
                )
                .OrderBy(
                    x => x.Key
                )
                .ToList();

        var resultados =
            new List<ResultadoAnomaliaArea71>();

        // ========================================================
        // 3. CRUZAR CONTRA EL CORTE OFICIAL
        // ========================================================

        foreach (
            var grupo
            in grupos
        )
        {
            string placa =
                grupo.Key;

            var activo =
                activosFinales
                    .FirstOrDefault(
                        x =>
                        x.Placa.Equals(
                            placa,
                            StringComparison.OrdinalIgnoreCase
                        )
                    );

            var resultado =
                new ResultadoAnomaliaArea71
                {
                    Placa = placa,

                    HojasOrigen =
                        grupo
                            .Select(x => x.Hoja)
                            .Distinct(
                                StringComparer.OrdinalIgnoreCase
                            )
                            .OrderBy(x => x)
                            .ToList(),

                    FilasOrigen =
                        grupo
                            .Select(x => x.Fila)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList(),

                    CantidadAparicionesExcel =
                        grupo.Count()
                };

            // ====================================================
            // NO EXISTE EN EL CORTE OFICIAL
            // ====================================================

            if (activo == null)
            {
                resultado.Clasificacion =
                    "NO_ENCONTRADA";

                resultado.Observacion =
                    "La placa aparece en el archivo de anomalías, " +
                    "pero no existe entre los activos finales del " +
                    "corte oficial.";

                resultados.Add(resultado);

                continue;
            }

            resultado.AreaActualNumero =
                activo.AreaActualNumero;

            resultado.SituacionActual =
                activo.SituacionPreparada;

            // ====================================================
            // ÚLTIMA FECHA REGISTRADA PARA EL ACTIVO
            // ====================================================

            // ====================================================
            // ÚLTIMA FECHA REGISTRADA PARA EL ACTIVO
            // ====================================================

            DateTime? ultimaFechaMovimiento =
                movimientosPreparados
                    .Where(
                        x =>
                        x.Placa.Equals(
                            activo.Placa,
                            StringComparison.OrdinalIgnoreCase
                        ) &&
                        x.FechaMovimiento.HasValue
                    )
                    .OrderByDescending(
                        x => x.FechaMovimiento
                    )
                    .Select(
                        x => x.FechaMovimiento
                    )
                    .FirstOrDefault();

            if (ultimaFechaMovimiento.HasValue)
            {
                resultado.FechaUltimoRegistro =
                    ultimaFechaMovimiento.Value;

                resultado.OrigenFechaUltimoRegistro =
                    "ULTIMO_MOVIMIENTO";
            }
            else if (activo.FechaAdquisicionNormalizada.HasValue)
            {
                resultado.FechaUltimoRegistro =
                    activo.FechaAdquisicionNormalizada.Value;

                resultado.OrigenFechaUltimoRegistro =
                    "FECHA_ADQUISICION";
            }
            else
            {
                resultado.FechaUltimoRegistro =
                    null;

                resultado.OrigenFechaUltimoRegistro =
                    "SIN_FECHA";
            }

            // ====================================================
            // EL ACTIVO ESTÁ DADO DE BAJA
            // ====================================================

            if (
    activo.SituacionPreparada.Equals(
        "DADO_DE_BAJA",
        StringComparison.OrdinalIgnoreCase
    )
)
            {
                resultado.Clasificacion =
                    "CANDIDATA_AREA_71_BAJA";

                resultado.Observacion =
                    activo.AreaActualNumero.HasValue
                        ? $"Activo dado de baja. Área registrada: " +
                          $"{activo.AreaActualNumero.Value}. " +
                          "Debe incorporarse al Área 71 conservando " +
                          "su situación DADO_DE_BAJA."
                        : "Activo dado de baja sin área numérica actual. " +
                          "Debe incorporarse al Área 71 conservando " +
                          "su situación DADO_DE_BAJA.";

                resultados.Add(resultado);

                continue;
            }

            // ====================================================
            // YA ESTÁ EN ÁREA 71
            // ====================================================

            if (activo.AreaActualNumero == 71)
            {
                resultado.Clasificacion =
                 "CANDIDATA_AREA_71_ACTIVA";

                resultado.Observacion =
                    "El activo ya se encuentra actualmente en Área 71. " +
                    "No debe generarse otro traslado.";

                resultados.Add(resultado);

                continue;
            }

            // ====================================================
            // ACTIVO VIGENTE CANDIDATO A ÁREA 71
            // ====================================================

            resultado.Clasificacion =
               "CANDIDATA_AREA_71_ACTIVA";

            resultado.Observacion =
                activo.AreaActualNumero.HasValue
                    ? $"Activo vigente. Área actual: " +
                      $"{activo.AreaActualNumero.Value}. " +
                      "Puede prepararse un traslado normal a Área 71."
                    : "Activo vigente, pero no posee un área numérica " +
                      "actual. Debe revisarse antes de preparar el " +
                      "traslado a Área 71.";

            resultados.Add(resultado);
        }

        return resultados;
    }

    // ============================================================
    // REPRESENTACIÓN INTERNA DE CADA APARICIÓN EN EL EXCEL
    // ============================================================

    private class AparicionAnomalia
    {
        public string Placa { get; set; } =
            string.Empty;

        public string Hoja { get; set; } =
            string.Empty;

        public int Fila { get; set; }
    }
}