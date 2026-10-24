using EscuelaInventario.Migracion.Modelos;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class DetectorActivosIdentificacionIrregularService
{
    private static readonly Regex PatronConEspacioInterno =
        new(
            @"^\s*4615\s*-\s+(\d{3,4})\s*$",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase
        );

    private static readonly Regex PatronPrefijoDistinto =
        new(
            @"^\s*(\d{4})\s*-\s*(\d{3,4})\s*$",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase
        );

    public List<ActivoIdentificacionIrregular> Detectar(
        List<FilaHistoricaInventario> historicosNoAsignados)
    {
        var resultados =
            new List<ActivoIdentificacionIrregular>();

        foreach (var fila in historicosNoAsignados)
        {
            string identificacion =
                fila.IdentificacionOriginal
                    .Trim();

            if (string.IsNullOrWhiteSpace(identificacion))
            {
                continue;
            }

            // ============================================
            // CASO 1:
            // PREFIJO CORRECTO 4615,
            // PERO FORMATO IRREGULAR
            //
            // Ejemplo:
            // 4615- 3312
            // ============================================

            var coincidenciaEspacio =
                PatronConEspacioInterno.Match(
                    identificacion
                );

            if (coincidenciaEspacio.Success)
            {
                string consecutivo =
                    coincidenciaEspacio
                        .Groups[1]
                        .Value
                        .PadLeft(
                            4,
                            '0'
                        );

                resultados.Add(
                    new ActivoIdentificacionIrregular
                    {
                        NumeroFilaExcel =
                            fila.NumeroFilaExcel,

                        IdentificacionOriginal =
                            fila.IdentificacionOriginal,

                        Descripcion =
                            fila.DescripcionOriginal,

                        Observaciones =
                            fila.ObservacionesOriginales,

                        Tomo =
                            fila.Tomo,

                        Folio =
                            fila.Folio,

                        Asiento =
                            fila.Asiento,

                        TipoIrregularidad =
                            "FORMATO_PLACA_IRREGULAR",

                        PlacaSugerida =
                            $"4615-{consecutivo}",

                        RequiereRevision =
                            true
                    }
                );

                continue;
            }

            // ============================================
            // CASO 2:
            // TIENE ESTRUCTURA DE PLACA,
            // PERO EL PREFIJO NO ES 4615
            //
            // Ejemplos:
            // 4616-4060
            // 4614-4088
            // 4215-4217
            // ============================================

            var coincidenciaPrefijo =
                PatronPrefijoDistinto.Match(
                    identificacion
                );

            if (coincidenciaPrefijo.Success)
            {
                string prefijo =
                    coincidenciaPrefijo
                        .Groups[1]
                        .Value;

                string consecutivo =
                    coincidenciaPrefijo
                        .Groups[2]
                        .Value;

                if (prefijo != "4615")
                {
                    resultados.Add(
                        new ActivoIdentificacionIrregular
                        {
                            NumeroFilaExcel =
                                fila.NumeroFilaExcel,

                            IdentificacionOriginal =
                                fila.IdentificacionOriginal,

                            Descripcion =
                                fila.DescripcionOriginal,

                            Observaciones =
                                fila.ObservacionesOriginales,

                            Tomo =
                                fila.Tomo,

                            Folio =
                                fila.Folio,

                            Asiento =
                                fila.Asiento,

                            TipoIrregularidad =
                                "PREFIJO_PLACA_NO_RECONOCIDO",

                            PlacaSugerida =
                                null,

                            RequiereRevision =
                                true
                        }
                    );
                }

                continue;
            }
        }

        return resultados;
    }
}