using EscuelaInventario.Migracion.Modelos;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class ClasificadorMovimientosHistoricosService
{
    public void Clasificar(
        List<MovimientoHistoricoDetectado> movimientos)
    {
        foreach (var movimiento in movimientos)
        {
            movimiento.TipoMovimiento =
                ClasificarTexto(
                    movimiento.TextoCompleto
                );
        }
    }

    private static string ClasificarTexto(
        string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "NO_CLASIFICADO";
        }

        string textoNormalizado =
            NormalizarTexto(texto);

        // ============================================
        // BAJA
        // ============================================

        if (
            ContieneAlguno(
                textoNormalizado,
                "dado de baja",
                "dada de baja",
                "dados de baja",
                "dadas de baja",
                "da de baja",
                "dan de baja",
                "se les da de baja",
                "se da de baja",
                "excluye del patrimonio",
                "excluyen del patrimonio"
            )
        )
        {
            return "BAJA";
        }

        // ============================================
        // CAMBIO DE ESTADO FÍSICO
        //
        // IMPORTANTE:
        //
        // Solo clasificamos como CAMBIO_ESTADO
        // cuando existe una transición entre los
        // estados institucionales:
        //
        // Excelente
        // Bueno
        // Regular
        // Malo
        //
        // Esto evita confundir expresiones como:
        //
        // "cambio de estado de color verde a azul"
        // "cambio de estado sin control"
        // "cambio de estado de cantidad..."
        //
        // que realmente son MODIFICACIONES.
        // ============================================

        if (EsCambioEstadoFisico(textoNormalizado))
        {
            return "CAMBIO_ESTADO";
        }

        // ============================================
        // TRASLADO
        // ============================================

        if (
            Regex.IsMatch(
                textoNormalizado,
                @"\btrasladad[oa]s?\b",
                RegexOptions.IgnoreCase
            )
            ||
            Regex.IsMatch(
                textoNormalizado,
                @"\bse traslado\b",
                RegexOptions.IgnoreCase
            )
            ||
            Regex.IsMatch(
                textoNormalizado,
                @"\bse traslada\b",
                RegexOptions.IgnoreCase
            )
            ||
            ContieneAlguno(
                textoNormalizado,
                "fue trasladado",
                "fue trasladada",
                "fueron trasladados",
                "fueron trasladadas",
                "se traslada de ubicacion",
                "se traslado de",
                "cambio de ubicacion",
                "cambia de ubicacion"
            )
        )
        {
            return "TRASLADO";
        }

        // ============================================
        // MODIFICACIÓN / CORRECCIÓN
        // ============================================

        if (
            ContieneAlguno(
                textoNormalizado,

                // Correcciones generales
                "registra cambio",
                "cambio de color",
                "cambio de descripcion",
                "cambia de color",
                "pasa a ser",
                "se modifica",
                "fue modificado",
                "fue modificada",
                "se corrige",
                "se actualiza",

                // Expresiones históricas que utilizan
                // "estado" para hablar de características,
                // no del estado físico institucional.
                "cambio de estado",
                "cambia de estado",
                "cambio su estado",
                "cambia su estado",
                "estado cambia",
                "cabio estado",
                "cabio de estado",

                // Corrección de ubicación
                "ubicacion correcta es",
                "su ubicacion correcta es",

                // Correcciones explícitas
                "lease correctamente",
                "descripcion correcta es",
                "su descripcion correcta es",
                "fue registrado por error",
                "fue registrada por error",

                // Información adicional de placa
                "reporta tener placa"
            )
        )
        {
            return "MODIFICACION";
        }

        // ============================================
        // CAMBIO TEXTUAL DE ALGUNA CARACTERÍSTICA
        //
        // Ejemplos:
        //
        // cambio de color verde a azul
        // cambio de olla pequeña a olla blanca
        // cambio de cantidad de 5 a 2
        //
        // Si fuese un verdadero cambio de estado
        // físico ya habría sido detectado arriba.
        // ============================================

        if (
            textoNormalizado.Contains(
                "cambio de ",
                StringComparison.OrdinalIgnoreCase
            )
            &&
            textoNormalizado.Contains(
                " a ",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return "MODIFICACION";
        }

        // ============================================
        // PERMANENCIA / REASIGNACIÓN DEL ESPACIO
        // ============================================

        if (
            ContieneAlguno(
                textoNormalizado,
                "permanece en",
                "se encuentra antigua aula",
                "se encuentra aula"
            )
        )
        {
            return "MODIFICACION";
        }

        return "NO_CLASIFICADO";
    }

    private static bool EsCambioEstadoFisico(
        string texto)
    {
        // ============================================
        // ESTADOS HISTÓRICOS RECONOCIDOS
        //
        // Incluimos variantes de género y formas
        // como "mal estado" / "buen estado".
        // ============================================

        const string estados =
            @"(?:excelente|" +
            @"bueno|" +
            @"buena|" +
            @"buen\s+estado|" +
            @"regular|" +
            @"malo|" +
            @"mala|" +
            @"mal\s+estado)";

        // ============================================
        // PASA / PASANDO
        //
        // Ejemplos:
        //
        // pasa de estado bueno a mal estado
        // pasa de Bueno a Regular
        // pasando de Bueno a Regular
        // ============================================

        if (
            Regex.IsMatch(
                texto,
                $@"\b(?:pasa|pasan|pasando)\s+" +
                $@"de(?:\s+estado)?\s+" +
                $@"{estados}\s+a\s+{estados}\b",
                RegexOptions.IgnoreCase
            )
        )
        {
            return true;
        }

        // ============================================
        // CAMBIA / CAMBIO / CAMBIÓ
        //
        // Como las tildes ya fueron eliminadas,
        // "cambió" llega aquí como "cambio".
        //
        // Reconoce:
        //
        // cambia de estado de bueno a malo
        // cambia de estado bueno a malo
        // cambió de estado de Bueno a Malo
        // cambió su estado de bueno a malo
        // su estado cambia de bueno a malo
        // cambio de Excelente a Regular
        // ============================================

        if (
            Regex.IsMatch(
                texto,
                $@"\b(?:cambia|cambio)\s+" +
                $@"(?:su\s+estado\s+)?" +
                $@"(?:de\s+estado\s+)?" +
                $@"(?:de\s+)?" +
                $@"{estados}\s+a\s+{estados}\b",
                RegexOptions.IgnoreCase
            )
        )
        {
            return true;
        }

        // ============================================
        // FORMA:
        //
        // "su estado cambia de bueno a malo"
        // ============================================

        if (
            Regex.IsMatch(
                texto,
                $@"\bsu\s+estado\s+" +
                $@"(?:cambia|cambio)\s+de\s+" +
                $@"{estados}\s+a\s+{estados}\b",
                RegexOptions.IgnoreCase
            )
        )
        {
            return true;
        }

        return false;
    }

    private static bool ContieneAlguno(
        string texto,
        params string[] patrones)
    {
        return patrones.Any(
            patron =>
                texto.Contains(
                    patron,
                    StringComparison.OrdinalIgnoreCase
                )
        );
    }

    private static string NormalizarTexto(
        string texto)
    {
        string resultado =
            texto
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