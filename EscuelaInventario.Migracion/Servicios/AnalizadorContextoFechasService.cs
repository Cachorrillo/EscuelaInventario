using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class AnalizadorContextoFechasService
{
    private static readonly string[] FechasProblematicas =
    {
        "/3/2024",
        "06/12/20204",
        "/10/2023",
        "25/02025",
        "3107/2026"
    };

    public void MostrarContexto(
        List<FilaInventario> registros,
        int cantidadVecinos = 5)
    {
        Console.WriteLine();
        Console.WriteLine("============================================");
        Console.WriteLine("   CONTEXTO DE FECHAS PROBLEMÁTICAS");
        Console.WriteLine("============================================");
        Console.WriteLine();

        foreach (string fechaProblematica in FechasProblematicas)
        {
            var afectados = registros
                .Where(x =>
                    x.FechaAdquisicionOriginal
                        .Trim()
                        .Equals(
                            fechaProblematica,
                            StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.NumeroFilaExcel)
                .ToList();

            if (afectados.Count == 0)
                continue;

            Console.ForegroundColor =
                ConsoleColor.Yellow;

            Console.WriteLine(
                $"FECHA PROBLEMÁTICA: '{fechaProblematica}'"
            );

            Console.ResetColor();

            Console.WriteLine(
                $"Registros afectados: {afectados.Count}"
            );

            Console.WriteLine();

            foreach (var afectado in afectados)
            {
                Console.ForegroundColor =
                    ConsoleColor.Cyan;

                Console.WriteLine(
                    $"ACTIVO: {afectado.Placa}"
                );

                Console.ResetColor();

                Console.WriteLine(
                    $"Fila Excel: {afectado.NumeroFilaExcel}"
                );

                Console.WriteLine(
                    $"Descripción: {afectado.Descripcion}"
                );

                Console.WriteLine(
                    $"Ubicación: {afectado.UbicacionOriginal}"
                );

                Console.WriteLine(
                    $"Fecha original: '{afectado.FechaAdquisicionOriginal}'"
                );

                Console.WriteLine();

                int posicion =
                    registros.FindIndex(x =>
                        x.NumeroFilaExcel ==
                        afectado.NumeroFilaExcel);

                if (posicion < 0)
                {
                    Console.WriteLine(
                        "No se pudo localizar el registro en la lista."
                    );

                    Console.WriteLine(
                        "--------------------------------------------"
                    );

                    continue;
                }

                int inicio =
                    Math.Max(
                        0,
                        posicion - cantidadVecinos
                    );

                int fin =
                    Math.Min(
                        registros.Count - 1,
                        posicion + cantidadVecinos
                    );

                Console.WriteLine(
                    "Registros cercanos:"
                );

                Console.WriteLine();

                for (int i = inicio; i <= fin; i++)
                {
                    var vecino =
                        registros[i];

                    bool esActual =
                        vecino.NumeroFilaExcel ==
                        afectado.NumeroFilaExcel;

                    if (esActual)
                    {
                        Console.ForegroundColor =
                            ConsoleColor.Red;

                        Console.Write(">>> ");
                    }
                    else
                    {
                        Console.Write("    ");
                    }

                    string fechaNormalizada =
                        vecino.FechaAdquisicionNormalizada
                            ?.ToString("dd/MM/yyyy")
                        ?? "[SIN NORMALIZAR]";

                    Console.WriteLine(
                        $"Fila {vecino.NumeroFilaExcel} | " +
                        $"{vecino.Placa} | " +
                        $"Original: '{vecino.FechaAdquisicionOriginal}' | " +
                        $"Normalizada: {fechaNormalizada}"
                    );

                    Console.ResetColor();
                }

                Console.WriteLine();

                Console.WriteLine(
                    "--------------------------------------------"
                );

                Console.WriteLine();
            }

            Console.WriteLine(
                "============================================"
            );

            Console.WriteLine();
        }
    }
}