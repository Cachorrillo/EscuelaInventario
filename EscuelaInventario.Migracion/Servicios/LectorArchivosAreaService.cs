using System.Text.RegularExpressions;
using ClosedXML.Excel;
using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class ResultadoLecturaAreas
{
    public List<FilaAreaInventario> Registros { get; set; } = new();

    public List<IncidenciaArchivoArea> Incidencias { get; set; } = new();
}

public class LectorArchivosAreaService
{
    private static readonly Regex RegexPlacaValida =
        new(
            @"^4615-\d{4}$",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase
        );

    public ResultadoLecturaAreas Leer(
        List<ArchivoAreaDetectado> archivos)
    {
        var resultado =
            new ResultadoLecturaAreas();

        foreach (
            var archivo
            in archivos
                .Where(x => x.NumeroAreaDetectado.HasValue)
                .OrderBy(x => x.NumeroAreaDetectado)
        )
        {
            LeerArchivo(
                archivo,
                resultado
            );
        }

        return resultado;
    }

    private static void LeerArchivo(
        ArchivoAreaDetectado archivo,
        ResultadoLecturaAreas resultado)
    {
        using var workbook =
            new XLWorkbook(
                archivo.RutaCompleta
            );

        foreach (
            var worksheet
            in workbook.Worksheets
        )
        {
            LeerHoja(
                archivo,
                worksheet,
                resultado
            );
        }
    }

    private static void LeerHoja(
        ArchivoAreaDetectado archivo,
        IXLWorksheet worksheet,
        ResultadoLecturaAreas resultado)
    {
        int numeroArea =
            archivo.NumeroAreaDetectado!.Value;

        int ultimaFila =
            ObtenerUltimaFilaReal(
                worksheet
            );

        if (ultimaFila == 0)
        {
            return;
        }

        bool esHojaExclusivaSinPlaca =
            EsHojaExclusivaSinPlaca(
                worksheet
            );

        for (
            int fila = 1;
            fila <= ultimaFila;
            fila++
        )
        {
            if (
                FilaVacia(
                    worksheet,
                    fila
                )
            )
            {
                continue;
            }

            string columna1 =
                ObtenerTexto(
                    worksheet,
                    fila,
                    1
                );

            string columna2 =
                ObtenerTexto(
                    worksheet,
                    fila,
                    2
                );

            if (
                EsFilaTitulo(
                    columna1,
                    columna2
                )
            )
            {
                continue;
            }

            if (esHojaExclusivaSinPlaca)
            {
                ProcesarFilaHojaSinPlaca(
                    archivo,
                    worksheet,
                    fila,
                    resultado
                );

                continue;
            }

            ProcesarFilaHojaNormal(
                archivo,
                worksheet,
                fila,
                numeroArea,
                resultado
            );
        }
    }

