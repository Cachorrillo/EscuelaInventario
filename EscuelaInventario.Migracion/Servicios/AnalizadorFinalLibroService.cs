using ClosedXML.Excel;

namespace EscuelaInventario.Migracion.Servicios;

public class AnalizadorFinalLibroService
{
    public void MostrarFinal(
        string rutaArchivo,
        int filaInicio = 17040,
        int filaFin = 17063)
    {
        using var libro =
            new XLWorkbook(rutaArchivo);

        var hoja =
            libro.Worksheet(1);

        Console.WriteLine();
        Console.WriteLine("============================================");
        Console.WriteLine("   ANÁLISIS DEL FINAL DEL LIBRO");
        Console.WriteLine("============================================");
        Console.WriteLine();

        for (int fila = filaInicio; fila <= filaFin; fila++)
        {
            Console.ForegroundColor =
                ConsoleColor.Cyan;

            Console.WriteLine(
                $"FILA EXCEL {fila}"
            );

            Console.ResetColor();

            bool tieneContenidoReal = false;

            for (int columna = 1; columna <= 13; columna++)
            {
                string valor =
                    hoja
                        .Cell(fila, columna)
                        .GetFormattedString()
                        .Trim();

                if (string.IsNullOrWhiteSpace(valor))
                    continue;

                tieneContenidoReal = true;

                Console.WriteLine(
                    $"Columna {columna}: '{valor}'"
                );
            }

            if (!tieneContenidoReal)
            {
                Console.WriteLine(
                    "[FILA COMPLETAMENTE VACÍA]"
                );
            }

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }
}