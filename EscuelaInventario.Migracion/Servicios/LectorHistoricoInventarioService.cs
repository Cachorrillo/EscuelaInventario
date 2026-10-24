using ClosedXML.Excel;
using EscuelaInventario.Migracion.Modelos;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class LectorHistoricoInventarioService
{
    private static readonly Regex PatronPlaca =
        new(@"^4615-\d{4}$", RegexOptions.Compiled);

    public List<FilaHistoricaInventario> Leer(
        string rutaArchivo)
    {
        var registros =
            new List<FilaHistoricaInventario>();

        using var libro =
            new XLWorkbook(rutaArchivo);

        var hoja =
            libro.Worksheet(1);

        var rango =
            hoja.RangeUsed();

        if (rango == null)
            return registros;

        int ultimaFila =
            rango.LastRow().RowNumber();

        for (int fila = 4; fila <= ultimaFila; fila++)
        {
            string registradoEn =
                ObtenerTexto(
                    hoja,
                    fila,
                    1
                );

            string identificacion =
                ObtenerTexto(
                    hoja,
                    fila,
                    2
                );

            string descripcion =
                ObtenerTexto(
                    hoja,
                    fila,
                    3
                );

            string observaciones =
                ObtenerTexto(
                    hoja,
                    fila,
                    11
                );

            // ============================================
            // ENCABEZADOS REPETIDOS
            // ============================================

            if (EsEncabezadoRepetido(
                registradoEn,
                identificacion,
                descripcion,
                observaciones))
            {
                continue;
            }

            // ============================================
            // ACTIVO PRINCIPAL
            // ============================================

            if (PatronPlaca.IsMatch(
                identificacion))
            {
                continue;
            }

            // ============================================
            // COMPROBAR CONTENIDO REAL
            //
            // Columna 1 contiene la posición del libro.
            //
            // Revisamos columnas 2 a 13 para saber si
            // realmente existe información administrativa
            // en esa posición.
            // ============================================

            bool tieneContenidoReal = false;

            for (int columna = 2;
                 columna <= 13;
                 columna++)
            {
                string contenido =
                    ObtenerTexto(
                        hoja,
                        fila,
                        columna
                    );

                if (!string.IsNullOrWhiteSpace(
                    contenido))
                {
                    tieneContenidoReal = true;
                    break;
                }
            }

            // ============================================
            // FILA TOTALMENTE VACÍA
            //
            // Si ni siquiera tiene posición de libro,
            // no existe nada útil que conservar.
            // ============================================

            bool filaCompletamenteVacia =
                string.IsNullOrWhiteSpace(
                    registradoEn) &&
                !tieneContenidoReal;

            if (filaCompletamenteVacia)
            {
                continue;
            }

            // ============================================
            // REGISTRO HISTÓRICO / POSICIÓN DE PLANTILLA
            // ============================================

            var registro =
                new FilaHistoricaInventario
                {
                    NumeroFilaExcel = fila,

                    RegistradoEnOriginal =
                        registradoEn,

                    IdentificacionOriginal =
                        identificacion,

                    DescripcionOriginal =
                        descripcion,

                    ObservacionesOriginales =
                        observaciones,

                    TieneContenidoReal =
                        tieneContenidoReal
                };

            registros.Add(registro);
        }

        return registros;
    }

    private static bool EsEncabezadoRepetido(
        string registradoEn,
        string identificacion,
        string descripcion,
        string observaciones)
    {
        bool encabezadoRegistradoEn =
            registradoEn.Equals(
                "Registrado en",
                StringComparison.OrdinalIgnoreCase
            );

        bool encabezadoIdentificacion =
            identificacion.Equals(
                "N° Identificación",
                StringComparison.OrdinalIgnoreCase
            ) ||
            identificacion.Equals(
                "Nº Identificación",
                StringComparison.OrdinalIgnoreCase
            );

        bool encabezadoDescripcion =
            descripcion.Equals(
                "Descripción",
                StringComparison.OrdinalIgnoreCase
            );

        bool encabezadoObservaciones =
            observaciones.Equals(
                "Observaciones",
                StringComparison.OrdinalIgnoreCase
            );

        return
            encabezadoRegistradoEn &&
            encabezadoIdentificacion &&
            encabezadoDescripcion &&
            encabezadoObservaciones;
    }

    private static string ObtenerTexto(
        IXLWorksheet hoja,
        int fila,
        int columna)
    {
        return hoja
            .Cell(fila, columna)
            .GetFormattedString()
            .Trim();
    }
}