    private static void ProcesarFilaHojaNormal(
     ArchivoAreaDetectado archivo,
     IXLWorksheet worksheet,
     int fila,
     int numeroArea,
     ResultadoLecturaAreas resultado)
    {
        string placaOriginal =
            ObtenerTexto(
                worksheet,
                fila,
                2
            );

        string descripcion =
            ObtenerTexto(
                worksheet,
                fila,
                3
            );

        string placaNormalizada =
            NormalizarPlaca(
                placaOriginal
            );

        // 1. Placa válida
        if (
            RegexPlacaValida.IsMatch(
                placaNormalizada
            )
        )
        {
            resultado.Registros.Add(
                CrearRegistro(
                    archivo,
                    worksheet,
                    fila,
                    placaNormalizada
                )
            );

            return;
        }

        // 2. La fila parece ser un activo,
        // pero la identificación está completamente vacía
        if (
            string.IsNullOrWhiteSpace(
                placaOriginal
            )
        )
        {
            if (
                TieneContenidoDeActivo(
                    worksheet,
                    fila
                )
            )
            {
                resultado.Incidencias.Add(
                    new IncidenciaArchivoArea
                    {
                        NumeroAreaArchivo =
                            numeroArea,

                        NombreArchivo =
                            archivo.NombreArchivo,

                        NombreHoja =
                            worksheet.Name,

                        NumeroFilaExcel =
                            fila,

                        Tipo =
                            "IDENTIFICACION_VACIA",

                        ValorOriginal =
                            string.Empty,

                        Descripcion =
                            descripcion,

                        Mensaje =
                            "La fila parece contener un activo, pero no tiene ninguna identificación registrada."
                    }
                );
            }

            return;
        }

        // 3. El archivo dice expresamente que no tiene placa
        // o que la placa se cayó
        if (
            EsIndicadorSinPlaca(
                placaOriginal
            )
        )
        {
            resultado.Incidencias.Add(
                new IncidenciaArchivoArea
                {
                    NumeroAreaArchivo =
                        numeroArea,

                    NombreArchivo =
                        archivo.NombreArchivo,

                    NombreHoja =
                        worksheet.Name,

                    NumeroFilaExcel =
                        fila,

                    Tipo =
                        "ACTIVO_SIN_PLACA",

                    ValorOriginal =
                        placaOriginal,

                    Descripcion =
                        descripcion,

                    Mensaje =
                        "El registro está identificado como activo sin placa o con placa caída. No se asignará una placa automáticamente."
                }
            );

            return;
        }

        // 4. Tiene una identificación,
        // pero no cumple el formato correcto 4615-0000
        if (
            TieneContenidoDeActivo(
                worksheet,
                fila
            )
        )
        {
            resultado.Incidencias.Add(
                new IncidenciaArchivoArea
                {
                    NumeroAreaArchivo =
                        numeroArea,

                    NombreArchivo =
                        archivo.NombreArchivo,

                    NombreHoja =
                        worksheet.Name,

                    NumeroFilaExcel =
                        fila,

                    Tipo =
                        "PLACA_NO_VALIDA",

                    ValorOriginal =
                        placaOriginal,

                    Descripcion =
                        descripcion,

                    Mensaje =
                        "La fila parece contener un activo, pero la identificación no tiene el formato válido 4615-0000."
                }
            );
        }
    }

    private static void ProcesarFilaHojaSinPlaca(
        ArchivoAreaDetectado archivo,
        IXLWorksheet worksheet,
        int fila,
        ResultadoLecturaAreas resultado)
    {
        string identificacion =
            ObtenerTexto(
                worksheet,
                fila,
                1
            );

        string descripcion =
            ObtenerTexto(
                worksheet,
                fila,
                2
            );

        if (
            string.IsNullOrWhiteSpace(
                identificacion
            )
            &&
            string.IsNullOrWhiteSpace(
                descripcion
            )
        )
        {
            return;
        }

        resultado.Incidencias.Add(
            new IncidenciaArchivoArea
            {
                NumeroAreaArchivo =
                    archivo.NumeroAreaDetectado!.Value,

                NombreArchivo =
                    archivo.NombreArchivo,

                NombreHoja =
                    worksheet.Name,

                NumeroFilaExcel =
                    fila,

                Tipo =
                    "ACTIVO_SIN_PLACA",

                ValorOriginal =
                    identificacion,

                Descripcion =
                    descripcion,

                Mensaje =
                    "El registro pertenece a una hoja destinada a activos sin placa. No se asignará una placa automáticamente."
            }
        );
    }

    private static bool EsHojaExclusivaSinPlaca(
        IXLWorksheet worksheet)
    {
        int limite =
            Math.Min(
                ObtenerUltimaFilaReal(
                    worksheet
                ),
                3
            );

        for (
            int fila = 1;
            fila <= limite;
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
                            ObtenerTexto(
                                worksheet,
                                fila,
                                columna
                            )
                        )
                );

            if (
                textoFila.Contains(
                    "LEVANTAMIENTO",
                    StringComparison.OrdinalIgnoreCase
                )
                &&
                textoFila.Contains(
                    "SIN PLACA",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return true;
            }
        }

