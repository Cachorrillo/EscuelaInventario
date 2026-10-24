using System.Text.RegularExpressions;
using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class CruceInventarioAreasService
{
    private static readonly Regex RegexArea =
        new(
            @"(?:AREA|ÁREA)\s*#?\s*0*(\d{1,2})(?!\d)",
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled
        );

    public ResumenCruceAreas Comparar(
        List<FilaInventario> inventarioGeneral,
        ResultadoLecturaAreas resultadoAreas)
    {
        var resumen =
            new ResumenCruceAreas();

        var generalPorPlaca =
            inventarioGeneral
                .Where(
                    x =>
                    !string.IsNullOrWhiteSpace(
                        x.Placa
                    )
                )
                .GroupBy(
                    x => NormalizarPlaca(x.Placa),
                    StringComparer.OrdinalIgnoreCase
                )
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList(),
                    StringComparer.OrdinalIgnoreCase
                );

        var areasPorPlaca =
            resultadoAreas
                .Registros
                .GroupBy(
                    x => x.PlacaNormalizada,
                    StringComparer.OrdinalIgnoreCase
                )
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList(),
                    StringComparer.OrdinalIgnoreCase
                );

        var todasLasPlacas =
            generalPorPlaca.Keys
                .Union(
                    areasPorPlaca.Keys,
                    StringComparer.OrdinalIgnoreCase
                )
                .OrderBy(x => x)
                .ToList();

        foreach (
            string placa
            in todasLasPlacas
        )
        {
            generalPorPlaca.TryGetValue(
                placa,
                out var registrosGeneral
            );

            areasPorPlaca.TryGetValue(
                placa,
                out var registrosArea
            );

            registrosGeneral ??=
                new List<FilaInventario>();

            registrosArea ??=
                new List<FilaAreaInventario>();

            var resultado =
                new ResultadoCruceArea
                {
                    Placa = placa
                };

            resultado.AreasArchivos =
                registrosArea
                    .Select(
                        x => x.NumeroAreaArchivo
                    )
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            resultado.DetallesArchivosArea =
                registrosArea
                    .Select(
                        x =>
                        $"Área {x.NumeroAreaArchivo} | " +
                        $"{x.NombreArchivo} | " +
                        $"fila {x.NumeroFilaExcel}"
                    )
                    .ToList();

            // Existe únicamente en los archivos de área.
            if (
                registrosGeneral.Count == 0
            )
            {
                resultado.Clasificacion =
                    "SOLO_EN_ARCHIVOS_AREA";

                resultado.Observacion =
                    "La placa aparece en los archivos de áreas, pero no fue encontrada como placa válida en el inventario general.";

                resumen.Resultados.Add(
                    resultado
                );

                continue;
            }

            // Existe únicamente en el inventario general.
            if (
                registrosArea.Count == 0
            )
            {
                var general =
                    registrosGeneral.First();

                resultado.Clasificacion =
                    "SOLO_EN_GENERAL";

                resultado.FilaGeneral =
                    general.NumeroFilaExcel;

                resultado.UbicacionGeneralOriginal =
                    general.UbicacionOriginal;

                resultado.AreaGeneral =
                    ExtraerAreaNumerica(
                        general.UbicacionOriginal
                    );

                resultado.Observacion =
                    "La placa existe en el inventario general, pero no aparece en ninguno de los archivos de áreas.";

                resumen.Resultados.Add(
                    resultado
                );

                continue;
            }

            // La misma placa está duplicada dentro del inventario general.
            // No elegimos arbitrariamente una de las filas.
            if (
                registrosGeneral.Count > 1
            )
            {
                resultado.Clasificacion =
                    "GENERAL_DUPLICADO";

                resultado.Observacion =
                    $"La placa aparece {registrosGeneral.Count} veces en el inventario general. " +
                    $"Filas: {string.Join(", ", registrosGeneral.Select(x => x.NumeroFilaExcel))}.";

                resumen.Resultados.Add(
                    resultado
                );

                continue;
            }

            var registroGeneral =
                registrosGeneral.Single();

            resultado.FilaGeneral =
                registroGeneral.NumeroFilaExcel;

            resultado.UbicacionGeneralOriginal =
                registroGeneral.UbicacionOriginal;

            resultado.AreaGeneral =
                ExtraerAreaNumerica(
                    registroGeneral.UbicacionOriginal
                );

            // La placa está en más de un archivo de área.
            // Eso tiene prioridad sobre una comparación individual.
            if (
                resultado.AreasArchivos.Count > 1
            )
            {
                resultado.Clasificacion =
                    "PRESENTE_EN_VARIAS_AREAS";

                resultado.Observacion =
                    $"La placa aparece en múltiples archivos de área: " +
                    $"{string.Join(", ", resultado.AreasArchivos)}.";

                resumen.Resultados.Add(
                    resultado
                );

                continue;
            }

            // El inventario general no contiene un área numérica reconocible.
            if (
                !resultado.AreaGeneral.HasValue
            )
            {
                resultado.Clasificacion =
                    "AREA_GENERAL_NO_NUMERICA";

                resultado.Observacion =
                    $"La ubicación del inventario general es " +
                    $"'{registroGeneral.UbicacionOriginal}' y no se convirtió automáticamente a un área numérica.";

                resumen.Resultados.Add(
                    resultado
                );

                continue;
            }

            int areaArchivo =
                resultado.AreasArchivos.Single();

            if (
                resultado.AreaGeneral.Value ==
                areaArchivo
            )
            {
                resultado.Clasificacion =
                    "COINCIDE";

                resultado.Observacion =
                    "El área del inventario general coincide con el archivo de área.";

                resumen.Resultados.Add(
                    resultado
                );

                continue;
            }

            resultado.Clasificacion =
                "AREA_DIFERENTE";

            resultado.Observacion =
                $"Inventario general: Área {resultado.AreaGeneral.Value}. " +
                $"Archivo de área: Área {areaArchivo}.";

            resumen.Resultados.Add(
                resultado
            );
        }

        return resumen;
    }

    private static int? ExtraerAreaNumerica(
        string ubicacion)
    {
        if (
            string.IsNullOrWhiteSpace(
                ubicacion
            )
        )
        {
            return null;
        }

        string ubicacionLimpia =
            ubicacion.Trim();

        // Caso 1:
        // La ubicación contiene únicamente el número del área.
        //
        // Ejemplos válidos:
        // "1"
        // "10"
        // "70"
        //
        // Ejemplos no válidos:
        // "0"
        // "71"
        if (
            int.TryParse(
                ubicacionLimpia,
                out int numeroDirecto
            )
        )
        {
            if (
                numeroDirecto >= 1 &&
                numeroDirecto <= 70
            )
            {
                return numeroDirecto;
            }

            return null;
        }

        // Caso 2:
        // La ubicación contiene expresiones como:
        //
        // "AREA 10"
        // "ÁREA 10"
        // "AREA #10"
        // "ÁREA #10"
        var match =
            RegexArea.Match(
                ubicacionLimpia
            );

        if (
            !match.Success
        )
        {
            return null;
        }

        if (
            !int.TryParse(
                match.Groups[1].Value,
                out int numero
            )
        )
        {
            return null;
        }

        if (
            numero < 1 ||
            numero > 70
        )
        {
            return null;
        }

        return numero;
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
}