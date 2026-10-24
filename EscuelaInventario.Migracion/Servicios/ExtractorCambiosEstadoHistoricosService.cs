using EscuelaInventario.Migracion.Modelos;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class ExtractorCambiosEstadoHistoricosService
{
    public List<CambioEstadoHistoricoDetectado> Extraer(
        List<MovimientoHistoricoDetectado> movimientos)
    {
        var resultados =
            new List<CambioEstadoHistoricoDetectado>();

        var cambiosEstado =
            movimientos
                .Where(x =>
                    x.TipoMovimiento == "CAMBIO_ESTADO")
                .ToList();

        foreach (var movimiento in cambiosEstado)
        {
            var cambio =
                new CambioEstadoHistoricoDetectado
                {
                    Placas =
                        movimiento.Placas.ToList(),

                    TextoOriginal =
                        movimiento.TextoCompleto,

                    RegistrosLibro =
                        movimiento.RegistrosLibro.ToList()
                };

            ExtraerEstados(
                cambio
            );

            ExtraerFecha(
                cambio
            );

            EvaluarRevision(
                cambio
            );

            resultados.Add(
                cambio
            );
        }

        return resultados;
    }

    private static void ExtraerEstados(
    CambioEstadoHistoricoDetectado cambio)
    {
        string texto =
            cambio.TextoOriginal;

        // ============================================
        // ESTADOS QUE RECONOCEMOS
        // ============================================

        string estados =
            @"excelente|" +
            @"bueno|" +
            @"buena|" +
            @"regular|" +
            @"malo|" +
            @"mala|" +
            @"mal\s+estado|" +
            @"buen\s+estado";

        // ============================================
        // FORMAS HISTÓRICAS ENCONTRADAS
        //
        // Ejemplos:
        //
        // pasa de estado bueno a mal estado
        // pasa de Bueno a Regular
        // pasando de Bueno a Regular
        //
        // cambió su estado de bueno a malo
        // cambia de estado de bueno a regular
        // cambio de estado de Bueno a Regular
        // cambia de estado regular a malo
        //
        // su estado cambia de bueno a malo
        // ============================================

        var patrones =
            new[]
            {

            $@"\bcambio\s+de\s+" +
            $@"(?<anterior>{estados})\s+a\s+" +
            $@"(?<nuevo>{estados})\b",

            $@"pasa\s+de\s+estado\s+" +
            $@"(?<anterior>{estados})\s+a\s+" +
            $@"(?<nuevo>{estados})",

            $@"pasa\s+de\s+" +
            $@"(?<anterior>{estados})\s+a\s+" +
            $@"(?<nuevo>{estados})",

            $@"pasando\s+de\s+" +
            $@"(?<anterior>{estados})\s+a\s+" +
            $@"(?<nuevo>{estados})",

            // Reconoce:
            // cambia de estado de...
            // cambió de estado de...
            // cambio de estado de...
            // cambió su estado de...
            // cambia de estado regular a...
            $@"cambi(?:a|o|ó)\s+" +
            $@"(?:de\s+)?" +
            $@"(?:su\s+)?" +
            $@"estado\s+" +
            $@"(?:de\s+)?" +
            $@"(?<anterior>{estados})\s+a\s+" +
            $@"(?<nuevo>{estados})",

            // Reconoce:
            // su estado cambia de bueno a malo
            $@"su\s+estado\s+cambia\s+de\s+" +
            $@"(?<anterior>{estados})\s+a\s+" +
            $@"(?<nuevo>{estados})"
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

            cambio.EstadoAnteriorOriginal =
                LimpiarEstado(
                    coincidencia
                        .Groups["anterior"]
                        .Value
                );

            cambio.EstadoNuevoOriginal =
                LimpiarEstado(
                    coincidencia
                        .Groups["nuevo"]
                        .Value
                );

            cambio.EstadoAnteriorNormalizado =
                NormalizarEstado(
                    cambio.EstadoAnteriorOriginal
                );

            cambio.EstadoNuevoNormalizado =
                NormalizarEstado(
                    cambio.EstadoNuevoOriginal
                );

            return;
        }
    }

    private static string? NormalizarEstado(
        string valor)
    {
        string limpio =
            NormalizarTexto(
                valor
            );

        if (
            limpio == "excelente"
        )
        {
            return "Excelente";
        }

        if (
            limpio == "bueno" ||
            limpio == "buena" ||
            limpio == "buen estado"
        )
        {
            return "Bueno";
        }

        if (
            limpio == "regular"
        )
        {
            return "Regular";
        }

        if (
            limpio == "malo" ||
            limpio == "mala" ||
            limpio == "mal estado"
        )
        {
            return "Malo";
        }

        return null;
    }

    private static void ExtraerFecha(
        CambioEstadoHistoricoDetectado cambio)
    {
        string texto =
            cambio.TextoOriginal;

        // ============================================
        // FECHA NUMÉRICA
        // ============================================

        var fechaNumerica =
            Regex.Match(
                texto,
                @"(?<!\d)(\d{1,3})\s*[/-]+\s*(\d{1,3})\s*[/-]+\s*(\d{2,4})(?!\d)"
            );

        if (fechaNumerica.Success)
        {
            cambio.FechaOriginal =
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
                    cambio.FechaMovimiento =
                        new DateTime(
                            anio,
                            mes,
                            dia
                        );
                }
                catch
                {
                    cambio.FechaMovimiento =
                        null;
                }
            }

            return;
        }

        // ============================================
        // FECHA CON MES EN TEXTO
        // ============================================

        var fechaTexto =
            Regex.Match(
                texto,
                @"(?<!\d)(\d{1,2})\s+(?:de\s+)?([a-záéíóúñ]+)(?:\s+de|\s+del|,)?\s*(\d{4})(?!\d)",
                RegexOptions.IgnoreCase
            );

        if (!fechaTexto.Success)
        {
            return;
        }

        cambio.FechaOriginal =
            fechaTexto.Value.Trim();

        if (
            !int.TryParse(
                fechaTexto.Groups[1].Value,
                out int diaTexto
            ) ||
            !int.TryParse(
                fechaTexto.Groups[3].Value,
                out int anioTexto
            )
        )
        {
            return;
        }

        string mesTexto =
            NormalizarTexto(
                fechaTexto.Groups[2].Value
            );

        int? numeroMesTexto =
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

        if (!numeroMesTexto.HasValue)
        {
            return;
        }

        try
        {
            cambio.FechaMovimiento =
                new DateTime(
                    anioTexto,
                    numeroMesTexto.Value,
                    diaTexto
                );
        }
        catch
        {
            cambio.FechaMovimiento =
                null;
        }
    }

    private static void EvaluarRevision(
        CambioEstadoHistoricoDetectado cambio)
    {
        var motivos =
            new List<string>();

        if (
            string.IsNullOrWhiteSpace(
                cambio.EstadoAnteriorOriginal
            )
        )
        {
            motivos.Add(
                "No se pudo extraer el estado anterior."
            );
        }

        if (
            string.IsNullOrWhiteSpace(
                cambio.EstadoNuevoOriginal
            )
        )
        {
            motivos.Add(
                "No se pudo extraer el estado nuevo."
            );
        }

        if (
            cambio.EstadoAnteriorNormalizado == null
        )
        {
            motivos.Add(
                "El estado anterior no pudo normalizarse."
            );
        }

        if (
            cambio.EstadoNuevoNormalizado == null
        )
        {
            motivos.Add(
                "El estado nuevo no pudo normalizarse."
            );
        }

        if (!cambio.FechaMovimiento.HasValue)
        {
            motivos.Add(
                "No se pudo normalizar la fecha."
            );
        }

        cambio.RequiereRevision =
            motivos.Count > 0;

        cambio.MotivoRevision =
            string.Join(
                " ",
                motivos
            );
    }

    private static string LimpiarEstado(
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

        return resultado;
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