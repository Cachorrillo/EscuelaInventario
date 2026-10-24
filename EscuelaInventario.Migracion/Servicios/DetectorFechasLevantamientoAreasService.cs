using System.Text.RegularExpressions;
using ClosedXML.Excel;
using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class DetectorFechasLevantamientoAreasService
{
    private static readonly Regex RegexFecha =
        new(
            @"\b(\d{1,2})([/-])(\d{1,2})\2(\d{4})\b",
            RegexOptions.Compiled
        );

    public List<FechaLevantamientoArea> Detectar(
        List<ArchivoAreaDetectado> archivos)
    {
        var resultados =
            new List<FechaLevantamientoArea>();

        foreach (
            var archivo
            in archivos
                .Where(
                    x =>
                    x.NumeroAreaDetectado.HasValue
                )
                .OrderBy(
                    x =>
                    x.NumeroAreaDetectado
                )
        )
        {
            var resultado =
                DetectarArchivo(
                    archivo
                );

            resultados.Add(
                resultado
            );
        }

        return resultados;
    }

    private static FechaLevantamientoArea DetectarArchivo(
        ArchivoAreaDetectado archivo)
    {
        var resultado =
            new FechaLevantamientoArea
            {
                NumeroArea =
                    archivo.NumeroAreaDetectado!.Value,

                NombreArchivo =
                    archivo.NombreArchivo
            };

        using var workbook =
            new XLWorkbook(
                archivo.RutaCompleta
            );

        if (
            workbook.Worksheets.Count == 0
        )
        {
            resultado.RequiereRevision =
                true;

            resultado.MotivoRevision =
                "El archivo no contiene hojas.";

            return resultado;
        }

        var worksheet =
            workbook.Worksheet(1);

        resultado.NombreHoja =
            worksheet.Name;

        int limiteFilas =
            Math.Min(
                worksheet
                    .LastRowUsed()?
                    .RowNumber() ?? 0,
                5
            );

        for (
            int fila = 1;
            fila <= limiteFilas;
            fila++
        )
        {
            string textoFila =
                string.Join(
                    " ",
                    Enumerable
                        .Range(
                            1,
                            12
                        )
                        .Select(
                            columna =>
                            worksheet
                                .Cell(
                                    fila,
                                    columna
                                )
                                .GetFormattedString()
                                .Trim()
                        )
                        .Where(
                            texto =>
                            !string.IsNullOrWhiteSpace(
                                texto
                            )
                        )
                );

            if (
                !textoFila.Contains(
                    "LEVANTAMIENTO",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                continue;
            }

            resultado.TituloOriginal =
                textoFila;

            // Corrección controlada y confirmada.
            // En el archivo del Área 9 aparece:
            // "014/10/2026"
            // y corresponde a:
            // "14/10/2026".
            if (
                resultado.NumeroArea == 9 &&
                textoFila.Contains(
                    "014/10/2026",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                resultado.FechaOriginal =
                    "014/10/2026";

                resultado.FechaLevantamiento =
                    new DateTime(
                        2026,
                        10,
                        14
                    );

                return resultado;
            }

            var match =
                RegexFecha.Match(
                    textoFila
                );

            if (
                !match.Success
            )
            {
                resultado.RequiereRevision =
                    true;

                resultado.MotivoRevision =
                    "Se encontró el título del levantamiento, pero no una fecha válida en formato dd/MM/yyyy o dd-MM-yyyy.";

                return resultado;
            }

            resultado.FechaOriginal =
                match.Value;

            bool diaValido =
                int.TryParse(
                    match.Groups[1].Value,
                    out int dia
                );

            bool mesValido =
                int.TryParse(
                    match.Groups[3].Value,
                    out int mes
                );

            bool anioValido =
                int.TryParse(
                    match.Groups[4].Value,
                    out int anio
                );

            if (
                !diaValido ||
                !mesValido ||
                !anioValido
            )
            {
                resultado.RequiereRevision =
                    true;

                resultado.MotivoRevision =
                    $"La fecha '{match.Value}' no pudo interpretarse numéricamente.";

                return resultado;
            }

            try
            {
                resultado.FechaLevantamiento =
                    new DateTime(
                        anio,
                        mes,
                        dia
                    );

                return resultado;
            }
            catch (
                ArgumentOutOfRangeException
            )
            {
                resultado.RequiereRevision =
                    true;

                resultado.MotivoRevision =
                    $"La fecha '{match.Value}' contiene un día, mes o año no válido.";

                return resultado;
            }
        }

        resultado.RequiereRevision =
            true;

        resultado.MotivoRevision =
            "No se encontró un título de levantamiento en la primera hoja.";

        return resultado;
    }
}