        return false;
    }

    private static bool EsIndicadorSinPlaca(
        string valor)
    {
        if (
            string.IsNullOrWhiteSpace(
                valor
            )
        )
        {
            return false;
        }

        string normalizado =
            valor
                .Trim()
                .ToUpperInvariant()
                .Replace(
                    "Á",
                    "A"
                )
                .Replace(
                    "Í",
                    "I"
                );

        return
            normalizado == "SIN PLACA"
            ||
            normalizado == "S/P"
            ||
            normalizado == "SP"
            ||
            normalizado.Contains(
                "PLACA CAIDA"
            );
    }

    private static FilaAreaInventario CrearRegistro(
        ArchivoAreaDetectado archivo,
        IXLWorksheet worksheet,
        int fila,
        string placaNormalizada)
    {
        return new FilaAreaInventario
        {
            NumeroAreaArchivo =
                archivo.NumeroAreaDetectado!.Value,

            NombreArchivo =
                archivo.NombreArchivo,

            NombreHoja =
                worksheet.Name,

            NumeroFilaExcel =
                fila,

            RegistroLibroOriginal =
                ObtenerTexto(
                    worksheet,
                    fila,
                    1
                ),

            PlacaOriginal =
                ObtenerTexto(
                    worksheet,
                    fila,
                    2
                ),

            PlacaNormalizada =
                placaNormalizada,

            Descripcion =
                ObtenerTexto(
                    worksheet,
                    fila,
                    3
                ),

            Marca =
                ObtenerTexto(
                    worksheet,
                    fila,
                    4
                ),

            Modelo =
                ObtenerTexto(
                    worksheet,
                    fila,
                    5
                ),

            Serie =
                ObtenerTexto(
                    worksheet,
                    fila,
                    6
                ),

            EstadoOriginal =
                ObtenerTexto(
                    worksheet,
                    fila,
                    7
                ),

            UbicacionOriginal =
                ObtenerTexto(
                    worksheet,
                    fila,
                    8
                ),

            ModoAdquisicionOriginal =
                ObtenerTexto(
                    worksheet,
                    fila,
                    9
                ),

            PrecioOriginal =
                ObtenerTexto(
                    worksheet,
                    fila,
                    10
                ),

            Observaciones =
                ObtenerTexto(
                    worksheet,
                    fila,
                    11
                ),

            FechaAdquisicionOriginal =
                ObtenerTexto(
                    worksheet,
                    fila,
                    12
                )
        };
    }

    private static int ObtenerUltimaFilaReal(
        IXLWorksheet worksheet)
    {
        var ultimaCelda =
            worksheet
                .CellsUsed(
                    XLCellsUsedOptions.Contents
                )
                .LastOrDefault();

        return
            ultimaCelda?
                .Address
                .RowNumber
            ?? 0;
    }

    private static bool FilaVacia(
        IXLWorksheet worksheet,
        int fila)
    {
        for (
            int columna = 1;
            columna <= 12;
            columna++
        )
        {
            if (
                !string.IsNullOrWhiteSpace(
                    ObtenerTexto(
                        worksheet,
                        fila,
                        columna
                    )
                )
            )
            {
                return false;
            }
        }

        return true;
    }

    private static bool TieneContenidoDeActivo(
        IXLWorksheet worksheet,
        int fila)
    {
        string descripcion =
            ObtenerTexto(
                worksheet,
                fila,
                3
            );

        string estado =
            ObtenerTexto(
                worksheet,
                fila,
                7
            );

        string ubicacion =
            ObtenerTexto(
                worksheet,
                fila,
                8
            );

        return
            !string.IsNullOrWhiteSpace(
                descripcion
            )
            ||
            !string.IsNullOrWhiteSpace(
                estado
            )
            ||
            !string.IsNullOrWhiteSpace(
                ubicacion
            );
    }

    private static bool EsFilaTitulo(
        string columna1,
        string columna2)
    {
        string combinado =
            $"{columna1} {columna2}";

        return
            combinado.Contains(
                "LEVANTAMIENTO DE INVENTARIO",
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static string NormalizarPlaca(
        string placa)
    {
        return placa
            .Trim()
            .Replace(
                " ",
                string.Empty
            )
            .ToUpperInvariant();
    }

    private static string ObtenerTexto(
        IXLWorksheet worksheet,
        int fila,
        int columna)
    {
        return worksheet
            .Cell(
                fila,
                columna
            )
            .GetFormattedString()
            .Trim();
    }
}