using EscuelaInventario.Migracion.Modelos;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class ExtractorTrasladosHistoricosService
{
    public List<TrasladoHistoricoDetectado> Extraer(
        List<MovimientoHistoricoDetectado> movimientos)
    {
        var resultados =
            new List<TrasladoHistoricoDetectado>();

        var traslados =
            movimientos
                .Where(x =>
                    x.TipoMovimiento == "TRASLADO")
                .ToList();

        foreach (var movimiento in traslados)
        {
            var traslado =
                new TrasladoHistoricoDetectado
                {
                    Placas =
                        movimiento.Placas.ToList(),

                    TextoOriginal =
                        movimiento.TextoCompleto,

                    RegistrosLibro =
                        movimiento.RegistrosLibro.ToList()
                };

            ExtraerUbicaciones(
                traslado
            );

            ExtraerFecha(
                traslado
            );

            EvaluarRevision(
                traslado
            );

            resultados.Add(
                traslado
            );
        }

        return resultados;
    }

    private static void ExtraerUbicaciones(
        TrasladoHistoricoDetectado traslado)
    {
        string texto =
            traslado.TextoOriginal;

        // ============================================
        // ORIGEN + DESTINO
        // ============================================

        var patrones =
            new[]
            {
                @"(?:fue|fueron)?\s*trasladad[oa]s?\s+de(?:l| la)?\s+(?<origen>.+?)\s+al\s+(?<destino>(?:area|área|ârea|àrea|aréa|áre)\s*(?:n[º°o.]*)?\s*#?\s*\d{1,3})(?=\s*,?\s*(?:a\s+partir|apartir|partir|desde)\b|\s*[.]?\s*$)",

                @"(?:fue|fueron)?\s*trasladad[oa]s?\s+de(?:l| la)?\s+(?<origen>.+?)\s+a(?:l| la)?\s+(?<destino>.+?)(?=\s*,?\s*(?:a\s+partir|apartir|partir|desde)\b|\s*[.]?\s*$)",

                @"se\s+traslad[oó]\s+de(?:l| la)?\s+(?<origen>.+?)\s+a(?:l| la)?\s+(?<destino>.+?)(?=\s*,?\s*(?:a\s+partir|apartir|partir|desde)\b|\s*[.]?\s*$)",

                @"se\s+traslada\s+de\s+ubicaci[oó]n\s+(?:de\s+)?(?<origen>.+?)\s+a(?:l| la)?\s+(?<destino>.+?)(?=\s*,?\s*(?:a\s+partir|apartir|partir|desde)\b|\s*[.]?\s*$)",

                @"se\s+traslada\s+de(?:l| la)?\s+(?<origen>.+?)\s+a(?:l| la)?\s+(?<destino>.+?)(?=\s*,?\s*(?:a\s+partir|apartir|partir|desde)\b|\s*[.]?\s*$)",

                @"cambia\s+de\s+ubicaci[oó]n\s*,?\s*de(?:l| la)?\s+(?<origen>.+?)\s+a(?:l| la)?\s+(?<destino>.+?)(?=\s*,?\s*(?:a\s+partir|apartir|partir|desde)\b|\s*[.]?\s*$)"
            };

        foreach (string patron in patrones)
        {
            var coincidencia =
                Regex.Match(
                    texto,
                    patron,
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline
                );

            if (!coincidencia.Success)
            {
                continue;
            }

            traslado.UbicacionAnteriorOriginal =
                LimpiarUbicacion(
                    coincidencia
                        .Groups["origen"]
                        .Value
                );

            traslado.UbicacionNuevaOriginal =
                LimpiarUbicacion(
                    coincidencia
                        .Groups["destino"]
                        .Value
                );

            traslado.AreaAnteriorNumero =
                ExtraerNumeroArea(
                    traslado.UbicacionAnteriorOriginal
                );

            traslado.AreaNuevaNumero =
                ExtraerNumeroArea(
                    traslado.UbicacionNuevaOriginal
                );

            return;
        }

        // ============================================
        // SOLO DESTINO
        //
        // Ejemplo real:
        // "se trasladó a Aula #11"
        //
        // Se conserva el destino, pero seguirá
        // requiriendo revisión porque falta el origen.
        // ============================================

        var soloDestino =
            Regex.Match(
                texto,
                @"se\s+traslad[oó]\s+a(?:l| la)?\s+(?<destino>.+?)(?=\s*,?\s*(?:a\s+partir|apartir|partir|desde)\b|\s*[.]?\s*$)",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline
            );

        if (soloDestino.Success)
        {
            traslado.UbicacionNuevaOriginal =
                LimpiarUbicacion(
                    soloDestino
                        .Groups["destino"]
                        .Value
                );

            traslado.AreaNuevaNumero =
                ExtraerNumeroArea(
                    traslado.UbicacionNuevaOriginal
                );
        }
    }

    private static int? ExtraerNumeroArea(
        string ubicacion)
    {
        if (string.IsNullOrWhiteSpace(ubicacion))
        {
            return null;
        }

        string texto =
            ubicacion.Trim();

        // ============================================
        // ÁREA ESCRITA EXPLÍCITAMENTE
        //
        // Reconoce:
        //
        // Área 12
        // Area 12
        // AREA 12
        // Ârea 12
        // Aréa 12
        // Área #12
        // Área No. 12
        //
        // No convierte ubicaciones como:
        //
        // Área pasillo
        // Aula 12
        // Bodega 12
        // ============================================

        var coincidenciaArea =
            Regex.Match(
                texto,
                @"\b(?:area|área|ârea|àrea|aréa|áre)\s*(?:n[º°o.]*)?\s*#?\s*(\d{1,3})\b",
                RegexOptions.IgnoreCase
            );

        if (coincidenciaArea.Success)
        {
            if (
                int.TryParse(
                    coincidenciaArea.Groups[1].Value,
                    out int numeroArea
                ) &&
                numeroArea >= 1 &&
                numeroArea <= 70
            )
            {
                return numeroArea;
            }
        }


        // ============================================
        // NÚMERO SEGUIDO DE "AL ÁREA"
        //
        // Caso real del inventario:
        //
        // "Área 55 a la 56 al Área"
        //
        // El destino extraído queda como:
        // "56 al Área"
        //
        // En este caso el número 56 corresponde
        // al área de destino.
        // ============================================

        var numeroConAreaSobrante =
            Regex.Match(
                texto,
                @"^\s*#?\s*(\d{1,3})\s+al\s+(?:area|área|ârea|àrea|aréa|áre)\s*$",
                RegexOptions.IgnoreCase
            );

        if (
            numeroConAreaSobrante.Success &&
            int.TryParse(
                numeroConAreaSobrante.Groups[1].Value,
                out int numeroConSobrante
            ) &&
            numeroConSobrante >= 1 &&
            numeroConSobrante <= 71
        )
        {
            return numeroConSobrante;
        }

        // ============================================
        // UBICACIÓN QUE ES SOLAMENTE UN NÚMERO
        //
        // Ejemplo real:
        //
        // "fue trasladado del 12 al Área 6"
        //
        // El origen extraído es simplemente "12".
        //
        // Solo aceptamos números del 1 al 71.
        //
        // Esto evita convertir:
        //
        // Aula #20
        // Bodega 12
        // Pasillo 3
        // ============================================

        var soloNumero =
            Regex.Match(
                texto,
                @"^\s*#?\s*(\d{1,3})\s*$"
            );

        if (
            soloNumero.Success &&
            int.TryParse(
                soloNumero.Groups[1].Value,
                out int numeroSolo
            ) &&
            numeroSolo >= 1 &&
            numeroSolo <= 70
        )
        {
            return numeroSolo;
        }

        return null;
    }

    private static void ExtraerFecha(
        TrasladoHistoricoDetectado traslado)
    {
        string texto =
            traslado.TextoOriginal;

        // ============================================
        // FECHA NUMÉRICA
        //
        // Reconoce:
        //
        // 17/04/2015
        // 17-04-2015
        // 15/03/15
        // 21 /11 /2018
        // 30 / 11 / 2018
        // ============================================

        var fechaNumerica =
            Regex.Match(
                texto,
                @"(?<!\d)(\d{1,3})\s*[/-]\s*(\d{1,3})\s*[/-]\s*(\d{2,4})(?!\d)"
            );

        if (fechaNumerica.Success)
        {
            traslado.FechaOriginal =
                fechaNumerica.Value.Trim();

            if (
                int.TryParse(
                    fechaNumerica.Groups[1].Value,
                    out int dia
                ) &&
                int.TryParse(
                    fechaNumerica.Groups[2].Value,
                    out int mes
                ) &&
                int.TryParse(
                    fechaNumerica.Groups[3].Value,
                    out int anio
                )
            )
            {
                if (anio < 100)
                {
                    anio += 2000;
                }

                try
                {
                    traslado.FechaMovimiento =
                        new DateTime(
                            anio,
                            mes,
                            dia
                        );
                }
                catch
                {
                    traslado.FechaMovimiento =
                        null;
                }
            }

            return;
        }

        // ============================================
        // FECHA CON MES EN TEXTO
        //
        // Reconoce:
        //
        // 17 de abril de 2015
        // 17 abril, 2015
        // 17 abril,2015
        // 17 abril 2015
        // 15 de junio del 2015
        // ============================================

        var fechaTexto =
            Regex.Match(
                texto,
                @"(?<!\d)(\d{1,2})\s+(?:de\s+)?([a-záéíóúñ]+)(?:\s+de|\s+del|,)?\s*(\d{4})(?!\d)",
                RegexOptions.IgnoreCase
            );

        if (fechaTexto.Success)
        {
            traslado.FechaOriginal =
                fechaTexto.Value.Trim();

            if (
                IntentarParsearFechaTexto(
                    fechaTexto,
                    out DateTime fecha
                )
            )
            {
                traslado.FechaMovimiento =
                    fecha;
            }
        }
    }

    private static bool IntentarParsearFechaTexto(
        Match coincidencia,
        out DateTime fecha)
    {
        fecha =
            default;

        if (
            !int.TryParse(
                coincidencia.Groups[1].Value,
                out int dia
            )
        )
        {
            return false;
        }

        string mesTexto =
            NormalizarTexto(
                coincidencia.Groups[2].Value
            );

        if (
            !int.TryParse(
                coincidencia.Groups[3].Value,
                out int anio
            )
        )
        {
            return false;
        }

        int? mes =
            mesTexto switch
            {
                "enero" => 1,
                "febrero" => 2,
                "marzo" => 3,
                "abril" => 4,
                "mayo" => 5,
                "junio" => 6,
                "julio" => 7,
                "agosto" => 8,
                "septiembre" => 9,
                "setiembre" => 9,
                "octubre" => 10,
                "noviembre" => 11,
                "diciembre" => 12,
                _ => null
            };

        if (!mes.HasValue)
        {
            return false;
        }

        try
        {
            fecha =
                new DateTime(
                    anio,
                    mes.Value,
                    dia
                );

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void EvaluarRevision(
        TrasladoHistoricoDetectado traslado)
    {
        var motivos =
            new List<string>();

        if (
            string.IsNullOrWhiteSpace(
                traslado.UbicacionAnteriorOriginal
            )
        )
        {
            motivos.Add(
                "No se pudo extraer la ubicación anterior."
            );
        }

        if (
            string.IsNullOrWhiteSpace(
                traslado.UbicacionNuevaOriginal
            )
        )
        {
            motivos.Add(
                "No se pudo extraer la ubicación nueva."
            );
        }

        if (!traslado.FechaMovimiento.HasValue)
        {
            motivos.Add(
                "No se pudo normalizar la fecha."
            );
        }

        traslado.RequiereRevision =
            motivos.Count > 0;

        traslado.MotivoRevision =
            string.Join(
                " ",
                motivos
            );
    }

    private static string LimpiarUbicacion(
        string valor)
    {
        string resultado =
            valor
                .Trim()
                .Trim(',', '.', ';', ':');

        resultado =
            Regex.Replace(
                resultado,
                @"\s+",
                " "
            );

        return resultado.Trim();
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