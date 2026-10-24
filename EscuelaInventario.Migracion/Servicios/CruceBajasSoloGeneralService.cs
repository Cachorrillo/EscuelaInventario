using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class CruceBajasSoloGeneralService
{
    public ResumenCruceBajasSoloGeneral Comparar(
        ResumenCruceAreas cruceAreas,
        List<BajaHistoricaDetectada> bajasHistoricas)
    {
        var resumen =
            new ResumenCruceBajasSoloGeneral();

        var bajasPorPlaca =
            bajasHistoricas
                .SelectMany(
                    baja =>
                    baja.Placas.Select(
                        placa => new
                        {
                            Placa =
                                NormalizarPlaca(
                                    placa
                                ),

                            Baja =
                                baja
                        }
                    )
                )
                .Where(
                    x =>
                    !string.IsNullOrWhiteSpace(
                        x.Placa
                    )
                )
                .GroupBy(
                    x => x.Placa,
                    StringComparer.OrdinalIgnoreCase
                )
                .ToDictionary(
                    x => x.Key,
                    x => x
                        .Select(y => y.Baja)
                        .ToList(),
                    StringComparer.OrdinalIgnoreCase
                );

        var soloEnGeneral =
            cruceAreas
                .Resultados
                .Where(
                    x =>
                    x.Clasificacion ==
                    "SOLO_EN_GENERAL"
                )
                .OrderBy(
                    x => x.Placa
                )
                .ToList();

        foreach (
            var item
            in soloEnGeneral
        )
        {
            var resultado =
                new CruceBajaSoloGeneral
                {
                    Placa =
                        item.Placa,

                    FilaGeneral =
                        item.FilaGeneral,

                    UbicacionGeneralOriginal =
                        item.UbicacionGeneralOriginal
                };

            if (
                !bajasPorPlaca.TryGetValue(
                    item.Placa,
                    out var bajas
                )
            )
            {
                resultado.TieneBajaHistorica =
                    false;

                resultado.Observacion =
                    "La placa aparece únicamente en el inventario general y no se encontró una baja histórica asociada.";

                resumen.Resultados.Add(
                    resultado
                );

                continue;
            }

            resultado.TieneBajaHistorica =
                true;

            resultado.CantidadBajasEncontradas =
                bajas.Count;

            var bajaSeleccionada =
                bajas
                    .OrderByDescending(
                        x =>
                        x.FechaMovimiento
                    )
                    .First();

            resultado.MotivoBaja =
                bajaSeleccionada
                    .MotivoNormalizado;

            resultado.FechaBajaOriginal =
                bajaSeleccionada
                    .FechaOriginal;

            resultado.FechaBaja =
                bajaSeleccionada
                    .FechaMovimiento;

            if (
                bajas.Count > 1
            )
            {
                resultado.RequiereRevision =
                    true;

                resultado.Observacion =
                    $"La placa tiene {bajas.Count} bajas históricas asociadas. " +
                    "La ausencia en los archivos de área podría estar explicada por una baja, pero debe revisarse la existencia de múltiples registros.";
            }
            else
            {
                resultado.Observacion =
                    "La placa aparece únicamente en el inventario general y tiene una baja histórica registrada. " +
                    "Su ausencia en los archivos de área es coherente con esa baja.";
            }

            resumen.Resultados.Add(
                resultado
            );
        }

        return resumen;
    }

    private static string NormalizarPlaca(
        string placa)
    {
        if (
            string.IsNullOrWhiteSpace(
                placa
            )
        )
        {
            return string.Empty;
        }

        return placa
            .Trim()
            .Replace(
                " ",
                string.Empty
            )
            .ToUpperInvariant();
    }
}