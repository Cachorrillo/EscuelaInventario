using ClosedXML.Excel;

namespace EscuelaInventario.Migracion.Servicios;

public class EstructuraArchivoArea
{
    public int NumeroArea { get; set; }

    public string NombreArchivo { get; set; } = string.Empty;

    public int CantidadHojas { get; set; }

    public string NombreHoja { get; set; } = string.Empty;

    public int UltimaFilaUsada { get; set; }

    public int UltimaColumnaUsada { get; set; }

    public int? FilaEncabezadoDetectada { get; set; }

    public List<string> Encabezados { get; set; } = new();

    public bool RequiereRevision { get; set; }

    public string MotivoRevision { get; set; } = string.Empty;
}

public class AnalizadorEstructuraAreasService
{
    public List<EstructuraArchivoArea> Analizar(
        List<ArchivoAreaDetectado> archivos)
    {
        var resultados =
            new List<EstructuraArchivoArea>();

        foreach (
            var archivo
            in archivos
                .Where(x => x.NumeroAreaDetectado.HasValue)
                .OrderBy(x => x.NumeroAreaDetectado)
        )
        {
            var resultado =
                new EstructuraArchivoArea
                {
                    NumeroArea =
                        archivo.NumeroAreaDetectado!.Value,

                    NombreArchivo =
                        archivo.NombreArchivo
                };

            try
            {
                using var workbook =
                    new XLWorkbook(
                        archivo.RutaCompleta
                    );

                resultado.CantidadHojas =
                    workbook.Worksheets.Count;

                if (
                    workbook.Worksheets.Count == 0
                )
                {
                    resultado.RequiereRevision = true;
                    resultado.MotivoRevision =
                        "El archivo no contiene hojas.";

                    resultados.Add(
                        resultado
                    );

                    continue;
                }

                var worksheet =
                    workbook.Worksheet(1);

                resultado.NombreHoja =
                    worksheet.Name;

                var rangoUsado =
                    worksheet.RangeUsed();

                if (rangoUsado == null)
                {
                    resultado.RequiereRevision = true;
                    resultado.MotivoRevision =
                        "La primera hoja no contiene datos.";

                    resultados.Add(
                        resultado
                    );

                    continue;
                }

                resultado.UltimaFilaUsada =
                    rangoUsado.RangeAddress
                        .LastAddress
                        .RowNumber;

                resultado.UltimaColumnaUsada =
                    rangoUsado.RangeAddress
                        .LastAddress
                        .ColumnNumber;

                resultado.FilaEncabezadoDetectada =
                    DetectarFilaEncabezado(
                        worksheet,
                        resultado.UltimaFilaUsada,
                        resultado.UltimaColumnaUsada
                    );

                if (
                    resultado.FilaEncabezadoDetectada.HasValue
                )
                {
                    int filaEncabezado =
                        resultado
                            .FilaEncabezadoDetectada
                            .Value;

                    for (
                        int columna = 1;
                        columna <=
                        resultado.UltimaColumnaUsada;
                        columna++
                    )
                    {
                        string valor =
                            worksheet
                                .Cell(
                                    filaEncabezado,
                                    columna
                                )
                                .GetFormattedString()
                                .Trim();

                        resultado.Encabezados.Add(
                            valor
                        );
                    }
                }
                else
                {
                    resultado.RequiereRevision = true;

                    resultado.MotivoRevision =
                        "No se pudo identificar automáticamente la fila de encabezados.";
                }

                if (
                    resultado.CantidadHojas > 1
                )
                {
                    resultado.RequiereRevision = true;

                    AgregarMotivo(
                        resultado,
                        $"El archivo contiene {resultado.CantidadHojas} hojas."
                    );
                }
            }
            catch (Exception ex)
            {
                resultado.RequiereRevision = true;

                resultado.MotivoRevision =
                    $"Error al leer el archivo: {ex.Message}";
            }

            resultados.Add(
                resultado
            );
        }

        return resultados;
    }

    private static int? DetectarFilaEncabezado(
        IXLWorksheet worksheet,
        int ultimaFila,
        int ultimaColumna)
    {
        int limiteFilas =
            Math.Min(
                ultimaFila,
                20
            );

        for (
            int fila = 1;
            fila <= limiteFilas;
            fila++
        )
        {
            var valores =
                new List<string>();

            for (
                int columna = 1;
                columna <= ultimaColumna;
                columna++
            )
            {
                string valor =
                    worksheet
                        .Cell(
                            fila,
                            columna
                        )
                        .GetFormattedString()
                        .Trim();

                if (
                    !string.IsNullOrWhiteSpace(valor)
                )
                {
                    valores.Add(
                        NormalizarTexto(valor)
                    );
                }
            }

            if (
                valores.Count < 3
            )
            {
                continue;
            }

            bool tieneIdentificacion =
                valores.Any(
                    x =>
                    x.Contains("identificacion")
                    ||
                    x.Contains("placa")
                );

            bool tieneDescripcion =
                valores.Any(
                    x =>
                    x.Contains("descripcion")
                    ||
                    x.Contains("detalle")
                );

            if (
                tieneIdentificacion &&
                tieneDescripcion
            )
            {
                return fila;
            }
        }

        return null;
    }

    private static void AgregarMotivo(
        EstructuraArchivoArea resultado,
        string motivo)
    {
        if (
            string.IsNullOrWhiteSpace(
                resultado.MotivoRevision
            )
        )
        {
            resultado.MotivoRevision =
                motivo;
        }
        else
        {
            resultado.MotivoRevision +=
                " " + motivo;
        }
    }

    private static string NormalizarTexto(
        string valor)
    {
        return valor
            .ToLowerInvariant()
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("ú", "u")
            .Replace("ü", "u")
            .Trim();
    }
}