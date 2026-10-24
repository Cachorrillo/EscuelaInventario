using System.Text.RegularExpressions;
using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class CruceIrregularesAreasService
{
    private static readonly Regex RegexPlacaValida =
        new(
            @"^4615-\d{4}$",
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled
        );

    private static readonly Regex RegexConsecutivoFinal =
        new(
            @"(\d{3,4})\s*$",
            RegexOptions.Compiled
        );

    public ResumenCruceIrregularesAreas Comparar(
        List<ActivoIdentificacionIrregular> activosIrregulares,
        ResultadoLecturaAreas resultadoAreas,
        ResumenCruceAreas cruceGeneral)
    {
        var resumen =
            new ResumenCruceIrregularesAreas();

        var registrosAreaPorPlaca =
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

        var placasSoloEnArchivosArea =
            cruceGeneral
                .Resultados
                .Where(
                    x =>
                    x.Clasificacion ==
                    "SOLO_EN_ARCHIVOS_AREA"
                )
                .Select(
                    x => x.Placa
                )
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase
                );

        foreach (
            var irregular
            in activosIrregulares
                .OrderBy(
                    x => x.NumeroFilaExcel
                )
        )
        {
            string placaEquivalente =
                ObtenerPlacaEquivalente(
                    irregular
                );

            var resultado =
                new CruceIrregularArea
                {
                    IdentificacionGeneralOriginal =
                        irregular.IdentificacionOriginal,

                    PlacaEquivalenteComparada =
                        placaEquivalente,

                    FilaGeneral =
                        irregular.NumeroFilaExcel,

                    DescripcionGeneral =
                        irregular.Descripcion,

                    TipoIrregularidad =
                        irregular.TipoIrregularidad
                };

            if (
                string.IsNullOrWhiteSpace(
                    placaEquivalente
                )
            )
            {
                resultado.RequiereRevision = true;

                resultado.Observacion =
                    "No fue posible obtener un equivalente 4615-0000 únicamente para fines de comparación.";

                resumen.Resultados.Add(
                    resultado
                );

                continue;
            }

            if (
                !registrosAreaPorPlaca.TryGetValue(
                    placaEquivalente,
                    out var coincidenciasArea
                )
            )
            {
                resultado.EncontradoEnArchivosArea = false;

                resultado.Observacion =
                    "No se encontró el equivalente comparado en ninguno de los archivos de área.";

                resumen.Resultados.Add(
                    resultado
                );

                continue;
            }

            resultado.EncontradoEnArchivosArea = true;

            resultado.AreasEncontradas =
                coincidenciasArea
                    .Select(
                        x => x.NumeroAreaArchivo
                    )
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            resultado.DetallesArchivosArea =
                coincidenciasArea
                    .Select(
                        x =>
                        $"Área {x.NumeroAreaArchivo} | " +
                        $"{x.NombreArchivo} | " +
                        $"fila {x.NumeroFilaExcel}"
                    )
                    .ToList();

            resultado.EraSoloEnArchivosArea =
                placasSoloEnArchivosArea.Contains(
                    placaEquivalente
                );

            if (
                resultado.AreasEncontradas.Count > 1
            )
            {
                resultado.RequiereRevision = true;

                resultado.Observacion =
                    "El equivalente aparece en más de un archivo de área. No se puede determinar automáticamente una ubicación única.";
            }
            else if (
                resultado.EraSoloEnArchivosArea
            )
            {
                resultado.Observacion =
                    "La placa válida aparecía como SOLO_EN_ARCHIVOS_AREA, pero existe una identificación irregular equivalente en el inventario general. La coincidencia sirve como evidencia para revisión; no se corrige automáticamente.";
            }
            else
            {
                resultado.Observacion =
                    "Se encontró en los archivos de área una placa válida con el mismo consecutivo que la identificación irregular del inventario general. No se realiza ninguna corrección automática.";
            }

            resumen.Resultados.Add(
                resultado
            );
        }

        return resumen;
    }

    private static string ObtenerPlacaEquivalente(
        ActivoIdentificacionIrregular irregular)
    {
        if (
            !string.IsNullOrWhiteSpace(
                irregular.PlacaSugerida
            )
        )
        {
            string sugerida =
                NormalizarPlaca(
                    irregular.PlacaSugerida
                );

            if (
                RegexPlacaValida.IsMatch(
                    sugerida
                )
            )
            {
                return sugerida;
            }
        }

        if (
            string.IsNullOrWhiteSpace(
                irregular.IdentificacionOriginal
            )
        )
        {
            return string.Empty;
        }

        var match =
            RegexConsecutivoFinal.Match(
                irregular.IdentificacionOriginal
            );

        if (
            !match.Success
        )
        {
            return string.Empty;
        }

        string consecutivo =
            match
                .Groups[1]
                .Value
                .PadLeft(
                    4,
                    '0'
                );

        return $"4615-{consecutivo}";
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