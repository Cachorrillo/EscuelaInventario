using EscuelaInventario.Migracion.Modelos;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class ExtractorModificacionesHistoricasService
{
    public List<ModificacionHistoricaDetectada> Extraer(
        List<MovimientoHistoricoDetectado> movimientos)
    {
        var resultados =
            new List<ModificacionHistoricaDetectada>();

        var modificaciones =
            movimientos
                .Where(x =>
                    x.TipoMovimiento == "MODIFICACION")
                .ToList();

        foreach (var movimiento in modificaciones)
        {
            var modificacion =
                new ModificacionHistoricaDetectada
                {
                    Placas =
                        movimiento.Placas.ToList(),

                    TextoOriginal =
                        movimiento.TextoCompleto,

                    RegistrosLibro =
                        movimiento.RegistrosLibro.ToList()
                };

            DetectarTipo(
                modificacion
            );

            ExtraerValores(
                modificacion
            );

            ExtraerFecha(
                modificacion
            );

            EvaluarRevision(
                modificacion
            );

            resultados.Add(
                modificacion
            );
        }

        return resultados;
    }

    private static void DetectarTipo(
        ModificacionHistoricaDetectada modificacion)
    {
        string texto =
            NormalizarTexto(
                modificacion.TextoOriginal
            );

        // ============================================
        // COLOR
        // ============================================

        if (
            texto.Contains("cambio de color") ||
            texto.Contains("cambia de color") ||
            texto.Contains("estado de color") ||
            Regex.IsMatch(
                texto,
                @"\bcolor\s+\w+\s+a\s+(?:color\s+)?\w+\b"
            )
        )
        {
            modificacion.TipoModificacion =
                "COLOR";

            return;
        }

        // ============================================
        // MATERIAL
        // ============================================

        if (
            texto.Contains("no es de madera") ||
            texto.Contains("es plastica") ||
            texto.Contains("es plastico") ||
            texto.Contains("material")
        )
        {
            modificacion.TipoModificacion =
                "MATERIAL";

            return;
        }

        // ============================================
        // CANTIDAD
        // ============================================

        if (
            texto.Contains("cambio de cantidad") ||
            Regex.IsMatch(
                texto,
                @"\bcantidad\s+de\s+\d+\s+a\s+\d+\b"
            )
        )
        {
            modificacion.TipoModificacion =
                "CANTIDAD";

            return;
        }

        // ============================================
        // ACCESORIO / PIEZA
        // ============================================

        if (
            texto.Contains("se sustituyo") ||
            texto.Contains("se sustituyó") ||
            texto.Contains("sin control") ||
            texto.Contains("sin tapa") ||
            texto.Contains("con tapa")
        )
        {
            modificacion.TipoModificacion =
                "ACCESORIO";

            return;
        }

        // ============================================
        // UBICACIÓN DESCRIPTIVA
        //
        // No es un traslado real. Son correcciones
        // del texto descriptivo del bien.
        // ============================================

        if (
            texto.Contains("permanece en") ||
            texto.Contains("ubicacion correcta es") ||
            texto.Contains("su ubicacion correcta es") ||
            texto.Contains("se encuentra antigua aula") ||
            texto.Contains("se encuentra aula")
        )
        {
            modificacion.TipoModificacion =
                "UBICACION_DESCRIPTIVA";

            return;
        }

        // ============================================
        // CORRECCIÓN
        // ============================================

        if (
            texto.Contains("lease correctamente") ||
            texto.Contains("descripcion correcta") ||
            texto.Contains("se corrige") ||
            texto.Contains("registrado por error") ||
            texto.Contains("registrada por error") ||
            texto.Contains("reporta tener placa")
        )
        {
            modificacion.TipoModificacion =
                "CORRECCION";

            return;
        }

        // ============================================
        // DESCRIPCIÓN
        // ============================================

        if (
            texto.Contains("cambio de descripcion") ||
            texto.Contains("pasa a ser") ||
            texto.Contains("registra cambio") ||
            texto.Contains("cambio de ")
        )
        {
            modificacion.TipoModificacion =
                "DESCRIPCION";

            return;
        }

        modificacion.TipoModificacion =
            "OTRO";
    }

    private static void ExtraerValores(
        ModificacionHistoricaDetectada modificacion)
    {
        string texto =
            modificacion.TextoOriginal;

        // ============================================
        // CASOS DE COLOR
        //
        // Ejemplos:
        // de color verde a color azul
        // color amarillo a color rojo
        // ============================================

        if (
    modificacion.TipoModificacion ==
    "COLOR"
)
        {
            // ============================================
            // Forma explícita:
            //
            // de color verde a color azul
            // color amarillo a rojo
            // ============================================

            var coincidenciaColor =
                Regex.Match(
                    texto,
                    @"(?:de\s+)?color\s+" +
                    @"(?<anterior>[a-záéíóúñ]+)" +
                    @"\s+a\s+" +
                    @"(?:color\s+)?" +
                    @"(?<nuevo>[a-záéíóúñ]+)",
                    RegexOptions.IgnoreCase
                );

            if (coincidenciaColor.Success)
            {
                modificacion.ValorAnteriorOriginal =
                    LimpiarValor(
                        coincidenciaColor
                            .Groups["anterior"]
                            .Value
                    );

                modificacion.ValorNuevoOriginal =
                    LimpiarValor(
                        coincidenciaColor
                            .Groups["nuevo"]
                            .Value
                    );

                return;
            }

            // ============================================
            // Forma donde únicamente se expresa
            // el nuevo color:
            //
            // "registra cambio de color,
            //  y pasa a ser celeste y rosado"
            //
            // No inventamos un valor anterior.
            // ============================================

            var soloNuevoColor =
                Regex.Match(
                    texto,
                    @"cambio\s+de\s+color.*?" +
                    @"pasa\s+a\s+ser\s+" +
                    @"(?<nuevo>.+?)" +
                    @"(?=\s+a\s+partir\b|[.]?\s*$)",
                    RegexOptions.IgnoreCase |
                    RegexOptions.Singleline
                );

            if (soloNuevoColor.Success)
            {
                modificacion.ValorNuevoOriginal =
                    LimpiarValor(
                        soloNuevoColor
                            .Groups["nuevo"]
                            .Value
                    );
            }

            // Muy importante:
            // si es COLOR no dejamos que caiga al
            // regex general "cambio de ... a ...",
            // porque podría capturar texto incorrecto.
            return;
        }

        // ============================================
        // CASOS DE MATERIAL
        //
        // Ejemplo:
        // no es de madera es plástica
        // ============================================

        if (
            modificacion.TipoModificacion ==
            "MATERIAL"
        )
        {
            var coincidenciaMaterial =
                Regex.Match(
                    texto,
                    @"no\s+es\s+de\s+(?<anterior>[a-záéíóúñ]+)\s+es\s+(?<nuevo>[a-záéíóúñ]+)",
                    RegexOptions.IgnoreCase
                );

            if (coincidenciaMaterial.Success)
            {
                modificacion.ValorAnteriorOriginal =
                    LimpiarValor(
                        coincidenciaMaterial
                            .Groups["anterior"]
                            .Value
                    );

                modificacion.ValorNuevoOriginal =
                    LimpiarValor(
                        coincidenciaMaterial
                            .Groups["nuevo"]
                            .Value
                    );

                return;
            }
        }

        // ============================================
        // CASOS DE CANTIDAD
        //
        // Ejemplo:
        // cantidad de 5 a 2 baldes
        // ============================================

        if (
            modificacion.TipoModificacion ==
            "CANTIDAD"
        )
        {
            var coincidenciaCantidad =
                Regex.Match(
                    texto,
                    @"cantidad\s+de\s+(?<anterior>\d+)\s+a\s+(?<nuevo>\d+)",
                    RegexOptions.IgnoreCase
                );

            if (coincidenciaCantidad.Success)
            {
                modificacion.ValorAnteriorOriginal =
                    coincidenciaCantidad
                        .Groups["anterior"]
                        .Value;

                modificacion.ValorNuevoOriginal =
                    coincidenciaCantidad
                        .Groups["nuevo"]
                        .Value;

                return;
            }
        }

        // ============================================
        // CAMBIO GENERAL "DE ... A ..."
        //
        // Solo lo usamos para modificaciones donde
        // realmente hay una estructura explícita.
        // ============================================

        var coincidenciaGeneral =
            Regex.Match(
                texto,
                @"cambio\s+de\s+(?<anterior>.+?)\s+a\s+(?<nuevo>.+?)(?=\s*,?\s*a\s+partir\b|\s*,?\s*apartir\b|[.]?\s*$)",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline
            );

        if (coincidenciaGeneral.Success)
        {
            modificacion.ValorAnteriorOriginal =
                LimpiarValor(
                    coincidenciaGeneral
                        .Groups["anterior"]
                        .Value
                );

            modificacion.ValorNuevoOriginal =
                LimpiarValor(
                    coincidenciaGeneral
                        .Groups["nuevo"]
                        .Value
                );
        }
    }

    private static void ExtraerFecha(
        ModificacionHistoricaDetectada modificacion)
    {
        string texto =
            modificacion.TextoOriginal;

        var fechasNumericasEncontradas =
    Regex.Matches(
        texto,
        @"(?<!\d)(\d{1,3})\s*[/-]+\s*(\d{1,3})\s*[/-]+\s*(\d{2,4})(?!\d)"
    )
    .Select(x => x.Value.Trim())
    .Distinct(
        StringComparer.OrdinalIgnoreCase
    )
    .ToList();

        if (fechasNumericasEncontradas.Count > 1)
        {
            modificacion.TieneMultiplesFechas = true;

            modificacion.FechaOriginal =
                string.Join(
                    " | ",
                    fechasNumericasEncontradas
                );

            modificacion.FechaMovimiento = null;

            return;
        }

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
            modificacion.FechaOriginal =
                fechaNumerica.Value.Trim();

            if (
                int.TryParse(
                    fechaNumerica.Groups[1].Value,
                    out int dia
                )
                &&
                int.TryParse(
                    fechaNumerica.Groups[2].Value,
                    out int mes
                )
                &&
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
                    modificacion.FechaMovimiento =
                        new DateTime(
                            anio,
                            mes,
                            dia
                        );
                }
                catch
                {
                    modificacion.FechaMovimiento =
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

        modificacion.FechaOriginal =
            fechaTexto.Value.Trim();

        if (
            !int.TryParse(
                fechaTexto.Groups[1].Value,
                out int diaTexto
            )
            ||
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
            modificacion.FechaMovimiento =
                new DateTime(
                    anioTexto,
                    numeroMesTexto.Value,
                    diaTexto
                );
        }
        catch
        {
            modificacion.FechaMovimiento =
                null;
        }
    }

    private static void EvaluarRevision(
    ModificacionHistoricaDetectada modificacion)
    {
        var motivos =
            new List<string>();

        if (
            modificacion.TipoModificacion ==
            "OTRO"
        )
        {
            motivos.Add(
                "No se pudo determinar automáticamente el tipo de modificación."
            );
        }

        if (modificacion.TieneMultiplesFechas)
        {
            motivos.Add(
                "El registro contiene más de una fecha posible y requiere revisión manual."
            );
        }

        if (
            !modificacion.FechaMovimiento.HasValue &&
            !modificacion.TieneMultiplesFechas
        )
        {
            motivos.Add(
                "No se pudo normalizar la fecha."
            );
        }

        modificacion.RequiereRevision =
            motivos.Count > 0;

        modificacion.MotivoRevision =
            string.Join(
                " ",
                motivos
            );
    }

    private static string LimpiarValor(
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
        string resultado =
            valor
                .ToLowerInvariant()
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u")
                .Replace("ü", "u");

        resultado =
            Regex.Replace(
                resultado,
                @"\s+",
                " "
            );

        return resultado.Trim();
    }
}