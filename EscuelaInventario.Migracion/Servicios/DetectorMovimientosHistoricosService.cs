using EscuelaInventario.Migracion.Modelos;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class DetectorMovimientosHistoricosService
{
    // Reconoce:
    // 4615-1234
    // 4615- 1234
    // 4615 - 1234
    // 4615-866 -> 4615-0866
    private static readonly Regex PatronPlacaConPrefijo =
        new(
            @"\b4615\s*-\s*(\d{3,4})(?!\d)",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase
        );

    // Reconoce casos como:
    // "El activo con placa No 0910..."
    private static readonly Regex PatronPlacaAbreviada =
        new(
            @"\bplacas?\s+(?:n(?:o|º|°)?\.?\s*)?(\d{3,4})\b(?!\s*-)",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase
        );

    // Identificadores que aparecen solos en la
    // columna de identificación.
    //
    // Ejemplos:
    // 4616-4060
    // 4614-4088
    // 4615- 3314
    // 4215-4217
    private static readonly Regex PatronIdentificacionAislada =
        new(
            @"^\s*\d{4}\s*-\s*\d{3,4}\s*$",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase
        );

    public List<MovimientoHistoricoDetectado> Detectar(
        List<FilaHistoricaInventario> registrosHistoricos)
    {
        var movimientos =
            new List<MovimientoHistoricoDetectado>();

        MovimientoHistoricoDetectado? movimientoActual =
            null;

        var registrosOcupados =
            registrosHistoricos
                .Where(x =>
                    x.TieneContenidoReal &&
                    x.Tomo.HasValue &&
                    x.Folio.HasValue &&
                    x.Asiento.HasValue)
                .OrderBy(x => x.Tomo)
                .ThenBy(x => x.Folio)
                .ThenBy(x => x.Asiento)
                .ToList();

        foreach (var registro in registrosOcupados)
        {
            string textoFila =
                ObtenerTextoPrincipal(registro);

            if (string.IsNullOrWhiteSpace(textoFila))
            {
                continue;
            }

            // ============================================
            // IDENTIFICADOR AISLADO
            //
            // IMPORTANTE:
            // La comprobación se hace directamente
            // sobre la columna de identificación.
            //
            // No usamos el texto combinado porque
            // podría existir información adicional
            // en descripción u otras columnas.
            // ============================================

            if (
                EsIdentificacionAislada(
                    registro.IdentificacionOriginal
                )
            )
            {
                CerrarMovimientoActual(
                    movimientos,
                    ref movimientoActual
                );

                continue;
            }

            // ============================================
            // NOTAS ADMINISTRATIVAS
            // ============================================

            if (EsNotaAdministrativa(textoFila))
            {
                CerrarMovimientoActual(
                    movimientos,
                    ref movimientoActual
                );

                continue;
            }

            // ============================================
            // INICIO DE UNA NUEVA ANOTACIÓN
            // ============================================

            if (PareceInicioMovimiento(textoFila))
            {
                // ============================================
                // CONTINUACIÓN DE UNA LISTA GRUPAL
                //
                // Algunas anotaciones enumeran muchas placas
                // durante varias líneas.
                //
                // Ejemplo:
                //
                // "Los activos con placas No 4615-3225..."
                // "No 4615-3230, No 4615-3231..."
                //
                // La segunda línea NO inicia un movimiento nuevo.
                // ============================================

                bool empiezaConNoPlaca =
                    Regex.IsMatch(
                        textoFila.Trim(),
                        @"^n(?:o|º|°)?\.?\s+4615\s*-\s*\d{3,4}",
                        RegexOptions.IgnoreCase
                    );

                if (
                    empiezaConNoPlaca &&
                    movimientoActual != null
                )
                {
                    movimientoActual
                        .RegistrosLibro
                        .Add(registro);

                    continue;
                }

                CerrarMovimientoActual(
                    movimientos,
                    ref movimientoActual
                );

                movimientoActual =
                    new MovimientoHistoricoDetectado();

                movimientoActual
                    .RegistrosLibro
                    .Add(registro);

                continue;
            }

            // ============================================
            // CONTINUACIÓN
            // ============================================

            if (movimientoActual != null)
            {
                movimientoActual
                    .RegistrosLibro
                    .Add(registro);
            }
        }

        // ============================================
        // CERRAR ÚLTIMO MOVIMIENTO
        // ============================================

        CerrarMovimientoActual(
            movimientos,
            ref movimientoActual
        );

        return movimientos;
    }

    private static void CerrarMovimientoActual(
        List<MovimientoHistoricoDetectado> movimientos,
        ref MovimientoHistoricoDetectado? movimientoActual)
    {
        if (movimientoActual == null)
        {
            return;
        }

        FinalizarMovimiento(
            movimientoActual
        );

        movimientos.Add(
            movimientoActual
        );

        movimientoActual = null;
    }

    private static bool PareceInicioMovimiento(
        string texto)
    {
        string limpio =
            texto.Trim();

        // Reconoce:
        //
        // El activo con placa...
        // El activo con la placa...
        // Los activos con placa...
        // Los activos con placas...
        // Los activos con las placas...
        if (
            Regex.IsMatch(
                limpio,
                @"\bactivo(?:s)?\b.*\bplacas?\b",
                RegexOptions.IgnoreCase
            )
        )
        {
            return true;
        }

        // Algunos registros comienzan directamente:
        //
        // No 4615-3230, No 4615-3231...
        if (
            Regex.IsMatch(
                limpio,
                @"^\s*n(?:o|º|°)?\.?\s+4615\s*-\s*\d{3,4}",
                RegexOptions.IgnoreCase
            )
        )
        {
            return true;
        }

        return false;
    }

    private static bool EsIdentificacionAislada(
        string textoIdentificacion)
    {
        if (
            string.IsNullOrWhiteSpace(
                textoIdentificacion
            )
        )
        {
            return false;
        }

        return PatronIdentificacionAislada
            .IsMatch(
                textoIdentificacion.Trim()
            );
    }

    private static bool EsNotaAdministrativa(
        string texto)
    {
        string limpio =
            texto.Trim();

        if (
            limpio.StartsWith(
                "Se detalla que",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return true;
        }

        if (
            limpio.StartsWith(
                "Nota aclaratoria",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return true;
        }

        return false;
    }

    private static void FinalizarMovimiento(
        MovimientoHistoricoDetectado movimiento)
    {
        var partesTexto =
            movimiento.RegistrosLibro
                .Select(ObtenerTextoPrincipal)
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x))
                .ToList();

        movimiento.TextoCompleto =
            string.Join(
                " ",
                partesTexto
            )
            .Trim();

        movimiento.Placas =
            ExtraerPlacas(
                movimiento.TextoCompleto
            );

        if (movimiento.Placas.Count == 0)
        {
            movimiento.RequiereRevision =
                true;

            movimiento.MotivoRevision =
                "No se pudo identificar ninguna placa válida.";
        }
    }

    private static List<string> ExtraerPlacas(
        string texto)
    {
        var resultados =
            new List<(int Indice, string Placa)>();

        // ============================================
        // PLACAS COMPLETAS CON PREFIJO 4615
        // ============================================

        foreach (
            Match coincidencia
            in PatronPlacaConPrefijo.Matches(texto)
        )
        {
            string consecutivo =
                coincidencia.Groups[1].Value
                    .PadLeft(
                        4,
                        '0'
                    );

            resultados.Add(
                (
                    coincidencia.Index,
                    $"4615-{consecutivo}"
                )
            );
        }

        // ============================================
        // PLACAS ABREVIADAS
        //
        // Ejemplo:
        // "El activo con placa No 0910..."
        // ============================================

        foreach (
            Match coincidencia
            in PatronPlacaAbreviada.Matches(texto)
        )
        {
            string consecutivo =
                coincidencia.Groups[1].Value;

            // Evita interpretar el prefijo 4615
            // como si fuera un consecutivo.
            if (consecutivo == "4615")
            {
                continue;
            }

            consecutivo =
                consecutivo.PadLeft(
                    4,
                    '0'
                );

            resultados.Add(
                (
                    coincidencia.Index,
                    $"4615-{consecutivo}"
                )
            );
        }

        // Conservamos el orden en que aparecen
        // realmente dentro del texto.
        return resultados
            .OrderBy(x => x.Indice)
            .Select(x => x.Placa)
            .Distinct(
                StringComparer.OrdinalIgnoreCase
            )
            .ToList();
    }

    private static string ObtenerTextoPrincipal(
        FilaHistoricaInventario registro)
    {
        var partes =
            new List<string>();

        if (
            !string.IsNullOrWhiteSpace(
                registro.IdentificacionOriginal
            )
        )
        {
            partes.Add(
                registro
                    .IdentificacionOriginal
                    .Trim()
            );
        }

        if (
            !string.IsNullOrWhiteSpace(
                registro.DescripcionOriginal
            )
        )
        {
            partes.Add(
                registro
                    .DescripcionOriginal
                    .Trim()
            );
        }

        // Conservamos observaciones que contienen
        // información real del movimiento.
        //
        // Las referencias "Ver ..." no se incorporan
        // al texto principal porque son referencias
        // históricas y no descripción del movimiento.
        if (
            !string.IsNullOrWhiteSpace(
                registro.ObservacionesOriginales
            ) &&
            !registro.ObservacionesOriginales
                .Trim()
                .StartsWith(
                    "Ver ",
                    StringComparison.OrdinalIgnoreCase
                )
        )
        {
            partes.Add(
                registro
                    .ObservacionesOriginales
                    .Trim()
            );
        }

        return string.Join(
            " ",
            partes
        );
    }
}