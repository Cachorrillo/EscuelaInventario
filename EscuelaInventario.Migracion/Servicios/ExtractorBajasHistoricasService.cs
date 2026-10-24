using EscuelaInventario.Migracion.Modelos;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class ExtractorBajasHistoricasService
{
    public List<BajaHistoricaDetectada> Extraer(
        List<MovimientoHistoricoDetectado> movimientos)
    {
        var resultados =
            new List<BajaHistoricaDetectada>();

        var bajas =
            movimientos
                .Where(x =>
                    x.TipoMovimiento == "BAJA")
                .ToList();

        foreach (var movimiento in bajas)
        {
            var baja =
                new BajaHistoricaDetectada
                {
                    Placas =
                        movimiento.Placas.ToList(),

                    TextoOriginal =
                        movimiento.TextoCompleto,

                    RegistrosLibro =
                        movimiento.RegistrosLibro.ToList()
                };

            ExtraerFecha(
                baja
            );

            ClasificarMotivo(
                baja
            );

            EvaluarRevision(
                baja
            );

            resultados.Add(
                baja
            );
        }

        return resultados;
    }

    private static void ExtraerFecha(
        BajaHistoricaDetectada baja)
    {
        string texto =
            baja.TextoOriginal;

        // ============================================
        // FECHA NUMÉRICA
        //
        // Reconoce:
        // 15/06/2015
        // 15/06/15
        // 15 - 06 - 2015
        // 003/06/2026
        // ============================================

        var fechaNumerica =
            Regex.Match(
                texto,
                @"(?<!\d)(\d{1,3})\s*[/-]+\s*(\d{1,3})\s*[/-]+\s*(\d{2,4})(?!\d)"
            );

        if (fechaNumerica.Success)
        {
            baja.FechaOriginal =
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
                    baja.FechaMovimiento =
                        new DateTime(
                            anio,
                            mes,
                            dia
                        );
                }
                catch
                {
                    baja.FechaMovimiento =
                        null;
                }
            }

            return;
        }

        // ============================================
        // FECHA CON MES EN TEXTO
        //
        // Reconoce:
        // 15 de junio del 2015
        // 15 de junio de 2015
        // 15 junio, 2015
        // ============================================

        var fechaTexto =
            Regex.Match(
                texto,
                @"(?<!\d)(\d{1,2})\s+(?:de\s+)?([a-záéíóúñ]+)(?:\s+de|\s+del|,)?\s*(\d{4})(?!\d)",
                RegexOptions.IgnoreCase
            );

        if (fechaTexto.Success)
        {
            baja.FechaOriginal =
                fechaTexto.Value.Trim();

            if (
                IntentarParsearFechaTexto(
                    fechaTexto,
                    out DateTime fecha
                )
            )
            {
                baja.FechaMovimiento =
                    fecha;
            }
        }
    }

    private static void ClasificarMotivo(
        BajaHistoricaDetectada baja)
    {
        string texto =
            NormalizarTexto(
                baja.TextoOriginal
            );

        // ============================================
        // ROBO
        // ============================================

        if (
            texto.Contains("robo") ||
            texto.Contains("robado") ||
            texto.Contains("sustraccion") ||
            texto.Contains("sustraido")
        )
        {
            baja.MotivoNormalizado =
                "ROBO";

            baja.MotivoOriginal =
                "Motivo relacionado con robo o sustracción.";

            return;
        }

        // ============================================
        // ERROR DE PLAQUEO / REGISTRO
        // ============================================

        if (
    texto.Contains("plaqueado por error") ||
    texto.Contains("plaqueados por error") ||
    texto.Contains("plaqueada por error") ||
    texto.Contains("plaqueadas por error") ||
    texto.Contains("registrado por error") ||
    texto.Contains("registrados por error") ||

    (
        texto.Contains("por error") &&
        (
            texto.Contains("se plaqueo") ||
            texto.Contains("se plaquearon") ||
            texto.Contains("se plaqueo este articulo") ||
            texto.Contains("se plaqueo este activo")
        )
    )
)
        {
            baja.MotivoNormalizado =
                "ERROR_PLAQUEO";

            baja.MotivoOriginal =
                "Baja asociada a plaqueo o registro por error.";

            return;
        }

        // ============================================
        // INSERVIBILIDAD
        // ============================================

        if (
            texto.Contains("inservible") ||
            texto.Contains("inservibilidad") ||
            texto.Contains("mal estado") ||
            texto.Contains("deteriorado") ||
            texto.Contains("deteriorada") ||
            texto.Contains("deterioro") ||
            texto.Contains("dañado") ||
            texto.Contains("dañada")
        )
        {
            baja.MotivoNormalizado =
                "INSERVIBILIDAD";

            baja.MotivoOriginal =
                "Baja asociada a deterioro, daño o inservibilidad.";

            return;
        }

        // ============================================
        // OTRO / NO DETERMINADO
        // ============================================

        if (
            texto.Contains("se da de baja") ||
            texto.Contains("se le da de baja") ||
            texto.Contains("se les da de baja") ||
            texto.Contains("dar de baja") ||
            texto.Contains("dado de baja") ||
            texto.Contains("dados de baja")
        )
        {
            baja.MotivoNormalizado =
                "NO_DETERMINADO";

            baja.MotivoOriginal =
                "El texto indica una baja, pero no permite determinar con seguridad el motivo.";

            return;
        }

        baja.MotivoNormalizado =
            "OTRO";

        baja.MotivoOriginal =
            "Motivo no reconocido automáticamente.";
    }

    private static void EvaluarRevision(
        BajaHistoricaDetectada baja)
    {
        var motivos =
            new List<string>();

        if (!baja.FechaMovimiento.HasValue)
        {
            motivos.Add(
                "No se pudo normalizar la fecha."
            );
        }

        if (
            baja.MotivoNormalizado == "NO_DETERMINADO" ||
            baja.MotivoNormalizado == "OTRO"
        )
        {
            motivos.Add(
                "El motivo de la baja requiere revisión."
            );
        }

        baja.RequiereRevision =
            motivos.Count > 0;

        baja.MotivoRevision =
            string.Join(
                " ",
                motivos
            );
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