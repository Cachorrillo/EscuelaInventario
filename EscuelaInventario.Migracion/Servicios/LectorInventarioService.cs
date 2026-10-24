using ClosedXML.Excel;
using EscuelaInventario.Migracion.Modelos;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class LectorInventarioService
{
    private static readonly Regex PatronPlaca =
        new(@"^4615-\d{4}$", RegexOptions.Compiled);

    public List<FilaInventario> Leer(string rutaArchivo)
    {
        var registros = new List<FilaInventario>();

        using var libro = new XLWorkbook(rutaArchivo);

        var hoja = libro.Worksheet(1);
        var rango = hoja.RangeUsed();

        if (rango == null)
            return registros;

        int ultimaFila = rango.LastRow().RowNumber();

        // Fila 3 = encabezados
        // Los datos empiezan en la fila 4
        for (int fila = 4; fila <= ultimaFila; fila++)
        {
            string placa = ObtenerTexto(hoja, fila, 2);

            // Solo tomamos filas que tengan una placa válida
            // con el formato institucional 4615-XXXX.
            if (!PatronPlaca.IsMatch(placa))
                continue;

            var registro = new FilaInventario
            {
                NumeroFilaExcel = fila,

                RegistradoEn =
                    ObtenerTexto(hoja, fila, 1),

                Placa =
                    placa,

                Descripcion =
                    ObtenerTexto(hoja, fila, 3),

                Marca =
                    ObtenerTexto(hoja, fila, 4),

                Modelo =
                    ObtenerTexto(hoja, fila, 5),

                Serie =
                    ObtenerTexto(hoja, fila, 6),

                EstadoOriginal =
                    ObtenerTexto(hoja, fila, 7),

                UbicacionOriginal =
                    ObtenerTexto(hoja, fila, 8),

                ModoAdquisicionOriginal =
                    ObtenerTexto(hoja, fila, 9),

                PrecioOriginal =
                    ObtenerTexto(hoja, fila, 10),

                ObservacionesOriginales =
                    ObtenerTexto(hoja, fila, 11),

                FechaAdquisicionOriginal =
                    ObtenerTexto(hoja, fila, 12)
            };

            registros.Add(registro);
        }

        return registros;
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