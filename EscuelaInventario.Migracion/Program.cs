using EscuelaInventario.Migracion.Servicios;
using ClosedXML.Excel;
using EscuelaInventario.Migracion.Modelos;
using EscuelaInventario.Web.Data;
using Microsoft.EntityFrameworkCore;



Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("============================================");
Console.WriteLine("   HERRAMIENTA DE MIGRACIÓN DE INVENTARIO");
Console.WriteLine("============================================");
Console.WriteLine();

string rutaArchivo = Path.Combine(
    AppContext.BaseDirectory,
    "..",
    "..",
    "..",
    "Datos",
    "Inventario 2026.xlsx"
);

rutaArchivo = Path.GetFullPath(rutaArchivo);

if (!File.Exists(rutaArchivo))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("ERROR: No se encontró el archivo.");
    Console.ResetColor();

    Console.WriteLine();
    Console.WriteLine(rutaArchivo);

    return;
}

try
{
    // ============================================
    // 1. LEER INVENTARIO
    // ============================================

    var lector =
        new LectorInventarioService();

    var registros =
        lector.Leer(rutaArchivo);

    // ============================================
    // LEER FILAS HISTÓRICAS
    // ============================================

    var lectorHistorico =
        new LectorHistoricoInventarioService();

    var registrosHistoricos =
        lectorHistorico.Leer(rutaArchivo);

    var normalizadorLibroHistorico =
    new NormalizadorRegistroLibroHistoricoService();

    normalizadorLibroHistorico.Normalizar(
        registrosHistoricos
    );

    var detectorMovimientos =
    new DetectorMovimientosHistoricosService();

    var movimientosHistoricos =
        detectorMovimientos.Detectar(
            registrosHistoricos
        );

    var clasificadorMovimientos =
    new ClasificadorMovimientosHistoricosService();

    clasificadorMovimientos.Clasificar(
        movimientosHistoricos
    );

    var extractorTraslados =
    new ExtractorTrasladosHistoricosService();

    var trasladosHistoricos =
        extractorTraslados.Extraer(
            movimientosHistoricos
        );

    var extractorBajas =
    new ExtractorBajasHistoricasService();

    var bajasHistoricas =
        extractorBajas.Extraer(
            movimientosHistoricos
        );

    var extractorCambiosEstado =
    new ExtractorCambiosEstadoHistoricosService();

    var cambiosEstadoHistoricos =
        extractorCambiosEstado.Extraer(
            movimientosHistoricos
        );

    var extractorModificaciones =
    new ExtractorModificacionesHistoricasService();

    var modificacionesHistoricas =
        extractorModificaciones.Extraer(
            movimientosHistoricos
        );

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Archivo leído correctamente.");
    Console.ResetColor();

    Console.WriteLine();

    // ============================================
    // 2. NORMALIZAR DATOS DEL ACTIVO
    // ============================================

    var normalizador =
        new NormalizadorInventarioService();

    normalizador.Normalizar(registros);

    // ============================================
    // 3. NORMALIZAR REGISTRO DE LIBRO
    // ============================================

    var normalizadorLibro =
        new NormalizadorRegistroLibroService();

    normalizadorLibro.Normalizar(registros);

    // ============================================
    // HISTÓRICO DEL LIBRO
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   ANÁLISIS DEL HISTÓRICO DEL LIBRO");
    Console.WriteLine("============================================");
    Console.WriteLine();

    var historicosNormalizados = registrosHistoricos
        .Where(x =>
            x.Tomo.HasValue &&
            x.Folio.HasValue &&
            x.Asiento.HasValue)
        .ToList();

    var historicosSinPosicion = registrosHistoricos
        .Where(x =>
            string.IsNullOrWhiteSpace(
                x.RegistradoEnOriginal))
        .ToList();

    var historicosNoReconocidos = registrosHistoricos
        .Where(x =>
            !string.IsNullOrWhiteSpace(
                x.RegistradoEnOriginal) &&
            (!x.Tomo.HasValue ||
             !x.Folio.HasValue ||
             !x.Asiento.HasValue))
        .ToList();

    Console.WriteLine(
        $"Filas históricas encontradas: {registrosHistoricos.Count}"
    );

    Console.WriteLine(
        $"Posiciones históricas normalizadas: {historicosNormalizados.Count}"
    );

    Console.WriteLine(
        $"Históricos sin posición de libro: {historicosSinPosicion.Count}"
    );

    Console.WriteLine(
        $"Formatos históricos no reconocidos: {historicosNoReconocidos.Count}"
    );

    Console.WriteLine();

    // ============================================
    // FORMATOS HISTÓRICOS NO RECONOCIDOS
    // ============================================

    if (historicosNoReconocidos.Count > 0)
    {
        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.WriteLine(
            "Valores históricos de 'Registrado en' no reconocidos:"
        );

        Console.ResetColor();

        Console.WriteLine();

        var gruposNoReconocidos =
            historicosNoReconocidos
                .GroupBy(
                    x => x.RegistradoEnOriginal.Trim(),
                    StringComparer.OrdinalIgnoreCase
                )
                .Select(g => new
                {
                    ValorOriginal = g.Key,

                    Cantidad = g.Count(),

                    Ejemplos = g
                        .Take(5)
                        .Select(x =>
                            $"fila {x.NumeroFilaExcel}")
                        .ToList()
                })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.ValorOriginal)
                .ToList();

        foreach (var grupo in gruposNoReconocidos)
        {
            Console.WriteLine(
                $"Valor original: '{grupo.ValorOriginal}'"
            );

            Console.WriteLine(
                $"Cantidad: {grupo.Cantidad}"
            );

            Console.WriteLine(
                $"Ejemplos: {string.Join(", ", grupo.Ejemplos)}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();

    // ============================================
    // DUPLICADOS DENTRO DEL HISTÓRICO
    // ============================================

    var duplicadosHistoricos =
        historicosNormalizados
            .GroupBy(x => new
            {
                Tomo = x.Tomo!.Value,
                Folio = x.Folio!.Value,
                Asiento = x.Asiento!.Value
            })
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key.Tomo)
            .ThenBy(g => g.Key.Folio)
            .ThenBy(g => g.Key.Asiento)
            .ToList();

    Console.WriteLine(
        $"Posiciones históricas duplicadas: {duplicadosHistoricos.Count}"
    );

    Console.WriteLine();

    if (duplicadosHistoricos.Count > 0)
    {
        Console.ForegroundColor =
            ConsoleColor.Red;

        Console.WriteLine(
            "Detalle de duplicados históricos:"
        );

        Console.ResetColor();

        Console.WriteLine();

        foreach (var grupo in duplicadosHistoricos)
        {
            Console.WriteLine(
                $"Tomo {grupo.Key.Tomo}, " +
                $"Folio {grupo.Key.Folio}, " +
                $"Asiento {grupo.Key.Asiento}"
            );

            foreach (var registro in grupo)
            {
                Console.WriteLine(
                    $"  - Fila Excel {registro.NumeroFilaExcel}"
                );
            }

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();

    // ============================================
    // MAYOR POSICIÓN HISTÓRICA
    // ============================================

    var ultimaPosicionHistorica =
     historicosNormalizados
         .Where(x => x.TieneContenidoReal)
         .OrderByDescending(x => x.Tomo)
         .ThenByDescending(x => x.Folio)
         .ThenByDescending(x => x.Asiento)
         .FirstOrDefault();

    if (ultimaPosicionHistorica != null)
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "Mayor posición histórica encontrada:"
        );

        Console.ResetColor();

        Console.WriteLine(
            $"Tomo: {ultimaPosicionHistorica.Tomo}"
        );

        Console.WriteLine(
            $"Folio: {ultimaPosicionHistorica.Folio}"
        );

        Console.WriteLine(
            $"Asiento: {ultimaPosicionHistorica.Asiento}"
        );

        Console.WriteLine(
            $"Fila Excel: {ultimaPosicionHistorica.NumeroFilaExcel}"
        );

        Console.WriteLine(
            $"Texto: '{ultimaPosicionHistorica.IdentificacionOriginal}'"
        );

        Console.WriteLine(
            $"Observaciones: '{ultimaPosicionHistorica.ObservacionesOriginales}'"
        );
    }

    Console.WriteLine();



    // ============================================
    // 4. VALIDAR
    // ============================================

    var validador =
        new ValidadorInventarioService();

    var incidencias =
        validador.Validar(registros);

    // ============================================
    // 5. RESUMEN GENERAL
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   RESUMEN DEL INVENTARIO");
    Console.WriteLine("============================================");
    Console.WriteLine();

    Console.WriteLine(
        $"Registros encontrados: {registros.Count}"
    );

    int placasUnicas = registros
        .Select(x => x.Placa)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();

    Console.WriteLine(
        $"Placas únicas: {placasUnicas}"
    );

    Console.WriteLine(
        $"Incidencias encontradas: {incidencias.Count}"
    );

    Console.WriteLine();

    // ============================================
    // 6. ESTADOS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   NORMALIZACIÓN DE ESTADOS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    int estadosNormalizados = registros
        .Count(x =>
            !string.IsNullOrWhiteSpace(
                x.EstadoNormalizado));

    int estadosSinResolver = registros
        .Count(x =>
            string.IsNullOrWhiteSpace(
                x.EstadoNormalizado));

    Console.WriteLine(
        $"Estados normalizados: {estadosNormalizados}"
    );

    Console.WriteLine(
        $"Estados sin resolver: {estadosSinResolver}"
    );

    Console.WriteLine();

    // ============================================
    // 7. MODOS DE ADQUISICIÓN
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   NORMALIZACIÓN DE MODOS DE ADQUISICIÓN");
    Console.WriteLine("============================================");
    Console.WriteLine();

    int modosNormalizados = registros
        .Count(x =>
            !string.IsNullOrWhiteSpace(
                x.ModoAdquisicionNormalizado));

    int modosSinResolver = registros
        .Count(x =>
            string.IsNullOrWhiteSpace(
                x.ModoAdquisicionNormalizado));

    Console.WriteLine(
        $"Modos normalizados: {modosNormalizados}"
    );

    Console.WriteLine(
        $"Modos sin resolver: {modosSinResolver}"
    );

    Console.WriteLine();

    // ============================================
    // 8. PRECIOS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   NORMALIZACIÓN DE PRECIOS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    int preciosNormalizados = registros
        .Count(x =>
            x.PrecioNormalizado.HasValue);

    int preciosSinValor = registros
        .Count(x =>
            !x.PrecioNormalizado.HasValue);

    Console.WriteLine(
        $"Precios normalizados: {preciosNormalizados}"
    );

    Console.WriteLine(
        $"Registros sin precio numérico: {preciosSinValor}"
    );

    Console.WriteLine();

    // ============================================
    // 9. FECHAS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   CLASIFICACIÓN DE FECHAS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    var fechasNormalizadas = registros
        .Where(x =>
            x.FechaAdquisicionNormalizada.HasValue)
        .ToList();

    var fechasSinDato = registros
        .Where(x =>
            !x.FechaAdquisicionNormalizada.HasValue &&
            EsFechaSinDato(
                x.FechaAdquisicionOriginal))
        .ToList();

    var fechasParciales = registros
        .Where(x =>
            !x.FechaAdquisicionNormalizada.HasValue &&
            EsFechaParcial(
                x.FechaAdquisicionOriginal))
        .ToList();

    var fechasHistoricas = registros
        .Where(x =>
            !x.FechaAdquisicionNormalizada.HasValue &&
            EsReferenciaHistorica(
                x.FechaAdquisicionOriginal))
        .ToList();

    var fechasRevision = registros
        .Where(x =>
            !x.FechaAdquisicionNormalizada.HasValue &&
            !EsFechaSinDato(
                x.FechaAdquisicionOriginal) &&
            !EsFechaParcial(
                x.FechaAdquisicionOriginal) &&
            !EsReferenciaHistorica(
                x.FechaAdquisicionOriginal))
        .ToList();

    Console.WriteLine(
        $"Fechas normalizadas: {fechasNormalizadas.Count}"
    );

    Console.WriteLine(
        $"Sin dato: {fechasSinDato.Count}"
    );

    Console.WriteLine(
        $"Dato parcial: {fechasParciales.Count}"
    );

    Console.WriteLine(
        $"Referencia histórica: {fechasHistoricas.Count}"
    );

    Console.WriteLine(
        $"Requieren revisión: {fechasRevision.Count}"
    );

    Console.WriteLine();

    if (fechasRevision.Count > 0)
    {
        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.WriteLine(
            "Fechas ambiguas o con formato no reconocido:"
        );

        Console.ResetColor();

        Console.WriteLine();

        var gruposRevision = fechasRevision
            .GroupBy(
                x => x.FechaAdquisicionOriginal.Trim(),
                StringComparer.OrdinalIgnoreCase
            )
            .Select(g => new
            {
                ValorOriginal = g.Key,
                Cantidad = g.Count(),

                Ejemplos = g
                    .Take(5)
                    .Select(x =>
                        $"{x.Placa} " +
                        $"(fila {x.NumeroFilaExcel})")
                    .ToList()
            })
            .OrderByDescending(x => x.Cantidad)
            .ThenBy(x => x.ValorOriginal)
            .ToList();

        foreach (var pendiente in gruposRevision)
        {
            Console.WriteLine(
                $"Valor original: '{pendiente.ValorOriginal}'"
            );

            Console.WriteLine(
                $"Cantidad: {pendiente.Cantidad}"
            );

            Console.WriteLine(
                $"Ejemplos: {string.Join(", ", pendiente.Ejemplos)}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();

    if (fechasParciales.Count > 0)
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "Datos parciales conservados históricamente:"
        );

        Console.ResetColor();

        Console.WriteLine();

        var gruposParciales = fechasParciales
            .GroupBy(
                x => x.FechaAdquisicionOriginal.Trim(),
                StringComparer.OrdinalIgnoreCase
            )
            .Select(g => new
            {
                ValorOriginal = g.Key,
                Cantidad = g.Count(),

                Ejemplos = g
                    .Take(3)
                    .Select(x =>
                        $"{x.Placa} " +
                        $"(fila {x.NumeroFilaExcel})")
                    .ToList()
            })
            .OrderByDescending(x => x.Cantidad)
            .ThenBy(x => x.ValorOriginal)
            .ToList();

        foreach (var parcial in gruposParciales)
        {
            Console.WriteLine(
                $"Valor original: '{parcial.ValorOriginal}'"
            );

            Console.WriteLine(
                $"Cantidad: {parcial.Cantidad}"
            );

            Console.WriteLine(
                $"Ejemplos: {string.Join(", ", parcial.Ejemplos)}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();

    // ============================================
    // 10. REGISTRO DE LIBRO
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   NORMALIZACIÓN DE REGISTRO DE LIBRO");
    Console.WriteLine("============================================");
    Console.WriteLine();

    var libroNormalizado = registros
        .Where(x =>
            x.Tomo.HasValue &&
            x.Folio.HasValue &&
            x.Asiento.HasValue)
        .ToList();

    var libroSinDato = registros
        .Where(x =>
            string.IsNullOrWhiteSpace(
                x.RegistradoEn))
        .ToList();

    var libroNoReconocido = registros
        .Where(x =>
            !string.IsNullOrWhiteSpace(
                x.RegistradoEn) &&
            (!x.Tomo.HasValue ||
             !x.Folio.HasValue ||
             !x.Asiento.HasValue))
        .ToList();

    Console.WriteLine(
        $"Registros de libro normalizados: {libroNormalizado.Count}"
    );

    Console.WriteLine(
        $"Sin dato de libro: {libroSinDato.Count}"
    );

    Console.WriteLine(
        $"Formato no reconocido: {libroNoReconocido.Count}"
    );

    Console.WriteLine();

    // ============================================
    // FORMATOS NO RECONOCIDOS
    // ============================================

    if (libroNoReconocido.Count > 0)
    {
        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.WriteLine(
            "Valores de 'Registrado en' no reconocidos:"
        );

        Console.ResetColor();

        Console.WriteLine();

        var gruposLibroNoReconocido =
            libroNoReconocido
                .GroupBy(
                    x => x.RegistradoEn.Trim(),
                    StringComparer.OrdinalIgnoreCase
                )
                .Select(g => new
                {
                    ValorOriginal = g.Key,

                    Cantidad = g.Count(),

                    Ejemplos = g
                        .Take(5)
                        .Select(x =>
                            $"{x.Placa} " +
                            $"(fila {x.NumeroFilaExcel})")
                        .ToList()
                })
                .OrderByDescending(x => x.Cantidad)
                .ThenBy(x => x.ValorOriginal)
                .ToList();

        foreach (var grupo in gruposLibroNoReconocido)
        {
            Console.WriteLine(
                $"Valor original: '{grupo.ValorOriginal}'"
            );

            Console.WriteLine(
                $"Cantidad: {grupo.Cantidad}"
            );

            Console.WriteLine(
                $"Ejemplos: {string.Join(", ", grupo.Ejemplos)}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();

    // ============================================
    // POSICIONES DE LIBRO DUPLICADAS
    // ============================================

    var posicionesDuplicadas =
        libroNormalizado
            .GroupBy(x => new
            {
                Tomo = x.Tomo!.Value,
                Folio = x.Folio!.Value,
                Asiento = x.Asiento!.Value
            })
            .Where(g => g.Count() > 1)
            .OrderBy(g => g.Key.Tomo)
            .ThenBy(g => g.Key.Folio)
            .ThenBy(g => g.Key.Asiento)
            .ToList();

    Console.WriteLine(
        $"Posiciones de libro duplicadas: {posicionesDuplicadas.Count}"
    );

    Console.WriteLine();

    if (posicionesDuplicadas.Count > 0)
    {
        Console.ForegroundColor =
            ConsoleColor.Red;

        Console.WriteLine(
            "Detalle de posiciones duplicadas:"
        );

        Console.ResetColor();

        Console.WriteLine();

        foreach (var grupo in posicionesDuplicadas)
        {
            Console.WriteLine(
                $"Tomo {grupo.Key.Tomo}, " +
                $"Folio {grupo.Key.Folio}, " +
                $"Asiento {grupo.Key.Asiento}"
            );

            foreach (var registro in grupo)
            {
                Console.WriteLine(
                    $"  - {registro.Placa} " +
                    $"(fila {registro.NumeroFilaExcel})"
                );
            }

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();

    // ============================================
    // ÚLTIMA POSICIÓN NORMALIZADA
    // ============================================

    var ultimaPosicion =
        libroNormalizado
            .OrderByDescending(x => x.Tomo)
            .ThenByDescending(x => x.Folio)
            .ThenByDescending(x => x.Asiento)
            .FirstOrDefault();

    if (ultimaPosicion != null)
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "Mayor posición de libro encontrada:"
        );

        Console.ResetColor();

        Console.WriteLine(
            $"Tomo: {ultimaPosicion.Tomo}"
        );

        Console.WriteLine(
            $"Folio: {ultimaPosicion.Folio}"
        );

        Console.WriteLine(
            $"Asiento: {ultimaPosicion.Asiento}"
        );

        Console.WriteLine(
            $"Placa: {ultimaPosicion.Placa}"
        );

        Console.WriteLine(
            $"Fila Excel: {ultimaPosicion.NumeroFilaExcel}"
        );
    }

    Console.WriteLine();

    // ============================================
    // CRUCE GLOBAL DE POSICIONES DEL LIBRO
    // ACTIVOS + HISTÓRICO OCUPADO
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   CRUCE GLOBAL DE POSICIONES DEL LIBRO");
    Console.WriteLine("============================================");
    Console.WriteLine();

    // ============================================
    // POSICIONES DE LOS ACTIVOS
    // ============================================

    var posicionesActivos =
        libroNormalizado
            .Select(x => new
            {
                Tipo = "ACTIVO",

                Tomo = x.Tomo!.Value,
                Folio = x.Folio!.Value,
                Asiento = x.Asiento!.Value,

                FilaExcel =
                    x.NumeroFilaExcel,

                Referencia =
                    x.Placa
            })
            .ToList();

    // ============================================
    // POSICIONES HISTÓRICAS REALMENTE OCUPADAS
    //
    // IMPORTANTE:
    // Las posiciones preparadas pero vacías,
    // como 5/805/21 a 5/805/32,
    // NO se consideran ocupadas.
    // ============================================

    var posicionesHistoricasOcupadas =
        historicosNormalizados
            .Where(x =>
                x.TieneContenidoReal)
            .Select(x => new
            {
                Tipo = "HISTÓRICO",

                Tomo = x.Tomo!.Value,
                Folio = x.Folio!.Value,
                Asiento = x.Asiento!.Value,

                FilaExcel =
                    x.NumeroFilaExcel,

                Referencia =
                    string.IsNullOrWhiteSpace(
                        x.IdentificacionOriginal)
                        ? "[SIN TEXTO EN IDENTIFICACIÓN]"
                        : x.IdentificacionOriginal
            })
            .ToList();

    // ============================================
    // UNIR AMBOS GRUPOS
    // ============================================

    var posicionesGlobales =
        posicionesActivos
            .Concat(
                posicionesHistoricasOcupadas
            )
            .ToList();

    Console.WriteLine(
        $"Posiciones de activos: {posicionesActivos.Count}"
    );

    Console.WriteLine(
        $"Posiciones históricas ocupadas: " +
        $"{posicionesHistoricasOcupadas.Count}"
    );

    Console.WriteLine(
        $"Registros de libro ocupados analizados: " +
        $"{posicionesGlobales.Count}"
    );

    Console.WriteLine();

    // ============================================
    // BUSCAR CONFLICTOS ENTRE ACTIVO E HISTÓRICO
    // ============================================

    var conflictosCruzados =
        posicionesGlobales
            .GroupBy(x => new
            {
                x.Tomo,
                x.Folio,
                x.Asiento
            })
            .Where(g =>
                g.Select(x => x.Tipo)
                    .Distinct()
                    .Count() > 1)
            .OrderBy(g => g.Key.Tomo)
            .ThenBy(g => g.Key.Folio)
            .ThenBy(g => g.Key.Asiento)
            .ToList();

    Console.WriteLine(
        $"Conflictos ACTIVO ↔ HISTÓRICO: " +
        $"{conflictosCruzados.Count}"
    );

    Console.WriteLine();

    if (conflictosCruzados.Count > 0)
    {
        Console.ForegroundColor =
            ConsoleColor.Red;

        Console.WriteLine(
            "Detalle de conflictos cruzados:"
        );

        Console.ResetColor();

        Console.WriteLine();

        foreach (var grupo in conflictosCruzados)
        {
            Console.WriteLine(
                $"Tomo {grupo.Key.Tomo}, " +
                $"Folio {grupo.Key.Folio}, " +
                $"Asiento {grupo.Key.Asiento}"
            );

            foreach (var registro in grupo)
            {
                Console.WriteLine(
                    $"  [{registro.Tipo}] " +
                    $"Fila {registro.FilaExcel}"
                );

                Console.WriteLine(
                    $"    {registro.Referencia}"
                );
            }

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }
    else
    {
        Console.ForegroundColor =
            ConsoleColor.Green;

        Console.WriteLine(
            "No se encontraron posiciones compartidas " +
            "entre activos e histórico."
        );

        Console.ResetColor();
    }

    Console.WriteLine();

    // ============================================
    // MAYOR POSICIÓN GLOBAL REALMENTE OCUPADA
    // ============================================

    var ultimaPosicionGlobal =
        posicionesGlobales
            .OrderByDescending(x => x.Tomo)
            .ThenByDescending(x => x.Folio)
            .ThenByDescending(x => x.Asiento)
            .FirstOrDefault();

    if (ultimaPosicionGlobal != null)
    {
        Console.ForegroundColor =
            ConsoleColor.Cyan;

        Console.WriteLine(
            "Mayor posición global ocupada:"
        );

        Console.ResetColor();

        Console.WriteLine(
            $"Tomo: {ultimaPosicionGlobal.Tomo}"
        );

        Console.WriteLine(
            $"Folio: {ultimaPosicionGlobal.Folio}"
        );

        Console.WriteLine(
            $"Asiento: {ultimaPosicionGlobal.Asiento}"
        );

        Console.WriteLine(
            $"Tipo: {ultimaPosicionGlobal.Tipo}"
        );

        Console.WriteLine(
            $"Fila Excel: {ultimaPosicionGlobal.FilaExcel}"
        );
    }

    Console.WriteLine();


    // ============================================
    // 11. CONTEXTO DE FECHAS PROBLEMÁTICAS
    // ============================================

    var analizadorFechas =
        new AnalizadorContextoFechasService();

    analizadorFechas.MostrarContexto(
        registros,
        5
    );

    var analizadorFinalLibro =
    new AnalizadorFinalLibroService();

    analizadorFinalLibro.MostrarFinal(
        rutaArchivo,
        17040,
        17063
    );

    // ============================================
    // MOVIMIENTOS HISTÓRICOS DETECTADOS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   MOVIMIENTOS HISTÓRICOS DETECTADOS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    Console.WriteLine(
        $"Movimientos detectados: {movimientosHistoricos.Count}"
    );

    Console.WriteLine();

    var distribucionLineas =
        movimientosHistoricos
            .GroupBy(x => x.RegistrosLibro.Count)
            .OrderBy(x => x.Key)
            .ToList();

    Console.WriteLine(
        "Cantidad de posiciones de libro por movimiento:"
    );

    Console.WriteLine();

    foreach (var grupo in distribucionLineas)
    {
        Console.WriteLine(
            $"{grupo.Key} posición(es): {grupo.Count()} movimientos"
        );
    }

    Console.WriteLine();

    // ============================================
    // PRIMEROS MOVIMIENTOS COMO MUESTRA
    // ============================================

    Console.ForegroundColor =
        ConsoleColor.Cyan;

    Console.WriteLine(
        "Primeros movimientos detectados:"
    );

    Console.ResetColor();

    Console.WriteLine();

    foreach (var movimiento in movimientosHistoricos.Take(20))
    {
        Console.WriteLine(
            $"Placa: {movimiento.PlacaPrincipal}"
        );

        Console.WriteLine(
            $"Posiciones utilizadas: {movimiento.RegistrosLibro.Count}"
        );

        foreach (var registro in movimiento.RegistrosLibro)
        {
            Console.WriteLine(
                $"  - {registro.Tomo}/" +
                $"{registro.Folio}/" +
                $"{registro.Asiento} " +
                $"(fila {registro.NumeroFilaExcel})"
            );
        }

        Console.WriteLine(
            $"Texto completo: {movimiento.TextoCompleto}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // MOVIMIENTOS HISTÓRICOS SOSPECHOSAMENTE LARGOS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   MOVIMIENTOS HISTÓRICOS LARGOS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    var movimientosLargos =
        movimientosHistoricos
            .Where(x => x.RegistrosLibro.Count >= 5)
            .OrderByDescending(x => x.RegistrosLibro.Count)
            .ToList();

    Console.WriteLine(
        $"Movimientos con 5 o más posiciones: {movimientosLargos.Count}"
    );

    Console.WriteLine();

    foreach (var movimiento in movimientosLargos)
    {
        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.WriteLine(
            $"Placa detectada: {movimiento.PlacaPrincipal}"
        );

        Console.ResetColor();

        Console.WriteLine(
            $"Cantidad de posiciones: {movimiento.RegistrosLibro.Count}"
        );

        Console.WriteLine(
            "Posiciones y texto:"
        );

        Console.WriteLine();

        foreach (var registro in movimiento.RegistrosLibro)
        {
            Console.WriteLine(
                $"  {registro.Tomo}/" +
                $"{registro.Folio}/" +
                $"{registro.Asiento} " +
                $"(fila {registro.NumeroFilaExcel})"
            );

            Console.WriteLine(
                $"  Texto: '{registro.IdentificacionOriginal}'"
            );

            if (!string.IsNullOrWhiteSpace(
                registro.ObservacionesOriginales))
            {
                Console.WriteLine(
                    $"  Observaciones: " +
                    $"'{registro.ObservacionesOriginales}'"
                );
            }

            Console.WriteLine();
        }

        Console.WriteLine(
            "--------------------------------------------"
        );

        Console.WriteLine();
    }

    // ============================================
    // POSICIONES HISTÓRICAS NO ASIGNADAS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   POSICIONES HISTÓRICAS NO ASIGNADAS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    // Todas las filas que sí tienen contenido real
    // y una posición válida de libro.
    var historicosOcupados =
        registrosHistoricos
            .Where(x =>
                x.TieneContenidoReal &&
                x.Tomo.HasValue &&
                x.Folio.HasValue &&
                x.Asiento.HasValue)
            .ToList();

    // Posiciones que ya forman parte de algún
    // movimiento detectado.
    var clavesAsignadas =
        movimientosHistoricos
            .SelectMany(x => x.RegistrosLibro)
            .Select(x =>
                $"{x.Tomo}/{x.Folio}/{x.Asiento}")
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase
            );

    // Filas ocupadas que quedaron fuera.
    var historicosNoAsignados =
        historicosOcupados
            .Where(x =>
                !clavesAsignadas.Contains(
                    $"{x.Tomo}/{x.Folio}/{x.Asiento}"
                ))
            .OrderBy(x => x.Tomo)
            .ThenBy(x => x.Folio)
            .ThenBy(x => x.Asiento)
            .ToList();

    Console.WriteLine(
        $"Posiciones históricas ocupadas: " +
        $"{historicosOcupados.Count}"
    );

    var detectorActivosIrregulares =
    new DetectorActivosIdentificacionIrregularService();

    var activosIrregulares =
        detectorActivosIrregulares.Detectar(
            historicosNoAsignados
        );

    Console.WriteLine(
        $"Posiciones asignadas a movimientos: " +
        $"{clavesAsignadas.Count}"
    );

    Console.WriteLine(
        $"Posiciones ocupadas no asignadas: " +
        $"{historicosNoAsignados.Count}"
    );

    Console.WriteLine();

    foreach (var registro in historicosNoAsignados)
    {
        Console.WriteLine(
            $"{registro.Tomo}/" +
            $"{registro.Folio}/" +
            $"{registro.Asiento} " +
            $"(fila {registro.NumeroFilaExcel})"
        );

        Console.WriteLine(
            $"  Identificación: " +
            $"'{registro.IdentificacionOriginal}'"
        );

        if (!string.IsNullOrWhiteSpace(
            registro.DescripcionOriginal))
        {
            Console.WriteLine(
                $"  Descripción: " +
                $"'{registro.DescripcionOriginal}'"
            );
        }

        if (!string.IsNullOrWhiteSpace(
            registro.ObservacionesOriginales))
        {
            Console.WriteLine(
                $"  Observaciones: " +
                $"'{registro.ObservacionesOriginales}'"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // ACTIVOS CON IDENTIFICACIÓN IRREGULAR
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   ACTIVOS CON IDENTIFICACIÓN IRREGULAR");
    Console.WriteLine("============================================");
    Console.WriteLine();

    Console.WriteLine(
        $"Total detectados: {activosIrregulares.Count}"
    );

    Console.WriteLine();

    var gruposIrregularidad =
        activosIrregulares
            .GroupBy(x => x.TipoIrregularidad)
            .OrderBy(x => x.Key)
            .ToList();

    foreach (var grupo in gruposIrregularidad)
    {
        Console.WriteLine(
            $"{grupo.Key}: {grupo.Count()}"
        );
    }

    Console.WriteLine();

    foreach (var activo in activosIrregulares)
    {
        Console.WriteLine(
            $"Fila Excel: {activo.NumeroFilaExcel}"
        );

        Console.WriteLine(
            $"Posición libro: " +
            $"{activo.Tomo}/{activo.Folio}/{activo.Asiento}"
        );

        Console.WriteLine(
            $"Identificación original: " +
            $"'{activo.IdentificacionOriginal}'"
        );

        Console.WriteLine(
            $"Descripción: '{activo.Descripcion}'"
        );

        Console.WriteLine(
            $"Tipo irregularidad: " +
            $"{activo.TipoIrregularidad}"
        );

        Console.WriteLine(
            $"Placa sugerida: " +
            $"{activo.PlacaSugerida ?? "[NO SE SUGIERE]"}"
        );

        if (!string.IsNullOrWhiteSpace(
            activo.Observaciones))
        {
            Console.WriteLine(
                $"Observaciones: " +
                $"'{activo.Observaciones}'"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // CRUCE DE IDENTIFICACIONES IRREGULARES
    // CON PLACAS VÁLIDAS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   CRUCE IRREGULARES ↔ PLACAS VÁLIDAS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    int conCoincidencia = 0;
    int sinCoincidencia = 0;

    foreach (var irregular in activosIrregulares)
    {
        // Extraemos solamente el consecutivo final.
        // Esto NO significa que estemos corrigiendo
        // automáticamente el prefijo.
        var coincidenciaNumero =
            System.Text.RegularExpressions.Regex.Match(
                irregular.IdentificacionOriginal,
                @"(\d{3,4})\s*$"
            );

        if (!coincidenciaNumero.Success)
        {
            continue;
        }

        string consecutivo =
            coincidenciaNumero
                .Groups[1]
                .Value
                .PadLeft(
                    4,
                    '0'
                );

        string placaEquivalente =
            $"4615-{consecutivo}";

        var coincidenciasValidas =
            registros
                .Where(x =>
                    x.Placa.Equals(
                        placaEquivalente,
                        StringComparison.OrdinalIgnoreCase
                    ))
                .ToList();

        Console.WriteLine(
            $"Original irregular: " +
            $"'{irregular.IdentificacionOriginal}'"
        );

        Console.WriteLine(
            $"Fila irregular: {irregular.NumeroFilaExcel}"
        );

        Console.WriteLine(
            $"Descripción irregular: " +
            $"'{irregular.Descripcion}'"
        );

        Console.WriteLine(
            $"Equivalente a comparar: {placaEquivalente}"
        );

        if (coincidenciasValidas.Count > 0)
        {
            conCoincidencia++;

            Console.ForegroundColor =
                ConsoleColor.Yellow;

            Console.WriteLine(
                $"Coincidencia válida encontrada: " +
                $"{coincidenciasValidas.Count}"
            );

            Console.ResetColor();

            foreach (var valido in coincidenciasValidas)
            {
                Console.WriteLine(
                    $"  Fila válida: " +
                    $"{valido.NumeroFilaExcel}"
                );

                Console.WriteLine(
                    $"  Placa válida: " +
                    $"{valido.Placa}"
                );

                Console.WriteLine(
                    $"  Descripción válida: " +
                    $"'{valido.Descripcion}'"
                );

                Console.WriteLine(
                    $"  Observaciones válidas: " +
                    $"'{valido.ObservacionesOriginales}'"
                );
            }
        }
        else
        {
            sinCoincidencia++;

            Console.ForegroundColor =
                ConsoleColor.Cyan;

            Console.WriteLine(
                "No existe una placa válida " +
                "con ese consecutivo."
            );

            Console.ResetColor();
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        $"Irregulares cuyo equivalente 4615 existe: " +
        $"{conCoincidencia}"
    );

    Console.WriteLine(
        $"Irregulares cuyo equivalente 4615 NO existe: " +
        $"{sinCoincidencia}"
    );

    Console.WriteLine();

    // ============================================
    // CONTEXTO DE IDENTIFICACIONES IRREGULARES
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   CONTEXTO DE IDENTIFICACIONES IRREGULARES");
    Console.WriteLine("============================================");
    Console.WriteLine();

    var gruposFilasIrregulares =
    GroupByConsecutivos(
        activosIrregulares
            .OrderBy(x => x.NumeroFilaExcel),
        x => x.NumeroFilaExcel
    )
    .ToList();

    foreach (var grupo in gruposFilasIrregulares)
    {
        int primeraFila =
            grupo.Min(x => x.NumeroFilaExcel);

        int ultimaFila =
            grupo.Max(x => x.NumeroFilaExcel);

        Console.WriteLine(
            $"Bloque irregular: filas " +
            $"{primeraFila} a {ultimaFila}"
        );

        Console.WriteLine();

        var contextoActivosValidos =
            registros
                .Where(x =>
                    x.NumeroFilaExcel >= primeraFila - 5 &&
                    x.NumeroFilaExcel <= ultimaFila + 5)
                .OrderBy(x => x.NumeroFilaExcel)
                .ToList();

        foreach (var fila in contextoActivosValidos)
        {
            Console.WriteLine(
                $"Fila {fila.NumeroFilaExcel}: " +
                $"{fila.Placa} | " +
                $"{fila.Descripcion}"
            );
        }

        Console.WriteLine();

        Console.WriteLine(
            "Irregulares dentro del bloque:"
        );

        foreach (var irregular in grupo)
        {
            Console.WriteLine(
                $"  Fila {irregular.NumeroFilaExcel}: " +
                $"{irregular.IdentificacionOriginal} | " +
                $"{irregular.Descripcion}"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );

        Console.WriteLine();
    }

    // ============================================
    // CLASIFICACIÓN DE MOVIMIENTOS HISTÓRICOS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   CLASIFICACIÓN DE MOVIMIENTOS HISTÓRICOS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    var clasificacionMovimientos =
        movimientosHistoricos
            .GroupBy(x => x.TipoMovimiento)
            .OrderByDescending(x => x.Count())
            .ToList();

    foreach (var grupo in clasificacionMovimientos)
    {
        Console.WriteLine(
            $"{grupo.Key}: {grupo.Count()}"
        );
    }

    Console.WriteLine();

    var noClasificados =
        movimientosHistoricos
            .Where(x =>
                x.TipoMovimiento ==
                "NO_CLASIFICADO")
            .ToList();

    Console.WriteLine(
        $"Total no clasificados: {noClasificados.Count}"
    );

    Console.WriteLine();

    Console.ForegroundColor =
        ConsoleColor.Yellow;

    Console.WriteLine(
        "Primeros 30 movimientos no clasificados:"
    );

    Console.ResetColor();

    Console.WriteLine();

    foreach (var movimiento in noClasificados.Take(30))
    {
        Console.WriteLine(
            $"Placa principal: " +
            $"{movimiento.PlacaPrincipal}"
        );

        Console.WriteLine(
            $"Placas asociadas: " +
            $"{string.Join(", ", movimiento.Placas)}"
        );

        Console.WriteLine(
            $"Posición inicial: " +
            $"{movimiento.RegistrosLibro.First().Tomo}/" +
            $"{movimiento.RegistrosLibro.First().Folio}/" +
            $"{movimiento.RegistrosLibro.First().Asiento}"
        );

        Console.WriteLine(
            $"Texto: {movimiento.TextoCompleto}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // CONTEXTO DE LOS MOVIMIENTOS NO CLASIFICADOS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   CONTEXTO DE NO CLASIFICADOS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    foreach (var movimiento in noClasificados)
    {
        var primeraFilaMovimiento =
            movimiento.RegistrosLibro
                .OrderBy(x => x.NumeroFilaExcel)
                .First();

        int filaCentral =
            primeraFilaMovimiento.NumeroFilaExcel;

        Console.ForegroundColor =
            ConsoleColor.Yellow;

        Console.WriteLine(
            $"MOVIMIENTO: {movimiento.PlacaPrincipal}"
        );

        Console.ResetColor();

        Console.WriteLine(
            $"Fila inicial Excel: {filaCentral}"
        );

        Console.WriteLine(
            $"Posición inicial: " +
            $"{primeraFilaMovimiento.Tomo}/" +
            $"{primeraFilaMovimiento.Folio}/" +
            $"{primeraFilaMovimiento.Asiento}"
        );

        Console.WriteLine();

        Console.WriteLine(
            $"Contexto de filas {filaCentral - 6} " +
            $"a {filaCentral + 10}:"
        );

        Console.WriteLine();

        var contexto =
            registrosHistoricos
                .Where(x =>
                    x.NumeroFilaExcel >= filaCentral - 6 &&
                    x.NumeroFilaExcel <= filaCentral + 10)
                .OrderBy(x => x.NumeroFilaExcel)
                .ToList();

        foreach (var fila in contexto)
        {
            Console.WriteLine(
                $"Fila {fila.NumeroFilaExcel} | " +
                $"{fila.Tomo}/{fila.Folio}/{fila.Asiento}"
            );

            Console.WriteLine(
                $"  Identificación: " +
                $"'{fila.IdentificacionOriginal}'"
            );

            if (!string.IsNullOrWhiteSpace(
                fila.DescripcionOriginal))
            {
                Console.WriteLine(
                    $"  Descripción: " +
                    $"'{fila.DescripcionOriginal}'"
                );
            }

            if (!string.IsNullOrWhiteSpace(
                fila.ObservacionesOriginales))
            {
                Console.WriteLine(
                    $"  Observaciones: " +
                    $"'{fila.ObservacionesOriginales}'"
                );
            }

            Console.WriteLine();
        }

        Console.WriteLine(
            "============================================"
        );

        Console.WriteLine();
    }

    // ============================================
    // ANÁLISIS DE TRASLADOS HISTÓRICOS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   ANÁLISIS DE TRASLADOS HISTÓRICOS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    Console.WriteLine(
        $"Traslados detectados: {trasladosHistoricos.Count}"
    );

    Console.WriteLine();

    // ============================================
    // UBICACIONES EXTRAÍDAS
    // ============================================

    var conAmbasUbicaciones =
        trasladosHistoricos
            .Count(x =>
                !string.IsNullOrWhiteSpace(
                    x.UbicacionAnteriorOriginal
                ) &&
                !string.IsNullOrWhiteSpace(
                    x.UbicacionNuevaOriginal
                ));

    var sinAmbasUbicaciones =
        trasladosHistoricos.Count -
        conAmbasUbicaciones;

    Console.WriteLine(
        $"Con origen y destino extraídos: " +
        $"{conAmbasUbicaciones}"
    );

    Console.WriteLine(
        $"Sin origen/destino completo: " +
        $"{sinAmbasUbicaciones}"
    );

    Console.WriteLine();

    // ============================================
    // TIPOS DE UBICACIÓN
    // ============================================

    var areaAArea =
        trasladosHistoricos
            .Count(x =>
                x.AreaAnteriorNumero.HasValue &&
                x.AreaNuevaNumero.HasValue);

    var soloAreaAnterior =
        trasladosHistoricos
            .Count(x =>
                x.AreaAnteriorNumero.HasValue &&
                !x.AreaNuevaNumero.HasValue &&
                !string.IsNullOrWhiteSpace(
                    x.UbicacionNuevaOriginal
                ));

    var soloAreaNueva =
        trasladosHistoricos
            .Count(x =>
                !x.AreaAnteriorNumero.HasValue &&
                x.AreaNuevaNumero.HasValue &&
                !string.IsNullOrWhiteSpace(
                    x.UbicacionAnteriorOriginal
                ));

    var ubicacionesHistoricas =
        trasladosHistoricos
            .Count(x =>
                !string.IsNullOrWhiteSpace(
                    x.UbicacionAnteriorOriginal
                ) &&
                !string.IsNullOrWhiteSpace(
                    x.UbicacionNuevaOriginal
                ) &&
                !x.AreaAnteriorNumero.HasValue &&
                !x.AreaNuevaNumero.HasValue);

    Console.WriteLine(
        $"Área numérica → Área numérica: {areaAArea}"
    );

    Console.WriteLine(
        $"Área numérica → Ubicación histórica: " +
        $"{soloAreaAnterior}"
    );

    Console.WriteLine(
        $"Ubicación histórica → Área numérica: " +
        $"{soloAreaNueva}"
    );

    Console.WriteLine(
        $"Ubicación histórica → Ubicación histórica: " +
        $"{ubicacionesHistoricas}"
    );

    Console.WriteLine();

    // ============================================
    // FECHAS
    // ============================================

    var cantidadFechasTrasladoNormalizadas =
    trasladosHistoricos
        .Count(x =>
            x.FechaMovimiento.HasValue);

    var cantidadFechasTrasladoNoNormalizadas =
        trasladosHistoricos.Count -
        cantidadFechasTrasladoNormalizadas;

    Console.WriteLine(
        $"Fechas normalizadas: " +
        $"{cantidadFechasTrasladoNormalizadas}"
    );

    Console.WriteLine(
        $"Fechas no normalizadas: " +
        $"{cantidadFechasTrasladoNoNormalizadas}"
    );

    Console.WriteLine();

    // ============================================
    // REVISIÓN
    // ============================================

    var requierenRevision =
        trasladosHistoricos
            .Where(x =>
                x.RequiereRevision)
            .ToList();

    Console.WriteLine(
        $"Traslados que requieren revisión: " +
        $"{requierenRevision.Count}"
    );

    Console.WriteLine();

    // ============================================
    // MUESTRA DE TRASLADOS EXTRAÍDOS
    // ============================================

    Console.ForegroundColor =
        ConsoleColor.Cyan;

    Console.WriteLine(
        "Primeros 20 traslados estructurados:"
    );

    Console.ResetColor();

    Console.WriteLine();

    foreach (var traslado in trasladosHistoricos.Take(20))
    {
        Console.WriteLine(
            $"Placa(s): " +
            $"{string.Join(", ", traslado.Placas)}"
        );

        Console.WriteLine(
            $"Origen original: " +
            $"'{traslado.UbicacionAnteriorOriginal}'"
        );

        Console.WriteLine(
            $"Destino original: " +
            $"'{traslado.UbicacionNuevaOriginal}'"
        );

        Console.WriteLine(
            $"Área anterior: " +
            $"{traslado.AreaAnteriorNumero?.ToString() ?? "[NO NUMÉRICA]"}"
        );

        Console.WriteLine(
            $"Área nueva: " +
            $"{traslado.AreaNuevaNumero?.ToString() ?? "[NO NUMÉRICA]"}"
        );

        Console.WriteLine(
            $"Fecha original: " +
            $"'{traslado.FechaOriginal}'"
        );

        Console.WriteLine(
            $"Fecha normalizada: " +
            $"{(
                traslado.FechaMovimiento.HasValue
                    ? traslado.FechaMovimiento.Value
                        .ToString("dd/MM/yyyy")
                    : "[NO NORMALIZADA]"
            )}"
        );

        Console.WriteLine(
            $"Requiere revisión: " +
            $"{(traslado.RequiereRevision ? "SÍ" : "NO")}"
        );

        if (traslado.RequiereRevision)
        {
            Console.WriteLine(
                $"Motivo: {traslado.MotivoRevision}"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // PRIMEROS CASOS QUE REQUIEREN REVISIÓN
    // ============================================

    Console.ForegroundColor =
        ConsoleColor.Yellow;

    Console.WriteLine(
        "Primeros 30 traslados que requieren revisión:"
    );

    Console.ResetColor();

    Console.WriteLine();

    foreach (var traslado in requierenRevision.Take(30))
    {
        Console.WriteLine(
            $"Placa(s): " +
            $"{string.Join(", ", traslado.Placas)}"
        );

        Console.WriteLine(
            $"Origen: '{traslado.UbicacionAnteriorOriginal}'"
        );

        Console.WriteLine(
            $"Destino: '{traslado.UbicacionNuevaOriginal}'"
        );

        Console.WriteLine(
            $"Fecha original: '{traslado.FechaOriginal}'"
        );

        Console.WriteLine(
            $"Motivo: {traslado.MotivoRevision}"
        );

        Console.WriteLine(
            $"Texto: {traslado.TextoOriginal}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // ANÁLISIS DE BAJAS HISTÓRICAS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   ANÁLISIS DE BAJAS HISTÓRICAS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    Console.WriteLine(
        $"Bajas detectadas: {bajasHistoricas.Count}"
    );

    Console.WriteLine();

    // ============================================
    // MOTIVOS
    // ============================================

    var bajasPorMotivo =
        bajasHistoricas
            .GroupBy(x => x.MotivoNormalizado)
            .OrderByDescending(x => x.Count())
            .ToList();

    Console.WriteLine("Distribución por motivo:");
    Console.WriteLine();

    foreach (var grupo in bajasPorMotivo)
    {
        Console.WriteLine(
            $"{grupo.Key}: {grupo.Count()}"
        );
    }

    Console.WriteLine();

    // ============================================
    // FECHAS
    // ============================================

    var cantidadFechasBajaNormalizadas =
        bajasHistoricas
            .Count(x =>
                x.FechaMovimiento.HasValue);

    var cantidadFechasBajaNoNormalizadas =
        bajasHistoricas.Count -
        cantidadFechasBajaNormalizadas;

    Console.WriteLine(
        $"Fechas normalizadas: " +
        $"{cantidadFechasBajaNormalizadas}"
    );

    Console.WriteLine(
        $"Fechas no normalizadas: " +
        $"{cantidadFechasBajaNoNormalizadas}"
    );

    Console.WriteLine();

    // ============================================
    // REVISIÓN
    // ============================================

    var bajasQueRequierenRevision =
        bajasHistoricas
            .Where(x =>
                x.RequiereRevision)
            .ToList();

    Console.WriteLine(
        $"Bajas que requieren revisión: " +
        $"{bajasQueRequierenRevision.Count}"
    );

    Console.WriteLine();

    // ============================================
    // PRIMERAS BAJAS ESTRUCTURADAS
    // ============================================

    Console.ForegroundColor =
        ConsoleColor.Cyan;

    Console.WriteLine(
        "Primeras 20 bajas estructuradas:"
    );

    Console.ResetColor();

    Console.WriteLine();

    foreach (var baja in bajasHistoricas.Take(20))
    {
        Console.WriteLine(
            $"Placa(s): " +
            $"{string.Join(", ", baja.Placas)}"
        );

        Console.WriteLine(
            $"Motivo normalizado: " +
            $"{baja.MotivoNormalizado}"
        );

        Console.WriteLine(
            $"Motivo original/técnico: " +
            $"{baja.MotivoOriginal}"
        );

        Console.WriteLine(
            $"Fecha original: " +
            $"'{baja.FechaOriginal}'"
        );

        Console.WriteLine(
            $"Fecha normalizada: " +
            $"{(
                baja.FechaMovimiento.HasValue
                    ? baja.FechaMovimiento.Value
                        .ToString("dd/MM/yyyy")
                    : "[NO NORMALIZADA]"
            )}"
        );

        Console.WriteLine(
            $"Requiere revisión: " +
            $"{(baja.RequiereRevision ? "SÍ" : "NO")}"
        );

        if (baja.RequiereRevision)
        {
            Console.WriteLine(
                $"Motivo revisión: " +
                $"{baja.MotivoRevision}"
            );
        }

        Console.WriteLine(
            $"Texto: {baja.TextoOriginal}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // PRIMERAS BAJAS QUE REQUIEREN REVISIÓN
    // ============================================

    Console.ForegroundColor =
        ConsoleColor.Yellow;

    Console.WriteLine(
        "Primeras 30 bajas que requieren revisión:"
    );

    Console.ResetColor();

    Console.WriteLine();

    foreach (var baja in bajasQueRequierenRevision.Take(30))
    {
        Console.WriteLine(
            $"Placa(s): " +
            $"{string.Join(", ", baja.Placas)}"
        );

        Console.WriteLine(
            $"Motivo normalizado: " +
            $"{baja.MotivoNormalizado}"
        );

        Console.WriteLine(
            $"Fecha original: " +
            $"'{baja.FechaOriginal}'"
        );

        Console.WriteLine(
            $"Motivo revisión: " +
            $"{baja.MotivoRevision}"
        );

        Console.WriteLine(
            $"Texto: {baja.TextoOriginal}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();


    // ============================================
    // ANÁLISIS DE CAMBIOS DE ESTADO HISTÓRICOS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   ANÁLISIS DE CAMBIOS DE ESTADO HISTÓRICOS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    Console.WriteLine(
        $"Cambios de estado detectados: " +
        $"{cambiosEstadoHistoricos.Count}"
    );

    Console.WriteLine();

    // ============================================
    // ESTADOS EXTRAÍDOS
    // ============================================

    var cambiosConAmbosEstados =
        cambiosEstadoHistoricos
            .Count(x =>
                !string.IsNullOrWhiteSpace(
                    x.EstadoAnteriorOriginal
                ) &&
                !string.IsNullOrWhiteSpace(
                    x.EstadoNuevoOriginal
                ));

    var cambiosSinAmbosEstados =
        cambiosEstadoHistoricos.Count -
        cambiosConAmbosEstados;

    Console.WriteLine(
        $"Con ambos estados extraídos: " +
        $"{cambiosConAmbosEstados}"
    );

    Console.WriteLine(
        $"Sin ambos estados completos: " +
        $"{cambiosSinAmbosEstados}"
    );

    Console.WriteLine();

    // ============================================
    // NORMALIZACIÓN
    // ============================================

    var ambosEstadosNormalizados =
        cambiosEstadoHistoricos
            .Count(x =>
                x.EstadoAnteriorNormalizado != null &&
                x.EstadoNuevoNormalizado != null);

    var algunEstadoNoNormalizado =
        cambiosEstadoHistoricos.Count -
        ambosEstadosNormalizados;

    Console.WriteLine(
        $"Con ambos estados normalizados: " +
        $"{ambosEstadosNormalizados}"
    );

    Console.WriteLine(
        $"Con algún estado no normalizado: " +
        $"{algunEstadoNoNormalizado}"
    );

    Console.WriteLine();

    // ============================================
    // TRANSICIONES MÁS FRECUENTES
    // ============================================

    var transiciones =
        cambiosEstadoHistoricos
            .Where(x =>
                x.EstadoAnteriorNormalizado != null &&
                x.EstadoNuevoNormalizado != null)
            .GroupBy(x =>
                $"{x.EstadoAnteriorNormalizado} -> " +
                $"{x.EstadoNuevoNormalizado}")
            .OrderByDescending(x => x.Count())
            .ToList();

    Console.WriteLine("Transiciones detectadas:");
    Console.WriteLine();

    foreach (var grupo in transiciones)
    {
        Console.WriteLine(
            $"{grupo.Key}: {grupo.Count()}"
        );
    }

    Console.WriteLine();

    // ============================================
    // FECHAS
    // ============================================

    var cantidadFechasCambioEstadoNormalizadas =
        cambiosEstadoHistoricos
            .Count(x =>
                x.FechaMovimiento.HasValue);

    var cantidadFechasCambioEstadoNoNormalizadas =
        cambiosEstadoHistoricos.Count -
        cantidadFechasCambioEstadoNormalizadas;

    Console.WriteLine(
        $"Fechas normalizadas: " +
        $"{cantidadFechasCambioEstadoNormalizadas}"
    );

    Console.WriteLine(
        $"Fechas no normalizadas: " +
        $"{cantidadFechasCambioEstadoNoNormalizadas}"
    );

    Console.WriteLine();

    // ============================================
    // REVISIÓN
    // ============================================

    var cambiosEstadoQueRequierenRevision =
        cambiosEstadoHistoricos
            .Where(x =>
                x.RequiereRevision)
            .ToList();

    Console.WriteLine(
        $"Cambios de estado que requieren revisión: " +
        $"{cambiosEstadoQueRequierenRevision.Count}"
    );

    Console.WriteLine();

    // ============================================
    // PRIMEROS 20 CAMBIOS ESTRUCTURADOS
    // ============================================

    Console.ForegroundColor =
        ConsoleColor.Cyan;

    Console.WriteLine(
        "Primeros 20 cambios de estado estructurados:"
    );

    Console.ResetColor();

    Console.WriteLine();

    foreach (var cambio in cambiosEstadoHistoricos.Take(20))
    {
        Console.WriteLine(
            $"Placa(s): " +
            $"{string.Join(", ", cambio.Placas)}"
        );

        Console.WriteLine(
            $"Estado anterior original: " +
            $"'{cambio.EstadoAnteriorOriginal}'"
        );

        Console.WriteLine(
            $"Estado nuevo original: " +
            $"'{cambio.EstadoNuevoOriginal}'"
        );

        Console.WriteLine(
            $"Estado anterior normalizado: " +
            $"{cambio.EstadoAnteriorNormalizado ?? "[NO NORMALIZADO]"}"
        );

        Console.WriteLine(
            $"Estado nuevo normalizado: " +
            $"{cambio.EstadoNuevoNormalizado ?? "[NO NORMALIZADO]"}"
        );

        Console.WriteLine(
            $"Fecha original: " +
            $"'{cambio.FechaOriginal}'"
        );

        Console.WriteLine(
            $"Fecha normalizada: " +
            $"{(
                cambio.FechaMovimiento.HasValue
                    ? cambio.FechaMovimiento.Value
                        .ToString("dd/MM/yyyy")
                    : "[NO NORMALIZADA]"
            )}"
        );

        Console.WriteLine(
            $"Requiere revisión: " +
            $"{(cambio.RequiereRevision ? "SÍ" : "NO")}"
        );

        if (cambio.RequiereRevision)
        {
            Console.WriteLine(
                $"Motivo revisión: " +
                $"{cambio.MotivoRevision}"
            );
        }

        Console.WriteLine(
            $"Texto: {cambio.TextoOriginal}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // PRIMEROS CASOS QUE REQUIEREN REVISIÓN
    // ============================================

    Console.ForegroundColor =
        ConsoleColor.Yellow;

    Console.WriteLine(
        "Primeros 30 cambios de estado que requieren revisión:"
    );

    Console.ResetColor();

    Console.WriteLine();

    foreach (
        var cambio
        in cambiosEstadoQueRequierenRevision.Take(30)
    )
    {
        Console.WriteLine(
            $"Placa(s): " +
            $"{string.Join(", ", cambio.Placas)}"
        );

        Console.WriteLine(
            $"Anterior: " +
            $"'{cambio.EstadoAnteriorOriginal}'"
        );

        Console.WriteLine(
            $"Nuevo: " +
            $"'{cambio.EstadoNuevoOriginal}'"
        );

        Console.WriteLine(
            $"Fecha original: " +
            $"'{cambio.FechaOriginal}'"
        );

        Console.WriteLine(
            $"Motivo revisión: " +
            $"{cambio.MotivoRevision}"
        );

        Console.WriteLine(
            $"Texto: {cambio.TextoOriginal}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // ANÁLISIS DE MODIFICACIONES HISTÓRICAS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   ANÁLISIS DE MODIFICACIONES HISTÓRICAS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    Console.WriteLine(
        $"Modificaciones detectadas: " +
        $"{modificacionesHistoricas.Count}"
    );

    Console.WriteLine();

    // ============================================
    // DISTRIBUCIÓN POR TIPO
    // ============================================

    var modificacionesPorTipo =
        modificacionesHistoricas
            .GroupBy(x =>
                x.TipoModificacion)
            .OrderByDescending(x =>
                x.Count())
            .ToList();

    Console.WriteLine("Distribución por tipo:");
    Console.WriteLine();

    foreach (var grupo in modificacionesPorTipo)
    {
        Console.WriteLine(
            $"{grupo.Key}: {grupo.Count()}"
        );
    }

    Console.WriteLine();

    // ============================================
    // VALORES ANTERIOR / NUEVO
    // ============================================

    var modificacionesConAmbosValores =
        modificacionesHistoricas
            .Count(x =>
                !string.IsNullOrWhiteSpace(
                    x.ValorAnteriorOriginal
                )
                &&
                !string.IsNullOrWhiteSpace(
                    x.ValorNuevoOriginal
                ));

    Console.WriteLine(
        $"Con valor anterior y nuevo extraídos: " +
        $"{modificacionesConAmbosValores}"
    );

    Console.WriteLine(
        $"Sin ambos valores completos: " +
        $"{modificacionesHistoricas.Count - modificacionesConAmbosValores}"
    );

    Console.WriteLine();

    // ============================================
    // FECHAS
    // ============================================

    var cantidadFechasModificacionNormalizadas =
        modificacionesHistoricas
            .Count(x =>
                x.FechaMovimiento.HasValue);

    var cantidadFechasModificacionNoNormalizadas =
        modificacionesHistoricas.Count -
        cantidadFechasModificacionNormalizadas;

    Console.WriteLine(
        $"Fechas normalizadas: " +
        $"{cantidadFechasModificacionNormalizadas}"
    );

    Console.WriteLine(
        $"Fechas no normalizadas: " +
        $"{cantidadFechasModificacionNoNormalizadas}"
    );

    Console.WriteLine();

    // ============================================
    // REVISIÓN
    // ============================================

    var modificacionesQueRequierenRevision =
        modificacionesHistoricas
            .Where(x =>
                x.RequiereRevision)
            .ToList();

    Console.WriteLine(
        $"Modificaciones que requieren revisión: " +
        $"{modificacionesQueRequierenRevision.Count}"
    );

    Console.WriteLine();

    // ============================================
    // PRIMERAS 25 MODIFICACIONES
    // ============================================

    Console.ForegroundColor =
        ConsoleColor.Cyan;

    Console.WriteLine(
        "Primeras 25 modificaciones estructuradas:"
    );

    Console.ResetColor();

    Console.WriteLine();

    foreach (
        var modificacion
        in modificacionesHistoricas.Take(25)
    )
    {
        Console.WriteLine(
            $"Placa(s): " +
            $"{string.Join(", ", modificacion.Placas)}"
        );

        Console.WriteLine(
            $"Tipo: " +
            $"{modificacion.TipoModificacion}"
        );

        Console.WriteLine(
            $"Valor anterior: " +
            $"'{modificacion.ValorAnteriorOriginal}'"
        );

        Console.WriteLine(
            $"Valor nuevo: " +
            $"'{modificacion.ValorNuevoOriginal}'"
        );

        Console.WriteLine(
            $"Fecha original: " +
            $"'{modificacion.FechaOriginal}'"
        );

        Console.WriteLine(
            $"Fecha normalizada: " +
            $"{(
                modificacion.FechaMovimiento.HasValue
                    ? modificacion.FechaMovimiento.Value
                        .ToString("dd/MM/yyyy")
                    : "[NO NORMALIZADA]"
            )}"
        );

        Console.WriteLine(
            $"Requiere revisión: " +
            $"{(modificacion.RequiereRevision ? "SÍ" : "NO")}"
        );

        if (modificacion.RequiereRevision)
        {
            Console.WriteLine(
                $"Motivo revisión: " +
                $"{modificacion.MotivoRevision}"
            );
        }

        Console.WriteLine(
            $"Texto: {modificacion.TextoOriginal}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // CASOS QUE REQUIEREN REVISIÓN
    // ============================================

    Console.ForegroundColor =
        ConsoleColor.Yellow;

    Console.WriteLine(
        "Primeras 30 modificaciones que requieren revisión:"
    );

    Console.ResetColor();

    Console.WriteLine();

    foreach (
        var modificacion
        in modificacionesQueRequierenRevision.Take(30)
    )
    {
        Console.WriteLine(
            $"Placa(s): " +
            $"{string.Join(", ", modificacion.Placas)}"
        );

        Console.WriteLine(
            $"Tipo: " +
            $"{modificacion.TipoModificacion}"
        );

        Console.WriteLine(
            $"Valor anterior: " +
            $"'{modificacion.ValorAnteriorOriginal}'"
        );

        Console.WriteLine(
            $"Valor nuevo: " +
            $"'{modificacion.ValorNuevoOriginal}'"
        );

        Console.WriteLine(
            $"Fecha original: " +
            $"'{modificacion.FechaOriginal}'"
        );

        Console.WriteLine(
            $"Motivo revisión: " +
            $"{modificacion.MotivoRevision}"
        );

        Console.WriteLine(
            $"Texto: {modificacion.TextoOriginal}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   DETECCIÓN DE ARCHIVOS DE ÁREAS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    string carpetaDatos =
        Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "Datos"
        );

    carpetaDatos =
        Path.GetFullPath(
            carpetaDatos
        );

    var detectorArchivosArea =
        new DetectorArchivosAreaService();

    var archivosArea =
        detectorArchivosArea.Detectar(
            carpetaDatos
        );

    Console.WriteLine(
        $"Archivos de área detectados: {archivosArea.Count}"
    );

    Console.WriteLine();

    foreach (
        var archivo
        in archivosArea
    )
    {
        Console.WriteLine(
            $"Archivo: {archivo.NombreArchivo}"
        );

        Console.WriteLine(
            $"Área detectada: {(archivo.NumeroAreaDetectado.HasValue ? archivo.NumeroAreaDetectado.Value.ToString() : "[NO DETECTADA]")}"
        );

        Console.WriteLine(
            $"Requiere revisión: {(archivo.RequiereRevision ? "SÍ" : "NO")}"
        );

        if (
            archivo.RequiereRevision
        )
        {
            Console.WriteLine(
                $"Motivo: {archivo.MotivoRevision}"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    var areasDetectadas =
        archivosArea
            .Where(
                x =>
                x.NumeroAreaDetectado.HasValue
            )
            .GroupBy(
                x =>
                x.NumeroAreaDetectado!.Value
            )
            .OrderBy(
                x =>
                x.Key
            )
            .ToList();

    var areasRepetidas =
        areasDetectadas
            .Where(
                x =>
                x.Count() > 1
            )
            .ToList();

    var numerosDetectados =
        areasDetectadas
            .Select(
                x =>
                x.Key
            )
            .ToHashSet();

    var areasFaltantes =
        Enumerable
            .Range(
                1,
                70
            )
            .Where(
                numero =>
                !numerosDetectados.Contains(
                    numero
                )
            )
            .ToList();

    Console.WriteLine(
        $"Áreas únicas detectadas: {numerosDetectados.Count}"
    );

    Console.WriteLine(
        $"Archivos sin número de área reconocido: {archivosArea.Count(x => !x.NumeroAreaDetectado.HasValue)}"
    );

    Console.WriteLine(
        $"Áreas repetidas: {areasRepetidas.Count}"
    );

    if (
        areasRepetidas.Count > 0
    )
    {
        foreach (
            var grupo
            in areasRepetidas
        )
        {
            Console.WriteLine(
                $"  Área {grupo.Key}:"
            );

            foreach (
                var archivo
                in grupo
            )
            {
                Console.WriteLine(
                    $"    - {archivo.NombreArchivo}"
                );
            }
        }
    }

    Console.WriteLine(
        $"Áreas faltantes: {areasFaltantes.Count}"
    );

    if (
        areasFaltantes.Count > 0
    )
    {
        Console.WriteLine(
            $"  {string.Join(", ", areasFaltantes)}"
        );
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   ESTRUCTURA DE ARCHIVOS DE ÁREAS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var analizadorEstructuraAreas =
        new AnalizadorEstructuraAreasService();

    var estructurasAreas =
        analizadorEstructuraAreas.Analizar(
            archivosArea
        );

    Console.WriteLine(
        $"Archivos analizados: {estructurasAreas.Count}"
    );

    Console.WriteLine(
        $"Archivos que requieren revisión: {estructurasAreas.Count(x => x.RequiereRevision)}"
    );

    Console.WriteLine();

    var gruposEstructura =
        estructurasAreas
            .GroupBy(
                x =>
                    $"{x.FilaEncabezadoDetectada}|" +
                    $"{x.UltimaColumnaUsada}|" +
                    string.Join(
                        "|",
                        x.Encabezados
                            .Select(y => y.Trim())
                    )
            )
            .OrderByDescending(
                x => x.Count()
            )
            .ToList();

    Console.WriteLine(
        $"Estructuras diferentes encontradas: {gruposEstructura.Count}"
    );

    Console.WriteLine();

    int numeroGrupo = 1;

    foreach (
        var grupo
        in gruposEstructura
    )
    {
        Console.WriteLine(
            $"ESTRUCTURA #{numeroGrupo}"
        );

        Console.WriteLine(
            $"Cantidad de archivos: {grupo.Count()}"
        );

        Console.WriteLine(
            $"Áreas: {string.Join(", ", grupo.Select(x => x.NumeroArea).OrderBy(x => x))}"
        );

        var ejemplo =
            grupo.First();

        Console.WriteLine(
            $"Fila de encabezado: {(ejemplo.FilaEncabezadoDetectada.HasValue ? ejemplo.FilaEncabezadoDetectada.Value.ToString() : "[NO DETECTADA]")}"
        );

        Console.WriteLine(
            $"Cantidad de columnas usadas: {ejemplo.UltimaColumnaUsada}"
        );

        Console.WriteLine(
            "Encabezados:"
        );

        for (
            int i = 0;
            i < ejemplo.Encabezados.Count;
            i++
        )
        {
            Console.WriteLine(
                $"  {i + 1}. '{ejemplo.Encabezados[i]}'"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );

        numeroGrupo++;
    }

    var archivosRevision =
        estructurasAreas
            .Where(
                x => x.RequiereRevision
            )
            .OrderBy(
                x => x.NumeroArea
            )
            .ToList();

    if (
        archivosRevision.Count > 0
    )
    {
        Console.WriteLine();
        Console.WriteLine(
            "ARCHIVOS QUE REQUIEREN REVISIÓN:"
        );

        foreach (
            var archivo
            in archivosRevision
        )
        {
            Console.WriteLine(
                $"Área {archivo.NumeroArea} | {archivo.NombreArchivo}"
            );

            Console.WriteLine(
                $"Motivo: {archivo.MotivoRevision}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   INSPECCIÓN DE ARCHIVOS DE ÁREA"
    );
    Console.WriteLine(
        "============================================"
    );

    int[] areasAInspeccionar =
    {
    1,
    32,
    56,
    4,
    70
};

    foreach (int numeroArea in areasAInspeccionar)
    {
        var archivo =
            archivosArea.FirstOrDefault(
                x =>
                x.NumeroAreaDetectado == numeroArea
            );

        if (archivo == null)
        {
            continue;
        }

        Console.WriteLine();
        Console.WriteLine(
            $"========== ÁREA {numeroArea} =========="
        );

        using var workbook =
            new ClosedXML.Excel.XLWorkbook(
                archivo.RutaCompleta
            );

        Console.WriteLine(
            $"Hojas: {workbook.Worksheets.Count}"
        );

        foreach (
            var worksheet
            in workbook.Worksheets
        )
        {
            Console.WriteLine();
            Console.WriteLine(
                $"HOJA: {worksheet.Name}"
            );

            int ultimaFila =
                Math.Min(
                    worksheet.LastRowUsed()?.RowNumber() ?? 0,
                    20
                );

            int ultimaColumna =
                Math.Min(
                    worksheet.LastColumnUsed()?.ColumnNumber() ?? 0,
                    15
                );

            Console.WriteLine(
                $"Filas mostradas: {ultimaFila}"
            );

            Console.WriteLine(
                $"Columnas mostradas: {ultimaColumna}"
            );

            for (
                int fila = 1;
                fila <= ultimaFila;
                fila++
            )
            {
                var valores =
                    new List<string>();

                for (
                    int columna = 1;
                    columna <= ultimaColumna;
                    columna++
                )
                {
                    string valor =
                        worksheet
                            .Cell(
                                fila,
                                columna
                            )
                            .GetFormattedString()
                            .Trim();

                    valores.Add(
                        $"{columna}:'{valor}'"
                    );
                }

                Console.WriteLine(
                    $"Fila {fila}: {string.Join(" | ", valores)}"
                );
            }
        }
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   LECTURA REAL DE ARCHIVOS DE ÁREAS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var lectorArchivosArea =
        new LectorArchivosAreaService();

    var resultadoAreas =
        lectorArchivosArea.Leer(
            archivosArea
        );

    Console.WriteLine(
        $"Registros con placa válida: {resultadoAreas.Registros.Count}"
    );

    Console.WriteLine(
        $"Incidencias detectadas: {resultadoAreas.Incidencias.Count}"
    );

    Console.WriteLine();

    var placasUnicasAreas =
        resultadoAreas
            .Registros
            .Select(
                x => x.PlacaNormalizada
            )
            .Distinct(
                StringComparer.OrdinalIgnoreCase
            )
            .Count();

    Console.WriteLine(
        $"Placas únicas encontradas: {placasUnicasAreas}"
    );

    var placasRepetidasEntreAreas =
        resultadoAreas
            .Registros
            .GroupBy(
                x => x.PlacaNormalizada,
                StringComparer.OrdinalIgnoreCase
            )
            .Where(
                x =>
                x.Select(
                    y => y.NumeroAreaArchivo
                )
                .Distinct()
                .Count() > 1
            )
            .OrderBy(
                x => x.Key
            )
            .ToList();

    Console.WriteLine(
        $"Placas presentes en más de un área: {placasRepetidasEntreAreas.Count}"
    );

    Console.WriteLine();

    var incidenciasPorTipo =
        resultadoAreas
            .Incidencias
            .GroupBy(
                x => x.Tipo
            )
            .OrderByDescending(
                x => x.Count()
            );

    Console.WriteLine(
        "Incidencias por tipo:"
    );

    foreach (
        var grupo
        in incidenciasPorTipo
    )
    {
        Console.WriteLine(
            $"  {grupo.Key}: {grupo.Count()}"
        );
    }

    Console.WriteLine();

    if (
        placasRepetidasEntreAreas.Count > 0
    )
    {
        Console.WriteLine(
            "PLACAS PRESENTES EN MÁS DE UN ARCHIVO DE ÁREA:"
        );

        foreach (
            var grupo
            in placasRepetidasEntreAreas
                .Take(50)
        )
        {
            Console.WriteLine(
                $"Placa: {grupo.Key}"
            );

            foreach (
                var registro
                in grupo
            )
            {
                Console.WriteLine(
                    $"  Área {registro.NumeroAreaArchivo} | {registro.NombreArchivo} | fila {registro.NumeroFilaExcel}"
                );
            }

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();

    var primerasIncidencias =
        resultadoAreas
            .Incidencias
            .Take(50)
            .ToList();

    if (
        primerasIncidencias.Count > 0
    )
    {
        Console.WriteLine(
            "PRIMERAS INCIDENCIAS:"
        );

        foreach (
            var incidencia
            in primerasIncidencias
        )
        {
            Console.WriteLine(
                $"Área: {incidencia.NumeroAreaArchivo}"
            );

            Console.WriteLine(
                $"Archivo: {incidencia.NombreArchivo}"
            );

            Console.WriteLine(
                $"Hoja: {incidencia.NombreHoja}"
            );

            Console.WriteLine(
                $"Fila: {incidencia.NumeroFilaExcel}"
            );

            Console.WriteLine(
                $"Tipo: {incidencia.Tipo}"
            );

            Console.WriteLine(
                $"Valor original: '{incidencia.ValorOriginal}'"
            );

            Console.WriteLine(
                $"Descripción: '{incidencia.Descripcion}'"
            );

            Console.WriteLine(
                $"Detalle: {incidencia.Mensaje}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CRUCE INVENTARIO GENERAL VS ÁREAS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var servicioCruceAreas =
        new CruceInventarioAreasService();

    var cruceAreas =
    servicioCruceAreas.Comparar(
        registros,
        resultadoAreas
    );

    Console.WriteLine(
        $"Placas analizadas en el cruce: {cruceAreas.Resultados.Count}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"COINCIDE: {cruceAreas.Coinciden}"
    );

    Console.WriteLine(
        $"AREA_DIFERENTE: {cruceAreas.AreaDiferente}"
    );

    Console.WriteLine(
        $"SOLO_EN_GENERAL: {cruceAreas.SoloEnGeneral}"
    );

    Console.WriteLine(
        $"SOLO_EN_ARCHIVOS_AREA: {cruceAreas.SoloEnArchivosArea}"
    );

    Console.WriteLine(
        $"PRESENTE_EN_VARIAS_AREAS: {cruceAreas.PresenteEnVariasAreas}"
    );

    Console.WriteLine(
        $"GENERAL_DUPLICADO: {cruceAreas.GeneralDuplicado}"
    );

    Console.WriteLine(
        $"AREA_GENERAL_NO_NUMERICA: {cruceAreas.AreaGeneralNoNumerica}"
    );

    Console.WriteLine();

    var diferenciasArea =
        cruceAreas
            .Resultados
            .Where(
                x =>
                x.Clasificacion ==
                "AREA_DIFERENTE"
            )
            .ToList();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   PRIMERAS DIFERENCIAS DE ÁREA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var item
        in diferenciasArea.Take(50)
    )
    {
        Console.WriteLine(
            $"Placa: {item.Placa}"
        );

        Console.WriteLine(
            $"Fila inventario general: {item.FilaGeneral}"
        );

        Console.WriteLine(
            $"Ubicación general original: '{item.UbicacionGeneralOriginal}'"
        );

        Console.WriteLine(
            $"Área general detectada: {item.AreaGeneral}"
        );

        Console.WriteLine(
            $"Área(s) en archivos: {string.Join(", ", item.AreasArchivos)}"
        );

        Console.WriteLine(
            $"Detalle: {item.Observacion}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    var soloAreas =
        cruceAreas
            .Resultados
            .Where(
                x =>
                x.Clasificacion ==
                "SOLO_EN_ARCHIVOS_AREA"
            )
            .ToList();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   PRIMERAS PLACAS SOLO EN ARCHIVOS DE ÁREA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var item
        in soloAreas.Take(30)
    )
    {
        Console.WriteLine(
            $"Placa: {item.Placa}"
        );

        Console.WriteLine(
            $"Área(s): {string.Join(", ", item.AreasArchivos)}"
        );

        foreach (
            string detalle
            in item.DetallesArchivosArea
        )
        {
            Console.WriteLine(
                $"  {detalle}"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CRUCE IRREGULARES VS ARCHIVOS DE ÁREA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var servicioCruceIrregularesAreas =
        new CruceIrregularesAreasService();

    var cruceIrregularesAreas =
        servicioCruceIrregularesAreas.Comparar(
            activosIrregulares,
            resultadoAreas,
            cruceAreas
        );

    Console.WriteLine(
        $"Identificaciones irregulares analizadas: {cruceIrregularesAreas.TotalIrregulares}"
    );

    Console.WriteLine(
        $"Con equivalente comparable 4615-0000: {cruceIrregularesAreas.ConEquivalenteComparable}"
    );

    Console.WriteLine(
        $"Equivalentes encontrados en archivos de área: {cruceIrregularesAreas.EncontradosEnAreas}"
    );

    Console.WriteLine(
        $"Equivalentes NO encontrados en archivos de área: {cruceIrregularesAreas.NoEncontradosEnAreas}"
    );

    Console.WriteLine(
        $"Casos que explican SOLO_EN_ARCHIVOS_AREA: {cruceIrregularesAreas.ExplicanSoloEnArchivosArea}"
    );

    Console.WriteLine(
        $"Equivalentes presentes en varias áreas: {cruceIrregularesAreas.EncontradosEnVariasAreas}"
    );

    Console.WriteLine();

    var casosQueExplicanSoloArea =
        cruceIrregularesAreas
            .Resultados
            .Where(
                x => x.EraSoloEnArchivosArea
            )
            .ToList();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   SOLO_EN_ARCHIVOS_AREA EXPLICADOS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var item
        in casosQueExplicanSoloArea
    )
    {
        Console.WriteLine(
            $"Identificación general original: '{item.IdentificacionGeneralOriginal}'"
        );

        Console.WriteLine(
            $"Fila general: {item.FilaGeneral}"
        );

        Console.WriteLine(
            $"Descripción general: '{item.DescripcionGeneral}'"
        );

        Console.WriteLine(
            $"Tipo irregularidad: {item.TipoIrregularidad}"
        );

        Console.WriteLine(
            $"Equivalente comparado: {item.PlacaEquivalenteComparada}"
        );

        Console.WriteLine(
            $"Área(s): {string.Join(", ", item.AreasEncontradas)}"
        );

        foreach (
            string detalle
            in item.DetallesArchivosArea
        )
        {
            Console.WriteLine(
                $"  {detalle}"
            );
        }

        Console.WriteLine(
            $"Conclusión: {item.Observacion}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   TODAS LAS COINCIDENCIAS IRREGULARES"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var item
        in cruceIrregularesAreas
            .Resultados
            .Where(
                x => x.EncontradoEnArchivosArea
            )
    )
    {
        Console.WriteLine(
            $"Original general: '{item.IdentificacionGeneralOriginal}'"
        );

        Console.WriteLine(
            $"Equivalente: {item.PlacaEquivalenteComparada}"
        );

        Console.WriteLine(
            $"Fila general: {item.FilaGeneral}"
        );

        Console.WriteLine(
            $"Área(s) encontrada(s): {string.Join(", ", item.AreasEncontradas)}"
        );

        Console.WriteLine(
            $"Era SOLO_EN_ARCHIVOS_AREA: {(item.EraSoloEnArchivosArea ? "SÍ" : "NO")}"
        );

        Console.WriteLine(
            $"Requiere revisión: {(item.RequiereRevision ? "SÍ" : "NO")}"
        );

        Console.WriteLine(
            $"Conclusión: {item.Observacion}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CRUCE SOLO_EN_GENERAL VS BAJAS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var servicioCruceBajasSoloGeneral =
        new CruceBajasSoloGeneralService();

    var cruceBajasSoloGeneral =
        servicioCruceBajasSoloGeneral.Comparar(
            cruceAreas,
            bajasHistoricas
        );

    Console.WriteLine(
        $"Total SOLO_EN_GENERAL analizados: {cruceBajasSoloGeneral.TotalSoloGeneral}"
    );

    Console.WriteLine(
        $"Explicados por una baja histórica: {cruceBajasSoloGeneral.ExplicadosPorBaja}"
    );

    Console.WriteLine(
        $"Sin baja histórica encontrada: {cruceBajasSoloGeneral.SinBajaHistorica}"
    );

    Console.WriteLine(
        $"Con múltiples bajas históricas: {cruceBajasSoloGeneral.ConMultiplesBajas}"
    );

    Console.WriteLine(
        $"Casos que requieren revisión: {cruceBajasSoloGeneral.RequierenRevision}"
    );

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   PRIMEROS SOLO_EN_GENERAL EXPLICADOS POR BAJA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var item
        in cruceBajasSoloGeneral
            .Resultados
            .Where(
                x => x.TieneBajaHistorica
            )
            .Take(50)
    )
    {
        Console.WriteLine(
            $"Placa: {item.Placa}"
        );

        Console.WriteLine(
            $"Fila inventario general: {item.FilaGeneral}"
        );

        Console.WriteLine(
            $"Ubicación general: '{item.UbicacionGeneralOriginal}'"
        );

        Console.WriteLine(
            $"Motivo de baja: {item.MotivoBaja}"
        );

        Console.WriteLine(
            $"Fecha original: '{item.FechaBajaOriginal}'"
        );

        Console.WriteLine(
            $"Fecha normalizada: {(item.FechaBaja.HasValue ? item.FechaBaja.Value.ToString("dd/MM/yyyy") : "[NO NORMALIZADA]")}"
        );

        Console.WriteLine(
            $"Cantidad de bajas encontradas: {item.CantidadBajasEncontradas}"
        );

        Console.WriteLine(
            $"Requiere revisión: {(item.RequiereRevision ? "SÍ" : "NO")}"
        );

        Console.WriteLine(
            $"Conclusión: {item.Observacion}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   FECHAS DE LEVANTAMIENTO POR ÁREA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var detectorFechasAreas =
        new DetectorFechasLevantamientoAreasService();

    var fechasLevantamientoAreas =
        detectorFechasAreas.Detectar(
            archivosArea
        );

    Console.WriteLine(
        $"Áreas analizadas: {fechasLevantamientoAreas.Count}"
    );

    Console.WriteLine(
        $"Fechas normalizadas: {fechasLevantamientoAreas.Count(x => x.FechaLevantamiento.HasValue)}"
    );

    Console.WriteLine(
        $"Áreas que requieren revisión: {fechasLevantamientoAreas.Count(x => x.RequiereRevision)}"
    );

    Console.WriteLine();

    foreach (
        var item
        in fechasLevantamientoAreas
    )
    {
        Console.WriteLine(
            $"Área {item.NumeroArea}"
        );

        Console.WriteLine(
            $"Archivo: {item.NombreArchivo}"
        );

        Console.WriteLine(
            $"Fecha: {(item.FechaLevantamiento.HasValue ? item.FechaLevantamiento.Value.ToString("dd/MM/yyyy") : "[NO DETECTADA]")}"
        );

        if (
    item.RequiereRevision
)
        {
            Console.WriteLine(
                $"Requiere revisión: SÍ"
            );

            Console.WriteLine(
                $"Título original: '{item.TituloOriginal}'"
            );

            Console.WriteLine(
                $"Fecha original detectada: '{item.FechaOriginal}'"
            );

            Console.WriteLine(
                $"Motivo: {item.MotivoRevision}"
            );
        }
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CRUCE TEMPORAL DE DIFERENCIAS DE ÁREA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var diferenciasAreaTemporales =
        cruceAreas
            .Resultados
            .Where(
                x =>
                x.Clasificacion ==
                "AREA_DIFERENTE"
            )
            .ToList();

    int conFechaLevantamiento = 0;
    int explicadasPorTrasladoDirecto = 0;
    int explicadasPorCadenaTraslados = 0;
    int trasladoMismoDia = 0;
    int sinExplicacionTemporal = 0;
    int sinFechaLevantamiento = 0;

    var ejemplosTrasladoDirecto =
        new List<string>();

    var ejemplosCadena =
        new List<string>();

    var ejemplosMismoDia =
        new List<string>();

    var ejemplosSinExplicacion =
        new List<string>();

    foreach (
        var diferencia
        in diferenciasAreaTemporales
    )
    {
        if (
            !diferencia.AreaGeneral.HasValue ||
            diferencia.AreasArchivos.Count != 1
        )
        {
            continue;
        }

        int areaGeneral =
            diferencia.AreaGeneral.Value;

        int areaArchivo =
            diferencia.AreasArchivos.Single();

        var informacionFecha =
            fechasLevantamientoAreas
                .FirstOrDefault(
                    x =>
                    x.NumeroArea == areaArchivo
                );

        if (
            informacionFecha == null ||
            !informacionFecha.FechaLevantamiento.HasValue
        )
        {
            sinFechaLevantamiento++;

            continue;
        }

        conFechaLevantamiento++;

        DateTime fechaLevantamiento =
            informacionFecha
                .FechaLevantamiento
                .Value
                .Date;

        var trasladosDeLaPlaca =
            trasladosHistoricos
                .Where(
                    traslado =>
                    traslado.Placas.Any(
                        placa =>
                        placa.Equals(
                            diferencia.Placa,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .Where(
                    traslado =>
                    traslado.FechaMovimiento.HasValue
                )
                .OrderBy(
                    traslado =>
                    traslado.FechaMovimiento
                )
                .ToList();

        // ============================================
        // 1. TRASLADO DIRECTO POSTERIOR AL LEVANTAMIENTO
        //
        // Ejemplo:
        // Archivo Área 5
        // Levantamiento 01/05/2026
        // Traslado 5 -> 31 el 20/05/2026
        // General actual Área 31
        // ============================================

        var trasladoDirecto =
            trasladosDeLaPlaca
                .Where(
                    traslado =>
                    traslado.AreaAnteriorNumero ==
                        areaArchivo &&
                    traslado.AreaNuevaNumero ==
                        areaGeneral &&
                    traslado.FechaMovimiento!.Value.Date >
                        fechaLevantamiento
                )
                .OrderBy(
                    traslado =>
                    traslado.FechaMovimiento
                )
                .FirstOrDefault();

        if (
            trasladoDirecto != null
        )
        {
            explicadasPorTrasladoDirecto++;

            if (
                ejemplosTrasladoDirecto.Count < 30
            )
            {
                ejemplosTrasladoDirecto.Add(
                    $"Placa: {diferencia.Placa}\n" +
                    $"Archivo de área: {areaArchivo}\n" +
                    $"Fecha levantamiento: {fechaLevantamiento:dd/MM/yyyy}\n" +
                    $"General actual: Área {areaGeneral}\n" +
                    $"Traslado histórico: " +
                    $"{trasladoDirecto.AreaAnteriorNumero} -> " +
                    $"{trasladoDirecto.AreaNuevaNumero}\n" +
                    $"Fecha traslado: " +
                    $"{trasladoDirecto.FechaMovimiento!.Value:dd/MM/yyyy}\n" +
                    $"Conclusión: diferencia explicada por traslado posterior al levantamiento."
                );
            }

            continue;
        }

        // ============================================
        // 2. TRASLADO EL MISMO DÍA
        //
        // No podemos saber automáticamente si el
        // levantamiento fue antes o después del traslado.
        // ============================================

        var trasladoDelMismoDia =
            trasladosDeLaPlaca
                .FirstOrDefault(
                    traslado =>
                    traslado.AreaAnteriorNumero ==
                        areaArchivo &&
                    traslado.AreaNuevaNumero ==
                        areaGeneral &&
                    traslado.FechaMovimiento!.Value.Date ==
                        fechaLevantamiento
                );

        if (
            trasladoDelMismoDia != null
        )
        {
            trasladoMismoDia++;

            if (
                ejemplosMismoDia.Count < 20
            )
            {
                ejemplosMismoDia.Add(
                    $"Placa: {diferencia.Placa}\n" +
                    $"Archivo de área: {areaArchivo}\n" +
                    $"General actual: Área {areaGeneral}\n" +
                    $"Fecha levantamiento: {fechaLevantamiento:dd/MM/yyyy}\n" +
                    $"Fecha traslado: " +
                    $"{trasladoDelMismoDia.FechaMovimiento!.Value:dd/MM/yyyy}\n" +
                    $"Conclusión: traslado y levantamiento ocurrieron el mismo día; requiere revisión."
                );
            }

            continue;
        }

        // ============================================
        // 3. CADENA DE TRASLADOS
        //
        // Ejemplo:
        //
        // Levantamiento:
        // Área 5
        //
        // Después:
        // 5 -> 12
        // 12 -> 31
        //
        // General actual:
        // Área 31
        //
        // Aunque no exista un traslado directo
        // 5 -> 31, la cadena explica la diferencia.
        // ============================================

        int areaReconstruida =
            areaArchivo;

        var cadenaAplicada =
            new List<string>();

        foreach (
            var traslado
            in trasladosDeLaPlaca
                .Where(
                    x =>
                    x.FechaMovimiento!.Value.Date >
                        fechaLevantamiento
                )
        )
        {
            if (
                !traslado.AreaAnteriorNumero.HasValue ||
                !traslado.AreaNuevaNumero.HasValue
            )
            {
                continue;
            }

            if (
                traslado.AreaAnteriorNumero.Value !=
                areaReconstruida
            )
            {
                continue;
            }

            cadenaAplicada.Add(
                $"{traslado.AreaAnteriorNumero.Value} -> " +
                $"{traslado.AreaNuevaNumero.Value} " +
                $"({traslado.FechaMovimiento!.Value:dd/MM/yyyy})"
            );

            areaReconstruida =
                traslado.AreaNuevaNumero.Value;
        }

        if (
            cadenaAplicada.Count > 0 &&
            areaReconstruida == areaGeneral
        )
        {
            explicadasPorCadenaTraslados++;

            if (
                ejemplosCadena.Count < 30
            )
            {
                ejemplosCadena.Add(
                    $"Placa: {diferencia.Placa}\n" +
                    $"Área en levantamiento: {areaArchivo}\n" +
                    $"Fecha levantamiento: {fechaLevantamiento:dd/MM/yyyy}\n" +
                    $"Área general actual: {areaGeneral}\n" +
                    $"Cadena:\n  " +
                    string.Join(
                        "\n  ",
                        cadenaAplicada
                    ) +
                    "\nConclusión: diferencia explicada por una cadena de traslados posteriores."
                );
            }

            continue;
        }

        // ============================================
        // 4. SIN EXPLICACIÓN
        // ============================================

        sinExplicacionTemporal++;

        if (
            ejemplosSinExplicacion.Count < 30
        )
        {
            ejemplosSinExplicacion.Add(
                $"Placa: {diferencia.Placa}\n" +
                $"Área archivo: {areaArchivo}\n" +
                $"Fecha levantamiento: {fechaLevantamiento:dd/MM/yyyy}\n" +
                $"Área general: {areaGeneral}\n" +
                $"Traslados históricos con fecha encontrados: " +
                $"{trasladosDeLaPlaca.Count}"
            );
        }
    }

    Console.WriteLine(
        $"AREA_DIFERENTE analizados: {diferenciasAreaTemporales.Count}"
    );

    Console.WriteLine(
        $"Con fecha de levantamiento disponible: {conFechaLevantamiento}"
    );

    Console.WriteLine(
        $"Sin fecha de levantamiento: {sinFechaLevantamiento}"
    );

    Console.WriteLine(
        $"Explicadas por traslado directo posterior: {explicadasPorTrasladoDirecto}"
    );

    Console.WriteLine(
        $"Explicadas por cadena de traslados posteriores: {explicadasPorCadenaTraslados}"
    );

    Console.WriteLine(
        $"Traslado ocurrido el mismo día del levantamiento: {trasladoMismoDia}"
    );

    Console.WriteLine(
        $"Sin explicación temporal encontrada: {sinExplicacionTemporal}"
    );

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: TRASLADO DIRECTO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosTrasladoDirecto
    )
    {
        Console.WriteLine(ejemplo);

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: CADENA DE TRASLADOS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosCadena
    )
    {
        Console.WriteLine(ejemplo);

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: TRASLADO EL MISMO DÍA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosMismoDia
    )
    {
        Console.WriteLine(ejemplo);

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: SIN EXPLICACIÓN TEMPORAL"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosSinExplicacion
    )
    {
        Console.WriteLine(ejemplo);

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   ÚLTIMO TRASLADO VS DIFERENCIA DE ÁREA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    int ultimoApuntaGeneral = 0;
    int ultimoApuntaArchivo = 0;
    int ultimoApuntaOtraArea = 0;
    int sinTrasladoNumerico = 0;

    var ejemplosUltimoGeneral =
        new List<string>();

    var ejemplosUltimoArchivo =
        new List<string>();

    var ejemplosOtraArea =
        new List<string>();

    var ejemplosSinTraslado =
        new List<string>();

    foreach (
        var diferencia
        in diferenciasAreaTemporales
    )
    {
        if (
            !diferencia.AreaGeneral.HasValue ||
            diferencia.AreasArchivos.Count != 1
        )
        {
            continue;
        }

        int areaGeneral =
            diferencia.AreaGeneral.Value;

        int areaArchivo =
            diferencia.AreasArchivos.Single();

        var trasladosNumericos =
            trasladosHistoricos
                .Where(
                    traslado =>
                    traslado.Placas.Any(
                        placa =>
                        placa.Equals(
                            diferencia.Placa,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .Where(
                    traslado =>
                    traslado.FechaMovimiento.HasValue &&
                    traslado.AreaAnteriorNumero.HasValue &&
                    traslado.AreaNuevaNumero.HasValue
                )
                .OrderBy(
                    traslado =>
                    traslado.FechaMovimiento
                )
                .ToList();

        if (
            trasladosNumericos.Count == 0
        )
        {
            sinTrasladoNumerico++;

            if (
                ejemplosSinTraslado.Count < 20
            )
            {
                ejemplosSinTraslado.Add(
                    $"Placa: {diferencia.Placa}\n" +
                    $"Área general: {areaGeneral}\n" +
                    $"Área archivo: {areaArchivo}\n" +
                    "No tiene traslados históricos numérico → numérico con fecha."
                );
            }

            continue;
        }

        var ultimoTraslado =
            trasladosNumericos.Last();

        int ultimaAreaDestino =
            ultimoTraslado.AreaNuevaNumero!.Value;

        if (
            ultimaAreaDestino ==
            areaGeneral
        )
        {
            ultimoApuntaGeneral++;

            if (
                ejemplosUltimoGeneral.Count < 30
            )
            {
                ejemplosUltimoGeneral.Add(
                    $"Placa: {diferencia.Placa}\n" +
                    $"Área general: {areaGeneral}\n" +
                    $"Área archivo: {areaArchivo}\n" +
                    $"Último traslado: " +
                    $"{ultimoTraslado.AreaAnteriorNumero} -> " +
                    $"{ultimoTraslado.AreaNuevaNumero}\n" +
                    $"Fecha: " +
                    $"{ultimoTraslado.FechaMovimiento!.Value:dd/MM/yyyy}\n" +
                    "Conclusión: el último traslado histórico respalda el área del inventario general."
                );
            }

            continue;
        }

        if (
            ultimaAreaDestino ==
            areaArchivo
        )
        {
            ultimoApuntaArchivo++;

            if (
                ejemplosUltimoArchivo.Count < 30
            )
            {
                ejemplosUltimoArchivo.Add(
                    $"Placa: {diferencia.Placa}\n" +
                    $"Área general: {areaGeneral}\n" +
                    $"Área archivo: {areaArchivo}\n" +
                    $"Último traslado: " +
                    $"{ultimoTraslado.AreaAnteriorNumero} -> " +
                    $"{ultimoTraslado.AreaNuevaNumero}\n" +
                    $"Fecha: " +
                    $"{ultimoTraslado.FechaMovimiento!.Value:dd/MM/yyyy}\n" +
                    "Conclusión: el último traslado histórico respalda el archivo de área."
                );
            }

            continue;
        }

        ultimoApuntaOtraArea++;

        if (
            ejemplosOtraArea.Count < 30
        )
        {
            ejemplosOtraArea.Add(
                $"Placa: {diferencia.Placa}\n" +
                $"Área general: {areaGeneral}\n" +
                $"Área archivo: {areaArchivo}\n" +
                $"Último traslado: " +
                $"{ultimoTraslado.AreaAnteriorNumero} -> " +
                $"{ultimoTraslado.AreaNuevaNumero}\n" +
                $"Fecha: " +
                $"{ultimoTraslado.FechaMovimiento!.Value:dd/MM/yyyy}\n" +
                $"Conclusión: el último traslado histórico apunta a un área distinta de ambas."
            );
        }
    }

    Console.WriteLine(
        $"Diferencias analizadas: {diferenciasAreaTemporales.Count}"
    );

    Console.WriteLine(
        $"Último traslado respalda GENERAL: {ultimoApuntaGeneral}"
    );

    Console.WriteLine(
        $"Último traslado respalda ARCHIVO DE ÁREA: {ultimoApuntaArchivo}"
    );

    Console.WriteLine(
        $"Último traslado apunta a OTRA ÁREA: {ultimoApuntaOtraArea}"
    );

    Console.WriteLine(
        $"Sin traslado numérico utilizable: {sinTrasladoNumerico}"
    );

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: HISTORIAL RESPALDA GENERAL"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosUltimoGeneral
    )
    {
        Console.WriteLine(ejemplo);

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: HISTORIAL RESPALDA ARCHIVO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosUltimoArchivo
    )
    {
        Console.WriteLine(ejemplo);

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: HISTORIAL APUNTA A OTRA ÁREA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosOtraArea
    )
    {
        Console.WriteLine(ejemplo);

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: SIN TRASLADO NUMÉRICO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosSinTraslado
    )
    {
        Console.WriteLine(ejemplo);

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   VALIDACIÓN TEMPORAL DEL ÚLTIMO TRASLADO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    int archivoConfirmadoAntesLevantamiento = 0;
    int generalRespaldadoAntesLevantamiento = 0;
    int otraAreaAntesLevantamiento = 0;

    int trasladoArchivoPosteriorLevantamiento = 0;
    int trasladoGeneralPosteriorLevantamiento = 0;
    int trasladoOtraAreaPosteriorLevantamiento = 0;

    int sinTrasladoAntesLevantamiento = 0;

    var ejemplosArchivoConfirmado =
        new List<string>();

    var ejemplosGeneralConfirmado =
        new List<string>();

    var ejemplosPosteriores =
        new List<string>();

    var ejemplosOtraAreaAntes =
        new List<string>();

    foreach (
        var diferencia
        in diferenciasAreaTemporales
    )
    {
        if (
            !diferencia.AreaGeneral.HasValue ||
            diferencia.AreasArchivos.Count != 1
        )
        {
            continue;
        }

        int areaGeneral =
            diferencia.AreaGeneral.Value;

        int areaArchivo =
            diferencia.AreasArchivos.Single();

        var informacionFecha =
            fechasLevantamientoAreas
                .FirstOrDefault(
                    x =>
                    x.NumeroArea == areaArchivo
                );

        if (
            informacionFecha == null ||
            !informacionFecha.FechaLevantamiento.HasValue
        )
        {
            continue;
        }

        DateTime fechaLevantamiento =
            informacionFecha
                .FechaLevantamiento
                .Value
                .Date;

        var trasladosNumericos =
            trasladosHistoricos
                .Where(
                    traslado =>
                    traslado.Placas.Any(
                        placa =>
                        placa.Equals(
                            diferencia.Placa,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .Where(
                    traslado =>
                    traslado.FechaMovimiento.HasValue &&
                    traslado.AreaAnteriorNumero.HasValue &&
                    traslado.AreaNuevaNumero.HasValue
                )
                .OrderBy(
                    traslado =>
                    traslado.FechaMovimiento
                )
                .ToList();

        // ============================================
        // ÚLTIMO TRASLADO OCURRIDO ANTES O EL MISMO
        // DÍA DEL LEVANTAMIENTO
        // ============================================

        var ultimoAntesDelLevantamiento =
            trasladosNumericos
                .Where(
                    traslado =>
                    traslado.FechaMovimiento!.Value.Date <=
                    fechaLevantamiento
                )
                .LastOrDefault();

        if (
            ultimoAntesDelLevantamiento == null
        )
        {
            sinTrasladoAntesLevantamiento++;

            continue;
        }

        int destino =
            ultimoAntesDelLevantamiento
                .AreaNuevaNumero!
                .Value;

        if (
            destino == areaArchivo
        )
        {
            archivoConfirmadoAntesLevantamiento++;

            if (
                ejemplosArchivoConfirmado.Count < 30
            )
            {
                ejemplosArchivoConfirmado.Add(
                    $"Placa: {diferencia.Placa}\n" +
                    $"Área general: {areaGeneral}\n" +
                    $"Área archivo: {areaArchivo}\n" +
                    $"Último traslado anterior al levantamiento: " +
                    $"{ultimoAntesDelLevantamiento.AreaAnteriorNumero} -> " +
                    $"{ultimoAntesDelLevantamiento.AreaNuevaNumero}\n" +
                    $"Fecha traslado: " +
                    $"{ultimoAntesDelLevantamiento.FechaMovimiento!.Value:dd/MM/yyyy}\n" +
                    $"Fecha levantamiento: {fechaLevantamiento:dd/MM/yyyy}\n" +
                    "Conclusión: historial y levantamiento físico respaldan el archivo de área."
                );
            }
        }
        else if (
            destino == areaGeneral
        )
        {
            generalRespaldadoAntesLevantamiento++;

            if (
                ejemplosGeneralConfirmado.Count < 30
            )
            {
                ejemplosGeneralConfirmado.Add(
                    $"Placa: {diferencia.Placa}\n" +
                    $"Área general: {areaGeneral}\n" +
                    $"Área archivo: {areaArchivo}\n" +
                    $"Último traslado anterior al levantamiento: " +
                    $"{ultimoAntesDelLevantamiento.AreaAnteriorNumero} -> " +
                    $"{ultimoAntesDelLevantamiento.AreaNuevaNumero}\n" +
                    $"Fecha traslado: " +
                    $"{ultimoAntesDelLevantamiento.FechaMovimiento!.Value:dd/MM/yyyy}\n" +
                    $"Fecha levantamiento: {fechaLevantamiento:dd/MM/yyyy}\n" +
                    "Conclusión: el historial previo respalda el área del inventario general."
                );
            }
        }
        else
        {
            otraAreaAntesLevantamiento++;

            if (
                ejemplosOtraAreaAntes.Count < 30
            )
            {
                ejemplosOtraAreaAntes.Add(
                    $"Placa: {diferencia.Placa}\n" +
                    $"Área general: {areaGeneral}\n" +
                    $"Área archivo: {areaArchivo}\n" +
                    $"Último traslado anterior al levantamiento: " +
                    $"{ultimoAntesDelLevantamiento.AreaAnteriorNumero} -> " +
                    $"{ultimoAntesDelLevantamiento.AreaNuevaNumero}\n" +
                    $"Fecha traslado: " +
                    $"{ultimoAntesDelLevantamiento.FechaMovimiento!.Value:dd/MM/yyyy}\n" +
                    $"Fecha levantamiento: {fechaLevantamiento:dd/MM/yyyy}"
                );
            }
        }

        // ============================================
        // COMPROBAR SI EXISTEN TRASLADOS POSTERIORES
        // AL LEVANTAMIENTO
        // ============================================

        var ultimoPosterior =
            trasladosNumericos
                .Where(
                    traslado =>
                    traslado.FechaMovimiento!.Value.Date >
                    fechaLevantamiento
                )
                .LastOrDefault();

        if (
            ultimoPosterior == null
        )
        {
            continue;
        }

        int destinoPosterior =
            ultimoPosterior
                .AreaNuevaNumero!
                .Value;

        if (
            destinoPosterior == areaArchivo
        )
        {
            trasladoArchivoPosteriorLevantamiento++;
        }
        else if (
            destinoPosterior == areaGeneral
        )
        {
            trasladoGeneralPosteriorLevantamiento++;
        }
        else
        {
            trasladoOtraAreaPosteriorLevantamiento++;
        }

        if (
            ejemplosPosteriores.Count < 30
        )
        {
            ejemplosPosteriores.Add(
                $"Placa: {diferencia.Placa}\n" +
                $"Área general: {areaGeneral}\n" +
                $"Área archivo: {areaArchivo}\n" +
                $"Fecha levantamiento: {fechaLevantamiento:dd/MM/yyyy}\n" +
                $"Traslado posterior: " +
                $"{ultimoPosterior.AreaAnteriorNumero} -> " +
                $"{ultimoPosterior.AreaNuevaNumero}\n" +
                $"Fecha traslado: " +
                $"{ultimoPosterior.FechaMovimiento!.Value:dd/MM/yyyy}"
            );
        }
    }

    Console.WriteLine(
        $"Diferencias analizadas: {diferenciasAreaTemporales.Count}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"ARCHIVO respaldado por último traslado previo: {archivoConfirmadoAntesLevantamiento}"
    );

    Console.WriteLine(
        $"GENERAL respaldado por último traslado previo: {generalRespaldadoAntesLevantamiento}"
    );

    Console.WriteLine(
        $"Último traslado previo apunta a OTRA ÁREA: {otraAreaAntesLevantamiento}"
    );

    Console.WriteLine(
        $"Sin traslado numérico previo al levantamiento: {sinTrasladoAntesLevantamiento}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Traslado posterior al levantamiento termina en ARCHIVO: {trasladoArchivoPosteriorLevantamiento}"
    );

    Console.WriteLine(
        $"Traslado posterior al levantamiento termina en GENERAL: {trasladoGeneralPosteriorLevantamiento}"
    );

    Console.WriteLine(
        $"Traslado posterior al levantamiento termina en OTRA ÁREA: {trasladoOtraAreaPosteriorLevantamiento}"
    );

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: ARCHIVO CONFIRMADO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosArchivoConfirmado
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: GENERAL CONFIRMADO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosGeneralConfirmado
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: OTRA ÁREA ANTES DEL LEVANTAMIENTO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosOtraAreaAntes
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: TRASLADOS POSTERIORES"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosPosteriores
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   SOLO_EN_GENERAL SIN BAJA VS TRASLADOS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var soloGeneralSinBaja =
        cruceBajasSoloGeneral
            .Resultados
            .Where(
                x => !x.TieneBajaHistorica
            )
            .ToList();

    int soloGeneralExplicadoPorTrasladoPosterior =
        0;

    int soloGeneralTrasladoPrevioYNoAparece =
        0;

    int soloGeneralUltimoTrasladoOtraArea =
        0;

    int soloGeneralSinTrasladoNumerico =
        0;

    int soloGeneralSinAreaGeneralNumerica =
        0;

    int soloGeneralSinFechaLevantamiento =
        0;

    var soloGeneralEjemplosPosteriores =
        new List<string>();

    var soloGeneralEjemplosPrevios =
        new List<string>();

    var soloGeneralEjemplosOtraArea =
        new List<string>();

    var soloGeneralEjemplosSinTraslado =
        new List<string>();

    foreach (
        var item
        in soloGeneralSinBaja
    )
    {
        var resultadoCruce =
            cruceAreas
                .Resultados
                .FirstOrDefault(
                    x =>
                    x.Placa.Equals(
                        item.Placa,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

        if (
            resultadoCruce == null ||
            !resultadoCruce.AreaGeneral.HasValue
        )
        {
            soloGeneralSinAreaGeneralNumerica++;

            continue;
        }

        int areaGeneral =
            resultadoCruce.AreaGeneral.Value;

        var fechaAreaGeneral =
            fechasLevantamientoAreas
                .FirstOrDefault(
                    x =>
                    x.NumeroArea ==
                    areaGeneral
                );

        if (
            fechaAreaGeneral == null ||
            !fechaAreaGeneral
                .FechaLevantamiento
                .HasValue
        )
        {
            soloGeneralSinFechaLevantamiento++;

            continue;
        }

        DateTime fechaLevantamiento =
            fechaAreaGeneral
                .FechaLevantamiento
                .Value
                .Date;

        var trasladosNumericosSoloGeneral =
            trasladosHistoricos
                .Where(
                    traslado =>
                    traslado.Placas.Any(
                        placa =>
                        placa.Equals(
                            item.Placa,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .Where(
                    traslado =>
                    traslado.FechaMovimiento.HasValue &&
                    traslado.AreaAnteriorNumero.HasValue &&
                    traslado.AreaNuevaNumero.HasValue
                )
                .OrderBy(
                    traslado =>
                    traslado.FechaMovimiento
                )
                .ToList();

        if (
            trasladosNumericosSoloGeneral.Count == 0
        )
        {
            soloGeneralSinTrasladoNumerico++;

            if (
                soloGeneralEjemplosSinTraslado.Count < 30
            )
            {
                soloGeneralEjemplosSinTraslado.Add(
                    $"Placa: {item.Placa}\n" +
                    $"Área general: {areaGeneral}\n" +
                    $"Fecha levantamiento del área: {fechaLevantamiento:dd/MM/yyyy}\n" +
                    "No tiene traslado histórico numérico utilizable."
                );
            }

            continue;
        }

        var ultimoTrasladoSoloGeneral =
            trasladosNumericosSoloGeneral.Last();

        int destinoUltimoTraslado =
            ultimoTrasladoSoloGeneral
                .AreaNuevaNumero!
                .Value;

        DateTime fechaUltimoTraslado =
            ultimoTrasladoSoloGeneral
                .FechaMovimiento!
                .Value
                .Date;

        if (
            destinoUltimoTraslado ==
            areaGeneral
        )
        {
            if (
                fechaUltimoTraslado >
                fechaLevantamiento
            )
            {
                soloGeneralExplicadoPorTrasladoPosterior++;

                if (
                    soloGeneralEjemplosPosteriores.Count < 30
                )
                {
                    soloGeneralEjemplosPosteriores.Add(
                        $"Placa: {item.Placa}\n" +
                        $"Área general: {areaGeneral}\n" +
                        $"Fecha levantamiento: {fechaLevantamiento:dd/MM/yyyy}\n" +
                        $"Último traslado: " +
                        $"{ultimoTrasladoSoloGeneral.AreaAnteriorNumero} -> " +
                        $"{ultimoTrasladoSoloGeneral.AreaNuevaNumero}\n" +
                        $"Fecha traslado: {fechaUltimoTraslado:dd/MM/yyyy}\n" +
                        "Conclusión: llegó al área después del levantamiento; su ausencia en el archivo es coherente."
                    );
                }
            }
            else
            {
                soloGeneralTrasladoPrevioYNoAparece++;

                if (
                    soloGeneralEjemplosPrevios.Count < 30
                )
                {
                    soloGeneralEjemplosPrevios.Add(
                        $"Placa: {item.Placa}\n" +
                        $"Área general: {areaGeneral}\n" +
                        $"Fecha levantamiento: {fechaLevantamiento:dd/MM/yyyy}\n" +
                        $"Último traslado: " +
                        $"{ultimoTrasladoSoloGeneral.AreaAnteriorNumero} -> " +
                        $"{ultimoTrasladoSoloGeneral.AreaNuevaNumero}\n" +
                        $"Fecha traslado: {fechaUltimoTraslado:dd/MM/yyyy}\n" +
                        "Conclusión: según el historial ya estaba en esa área antes del levantamiento, pero no aparece en su archivo."
                    );
                }
            }

            continue;
        }

        soloGeneralUltimoTrasladoOtraArea++;

        if (
            soloGeneralEjemplosOtraArea.Count < 30
        )
        {
            soloGeneralEjemplosOtraArea.Add(
                $"Placa: {item.Placa}\n" +
                $"Área general: {areaGeneral}\n" +
                $"Último traslado: " +
                $"{ultimoTrasladoSoloGeneral.AreaAnteriorNumero} -> " +
                $"{ultimoTrasladoSoloGeneral.AreaNuevaNumero}\n" +
                $"Fecha traslado: {fechaUltimoTraslado:dd/MM/yyyy}\n" +
                $"Fecha levantamiento del área general: {fechaLevantamiento:dd/MM/yyyy}\n" +
                "Conclusión: el inventario general y el último traslado histórico no coinciden."
            );
        }
    }

    Console.WriteLine(
        $"SOLO_EN_GENERAL sin baja analizados: {soloGeneralSinBaja.Count}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Explicados por traslado posterior al levantamiento: {soloGeneralExplicadoPorTrasladoPosterior}"
    );

    Console.WriteLine(
        $"Traslado previo al área general pero NO aparece en levantamiento: {soloGeneralTrasladoPrevioYNoAparece}"
    );

    Console.WriteLine(
        $"Último traslado apunta a otra área: {soloGeneralUltimoTrasladoOtraArea}"
    );

    Console.WriteLine(
        $"Sin traslado numérico utilizable: {soloGeneralSinTrasladoNumerico}"
    );

    Console.WriteLine(
        $"Sin área general numérica: {soloGeneralSinAreaGeneralNumerica}"
    );

    Console.WriteLine(
        $"Sin fecha de levantamiento del área general: {soloGeneralSinFechaLevantamiento}"
    );

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: AUSENCIA EXPLICADA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in soloGeneralEjemplosPosteriores
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: DEBÍA APARECER EN EL ÁREA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in soloGeneralEjemplosPrevios
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: ÚLTIMO TRASLADO A OTRA ÁREA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in soloGeneralEjemplosOtraArea
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS: SIN TRASLADO NUMÉRICO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in soloGeneralEjemplosSinTraslado
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   UBICACIONES NO NUMÉRICAS EN SOLO_EN_GENERAL"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var soloGeneralUbicacionNoNumerica =
        soloGeneralSinBaja
            .Select(
                item => new
                {
                    Item = item,

                    Cruce =
                        cruceAreas
                            .Resultados
                            .FirstOrDefault(
                                x =>
                                x.Placa.Equals(
                                    item.Placa,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                }
            )
            .Where(
                x =>
                x.Cruce != null &&
                !x.Cruce.AreaGeneral.HasValue
            )
            .ToList();

    Console.WriteLine(
        $"Casos con ubicación general no numérica: {soloGeneralUbicacionNoNumerica.Count}"
    );

    Console.WriteLine();

    var gruposUbicacionesNoNumericas =
        soloGeneralUbicacionNoNumerica
            .GroupBy(
                x =>
                string.IsNullOrWhiteSpace(
                    x.Cruce!.UbicacionGeneralOriginal
                )
                    ? "[VACÍA]"
                    : x.Cruce
                        .UbicacionGeneralOriginal
                        .Trim(),
                StringComparer.OrdinalIgnoreCase
            )
            .Select(
                grupo => new
                {
                    Ubicacion =
                        grupo.Key,

                    Cantidad =
                        grupo.Count(),

                    Placas =
                        grupo
                            .Select(
                                x => x.Item.Placa
                            )
                            .OrderBy(
                                x => x
                            )
                            .ToList()
                }
            )
            .OrderByDescending(
                x => x.Cantidad
            )
            .ThenBy(
                x => x.Ubicacion
            )
            .ToList();

    Console.WriteLine(
        $"Ubicaciones descriptivas distintas: {gruposUbicacionesNoNumericas.Count}"
    );

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   RESUMEN POR UBICACIÓN"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var grupo
        in gruposUbicacionesNoNumericas
    )
    {
        Console.WriteLine(
            $"Ubicación: '{grupo.Ubicacion}'"
        );

        Console.WriteLine(
            $"Cantidad de activos: {grupo.Cantidad}"
        );

        Console.WriteLine(
            $"Primeras placas: {string.Join(", ", grupo.Placas.Take(15))}"
        );

        if (
            grupo.Placas.Count > 15
        )
        {
            Console.WriteLine(
                $"... y {grupo.Placas.Count - 15} más."
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   PRIMEROS CASOS INDIVIDUALES"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var caso
        in soloGeneralUbicacionNoNumerica
            .Take(50)
    )
    {
        Console.WriteLine(
            $"Placa: {caso.Item.Placa}"
        );

        Console.WriteLine(
            $"Ubicación general original: '{caso.Cruce!.UbicacionGeneralOriginal}'"
        );

        Console.WriteLine(
            $"Fila general: {caso.Cruce.FilaGeneral}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   UBICACIONES DESCRIPTIVAS VS ÚLTIMO TRASLADO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var casosUbicacionDescriptiva =
        soloGeneralSinBaja
            .Select(
                item => new
                {
                    Item = item,

                    Cruce =
                        cruceAreas
                            .Resultados
                            .FirstOrDefault(
                                x =>
                                x.Placa.Equals(
                                    item.Placa,
                                    StringComparison.OrdinalIgnoreCase
                                )
                            )
                }
            )
            .Where(
                x =>
                x.Cruce != null &&
                !x.Cruce.AreaGeneral.HasValue
            )
            .ToList();

    int descriptivosConUltimoTrasladoNumerico =
        0;

    int descriptivosSinTrasladoNumerico =
        0;

    var ejemplosDescriptivosConTraslado =
        new List<string>();

    var ejemplosDescriptivosSinTraslado =
        new List<string>();

    foreach (
        var caso
        in casosUbicacionDescriptiva
    )
    {
        var trasladosNumericosDescriptivos =
            trasladosHistoricos
                .Where(
                    traslado =>
                    traslado.Placas.Any(
                        placa =>
                        placa.Equals(
                            caso.Item.Placa,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .Where(
                    traslado =>
                    traslado.FechaMovimiento.HasValue &&
                    traslado.AreaNuevaNumero.HasValue
                )
                .OrderBy(
                    traslado =>
                    traslado.FechaMovimiento
                )
                .ToList();

        if (
            trasladosNumericosDescriptivos.Count == 0
        )
        {
            descriptivosSinTrasladoNumerico++;

            if (
                ejemplosDescriptivosSinTraslado.Count < 40
            )
            {
                ejemplosDescriptivosSinTraslado.Add(
                    $"Placa: {caso.Item.Placa}\n" +
                    $"Ubicación general: '{caso.Cruce!.UbicacionGeneralOriginal}'\n" +
                    $"Fila general: {caso.Cruce.FilaGeneral}\n" +
                    "No tiene traslado histórico con destino numérico utilizable."
                );
            }

            continue;
        }

        descriptivosConUltimoTrasladoNumerico++;

        var ultimoTrasladoNumerico =
            trasladosNumericosDescriptivos.Last();

        if (
            ejemplosDescriptivosConTraslado.Count < 60
        )
        {
            ejemplosDescriptivosConTraslado.Add(
                $"Placa: {caso.Item.Placa}\n" +
                $"Ubicación general: '{caso.Cruce!.UbicacionGeneralOriginal}'\n" +
                $"Fila general: {caso.Cruce.FilaGeneral}\n" +
                $"Último destino numérico histórico: Área {ultimoTrasladoNumerico.AreaNuevaNumero}\n" +
                $"Fecha traslado: {ultimoTrasladoNumerico.FechaMovimiento:dd/MM/yyyy}\n" +
                $"Origen original: '{ultimoTrasladoNumerico.UbicacionAnteriorOriginal}'\n" +
                $"Destino original: '{ultimoTrasladoNumerico.UbicacionNuevaOriginal}'"
                            );
        }
    }

    Console.WriteLine(
        $"Casos descriptivos analizados: {casosUbicacionDescriptiva.Count}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Con último traslado histórico a área numérica: {descriptivosConUltimoTrasladoNumerico}"
    );

    Console.WriteLine(
        $"Sin traslado histórico numérico utilizable: {descriptivosSinTrasladoNumerico}"
    );

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS CON ÚLTIMO TRASLADO NUMÉRICO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosDescriptivosConTraslado
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS SIN TRASLADO NUMÉRICO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosDescriptivosSinTraslado
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   UBICACIÓN DESCRIPTIVA VS ÚLTIMO TRASLADO REAL"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    int descriptivoCoincideConUltimoDestino =
        0;

    int descriptivoUltimoDestinoDiferente =
        0;

    int descriptivoUltimoDestinoNumerico =
        0;

    int descriptivoSinTrasladoHistorico =
        0;

    var ejemplosCoinciden =
        new List<string>();

    var ejemplosDiferentes =
        new List<string>();

    var ejemplosDestinoNumerico =
        new List<string>();

    var ejemplosSinHistorial =
        new List<string>();

    foreach (
        var caso
        in casosUbicacionDescriptiva
    )
    {
        string ubicacionGeneral =
            caso.Cruce!
                .UbicacionGeneralOriginal
                ?.Trim()
            ?? string.Empty;

        var trasladosDelActivo =
            trasladosHistoricos
                .Where(
                    traslado =>
                    traslado.Placas.Any(
                        placa =>
                        placa.Equals(
                            caso.Item.Placa,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .Where(
                    traslado =>
                    traslado.FechaMovimiento.HasValue
                )
                .OrderBy(
                    traslado =>
                    traslado.FechaMovimiento
                )
                .ToList();

        if (
            trasladosDelActivo.Count == 0
        )
        {
            descriptivoSinTrasladoHistorico++;

            if (
                ejemplosSinHistorial.Count < 40
            )
            {
                ejemplosSinHistorial.Add(
                    $"Placa: {caso.Item.Placa}\n" +
                    $"Ubicación general: '{ubicacionGeneral}'\n" +
                    $"Fila general: {caso.Cruce.FilaGeneral}\n" +
                    "No tiene traslado histórico con fecha utilizable."
                );
            }

            continue;
        }

        var ultimoTraslado =
            trasladosDelActivo.Last();

        string destinoOriginal =
            ultimoTraslado
                .UbicacionNuevaOriginal
                ?.Trim()
            ?? string.Empty;

        if (
            ultimoTraslado.AreaNuevaNumero.HasValue
        )
        {
            descriptivoUltimoDestinoNumerico++;

            if (
                ejemplosDestinoNumerico.Count < 40
            )
            {
                ejemplosDestinoNumerico.Add(
                    $"Placa: {caso.Item.Placa}\n" +
                    $"Ubicación general: '{ubicacionGeneral}'\n" +
                    $"Último destino histórico: Área {ultimoTraslado.AreaNuevaNumero}\n" +
                    $"Destino original: '{destinoOriginal}'\n" +
                    $"Fecha traslado: {ultimoTraslado.FechaMovimiento:dd/MM/yyyy}"
                );
            }

            continue;
        }

        string ubicacionGeneralNormalizada =
            NormalizarUbicacionParaComparar(
                ubicacionGeneral
            );

        string destinoOriginalNormalizado =
            NormalizarUbicacionParaComparar(
                destinoOriginal
            );

        if (
            ubicacionGeneralNormalizada ==
            destinoOriginalNormalizado &&
            !string.IsNullOrWhiteSpace(
                ubicacionGeneralNormalizada
            )
        )
        {
            descriptivoCoincideConUltimoDestino++;

            if (
                ejemplosCoinciden.Count < 40
            )
            {
                ejemplosCoinciden.Add(
                    $"Placa: {caso.Item.Placa}\n" +
                    $"Ubicación general: '{ubicacionGeneral}'\n" +
                    $"Último destino histórico: '{destinoOriginal}'\n" +
                    $"Fecha traslado: {ultimoTraslado.FechaMovimiento:dd/MM/yyyy}\n" +
                    "Conclusión: el historial respalda la ubicación descriptiva del inventario general."
                );
            }
        }
        else
        {
            descriptivoUltimoDestinoDiferente++;

            if (
                ejemplosDiferentes.Count < 40
            )
            {
                ejemplosDiferentes.Add(
                    $"Placa: {caso.Item.Placa}\n" +
                    $"Ubicación general: '{ubicacionGeneral}'\n" +
                    $"Último destino histórico: '{destinoOriginal}'\n" +
                    $"Fecha traslado: {ultimoTraslado.FechaMovimiento:dd/MM/yyyy}\n" +
                    "Conclusión: la ubicación general y el último destino histórico no coinciden."
                );
            }
        }
    }

    Console.WriteLine(
        $"Casos descriptivos analizados: {casosUbicacionDescriptiva.Count}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Último destino descriptivo coincide con general: {descriptivoCoincideConUltimoDestino}"
    );

    Console.WriteLine(
        $"Último destino descriptivo diferente: {descriptivoUltimoDestinoDiferente}"
    );

    Console.WriteLine(
        $"Último destino es área numérica: {descriptivoUltimoDestinoNumerico}"
    );

    Console.WriteLine(
        $"Sin traslado histórico utilizable: {descriptivoSinTrasladoHistorico}"
    );

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS QUE COINCIDEN"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosCoinciden
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS DIFERENTES"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosDiferentes
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS DESTINO NUMÉRICO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosDestinoNumerico
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS SIN HISTORIAL"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string ejemplo
        in ejemplosSinHistorial
    )
    {
        Console.WriteLine(ejemplo);
        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    static string NormalizarUbicacionParaComparar(
        string valor)
    {
        if (
            string.IsNullOrWhiteSpace(
                valor
            )
        )
        {
            return string.Empty;
        }

        string normalizado =
            valor
                .Trim()
                .ToUpperInvariant();

        normalizado =
            normalizado
                .Replace(
                    "Á",
                    "A"
                )
                .Replace(
                    "É",
                    "E"
                )
                .Replace(
                    "Í",
                    "I"
                )
                .Replace(
                    "Ó",
                    "O"
                )
                .Replace(
                    "Ú",
                    "U"
                )
                .Replace(
                    "Ñ",
                    "N"
                );

        normalizado =
            string.Join(
                " ",
                normalizado
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries
                    )
            );

        return normalizado;
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CLASIFICACIÓN FINAL DE AREA_DIFERENTE"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var clasificadorAreaDiferente =
        new ClasificadorAreaDiferenteService();

    var decisionesAreaDiferente =
        clasificadorAreaDiferente.Clasificar(
            cruceAreas,
            trasladosHistoricos,
            fechasLevantamientoAreas
        );

    Console.WriteLine(
        $"Casos AREA_DIFERENTE analizados: {decisionesAreaDiferente.Count}"
    );

    Console.WriteLine();

    var gruposDecisionArea =
        decisionesAreaDiferente
            .GroupBy(
                x => x.Clasificacion
            )
            .OrderByDescending(
                x => x.Count()
            )
            .ToList();

    Console.WriteLine(
        "Distribución por clasificación:"
    );

    Console.WriteLine();

    foreach (
        var grupo
        in gruposDecisionArea
    )
    {
        Console.WriteLine(
            $"{grupo.Key}: {grupo.Count()}"
        );
    }

    Console.WriteLine();

    int areaDiferenteRevision =
        decisionesAreaDiferente.Count(
            x => x.RequiereRevision
        );

    int areaDiferenteSinRevision =
        decisionesAreaDiferente.Count(
            x => !x.RequiereRevision
        );

    Console.WriteLine(
        $"Resueltos automáticamente: {areaDiferenteSinRevision}"
    );

    Console.WriteLine(
        $"Requieren revisión: {areaDiferenteRevision}"
    );

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS RESUELTOS AUTOMÁTICAMENTE"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var item
        in decisionesAreaDiferente
            .Where(
                x => !x.RequiereRevision
            )
            .Take(30)
    )
    {
        Console.WriteLine(
            $"Placa: {item.Placa}"
        );

        Console.WriteLine(
            $"Fila general: {item.FilaGeneral}"
        );

        Console.WriteLine(
            $"Área general: {item.AreaGeneral}"
        );

        Console.WriteLine(
            $"Área archivo: {item.AreaArchivo}"
        );

        Console.WriteLine(
            $"Fecha levantamiento: " +
            $"{(
                item.FechaLevantamiento.HasValue
                    ? item.FechaLevantamiento.Value.ToString("dd/MM/yyyy")
                    : "[SIN FECHA]"
            )}"
        );

        Console.WriteLine(
            $"Última área histórica previa: " +
            $"{(
                item.UltimaAreaHistoricaPrevia.HasValue
                    ? item.UltimaAreaHistoricaPrevia.Value.ToString()
                    : "[SIN DATO]"
            )}"
        );

        Console.WriteLine(
            $"Clasificación: {item.Clasificacion}"
        );

        Console.WriteLine(
            $"Observación: {item.Observacion}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS QUE REQUIEREN REVISIÓN"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var item
        in decisionesAreaDiferente
            .Where(
                x => x.RequiereRevision
            )
            .Take(50)
    )
    {
        Console.WriteLine(
            $"Placa: {item.Placa}"
        );

        Console.WriteLine(
            $"Fila general: {item.FilaGeneral}"
        );

        Console.WriteLine(
            $"Área general: {item.AreaGeneral}"
        );

        Console.WriteLine(
            $"Área archivo: {item.AreaArchivo}"
        );

        Console.WriteLine(
            $"Fecha levantamiento: " +
            $"{(
                item.FechaLevantamiento.HasValue
                    ? item.FechaLevantamiento.Value.ToString("dd/MM/yyyy")
                    : "[SIN FECHA]"
            )}"
        );

        Console.WriteLine(
            $"Última área histórica previa: " +
            $"{(
                item.UltimaAreaHistoricaPrevia.HasValue
                    ? item.UltimaAreaHistoricaPrevia.Value.ToString()
                    : "[SIN DATO]"
            )}"
        );

        Console.WriteLine(
            $"Clasificación: {item.Clasificacion}"
        );

        Console.WriteLine(
            $"Observación: {item.Observacion}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }




    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   ACTIVOS PREPARADOS PARA MIGRACIÓN"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    // ============================================
    // PREPARAR SITUACIÓN ADMINISTRATIVA
    // ============================================

    var placasConBajaHistorica =
        bajasHistoricas
            .SelectMany(
                baja =>
                baja.Placas
            )
            .Where(
                placa =>
                !string.IsNullOrWhiteSpace(
                    placa
                )
            )
            .Select(
                placa =>
                placa.Trim()
            )
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase
            );

    var placasConEvidenciaPosterior =
        new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        );

    var bajasAgrupadasSituacion =
        bajasHistoricas
            .SelectMany(
                baja =>
                baja.Placas.Select(
                    placa => new
                    {
                        Placa = placa,
                        Baja = baja
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
                x => x.Placa.Trim(),
                StringComparer.OrdinalIgnoreCase
            )
            .ToList();

    foreach (
        var grupo
        in bajasAgrupadasSituacion
    )
    {
        var bajasConFecha =
            grupo
                .Where(
                    x =>
                    x.Baja.FechaMovimiento.HasValue
                )
                .OrderBy(
                    x =>
                    x.Baja.FechaMovimiento
                )
                .ToList();

        if (
            bajasConFecha.Count == 0
        )
        {
            continue;
        }

        DateTime fechaUltimaBaja =
            bajasConFecha
                .Last()
                .Baja
                .FechaMovimiento!
                .Value
                .Date;

        bool existeTrasladoPosterior =
            trasladosHistoricos.Any(
                traslado =>
                traslado.FechaMovimiento.HasValue &&
                traslado.FechaMovimiento.Value.Date >
                    fechaUltimaBaja &&
                traslado.Placas.Any(
                    placa =>
                    placa.Equals(
                        grupo.Key,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );

        bool existeCambioEstadoPosterior =
            cambiosEstadoHistoricos.Any(
                cambio =>
                cambio.FechaMovimiento.HasValue &&
                cambio.FechaMovimiento.Value.Date >
                    fechaUltimaBaja &&
                cambio.Placas.Any(
                    placa =>
                    placa.Equals(
                        grupo.Key,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );

        bool existeModificacionPosterior =
            modificacionesHistoricas.Any(
                modificacion =>
                modificacion.FechaMovimiento.HasValue &&
                modificacion.FechaMovimiento.Value.Date >
                    fechaUltimaBaja &&
                modificacion.Placas.Any(
                    placa =>
                    placa.Equals(
                        grupo.Key,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );

        bool existeLevantamientoPosterior =
            resultadoAreas
                .Registros
                .Where(
                    registro =>
                    registro.PlacaNormalizada.Equals(
                        grupo.Key,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Any(
                    registro =>
                    fechasLevantamientoAreas.Any(
                        fecha =>
                        fecha.NumeroArea ==
                            registro.NumeroAreaArchivo &&
                        fecha.FechaLevantamiento.HasValue &&
                        fecha.FechaLevantamiento.Value.Date >
                            fechaUltimaBaja
                    )
                );

        if (
            existeTrasladoPosterior ||
            existeCambioEstadoPosterior ||
            existeModificacionPosterior ||
            existeLevantamientoPosterior
        )
        {
            placasConEvidenciaPosterior.Add(
                grupo.Key
            );
        }
    }

    // ============================================
    // PREPARAR ACTIVOS
    // ============================================

    var preparadorActivosMigracion =
        new PreparadorActivosMigracionService();

    var activosPreparados =
        preparadorActivosMigracion.Preparar(
            registros,
            cruceAreas,
            decisionesAreaDiferente,
            placasConBajaHistorica,
            placasConEvidenciaPosterior
        );

    int preparadosConAreaOficial =
        activosPreparados.Count(
            x => x.AreaActualNumero.HasValue
        );

    int preparadosConUbicacionHistorica =
        activosPreparados.Count(
            x =>
            !string.IsNullOrWhiteSpace(
                x.UbicacionHistorica
            )
        );

    int preparadosSinAreaNiUbicacion =
        activosPreparados.Count(
            x =>
            !x.AreaActualNumero.HasValue &&
            string.IsNullOrWhiteSpace(
                x.UbicacionHistorica
            )
        );

    var preparadosRevision =
        activosPreparados
            .Where(
                x => x.RequiereRevision
            )
            .ToList();

    Console.WriteLine(
        $"Total preparados: {activosPreparados.Count}"
    );

    Console.WriteLine(
        $"Con área oficial 1-70: {preparadosConAreaOficial}"
    );

    Console.WriteLine(
        $"Con ubicación histórica: {preparadosConUbicacionHistorica}"
    );

    Console.WriteLine(
        $"Sin área ni ubicación histórica: {preparadosSinAreaNiUbicacion}"
    );

    Console.WriteLine(
        $"Requieren revisión: {preparadosRevision.Count}"
    );


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   COMPROBACIÓN DE UBICACIONES ESPECIALES"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var sinAreaConRevision =
        activosPreparados
            .Where(
                x =>
                !x.AreaActualNumero.HasValue &&
                x.RequiereRevision
            )
            .ToList();

    Console.WriteLine(
        $"Sin área oficial y con revisión: {sinAreaConRevision.Count}"
    );

    Console.WriteLine();

    foreach (
        var activo
        in sinAreaConRevision
    )
    {
        Console.WriteLine(
            $"Placa: {activo.Placa}"
        );

        Console.WriteLine(
            $"Fila: {activo.NumeroFilaExcel}"
        );

        Console.WriteLine(
            $"Ubicación original: '{activo.UbicacionOriginal}'"
        );

        Console.WriteLine(
            $"Ubicación histórica preparada: '{activo.UbicacionHistorica ?? ""}'"
        );

        Console.WriteLine(
            $"Clasificación: {activo.ClasificacionOrigen}"
        );

        Console.WriteLine(
            $"Motivos: {activo.MotivoRevision}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   DISTRIBUCIÓN FINAL DE SITUACIÓN"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var distribucionSituacionPreparada =
        activosPreparados
            .GroupBy(
                x => new
                {
                    x.SituacionPreparada,
                    x.ClasificacionSituacion
                }
            )
            .OrderByDescending(
                x => x.Count()
            )
            .ToList();

    foreach (
        var grupo
        in distribucionSituacionPreparada
    )
    {
        Console.WriteLine(
            $"Situación: {grupo.Key.SituacionPreparada}"
        );

        Console.WriteLine(
            $"Clasificación: {grupo.Key.ClasificacionSituacion}"
        );

        Console.WriteLine(
            $"Cantidad: {grupo.Count()}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    var preparadosActivos =
        activosPreparados.Count(
            x =>
            x.SituacionPreparada ==
            "ACTIVO"
        );

    var preparadosBaja =
        activosPreparados.Count(
            x =>
            x.SituacionPreparada ==
            "DADO_DE_BAJA"
        );

    Console.WriteLine(
        $"Total preparados como ACTIVO: {preparadosActivos}"
    );

    Console.WriteLine(
        $"Total preparados como DADO_DE_BAJA: {preparadosBaja}"
    );

    Console.WriteLine();

    var contradiccionesSituacion =
        activosPreparados
            .Where(
                x =>
                x.ClasificacionSituacion ==
                "ACTIVO_CON_BAJA_HISTORICA_Y_EVIDENCIA_POSTERIOR"
            )
            .ToList();

    Console.WriteLine(
        $"Activos con baja histórica y evidencia posterior: {contradiccionesSituacion.Count}"
    );

    Console.WriteLine();

    foreach (
        var activo
        in contradiccionesSituacion.Take(40)
    )
    {
        Console.WriteLine(
            $"Placa: {activo.Placa}"
        );

        Console.WriteLine(
            $"Situación preparada: {activo.SituacionPreparada}"
        );

        Console.WriteLine(
            $"Clasificación: {activo.ClasificacionSituacion}"
        );

        Console.WriteLine(
            $"Requiere revisión: {(activo.RequiereRevision ? "SÍ" : "NO")}"
        );

        Console.WriteLine(
            $"Motivos: {activo.MotivoRevision}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   BAJAS HISTÓRICAS SIN ACTIVO PREPARADO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var placasActivosPreparados =
        activosPreparados
            .Select(
                x => x.Placa
            )
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase
            );

    var bajasSinActivoPreparado =
        placasConBajaHistorica
            .Where(
                placa =>
                !placasActivosPreparados.Contains(
                    placa
                )
            )
            .OrderBy(
                placa => placa
            )
            .ToList();

    Console.WriteLine(
        $"Placas con baja histórica sin activo preparado: {bajasSinActivoPreparado.Count}"
    );

    Console.WriteLine();

    foreach (
        string placa
        in bajasSinActivoPreparado
    )
    {
        Console.WriteLine(
            $"Placa: {placa}"
        );

        var bajasDeLaPlaca =
            bajasHistoricas
                .Where(
                    baja =>
                    baja.Placas.Any(
                        p =>
                        p.Equals(
                            placa,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .ToList();

        foreach (
            var baja
            in bajasDeLaPlaca
        )
        {
            Console.WriteLine(
                $"Fecha: " +
                $"{(baja.FechaMovimiento.HasValue ? baja.FechaMovimiento.Value.ToString("dd/MM/yyyy") : "[SIN FECHA]")}"
            );

            Console.WriteLine(
                $"Motivo: {baja.MotivoNormalizado}"
            );

            Console.WriteLine(
                $"Texto: {baja.TextoOriginal}"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CONTEXTO DE ACTIVOS SOLO HISTÓRICOS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string placaHistorica
        in bajasSinActivoPreparado
    )
    {
        Console.WriteLine(
            $"Placa: {placaHistorica}"
        );

        var movimientosDeLaPlaca =
            movimientosHistoricos
                .Where(
                    movimiento =>
                    movimiento.Placas.Any(
                        placa =>
                        placa.Equals(
                            placaHistorica,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .OrderBy(
                    movimiento =>
                    movimiento.RegistrosLibro
                        .Min(
                            registro =>
                            registro.NumeroFilaExcel
                        )
                )
                .ToList();

        Console.WriteLine(
            $"Movimientos históricos encontrados: {movimientosDeLaPlaca.Count}"
        );

        Console.WriteLine();

        foreach (
            var movimiento
            in movimientosDeLaPlaca
        )
        {
            Console.WriteLine(
                $"Tipo: {movimiento.TipoMovimiento}"
            );

            Console.WriteLine(
                $"Posiciones de libro: " +
                $"{string.Join(", ", movimiento.RegistrosLibro.Select(r => $"T{r.Tomo}/F{r.Folio}/A{r.Asiento}"))}"
            );

            Console.WriteLine(
                $"Texto: {movimiento.TextoCompleto}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }

        Console.WriteLine(
            "============================================"
        );
    }


    Console.WriteLine(
    "============================================"
);
    Console.WriteLine(
        "   FUENTE DEL ÁREA SELECCIONADA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var fuentesArea =
        activosPreparados
            .GroupBy(
                x => x.FuenteAreaSeleccionada
            )
            .OrderByDescending(
                x => x.Count()
            )
            .ToList();

    foreach (
        var grupo
        in fuentesArea
    )
    {
        Console.WriteLine(
            $"{grupo.Key}: {grupo.Count()}"
        );
    }

    Console.WriteLine();

    var ejemplosAreaRespaldada =
        activosPreparados
            .Where(
                x =>
                x.FuenteAreaSeleccionada ==
                "ARCHIVO_AREA_RESPALDADO_POR_HISTORIAL"
            )
            .Take(20)
            .ToList();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS DE ÁREA TOMADA DEL ARCHIVO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var activo
        in ejemplosAreaRespaldada
    )
    {
        Console.WriteLine(
            $"Placa: {activo.Placa}"
        );

        Console.WriteLine(
            $"Área preparada: Área {activo.AreaActualNumero}"
        );

        Console.WriteLine(
            $"Fuente: {activo.FuenteAreaSeleccionada}"
        );

        Console.WriteLine(
            $"Requiere revisión: {(activo.RequiereRevision ? "SÍ" : "NO")}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   VALIDACIÓN FINAL DE ESTADO Y MODO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    // ============================================
    // ESTADOS
    // ============================================

    var preparadosEstadoResuelto =
        activosPreparados
            .Where(
                x =>
                !string.IsNullOrWhiteSpace(
                    x.EstadoNormalizado
                )
            )
            .ToList();

    var preparadosEstadoSinResolver =
        activosPreparados
            .Where(
                x =>
                string.IsNullOrWhiteSpace(
                    x.EstadoNormalizado
                )
            )
            .ToList();

    Console.WriteLine(
        $"Estados resueltos: {preparadosEstadoResuelto.Count}"
    );

    Console.WriteLine(
        $"Estados sin resolver: {preparadosEstadoSinResolver.Count}"
    );

    Console.WriteLine();

    if (
        preparadosEstadoSinResolver.Count > 0
    )
    {
        Console.WriteLine(
            "Valores originales de estado sin resolver:"
        );

        Console.WriteLine();

        var gruposEstadoSinResolver =
            preparadosEstadoSinResolver
                .GroupBy(
                    x =>
                    string.IsNullOrWhiteSpace(
                        x.EstadoOriginal
                    )
                        ? "[VACÍO]"
                        : x.EstadoOriginal.Trim(),
                    StringComparer.OrdinalIgnoreCase
                )
                .OrderByDescending(
                    x => x.Count()
                )
                .ThenBy(
                    x => x.Key
                )
                .ToList();

        foreach (
            var grupo
            in gruposEstadoSinResolver
        )
        {
            Console.WriteLine(
                $"Estado original: '{grupo.Key}'"
            );

            Console.WriteLine(
                $"Cantidad: {grupo.Count()}"
            );

            Console.WriteLine(
                $"Ejemplos: {string.Join(", ", grupo.Take(10).Select(x => x.Placa))}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();

    // ============================================
    // MODOS DE ADQUISICIÓN
    // ============================================

    var preparadosModoResuelto =
        activosPreparados
            .Where(
                x =>
                !string.IsNullOrWhiteSpace(
                    x.ModoAdquisicionNormalizado
                )
            )
            .ToList();

    var preparadosModoSinResolver =
        activosPreparados
            .Where(
                x =>
                string.IsNullOrWhiteSpace(
                    x.ModoAdquisicionNormalizado
                )
            )
            .ToList();

    Console.WriteLine(
        $"Modos de adquisición resueltos: {preparadosModoResuelto.Count}"
    );

    Console.WriteLine(
        $"Modos de adquisición sin resolver: {preparadosModoSinResolver.Count}"
    );

    Console.WriteLine();

    if (
        preparadosModoSinResolver.Count > 0
    )
    {
        Console.WriteLine(
            "Valores originales de modo de adquisición sin resolver:"
        );

        Console.WriteLine();

        var gruposModoSinResolver =
            preparadosModoSinResolver
                .GroupBy(
                    x =>
                    string.IsNullOrWhiteSpace(
                        x.ModoAdquisicionOriginal
                    )
                        ? "[VACÍO]"
                        : x.ModoAdquisicionOriginal.Trim(),
                    StringComparer.OrdinalIgnoreCase
                )
                .OrderByDescending(
                    x => x.Count()
                )
                .ThenBy(
                    x => x.Key
                )
                .ToList();

        foreach (
            var grupo
            in gruposModoSinResolver
        )
        {
            Console.WriteLine(
                $"Modo original: '{grupo.Key}'"
            );

            Console.WriteLine(
                $"Cantidad: {grupo.Count()}"
            );

            Console.WriteLine(
                $"Ejemplos: {string.Join(", ", grupo.Take(10).Select(x => x.Placa))}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }


    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS CON UBICACIÓN HISTÓRICA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var activo
        in activosPreparados
            .Where(
                x =>
                !string.IsNullOrWhiteSpace(
                    x.UbicacionHistorica
                )
            )
            .Take(40)
    )
    {
        Console.WriteLine(
            $"Placa: {activo.Placa}"
        );

        Console.WriteLine(
            $"Fila Excel: {activo.NumeroFilaExcel}"
        );

        Console.WriteLine(
            $"Ubicación histórica: '{activo.UbicacionHistorica}'"
        );

        Console.WriteLine(
            $"Clasificación origen: {activo.ClasificacionOrigen}"
        );

        Console.WriteLine(
            $"Requiere revisión: {(activo.RequiereRevision ? "SÍ" : "NO")}"
        );

        if (
            activo.RequiereRevision &&
            !string.IsNullOrWhiteSpace(
                activo.MotivoRevision
            )
        )
        {
            Console.WriteLine(
                $"Motivo: {activo.MotivoRevision}"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   ACTIVOS PREPARADOS QUE REQUIEREN REVISIÓN"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        var activo
        in preparadosRevision.Take(50)
    )
    {
        Console.WriteLine(
            $"Placa: {activo.Placa}"
        );

        Console.WriteLine(
            $"Fila Excel: {activo.NumeroFilaExcel}"
        );

        Console.WriteLine(
            $"Área preparada: {(activo.AreaActualNumero.HasValue ? $"Área {activo.AreaActualNumero}" : "[SIN ÁREA]")}"
        );

        Console.WriteLine(
            $"Ubicación histórica: '{activo.UbicacionHistorica ?? ""}'"
        );

        Console.WriteLine(
            $"Clasificación origen: {activo.ClasificacionOrigen}"
        );

        Console.WriteLine(
            $"Motivo: {activo.MotivoRevision}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   VALIDACIÓN FINAL DE PRECIO Y FECHA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    // ============================================
    // PRECIO
    // ============================================

    var precioConValor =
        activosPreparados
            .Where(
                x =>
                x.PrecioNormalizado.HasValue
            )
            .ToList();

    var precioSinValor =
        activosPreparados
            .Where(
                x =>
                !x.PrecioNormalizado.HasValue
            )
            .ToList();

    Console.WriteLine(
        $"Precios normalizados: {precioConValor.Count}"
    );

    Console.WriteLine(
        $"Precios sin normalizar: {precioSinValor.Count}"
    );

    Console.WriteLine();

    var gruposPrecioSinNormalizar =
        precioSinValor
            .GroupBy(
                x =>
                string.IsNullOrWhiteSpace(
                    x.PrecioOriginal
                )
                    ? "[VACÍO]"
                    : x.PrecioOriginal.Trim(),
                StringComparer.OrdinalIgnoreCase
            )
            .OrderByDescending(
                x => x.Count()
            )
            .ThenBy(
                x => x.Key
            )
            .ToList();

    Console.WriteLine(
        "Valores originales de precio sin normalizar:"
    );

    Console.WriteLine();

    foreach (
        var grupo
        in gruposPrecioSinNormalizar
    )
    {
        Console.WriteLine(
            $"Precio original: '{grupo.Key}'"
        );

        Console.WriteLine(
            $"Cantidad: {grupo.Count()}"
        );

        Console.WriteLine(
            $"Ejemplos: {string.Join(", ", grupo.Take(10).Select(x => x.Placa))}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    Console.WriteLine();

    // ============================================
    // FECHA DE ADQUISICIÓN
    // ============================================

    var fechaNormalizada =
        activosPreparados
            .Where(
                x =>
                x.FechaAdquisicionNormalizada.HasValue
            )
            .ToList();

    var fechaSinNormalizar =
        activosPreparados
            .Where(
                x =>
                !x.FechaAdquisicionNormalizada.HasValue
            )
            .ToList();

    Console.WriteLine(
        $"Fechas normalizadas: {fechaNormalizada.Count}"
    );

    Console.WriteLine(
        $"Fechas sin normalizar: {fechaSinNormalizar.Count}"
    );

    Console.WriteLine();

    var gruposFechaSinNormalizar =
        fechaSinNormalizar
            .GroupBy(
                x =>
                string.IsNullOrWhiteSpace(
                    x.FechaAdquisicionOriginal
                )
                    ? "[VACÍO]"
                    : x.FechaAdquisicionOriginal.Trim(),
                StringComparer.OrdinalIgnoreCase
            )
            .OrderByDescending(
                x => x.Count()
            )
            .ThenBy(
                x => x.Key
            )
            .ToList();

    Console.WriteLine(
        "Valores originales de fecha sin normalizar:"
    );

    Console.WriteLine();

    foreach (
        var grupo
        in gruposFechaSinNormalizar
    )
    {
        Console.WriteLine(
            $"Fecha original: '{grupo.Key}'"
        );

        Console.WriteLine(
            $"Cantidad: {grupo.Count()}"
        );

        Console.WriteLine(
            $"Ejemplos: {string.Join(", ", grupo.Take(10).Select(x => x.Placa))}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CLASIFICACIÓN FINAL DE PRECIO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var clasificacionesPrecio =
        activosPreparados
            .GroupBy(
                x => x.ClasificacionPrecio
            )
            .OrderByDescending(
                x => x.Count()
            )
            .ToList();

    foreach (
        var grupo
        in clasificacionesPrecio
    )
    {
        Console.WriteLine(
            $"{grupo.Key}: {grupo.Count()}"
        );
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CLASIFICACIÓN FINAL DE FECHA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var clasificacionesFecha =
        activosPreparados
            .GroupBy(
                x => x.ClasificacionFechaAdquisicion
            )
            .OrderByDescending(
                x => x.Count()
            )
            .ToList();

    foreach (
        var grupo
        in clasificacionesFecha
    )
    {
        Console.WriteLine(
            $"{grupo.Key}: {grupo.Count()}"
        );
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CASOS AMBIGUOS DE PRECIO Y FECHA"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var casosAmbiguos =
        activosPreparados
            .Where(
                x =>
                x.ClasificacionPrecio == "AMBIGUO" ||
                x.ClasificacionFechaAdquisicion ==
                "INCOMPLETA_AMBIGUA"
            )
            .ToList();

    foreach (
        var activo
        in casosAmbiguos
    )
    {
        Console.WriteLine(
            $"Placa: {activo.Placa}"
        );

        Console.WriteLine(
            $"Precio original: '{activo.PrecioOriginal}'"
        );

        Console.WriteLine(
            $"Clasificación precio: {activo.ClasificacionPrecio}"
        );

        Console.WriteLine(
            $"Fecha original: '{activo.FechaAdquisicionOriginal}'"
        );

        Console.WriteLine(
            $"Clasificación fecha: {activo.ClasificacionFechaAdquisicion}"
        );

        Console.WriteLine(
            $"Motivos: {activo.MotivoRevision}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }



    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   VALIDACIÓN FINAL DE REGISTRO DE LIBRO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    // ============================================
    // REGISTROS COMPLETOS
    // ============================================

    var registrosLibroCompletosPreparados =
        activosPreparados
            .Where(
                x =>
                x.Tomo.HasValue &&
                x.Folio.HasValue &&
                x.Asiento.HasValue
            )
            .ToList();

    var registrosLibroIncompletosPreparados =
        activosPreparados
            .Where(
                x =>
                !x.Tomo.HasValue ||
                !x.Folio.HasValue ||
                !x.Asiento.HasValue
            )
            .ToList();

    Console.WriteLine(
        $"Registros completos: {registrosLibroCompletosPreparados.Count}"
    );

    Console.WriteLine(
        $"Registros incompletos: {registrosLibroIncompletosPreparados.Count}"
    );

    Console.WriteLine();

    // ============================================
    // POSICIONES DUPLICADAS
    // ============================================

    var posicionesDuplicadasPreparadas =
        registrosLibroCompletosPreparados
            .GroupBy(
                x => new
                {
                    Tomo = x.Tomo!.Value,
                    Folio = x.Folio!.Value,
                    Asiento = x.Asiento!.Value
                }
            )
            .Where(
                g => g.Count() > 1
            )
            .OrderBy(
                g => g.Key.Tomo
            )
            .ThenBy(
                g => g.Key.Folio
            )
            .ThenBy(
                g => g.Key.Asiento
            )
            .ToList();

    Console.WriteLine(
        $"Posiciones duplicadas: {posicionesDuplicadasPreparadas.Count}"
    );

    Console.WriteLine();

    foreach (
        var grupo
        in posicionesDuplicadasPreparadas
    )
    {
        Console.WriteLine(
            $"Posición: " +
            $"T{grupo.Key.Tomo} / " +
            $"F{grupo.Key.Folio} / " +
            $"A{grupo.Key.Asiento}"
        );

        Console.WriteLine(
            $"Cantidad de activos: {grupo.Count()}"
        );

        Console.WriteLine(
            $"Placas: {string.Join(", ", grupo.Select(x => x.Placa))}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }

    // ============================================
    // REGISTROS INCOMPLETOS
    // ============================================

    if (
        registrosLibroIncompletosPreparados.Count > 0
    )
    {
        Console.WriteLine();
        Console.WriteLine(
            "REGISTROS DE LIBRO INCOMPLETOS:"
        );

        Console.WriteLine();

        foreach (
            var activo
            in registrosLibroIncompletosPreparados.Take(50)
        )
        {
            Console.WriteLine(
                $"Placa: {activo.Placa}"
            );

            Console.WriteLine(
                $"Tomo: {(activo.Tomo.HasValue ? activo.Tomo.Value : "[VACÍO]")}"
            );

            Console.WriteLine(
                $"Folio: {(activo.Folio.HasValue ? activo.Folio.Value : "[VACÍO]")}"
            );

            Console.WriteLine(
                $"Asiento: {(activo.Asiento.HasValue ? activo.Asiento.Value : "[VACÍO]")}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   ACTIVOS HISTÓRICOS RECONSTRUIDOS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var reconstruidorActivosHistoricos =
        new ReconstruidorActivosHistoricosService();

    var activosHistoricosReconstruidos =
        reconstruidorActivosHistoricos.Reconstruir(
            rutaArchivo,
            registrosHistoricos
        );

    Console.WriteLine(
        $"Total reconstruidos: {activosHistoricosReconstruidos.Count}"
    );

    Console.WriteLine();

    foreach (
        var activoHistorico
        in activosHistoricosReconstruidos
    )
    {
        Console.WriteLine(
            $"Placa migrada: {activoHistorico.Placa}"
        );

        Console.WriteLine(
            $"Identificación original: {activoHistorico.IdentificacionOriginal}"
        );

        Console.WriteLine(
            $"Fila Excel: {activoHistorico.NumeroFilaExcel}"
        );

        Console.WriteLine(
            $"Descripción: {activoHistorico.Descripcion}"
        );

        Console.WriteLine(
            $"Marca: {activoHistorico.Marca}"
        );

        Console.WriteLine(
            $"Modelo: {activoHistorico.Modelo}"
        );

        Console.WriteLine(
            $"Serie: {activoHistorico.Serie}"
        );

        Console.WriteLine(
            $"Estado original: {activoHistorico.EstadoOriginal}"
        );

        Console.WriteLine(
            $"Estado normalizado: {activoHistorico.EstadoNormalizado ?? "[NO RESUELTO]"}"
        );

        Console.WriteLine(
            $"Ubicación original: {activoHistorico.UbicacionOriginal}"
        );

        Console.WriteLine(
            $"Área original: " +
            $"{(activoHistorico.AreaOriginalNumero.HasValue ? activoHistorico.AreaOriginalNumero.Value.ToString() : "[NO NUMÉRICA]")}"
        );

        Console.WriteLine(
            $"Modo original: {activoHistorico.ModoAdquisicionOriginal}"
        );

        Console.WriteLine(
            $"Modo normalizado: {activoHistorico.ModoAdquisicionNormalizado ?? "[NO RESUELTO]"}"
        );

        Console.WriteLine(
            $"Precio original: {activoHistorico.PrecioOriginal}"
        );

        Console.WriteLine(
            $"Precio normalizado: " +
            $"{(activoHistorico.PrecioNormalizado.HasValue ? activoHistorico.PrecioNormalizado.Value.ToString() : "[SIN DATO]")}"
        );

        Console.WriteLine(
            $"Fecha adquisición original: " +
            $"{(string.IsNullOrWhiteSpace(activoHistorico.FechaAdquisicionOriginal) ? "[SIN DATO]" : activoHistorico.FechaAdquisicionOriginal)}"
        );

        Console.WriteLine(
            $"Registro original: " +
            $"T{activoHistorico.TomoOriginal}/" +
            $"F{activoHistorico.FolioOriginal}/" +
            $"A{activoHistorico.AsientoOriginal}"
        );

        Console.WriteLine(
            $"Situación preparada: {activoHistorico.SituacionPreparada}"
        );

        Console.WriteLine(
            $"Requiere revisión: " +
            $"{(activoHistorico.RequiereRevision ? "SÍ" : "NO")}"
        );

        if (
            activoHistorico.RequiereRevision
        )
        {
            Console.WriteLine(
                $"Motivo revisión: {activoHistorico.MotivoRevision}"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   PLAN FINAL DE MIGRACIÓN"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var constructorPlanMigracion =
        new ConstructorPlanMigracionService();

    var planMigracion =
        constructorPlanMigracion.Construir(
            activosPreparados,
            activosHistoricosReconstruidos,
            registrosHistoricos,
            movimientosHistoricos
        );

    // ============================================
    // ACTIVOS
    // ============================================

    Console.WriteLine(
        $"Activos actuales: {planMigracion.ActivosActuales}"
    );

    Console.WriteLine(
        $"Activos históricos reconstruidos: " +
        $"{planMigracion.ActivosHistoricosReconstruidos}"
    );

    Console.WriteLine(
        $"Total de activos finales: {planMigracion.TotalActivos}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Situación ACTIVO: {planMigracion.ActivosSituacionActivo}"
    );

    Console.WriteLine(
        $"Situación DADO_DE_BAJA: {planMigracion.ActivosSituacionBaja}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Activos actuales que requieren revisión: " +
        $"{planMigracion.ActivosQueRequierenRevision}"
    );

    Console.WriteLine(
        $"Históricos reconstruidos que requieren revisión: " +
        $"{planMigracion.HistoricosReconstruidosQueRequierenRevision}"
    );

    Console.WriteLine();

    // ============================================
    // PLACAS
    // ============================================

    Console.WriteLine(
        $"Placas duplicadas finales: " +
        $"{planMigracion.PlacasDuplicadasFinales}"
    );

    foreach (
        string detalle
        in planMigracion.DetallePlacasDuplicadas
    )
    {
        Console.WriteLine(
            $"  {detalle}"
        );
    }

    Console.WriteLine();

    // ============================================
    // POSICIONES DEL LIBRO
    // ============================================

    Console.WriteLine(
        $"Posiciones actuales duplicadas: " +
        $"{planMigracion.PosicionesActualesDuplicadas}"
    );

    foreach (
        string detalle
        in planMigracion.DetallePosicionesActualesDuplicadas
    )
    {
        Console.WriteLine(
            $"  {detalle}"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        $"Posiciones históricas duplicadas: " +
        $"{planMigracion.PosicionesHistoricasDuplicadas}"
    );

    foreach (
        string detalle
        in planMigracion.DetallePosicionesHistoricasDuplicadas
    )
    {
        Console.WriteLine(
            $"  {detalle}"
        );
    }

    Console.WriteLine();

    Console.WriteLine(
        $"Conflictos actual ↔ histórico: " +
        $"{planMigracion.ConflictosActualHistorico}"
    );

    foreach (
        string detalle
        in planMigracion.DetalleConflictosActualHistorico
    )
    {
        Console.WriteLine(
            $"  {detalle}"
        );
    }

    Console.WriteLine();

    // ============================================
    // MOVIMIENTOS HISTÓRICOS
    // ============================================

    Console.WriteLine(
        $"Movimientos históricos detectados: " +
        $"{planMigracion.MovimientosHistoricosDetectados}"
    );

    Console.WriteLine(
        $"Movimientos sin placa: " +
        $"{planMigracion.MovimientosSinPlaca}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Placas históricas referenciadas sin activo final: " +
        $"{planMigracion.ReferenciasPlacaSinActivoFinal}"
    );

    foreach (
        string placa
        in planMigracion.PlacasReferenciadasSinActivo
    )
    {
        Console.WriteLine(
            $"  {placa}"
        );
    }

    Console.WriteLine();

    // ============================================
    // RESULTADO GENERAL
    // ============================================

    Console.WriteLine(
        "--------------------------------------------"
    );

    Console.WriteLine(
        $"¿Hay incidencias bloqueantes?: " +
        $"{(planMigracion.TieneIncidenciasBloqueantes ? "SÍ" : "NO")}"
    );

    Console.WriteLine(
        "--------------------------------------------"
    );


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   DIAGNÓSTICO DE PLACAS HISTÓRICAS SIN ACTIVO"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string diagPlaca
        in planMigracion.PlacasReferenciadasSinActivo
    )
    {
        Console.WriteLine(
            "============================================"
        );

        Console.WriteLine(
            $"PLACA: {diagPlaca}"
        );

        Console.WriteLine(
            "============================================"
        );

        string diagConsecutivo =
            diagPlaca
                .Split('-')
                .Last()
                .TrimStart('0');

        if (
            string.IsNullOrWhiteSpace(
                diagConsecutivo
            )
        )
        {
            diagConsecutivo =
                "0";
        }

        // ============================================
        // MOVIMIENTOS QUE HACEN REFERENCIA A LA PLACA
        // ============================================

        var diagMovimientos =
            movimientosHistoricos
                .Where(
                    movimiento =>
                    movimiento.Placas.Any(
                        placa =>
                        placa.Equals(
                            diagPlaca,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .ToList();

        Console.WriteLine();
        Console.WriteLine(
            $"Movimientos históricos encontrados: {diagMovimientos.Count}"
        );

        Console.WriteLine();

        int diagNumeroMovimiento = 1;

        foreach (
            var diagMovimiento
            in diagMovimientos
        )
        {
            Console.WriteLine(
                $"MOVIMIENTO {diagNumeroMovimiento}"
            );

            var diagPosiciones =
                diagMovimiento
                    .RegistrosLibro
                    .Where(
                        registro =>
                        registro.Tomo.HasValue &&
                        registro.Folio.HasValue &&
                        registro.Asiento.HasValue
                    )
                    .Select(
                        registro =>
                        $"T{registro.Tomo}/" +
                        $"F{registro.Folio}/" +
                        $"A{registro.Asiento}"
                    )
                    .ToList();

            Console.WriteLine(
                $"Posiciones: " +
                $"{string.Join(", ", diagPosiciones)}"
            );

            Console.WriteLine(
                $"Texto: {diagMovimiento.TextoCompleto}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );

            diagNumeroMovimiento++;
        }

        // ============================================
        // POSIBLES REGISTROS ORIGINALES
        //
        // Buscamos identificaciones históricas cuyo
        // consecutivo final coincida, incluso cuando
        // tengan:
        //
        // - espacio irregular
        // - prefijo distinto de 4615
        // ============================================

        var diagCandidatosOriginales =
            registrosHistoricos
                .Where(
                    registro =>
                    !string.IsNullOrWhiteSpace(
                        registro.IdentificacionOriginal
                    )
                )
                .Where(
                    registro =>
                    {
                        var coincidencia =
                            System.Text.RegularExpressions.Regex.Match(
                                registro.IdentificacionOriginal.Trim(),
                                @"^\s*\d{4}\s*-\s*(\d{3,4})\s*$"
                            );

                        if (!coincidencia.Success)
                        {
                            return false;
                        }

                        string consecutivoEncontrado =
                            coincidencia
                                .Groups[1]
                                .Value
                                .TrimStart('0');

                        if (
                            string.IsNullOrWhiteSpace(
                                consecutivoEncontrado
                            )
                        )
                        {
                            consecutivoEncontrado =
                                "0";
                        }

                        return consecutivoEncontrado.Equals(
                            diagConsecutivo,
                            StringComparison.OrdinalIgnoreCase
                        );
                    }
                )
                .OrderBy(
                    registro =>
                    registro.NumeroFilaExcel
                )
                .ToList();

        Console.WriteLine();
        Console.WriteLine(
            $"Posibles registros originales: {diagCandidatosOriginales.Count}"
        );

        Console.WriteLine();

        foreach (
            var diagRegistro
            in diagCandidatosOriginales
        )
        {
            Console.WriteLine(
                $"Fila Excel: {diagRegistro.NumeroFilaExcel}"
            );

            Console.WriteLine(
                $"Posición: " +
                $"T{diagRegistro.Tomo}/" +
                $"F{diagRegistro.Folio}/" +
                $"A{diagRegistro.Asiento}"
            );

            Console.WriteLine(
                $"Identificación original: " +
                $"'{diagRegistro.IdentificacionOriginal}'"
            );

            Console.WriteLine(
                $"Descripción: " +
                $"'{diagRegistro.DescripcionOriginal}'"
            );

            Console.WriteLine(
                $"Observaciones: " +
                $"'{diagRegistro.ObservacionesOriginales}'"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }

        Console.WriteLine();
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   ÚLTIMA EVIDENCIA DE LAS 19 PLACAS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    foreach (
        string placaPendiente
        in planMigracion.PlacasReferenciadasSinActivo
    )
    {
        Console.WriteLine(
            "============================================"
        );

        Console.WriteLine(
            $"PLACA: {placaPendiente}"
        );

        Console.WriteLine(
            "============================================"
        );

        // ============================================
        // BAJAS HISTÓRICAS
        // ============================================

        var bajasDeLaPlaca =
            bajasHistoricas
                .Where(
                    baja =>
                    baja.Placas.Any(
                        placa =>
                        placa.Equals(
                            placaPendiente,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .OrderBy(
                    baja =>
                    baja.FechaMovimiento
                )
                .ToList();

        var ultimaBaja =
            bajasDeLaPlaca
                .Where(
                    baja =>
                    baja.FechaMovimiento.HasValue
                )
                .LastOrDefault();

        Console.WriteLine();

        Console.WriteLine(
            $"Bajas históricas: {bajasDeLaPlaca.Count}"
        );

        if (ultimaBaja != null)
        {
            Console.WriteLine(
                $"Última baja: " +
                $"{ultimaBaja.FechaMovimiento!.Value:dd/MM/yyyy}"
            );

            Console.WriteLine(
                $"Motivo de baja: {ultimaBaja.MotivoNormalizado}"
            );
        }
        else
        {
            Console.WriteLine(
                "Última baja: [NO EXISTE]"
            );
        }

        // ============================================
        // TRASLADOS HISTÓRICOS
        // ============================================

        var trasladosDeLaPlaca =
            trasladosHistoricos
                .Where(
                    traslado =>
                    traslado.Placas.Any(
                        placa =>
                        placa.Equals(
                            placaPendiente,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .Where(
                    traslado =>
                    traslado.FechaMovimiento.HasValue
                )
                .OrderBy(
                    traslado =>
                    traslado.FechaMovimiento
                )
                .ToList();

        var ultimoTraslado =
            trasladosDeLaPlaca
                .LastOrDefault();

        Console.WriteLine();

        Console.WriteLine(
            $"Traslados con fecha: {trasladosDeLaPlaca.Count}"
        );

        if (ultimoTraslado != null)
        {
            Console.WriteLine(
                $"Último traslado: " +
                $"{ultimoTraslado.FechaMovimiento!.Value:dd/MM/yyyy}"
            );

            Console.WriteLine(
                $"Origen: " +
                $"{ultimoTraslado.UbicacionAnteriorOriginal}"
            );

            Console.WriteLine(
                $"Destino: " +
                $"{ultimoTraslado.UbicacionNuevaOriginal}"
            );

            Console.WriteLine(
                $"Área destino numérica: " +
                $"{(
                    ultimoTraslado.AreaNuevaNumero.HasValue
                        ? ultimoTraslado.AreaNuevaNumero.Value.ToString()
                        : "[NO NUMÉRICA]"
                )}"
            );
        }
        else
        {
            Console.WriteLine(
                "Último traslado: [NO EXISTE]"
            );
        }

        // ============================================
        // ARCHIVOS DE ÁREA
        // ============================================

        var crucePendiente =
            cruceAreas
                .Resultados
                .FirstOrDefault(
                    x =>
                    x.Placa.Equals(
                        placaPendiente,
                        StringComparison.OrdinalIgnoreCase
                    )
                );

        var areasEncontradas =
            crucePendiente?
                .AreasArchivos
                .Distinct()
                .OrderBy(
                    numero =>
                    numero
                )
                .ToList()
            ?? new List<int>();

        Console.WriteLine();

        Console.WriteLine(
            $"Aparece en archivos de área: " +
            $"{(areasEncontradas.Count > 0 ? "SÍ" : "NO")}"
        );

        if (areasEncontradas.Count > 0)
        {
            Console.WriteLine(
                $"Área(s) encontrada(s): " +
                $"{string.Join(", ", areasEncontradas)}"
            );
        }

        // ============================================
        // LEVANTAMIENTOS DE ÁREA
        // ============================================

        var evidenciasArea =
            areasEncontradas
                .Select(
                    numeroArea =>
                    {
                        var informacionFecha =
                            fechasLevantamientoAreas
                                .FirstOrDefault(
                                    x =>
                                    x.NumeroArea ==
                                    numeroArea
                                );

                        return new
                        {
                            NumeroArea =
                                numeroArea,

                            Fecha =
                                informacionFecha?
                                    .FechaLevantamiento
                        };
                    }
                )
                .ToList();

        foreach (
            var evidenciaArea
            in evidenciasArea
        )
        {
            Console.WriteLine(
                $"Área {evidenciaArea.NumeroArea} - " +
                $"Fecha levantamiento: " +
                $"{(
                    evidenciaArea.Fecha.HasValue
                        ? evidenciaArea.Fecha.Value.ToString("dd/MM/yyyy")
                        : "[SIN FECHA]"
                )}"
            );
        }

        var ultimaEvidenciaArea =
            evidenciasArea
                .Where(
                    x =>
                    x.Fecha.HasValue
                )
                .OrderBy(
                    x =>
                    x.Fecha
                )
                .LastOrDefault();

        // ============================================
        // DETERMINAR ÚLTIMA EVIDENCIA CRONOLÓGICA
        // ============================================

        DateTime? fechaUltimaBaja =
            ultimaBaja?
                .FechaMovimiento?
                .Date;

        DateTime? fechaUltimoTraslado =
            ultimoTraslado?
                .FechaMovimiento?
                .Date;

        DateTime? fechaUltimaArea =
            ultimaEvidenciaArea?
                .Fecha?
                .Date;

        var evidencias =
            new List<(string Tipo, DateTime Fecha, string Detalle)>();

        if (fechaUltimaBaja.HasValue)
        {
            evidencias.Add(
                (
                    "BAJA",
                    fechaUltimaBaja.Value,
                    ultimaBaja!.MotivoNormalizado
                )
            );
        }

        if (fechaUltimoTraslado.HasValue)
        {
            evidencias.Add(
                (
                    "TRASLADO",
                    fechaUltimoTraslado.Value,
                    ultimoTraslado!.UbicacionNuevaOriginal
                )
            );
        }

        if (
            fechaUltimaArea.HasValue &&
            ultimaEvidenciaArea != null
        )
        {
            evidencias.Add(
                (
                    "ARCHIVO_AREA",
                    fechaUltimaArea.Value,
                    $"Área {ultimaEvidenciaArea.NumeroArea}"
                )
            );
        }

        var evidenciaMasReciente =
            evidencias
                .OrderBy(
                    x =>
                    x.Fecha
                )
                .LastOrDefault();

        Console.WriteLine();
        Console.WriteLine(
            "ÚLTIMA EVIDENCIA:"
        );

        if (evidencias.Count == 0)
        {
            Console.WriteLine(
                "[SIN EVIDENCIA FECHADA]"
            );

            Console.WriteLine(
                "DECISIÓN PRELIMINAR: REQUIERE_REVISIÓN"
            );
        }
        else
        {
            Console.WriteLine(
                $"Tipo: {evidenciaMasReciente.Tipo}"
            );

            Console.WriteLine(
                $"Fecha: {evidenciaMasReciente.Fecha:dd/MM/yyyy}"
            );

            Console.WriteLine(
                $"Detalle: {evidenciaMasReciente.Detalle}"
            );

            Console.WriteLine();

            if (
                evidenciaMasReciente.Tipo ==
                "BAJA"
            )
            {
                Console.WriteLine(
                    "DECISIÓN PRELIMINAR: DADO_DE_BAJA"
                );
            }
            else if (
                evidenciaMasReciente.Tipo ==
                "ARCHIVO_AREA"
            )
            {
                Console.WriteLine(
                    "DECISIÓN PRELIMINAR: ACTIVO"
                );

                Console.WriteLine(
                    $"Área actual propuesta: " +
                    $"{ultimaEvidenciaArea!.NumeroArea}"
                );
            }
            else
            {
                Console.WriteLine(
                    "DECISIÓN PRELIMINAR: " +
                    "ACTIVO_CON_EVIDENCIA_HISTORICA"
                );

                Console.WriteLine(
                    "Observación: la última evidencia es un traslado, " +
                    "pero no existe levantamiento físico posterior que " +
                    "confirme su presencia actual."
                );
            }
        }

        Console.WriteLine();
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   DIAGNÓSTICO FINAL DE CONFLICTOS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var placasDiagnosticoFinal =
        new HashSet<string>(
            new[]
            {
            "4615-4678",
            "4615-0793",
            "4615-0800",
            "4615-4472",
            "4615-4474"
            },
            StringComparer.OrdinalIgnoreCase
        );

    var activosDiagnosticoFinal =
        activosPreparados
            .Where(
                x =>
                placasDiagnosticoFinal.Contains(
                    x.Placa
                )
            )
            .OrderBy(
                x =>
                x.Placa
            )
            .ThenBy(
                x =>
                x.NumeroFilaExcel
            )
            .ToList();

    foreach (
        var activoDiagnostico
        in activosDiagnosticoFinal
    )
    {
        Console.WriteLine(
            "============================================"
        );

        Console.WriteLine(
            $"Placa: {activoDiagnostico.Placa}"
        );

        Console.WriteLine(
            $"Fila Excel: {activoDiagnostico.NumeroFilaExcel}"
        );

        Console.WriteLine(
            $"Descripción: {activoDiagnostico.Descripcion}"
        );

        Console.WriteLine(
            $"Marca: {activoDiagnostico.Marca}"
        );

        Console.WriteLine(
            $"Modelo: {activoDiagnostico.Modelo}"
        );

        Console.WriteLine(
            $"Serie: {activoDiagnostico.Serie}"
        );

        Console.WriteLine(
            $"Estado original: {activoDiagnostico.EstadoOriginal}"
        );

        Console.WriteLine(
            $"Estado normalizado: " +
            $"{activoDiagnostico.EstadoNormalizado ?? "[SIN DATO]"}"
        );

        Console.WriteLine(
            $"Ubicación original: {activoDiagnostico.UbicacionOriginal}"
        );

        Console.WriteLine(
            $"Área actual preparada: " +
            $"{(
                activoDiagnostico.AreaActualNumero.HasValue
                    ? activoDiagnostico.AreaActualNumero.Value.ToString()
                    : "[SIN ÁREA]"
            )}"
        );

        Console.WriteLine(
            $"Ubicación histórica: " +
            $"{activoDiagnostico.UbicacionHistorica ?? "[SIN DATO]"}"
        );

        Console.WriteLine(
            $"Modo original: {activoDiagnostico.ModoAdquisicionOriginal}"
        );

        Console.WriteLine(
            $"Modo normalizado: " +
            $"{activoDiagnostico.ModoAdquisicionNormalizado ?? "[SIN DATO]"}"
        );

        Console.WriteLine(
            $"Precio original: {activoDiagnostico.PrecioOriginal}"
        );

        Console.WriteLine(
            $"Fecha adquisición original: " +
            $"{activoDiagnostico.FechaAdquisicionOriginal}"
        );

        Console.WriteLine(
            $"Observaciones: {activoDiagnostico.ObservacionesOriginales}"
        );

        Console.WriteLine(
            $"Registro: " +
            $"T{activoDiagnostico.Tomo}/" +
            $"F{activoDiagnostico.Folio}/" +
            $"A{activoDiagnostico.Asiento}"
        );

        Console.WriteLine(
            $"Situación: {activoDiagnostico.SituacionPreparada}"
        );

        Console.WriteLine(
            $"Requiere revisión: " +
            $"{(activoDiagnostico.RequiereRevision ? "SÍ" : "NO")}"
        );

        if (
            activoDiagnostico.MotivosRevision.Count > 0
        )
        {
            Console.WriteLine(
                "Motivos de revisión:"
            );

            foreach (
                string motivo
                in activoDiagnostico.MotivosRevision
            )
            {
                Console.WriteLine(
                    $"  - {motivo}"
                );
            }
        }

        Console.WriteLine();
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   ACTIVOS FINALES CONSOLIDADOS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var consolidadorActivosMigracion =
        new ConsolidadorActivosMigracionService();

    var activosFinalesMigracion =
        consolidadorActivosMigracion.Consolidar(
            activosPreparados,
            activosHistoricosReconstruidos
        );

    int totalActivosFinales =
        activosFinalesMigracion.Count;

    int placasUnicasFinales =
        activosFinalesMigracion
            .Select(
                x => x.Placa
            )
            .Distinct(
                StringComparer.OrdinalIgnoreCase
            )
            .Count();

    int placasDuplicadasFinalesConsolidadas =
        activosFinalesMigracion
            .GroupBy(
                x => x.Placa,
                StringComparer.OrdinalIgnoreCase
            )
            .Count(
                x => x.Count() > 1
            );

    int activosFinalesActivos =
        activosFinalesMigracion
            .Count(
                x =>
                x.SituacionPreparada.Equals(
                    "ACTIVO",
                    StringComparison.OrdinalIgnoreCase
                )
            );

    int activosFinalesBaja =
        activosFinalesMigracion
            .Count(
                x =>
                x.SituacionPreparada.Equals(
                    "DADO_DE_BAJA",
                    StringComparison.OrdinalIgnoreCase
                )
            );

    var activosSinRegistroInclusion =
        activosFinalesMigracion
            .Where(
                x =>
                !x.RegistrarPosicionLibroInclusion
            )
            .OrderBy(
                x => x.Placa
            )
            .ToList();

    Console.WriteLine(
        $"Total: {totalActivosFinales}"
    );

    Console.WriteLine(
        $"Placas únicas: {placasUnicasFinales}"
    );

    Console.WriteLine(
        $"Duplicados: {placasDuplicadasFinalesConsolidadas}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"ACTIVO: {activosFinalesActivos}"
    );

    Console.WriteLine(
        $"DADO_DE_BAJA: {activosFinalesBaja}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Sin RegistroLibro de inclusión por conflicto: " +
        $"{activosSinRegistroInclusion.Count}"
    );

    foreach (
        var activo
        in activosSinRegistroInclusion
    )
    {
        Console.WriteLine(
            $"  {activo.Placa}"
        );

        foreach (
            string posicion
            in activo.PosicionesLibroConflictivas
        )
        {
            Console.WriteLine(
                $"    Posición fuente: {posicion}"
            );
        }
    }

    Console.WriteLine();

    Console.WriteLine(
        $"Activos finales que requieren revisión: " +
        $"{activosFinalesMigracion.Count(x => x.RequiereRevision)}"
    );

    Console.WriteLine();

    bool consolidacionCorrecta =
        totalActivosFinales == 5355 &&
        placasUnicasFinales == 5355 &&
        placasDuplicadasFinalesConsolidadas == 0 &&
        activosFinalesActivos == 4193 &&
        activosFinalesBaja == 1162 &&
        activosSinRegistroInclusion.Count == 5;

    Console.WriteLine(
        "--------------------------------------------"
    );

    Console.WriteLine(
        $"¿Consolidación final válida?: " +
        $"{(consolidacionCorrecta ? "SÍ" : "NO")}"
    );

    Console.WriteLine(
        "--------------------------------------------"
    );


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CONEXIÓN DE VERIFICACIÓN A SQL SERVER"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    string cadenaConexionMigracion =
        @"Server=VIA\SQLEXPRESS;" +
        @"Database=EscuelaInventario;" +
        @"Trusted_Connection=True;" +
        @"TrustServerCertificate=True;";

    var opcionesContextoMigracion =
        new DbContextOptionsBuilder<EscuelaInventarioContext>()
            .UseSqlServer(
                cadenaConexionMigracion
            )
            .Options;

    await using var contextoMigracion =
        new EscuelaInventarioContext(
            opcionesContextoMigracion
        );

    var verificadorBaseDatos =
        new VerificadorBaseDatosService(
            contextoMigracion
        );

    await verificadorBaseDatos.VerificarAsync();

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   INICIO DE PRUEBA DE IMPORTACIÓN"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var importadorActivosMigracion =
        new ImportadorActivosMigracionService(
            contextoMigracion
        );

    await importadorActivosMigracion
        .ProbarImportacionAsync(
            activosFinalesMigracion
        );

    // Limpiamos entidades que EF tenga en memoria
    // después del rollback.
    contextoMigracion.ChangeTracker.Clear();

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   VERIFICACIÓN DESPUÉS DEL ROLLBACK"
    );
    Console.WriteLine(
        "============================================"
    );

    await verificadorBaseDatos.VerificarAsync();

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   ESQUEMA EF PARA IMPORTACIÓN"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    string[] entidadesMigracion =
    {
    "Area",
    "EstadoActivo",
    "SituacionActivo",
    "ModoAdquisicion",
    "Activo",
    "MovimientoInventario",
    "RegistroLibro",
    "ConfiguracionInventario"
};

    foreach (
        var entidad
        in contextoMigracion.Model.GetEntityTypes()
            .Where(
                x =>
                    entidadesMigracion.Contains(
                        x.ClrType.Name
                    )
            )
            .OrderBy(
                x => x.ClrType.Name
            )
    )
    {
        Console.WriteLine(
            $"ENTIDAD: {entidad.ClrType.Name}"
        );

        Console.WriteLine(
            $"Tabla: {entidad.GetTableName()}"
        );

        foreach (
            var propiedad
            in entidad.GetProperties()
                .OrderBy(
                    x => x.Name
                )
        )
        {
            Console.WriteLine(
                $"  {propiedad.Name}" +
                $" | Tipo: {propiedad.ClrType.Name}" +
                $" | Nullable: {propiedad.IsNullable}"
            );
        }

        Console.WriteLine(
            "--------------------------------------------"
        );
    }




    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   PLAN DE EXPANSIÓN DE MOVIMIENTOS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    int filasTraslados =
        trasladosHistoricos.Sum(
            x => x.Placas.Count
        );

    int filasBajas =
        bajasHistoricas.Sum(
            x => x.Placas.Count
        );

    int filasCambiosEstado =
        cambiosEstadoHistoricos.Sum(
            x => x.Placas.Count
        );

    int filasModificaciones =
        modificacionesHistoricas.Sum(
            x => x.Placas.Count
        );

    int totalFilasMovimiento =
        filasTraslados +
        filasBajas +
        filasCambiosEstado +
        filasModificaciones;

    int trasladosCompartidos =
        trasladosHistoricos.Count(
            x => x.Placas.Count > 1
        );

    int bajasCompartidas =
        bajasHistoricas.Count(
            x => x.Placas.Count > 1
        );

    int cambiosCompartidos =
        cambiosEstadoHistoricos.Count(
            x => x.Placas.Count > 1
        );

    int modificacionesCompartidas =
        modificacionesHistoricas.Count(
            x => x.Placas.Count > 1
        );

    int trasladosSinFecha =
        trasladosHistoricos.Count(
            x => !x.FechaMovimiento.HasValue
        );

    int bajasSinFecha =
        bajasHistoricas.Count(
            x => !x.FechaMovimiento.HasValue
        );

    int cambiosSinFecha =
        cambiosEstadoHistoricos.Count(
            x => !x.FechaMovimiento.HasValue
        );

    int modificacionesSinFecha =
        modificacionesHistoricas.Count(
            x => !x.FechaMovimiento.HasValue
        );

    Console.WriteLine(
        $"Anotaciones TRASLADO: {trasladosHistoricos.Count}"
    );

    Console.WriteLine(
        $"Filas MovimientoInventario TRASLADO: {filasTraslados}"
    );

    Console.WriteLine(
        $"Anotaciones compartidas TRASLADO: {trasladosCompartidos}"
    );

    Console.WriteLine(
        $"Sin fecha TRASLADO: {trasladosSinFecha}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Anotaciones BAJA: {bajasHistoricas.Count}"
    );

    Console.WriteLine(
        $"Filas MovimientoInventario BAJA: {filasBajas}"
    );

    Console.WriteLine(
        $"Anotaciones compartidas BAJA: {bajasCompartidas}"
    );

    Console.WriteLine(
        $"Sin fecha BAJA: {bajasSinFecha}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Anotaciones CAMBIO_ESTADO: {cambiosEstadoHistoricos.Count}"
    );

    Console.WriteLine(
        $"Filas MovimientoInventario CAMBIO_ESTADO: {filasCambiosEstado}"
    );

    Console.WriteLine(
        $"Anotaciones compartidas CAMBIO_ESTADO: {cambiosCompartidos}"
    );

    Console.WriteLine(
        $"Sin fecha CAMBIO_ESTADO: {cambiosSinFecha}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Anotaciones MODIFICACION: {modificacionesHistoricas.Count}"
    );

    Console.WriteLine(
        $"Filas MovimientoInventario MODIFICACION: {filasModificaciones}"
    );

    Console.WriteLine(
        $"Anotaciones compartidas MODIFICACION: {modificacionesCompartidas}"
    );

    Console.WriteLine(
        $"Sin fecha MODIFICACION: {modificacionesSinFecha}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"TOTAL FILAS MovimientoInventario A CREAR: {totalFilasMovimiento}"
    );

    Console.WriteLine(
        "============================================"
    );


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   CONSTRUCCIÓN DE MOVIMIENTOS PREPARADOS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var movimientosPreparadosMigracion =
        new List<MovimientoPreparadoMigracion>();


    // ============================================================
    // TRASLADOS
    // ============================================================

    foreach (var traslado in trasladosHistoricos)
    {
        var placas =
            traslado.Placas
                .Distinct(
                    StringComparer.OrdinalIgnoreCase
                )
                .ToList();

        for (int indice = 0; indice < placas.Count; indice++)
        {
            string placa =
                placas[indice];

            bool principal =
                indice == 0;

            var preparado =
                new MovimientoPreparadoMigracion
                {
                    Placa =
                        placa,

                    TipoMovimientoCodigo =
                        "TRASLADO",

                    FechaMovimiento =
                        traslado.FechaMovimiento,

                    AreaAnteriorNumero =
                        traslado.AreaAnteriorNumero,

                    AreaNuevaNumero =
                        traslado.AreaNuevaNumero,

                    UbicacionAnteriorOriginal =
                        traslado.UbicacionAnteriorOriginal,

                    UbicacionNuevaOriginal =
                        traslado.UbicacionNuevaOriginal,

                    EstadoAnteriorNombre =
                        null,

                    EstadoNuevoNombre =
                        null,

                    Motivo =
                        null,

                    Observacion =
                        traslado.TextoOriginal,

                    RequiereRevision =
                        traslado.RequiereRevision,

                    MotivoRevision =
                        traslado.MotivoRevision,

                    EsAnotacionCompartida =
                        placas.Count > 1,

                    RecibeRegistroLibro =
                        principal
                };

            if (principal)
            {
                var registrosLibroMovimiento =
                    traslado.RegistrosLibro
                        .Where(
                            x =>
                            x.Tomo.HasValue &&
                            x.Folio.HasValue &&
                            x.Asiento.HasValue
                        )
                        .OrderBy(
                            x => x.Tomo
                        )
                        .ThenBy(
                            x => x.Folio
                        )
                        .ThenBy(
                            x => x.Asiento
                        )
                        .ToList();

                for (
                    int i = 0;
                    i < registrosLibroMovimiento.Count;
                    i++
                )
                {
                    var registro =
                        registrosLibroMovimiento[i];

                    preparado.PosicionesLibro.Add(
                        new PosicionLibroPreparadaMigracion
                        {
                            Tomo =
                                registro.Tomo!.Value,

                            Folio =
                                registro.Folio!.Value,

                            Asiento =
                                registro.Asiento!.Value,

                            TextoOriginal =
                                registro.IdentificacionOriginal,

                            EsPrincipal =
                                i == 0
                        }
                    );
                }
            }

            movimientosPreparadosMigracion.Add(
                preparado
            );
        }
    }


    // ============================================================
    // BAJAS
    // ============================================================

    foreach (var baja in bajasHistoricas)
    {
        var placas =
            baja.Placas
                .Distinct(
                    StringComparer.OrdinalIgnoreCase
                )
                .ToList();

        for (int indice = 0; indice < placas.Count; indice++)
        {
            string placa =
                placas[indice];

            bool principal =
                indice == 0;

            var preparado =
                new MovimientoPreparadoMigracion
                {
                    Placa =
                        placa,

                    TipoMovimientoCodigo =
                        "BAJA",

                    FechaMovimiento =
                        baja.FechaMovimiento,

                    AreaAnteriorNumero =
                        null,

                    AreaNuevaNumero =
                        null,

                    EstadoAnteriorNombre =
                        null,

                    EstadoNuevoNombre =
                        null,

                    Motivo =
                        baja.MotivoNormalizado,

                    Observacion =
                        baja.TextoOriginal,

                    RequiereRevision =
                        baja.RequiereRevision,

                    MotivoRevision =
                        baja.MotivoRevision,

                    EsAnotacionCompartida =
                        placas.Count > 1,

                    RecibeRegistroLibro =
                        principal
                };

            if (principal)
            {
                var registrosLibroMovimiento =
                    baja.RegistrosLibro
                        .Where(
                            x =>
                            x.Tomo.HasValue &&
                            x.Folio.HasValue &&
                            x.Asiento.HasValue
                        )
                        .OrderBy(
                            x => x.Tomo
                        )
                        .ThenBy(
                            x => x.Folio
                        )
                        .ThenBy(
                            x => x.Asiento
                        )
                        .ToList();

                for (
                    int i = 0;
                    i < registrosLibroMovimiento.Count;
                    i++
                )
                {
                    var registro =
                        registrosLibroMovimiento[i];

                    preparado.PosicionesLibro.Add(
                        new PosicionLibroPreparadaMigracion
                        {
                            Tomo =
                                registro.Tomo!.Value,

                            Folio =
                                registro.Folio!.Value,

                            Asiento =
                                registro.Asiento!.Value,

                            TextoOriginal =
                                registro.IdentificacionOriginal,

                            EsPrincipal =
                                i == 0
                        }
                    );
                }
            }

            movimientosPreparadosMigracion.Add(
                preparado
            );
        }
    }


    // ============================================================
    // CAMBIOS DE ESTADO
    // ============================================================

    foreach (var cambio in cambiosEstadoHistoricos)
    {
        var placas =
            cambio.Placas
                .Distinct(
                    StringComparer.OrdinalIgnoreCase
                )
                .ToList();

        for (int indice = 0; indice < placas.Count; indice++)
        {
            string placa =
                placas[indice];

            bool principal =
                indice == 0;

            var preparado =
                new MovimientoPreparadoMigracion
                {
                    Placa =
                        placa,

                    TipoMovimientoCodigo =
                        "CAMBIO_ESTADO",

                    FechaMovimiento =
                        cambio.FechaMovimiento,

                    EstadoAnteriorNombre =
                        cambio.EstadoAnteriorNormalizado,

                    EstadoNuevoNombre =
                        cambio.EstadoNuevoNormalizado,

                    Motivo =
                        null,

                    Observacion =
                        cambio.TextoOriginal,

                    RequiereRevision =
                        cambio.RequiereRevision,

                    MotivoRevision =
                        cambio.MotivoRevision,

                    EsAnotacionCompartida =
                        placas.Count > 1,

                    RecibeRegistroLibro =
                        principal
                };

            if (principal)
            {
                var registrosLibroMovimiento =
                    cambio.RegistrosLibro
                        .Where(
                            x =>
                            x.Tomo.HasValue &&
                            x.Folio.HasValue &&
                            x.Asiento.HasValue
                        )
                        .OrderBy(
                            x => x.Tomo
                        )
                        .ThenBy(
                            x => x.Folio
                        )
                        .ThenBy(
                            x => x.Asiento
                        )
                        .ToList();

                for (
                    int i = 0;
                    i < registrosLibroMovimiento.Count;
                    i++
                )
                {
                    var registro =
                        registrosLibroMovimiento[i];

                    preparado.PosicionesLibro.Add(
                        new PosicionLibroPreparadaMigracion
                        {
                            Tomo =
                                registro.Tomo!.Value,

                            Folio =
                                registro.Folio!.Value,

                            Asiento =
                                registro.Asiento!.Value,

                            TextoOriginal =
                                registro.IdentificacionOriginal,

                            EsPrincipal =
                                i == 0
                        }
                    );
                }
            }

            movimientosPreparadosMigracion.Add(
                preparado
            );
        }
    }


    // ============================================================
    // MODIFICACIONES
    // ============================================================

    foreach (var modificacion in modificacionesHistoricas)
    {
        var placas =
            modificacion.Placas
                .Distinct(
                    StringComparer.OrdinalIgnoreCase
                )
                .ToList();

        for (int indice = 0; indice < placas.Count; indice++)
        {
            string placa =
                placas[indice];

            bool principal =
                indice == 0;

            var preparado =
                new MovimientoPreparadoMigracion
                {
                    Placa =
                        placa,

                    TipoMovimientoCodigo =
                        "MODIFICACION",

                    FechaMovimiento =
                        modificacion.FechaMovimiento,

                    Motivo =
                        "Modificación histórica",

                    Observacion =
                        modificacion.TextoOriginal,

                    RequiereRevision =
                        modificacion.RequiereRevision,

                    MotivoRevision =
                        modificacion.MotivoRevision,

                    EsAnotacionCompartida =
                        placas.Count > 1,

                    RecibeRegistroLibro =
                        principal
                };

            if (principal)
            {
                var registrosLibroMovimiento =
                    modificacion.RegistrosLibro
                        .Where(
                            x =>
                            x.Tomo.HasValue &&
                            x.Folio.HasValue &&
                            x.Asiento.HasValue
                        )
                        .OrderBy(
                            x => x.Tomo
                        )
                        .ThenBy(
                            x => x.Folio
                        )
                        .ThenBy(
                            x => x.Asiento
                        )
                        .ToList();

                for (
                    int i = 0;
                    i < registrosLibroMovimiento.Count;
                    i++
                )
                {
                    var registro =
                        registrosLibroMovimiento[i];

                    preparado.PosicionesLibro.Add(
                        new PosicionLibroPreparadaMigracion
                        {
                            Tomo =
                                registro.Tomo!.Value,

                            Folio =
                                registro.Folio!.Value,

                            Asiento =
                                registro.Asiento!.Value,

                            TextoOriginal =
                                registro.IdentificacionOriginal,

                            EsPrincipal =
                                i == 0
                        }
                    );
                }
            }

            movimientosPreparadosMigracion.Add(
                preparado
            );
        }
    }


    // ============================================================
    // 2 NO CLASIFICADOS
    // ============================================================

    var noClasificadosFinales =
        movimientosHistoricos
            .Where(
                x =>
                x.TipoMovimiento ==
                "NO_CLASIFICADO"
            )
            .ToList();

    foreach (
        var movimiento
        in noClasificadosFinales
    )
    {
        if (
            string.IsNullOrWhiteSpace(
                movimiento.PlacaPrincipal
            )
        )
        {
            continue;
        }

        var preparado =
            new MovimientoPreparadoMigracion
            {
                Placa =
                    movimiento.PlacaPrincipal,

                TipoMovimientoCodigo =
                    "MODIFICACION",

                FechaMovimiento =
                    null,

                Motivo =
                    "Anotación histórica no clasificada",

                Observacion =
                    movimiento.TextoCompleto,

                RequiereRevision =
                    true,

                MotivoRevision =
                    "La anotación histórica original está " +
                    "incompleta y no permite determinar con " +
                    "certeza la operación realizada.",

                EsAnotacionCompartida =
                    false,

                RecibeRegistroLibro =
                    true
            };

        var registrosLibroMovimiento =
            movimiento.RegistrosLibro
                .Where(
                    x =>
                    x.Tomo.HasValue &&
                    x.Folio.HasValue &&
                    x.Asiento.HasValue
                )
                .OrderBy(
                    x => x.Tomo
                )
                .ThenBy(
                    x => x.Folio
                )
                .ThenBy(
                    x => x.Asiento
                )
                .ToList();

        for (
            int i = 0;
            i < registrosLibroMovimiento.Count;
            i++
        )
        {
            var registro =
                registrosLibroMovimiento[i];

            preparado.PosicionesLibro.Add(
                new PosicionLibroPreparadaMigracion
                {
                    Tomo =
                        registro.Tomo!.Value,

                    Folio =
                        registro.Folio!.Value,

                    Asiento =
                        registro.Asiento!.Value,

                    TextoOriginal =
                        registro.IdentificacionOriginal,

                    EsPrincipal =
                        i == 0
                }
            );
        }

        movimientosPreparadosMigracion.Add(
            preparado
        );
    }


    // ============================================================
    // VALIDACIÓN
    // ============================================================

    int preparadosTraslado =
        movimientosPreparadosMigracion.Count(
            x =>
            x.TipoMovimientoCodigo ==
            "TRASLADO"
        );

    int totalPreparadosBaja =
        movimientosPreparadosMigracion.Count(
            x =>
            x.TipoMovimientoCodigo ==
            "BAJA"
        );

    int preparadosCambioEstado =
        movimientosPreparadosMigracion.Count(
            x =>
            x.TipoMovimientoCodigo ==
            "CAMBIO_ESTADO"
        );

    int preparadosModificacion =
        movimientosPreparadosMigracion.Count(
            x =>
            x.TipoMovimientoCodigo ==
            "MODIFICACION"
        );

    int preparadosSinFecha =
        movimientosPreparadosMigracion.Count(
            x =>
            !x.FechaMovimiento.HasValue
        );

    int preparadosConLibro =
        movimientosPreparadosMigracion.Count(
            x =>
            x.RecibeRegistroLibro
        );

    int posicionesLibroPreparadas =
        movimientosPreparadosMigracion.Sum(
            x =>
            x.PosicionesLibro.Count
        );

    int placasNoEncontradas =
        movimientosPreparadosMigracion.Count(
            movimiento =>
            !activosFinalesMigracion.Any(
                activo =>
                activo.Placa.Equals(
                    movimiento.Placa,
                    StringComparison.OrdinalIgnoreCase
                )
            )
        );

    Console.WriteLine(
        $"TRASLADO preparados: {preparadosTraslado}"
    );

    Console.WriteLine(
        $"BAJA preparados: {totalPreparadosBaja}"
    );

    Console.WriteLine(
        $"CAMBIO_ESTADO preparados: {preparadosCambioEstado}"
    );

    Console.WriteLine(
        $"MODIFICACION preparados: {preparadosModificacion}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"TOTAL históricos preparados: " +
        $"{movimientosPreparadosMigracion.Count}"
    );

    Console.WriteLine(
        $"Sin fecha: {preparadosSinFecha}"
    );

    Console.WriteLine(
        $"Movimientos propietarios de libro: " +
        $"{preparadosConLibro}"
    );

    Console.WriteLine(
        $"Posiciones de libro preparadas: " +
        $"{posicionesLibroPreparadas}"
    );

    Console.WriteLine(
        $"Movimientos cuya placa no existe: " +
        $"{placasNoEncontradas}"
    );

    Console.WriteLine();

    bool movimientosPreparadosValidos =
        preparadosTraslado == 3754 &&
        totalPreparadosBaja == 1229 &&
        preparadosCambioEstado == 166 &&
        preparadosModificacion == 71 &&
        movimientosPreparadosMigracion.Count == 5220 &&
        preparadosSinFecha == 24 &&
        placasNoEncontradas == 0;

    Console.WriteLine(
        $"¿Movimientos preparados válidos?: " +
        $"{(movimientosPreparadosValidos ? "SÍ" : "NO")}"
    );

    Console.WriteLine(
        "============================================"
    );



    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   INICIO DE PRUEBA INTEGRAL DE MIGRACIÓN"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    await importadorActivosMigracion
    .MigrarDefinitivamenteAsync(
        activosFinalesMigracion,
        movimientosPreparadosMigracion
    );

    contextoMigracion.ChangeTracker.Clear();

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   VERIFICACIÓN DESPUÉS DEL ROLLBACK INTEGRAL"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    await verificadorBaseDatos.VerificarAsync();


    // ============================================
    // 12. INCIDENCIAS
    // ============================================

    Console.WriteLine("============================================");
    Console.WriteLine("   INCIDENCIAS");
    Console.WriteLine("============================================");
    Console.WriteLine();

    if (incidencias.Count == 0)
    {
        Console.ForegroundColor =
            ConsoleColor.Green;

        Console.WriteLine(
            "No se encontraron incidencias."
        );

        Console.ResetColor();
    }
    else
    {
        foreach (var incidencia in incidencias)
        {
            if (incidencia.Severidad == "ERROR")
            {
                Console.ForegroundColor =
                    ConsoleColor.Red;
            }
            else if (
                incidencia.Severidad == "ADVERTENCIA")
            {
                Console.ForegroundColor =
                    ConsoleColor.Yellow;
            }
            else
            {
                Console.ForegroundColor =
                    ConsoleColor.Cyan;
            }

            Console.WriteLine(
                $"[{incidencia.Severidad}] " +
                $"{incidencia.Tipo}"
            );

            Console.ResetColor();

            Console.WriteLine(
                $"Placa: {incidencia.Placa}"
            );

            Console.WriteLine(
                $"Fila Excel: " +
                $"{incidencia.NumeroFilaExcel}"
            );

            Console.WriteLine(
                $"Detalle: {incidencia.Mensaje}"
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   BÚSQUEDA DE REGISTROS ORIGINALES"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var posicionesOriginalesBuscadas =
        new[]
        {
        new
        {
            Placa = "4615-3318",
            Tomo = 5,
            Folio = 334,
            Asiento = 23
        },
        new
        {
            Placa = "4615-3698",
            Tomo = 5,
            Folio = 435,
            Asiento = 31
        },
        new
        {
            Placa = "4615-3934",
            Tomo = 5,
            Folio = 497,
            Asiento = 14
        },
        new
        {
            Placa = "4615-4082",
            Tomo = 5,
            Folio = 546,
            Asiento = 23
        }
        };

    foreach (
        var buscado
        in posicionesOriginalesBuscadas
    )
    {
        Console.WriteLine(
            $"PLACA REFERENCIADA: {buscado.Placa}"
        );

        Console.WriteLine(
            $"POSICIÓN ORIGINAL REFERENCIADA: " +
            $"T{buscado.Tomo}/F{buscado.Folio}/A{buscado.Asiento}"
        );

        Console.WriteLine();

        // ========================================
        // BUSCAR ENTRE LOS 5333 ACTIVOS
        // ========================================

        var coincidenciasActivas =
            registros
                .Where(
                    x =>
                    x.Tomo == buscado.Tomo &&
                    x.Folio == buscado.Folio &&
                    x.Asiento == buscado.Asiento
                )
                .ToList();

        Console.WriteLine(
            $"Coincidencias en registros actuales: {coincidenciasActivas.Count}"
        );

        foreach (
            var registro
            in coincidenciasActivas
        )
        {
            Console.WriteLine(
                $"Fila Excel: {registro.NumeroFilaExcel}"
            );

            Console.WriteLine(
                $"Placa: '{registro.Placa}'"
            );

            Console.WriteLine(
                $"Descripción: '{registro.Descripcion}'"
            );

            Console.WriteLine(
                $"Marca: '{registro.Marca}'"
            );

            Console.WriteLine(
                $"Modelo: '{registro.Modelo}'"
            );

            Console.WriteLine(
                $"Serie: '{registro.Serie}'"
            );

            Console.WriteLine(
                $"Estado: '{registro.EstadoOriginal}'"
            );

            Console.WriteLine(
                $"Ubicación: '{registro.UbicacionOriginal}'"
            );

            Console.WriteLine(
                $"Modo adquisición: '{registro.ModoAdquisicionOriginal}'"
            );

            Console.WriteLine(
                $"Precio: '{registro.PrecioOriginal}'"
            );

            Console.WriteLine(
                $"Fecha adquisición: '{registro.FechaAdquisicionOriginal}'"
            );

            Console.WriteLine(
                $"Observaciones: '{registro.ObservacionesOriginales}'"
            );
        }

        Console.WriteLine();

        // ========================================
        // BUSCAR ENTRE LAS FILAS HISTÓRICAS
        // ========================================

        var coincidenciasHistoricas =
            registrosHistoricos
                .Where(
                    x =>
                    x.Tomo == buscado.Tomo &&
                    x.Folio == buscado.Folio &&
                    x.Asiento == buscado.Asiento
                )
                .ToList();

        Console.WriteLine(
            $"Coincidencias en registros históricos: {coincidenciasHistoricas.Count}"
        );

        foreach (
            var registro
            in coincidenciasHistoricas
        )
        {
            Console.WriteLine(
                $"Fila Excel: {registro.NumeroFilaExcel}"
            );

            Console.WriteLine(
                $"Identificación original: '{registro.IdentificacionOriginal}'"
            );

            Console.WriteLine(
                $"Descripción original: '{registro.DescripcionOriginal}'"
            );

            Console.WriteLine(
                $"Observaciones originales: '{registro.ObservacionesOriginales}'"
            );
        }

        Console.WriteLine();

        Console.WriteLine(
            "============================================"
        );

        Console.WriteLine();
    }


    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   DATOS COMPLETOS DE ACTIVOS HISTÓRICOS"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    int[] filasHistoricasAInspeccionar =
    {
    4123,
    4832,
    6868,
    8507
};

    using (
        var workbookHistorico =
            new ClosedXML.Excel.XLWorkbook(
                rutaArchivo
            )
    )
    {
        var hojaHistorica =
            workbookHistorico.Worksheets.First();

        foreach (
            int numeroFila
            in filasHistoricasAInspeccionar
        )
        {
            Console.WriteLine(
                $"FILA EXCEL: {numeroFila}"
            );

            for (
                int columna = 1;
                columna <= 13;
                columna++
            )
            {
                string valor =
                    hojaHistorica
                        .Cell(
                            numeroFila,
                            columna
                        )
                        .GetFormattedString()
                        .Trim();

                Console.WriteLine(
                    $"Columna {columna}: '{valor}'"
                );
            }

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }

    // ============================================
    // ANÁLISIS PREVIO DE SITUACIÓN ACTUAL
    // ============================================

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   ANÁLISIS PREVIO DE SITUACIÓN ACTUAL"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    var situacion_BajasPorPlaca =
        bajasHistoricas
            .SelectMany(
                baja =>
                baja.Placas.Select(
                    placa => new
                    {
                        Placa = placa,
                        Baja = baja
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
                x => x.Placa.Trim(),
                StringComparer.OrdinalIgnoreCase
            )
            .ToList();

    int situacion_PlacasConBaja =
        situacion_BajasPorPlaca.Count;

    int situacion_ConUnaBaja =
        situacion_BajasPorPlaca.Count(
            grupo => grupo.Count() == 1
        );

    int situacion_ConMultiplesBajas =
        situacion_BajasPorPlaca.Count(
            grupo => grupo.Count() > 1
        );

    int situacion_SinFechaBaja =
        0;

    int situacion_SinSenalesPosteriores =
        0;

    int situacion_ConSenalesPosteriores =
        0;

    int situacion_ConTrasladoPosterior =
        0;

    int situacion_ConCambioEstadoPosterior =
        0;

    int situacion_ConModificacionPosterior =
        0;

    int situacion_EnAreaPosterior =
        0;

    int situacion_EnAreaMismoDia =
        0;

    var situacion_Ejemplos =
        new List<string>();

    foreach (
        var grupoBajas
        in situacion_BajasPorPlaca
    )
    {
        string placaSituacion =
            grupoBajas.Key;

        var bajasConFecha =
            grupoBajas
                .Where(
                    x =>
                    x.Baja.FechaMovimiento.HasValue
                )
                .OrderBy(
                    x =>
                    x.Baja.FechaMovimiento
                )
                .ToList();

        if (
            bajasConFecha.Count == 0
        )
        {
            situacion_SinFechaBaja++;

            continue;
        }

        var ultimaBaja =
            bajasConFecha.Last().Baja;

        DateTime fechaUltimaBaja =
            ultimaBaja
                .FechaMovimiento!
                .Value
                .Date;

        // ========================================
        // TRASLADOS POSTERIORES
        // ========================================

        var trasladosPosteriores =
            trasladosHistoricos
                .Where(
                    traslado =>
                    traslado.FechaMovimiento.HasValue &&
                    traslado.FechaMovimiento.Value.Date >
                        fechaUltimaBaja &&
                    traslado.Placas.Any(
                        placa =>
                        placa.Equals(
                            placaSituacion,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .OrderBy(
                    x => x.FechaMovimiento
                )
                .ToList();

        // ========================================
        // CAMBIOS DE ESTADO POSTERIORES
        // ========================================

        var cambiosEstadoPosteriores =
            cambiosEstadoHistoricos
                .Where(
                    cambio =>
                    cambio.FechaMovimiento.HasValue &&
                    cambio.FechaMovimiento.Value.Date >
                        fechaUltimaBaja &&
                    cambio.Placas.Any(
                        placa =>
                        placa.Equals(
                            placaSituacion,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .OrderBy(
                    x => x.FechaMovimiento
                )
                .ToList();

        // ========================================
        // MODIFICACIONES POSTERIORES
        // ========================================

        var modificacionesPosteriores =
            modificacionesHistoricas
                .Where(
                    modificacion =>
                    modificacion.FechaMovimiento.HasValue &&
                    modificacion.FechaMovimiento.Value.Date >
                        fechaUltimaBaja &&
                    modificacion.Placas.Any(
                        placa =>
                        placa.Equals(
                            placaSituacion,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                )
                .OrderBy(
                    x => x.FechaMovimiento
                )
                .ToList();

        // ========================================
        // APARICIONES EN ARCHIVOS DE ÁREA
        // ========================================

        var aparicionesEnAreas =
            resultadoAreas
                .Registros
                .Where(
                    registro =>
                    registro.PlacaNormalizada.Equals(
                        placaSituacion,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Select(
                    registro => new
                    {
                        Registro = registro,

                        Fecha =
                            fechasLevantamientoAreas
                                .FirstOrDefault(
                                    fecha =>
                                    fecha.NumeroArea ==
                                        registro.NumeroAreaArchivo
                                )
                                ?.FechaLevantamiento
                    }
                )
                .Where(
                    x => x.Fecha.HasValue
                )
                .ToList();

        var areasPosteriores =
            aparicionesEnAreas
                .Where(
                    x =>
                    x.Fecha!.Value.Date >
                        fechaUltimaBaja
                )
                .ToList();

        var areasMismoDia =
            aparicionesEnAreas
                .Where(
                    x =>
                    x.Fecha!.Value.Date ==
                        fechaUltimaBaja
                )
                .ToList();

        bool tieneTrasladoPosterior =
            trasladosPosteriores.Count > 0;

        bool tieneCambioEstadoPosterior =
            cambiosEstadoPosteriores.Count > 0;

        bool tieneModificacionPosterior =
            modificacionesPosteriores.Count > 0;

        bool apareceAreaPosterior =
            areasPosteriores.Count > 0;

        bool apareceAreaMismoDia =
            areasMismoDia.Count > 0;

        if (
            tieneTrasladoPosterior
        )
        {
            situacion_ConTrasladoPosterior++;
        }

        if (
            tieneCambioEstadoPosterior
        )
        {
            situacion_ConCambioEstadoPosterior++;
        }

        if (
            tieneModificacionPosterior
        )
        {
            situacion_ConModificacionPosterior++;
        }

        if (
            apareceAreaPosterior
        )
        {
            situacion_EnAreaPosterior++;
        }

        if (
            apareceAreaMismoDia
        )
        {
            situacion_EnAreaMismoDia++;
        }

        bool tieneSenal =
            tieneTrasladoPosterior ||
            tieneCambioEstadoPosterior ||
            tieneModificacionPosterior ||
            apareceAreaPosterior ||
            apareceAreaMismoDia;

        if (
            !tieneSenal
        )
        {
            situacion_SinSenalesPosteriores++;

            continue;
        }

        situacion_ConSenalesPosteriores++;

        // ========================================
        // GUARDAR ALGUNOS EJEMPLOS
        // ========================================

        if (
            situacion_Ejemplos.Count < 40
        )
        {
            var detalles =
                new List<string>();

            if (
                tieneTrasladoPosterior
            )
            {
                var ultimoTraslado =
                    trasladosPosteriores.Last();

                detalles.Add(
                    "Traslado posterior: " +
                    $"{ultimoTraslado.FechaMovimiento!.Value:dd/MM/yyyy}"
                );
            }

            if (
                tieneCambioEstadoPosterior
            )
            {
                var ultimoCambio =
                    cambiosEstadoPosteriores.Last();

                detalles.Add(
                    "Cambio de estado posterior: " +
                    $"{ultimoCambio.FechaMovimiento!.Value:dd/MM/yyyy}"
                );
            }

            if (
                tieneModificacionPosterior
            )
            {
                var ultimaModificacion =
                    modificacionesPosteriores.Last();

                detalles.Add(
                    "Modificación posterior: " +
                    $"{ultimaModificacion.FechaMovimiento!.Value:dd/MM/yyyy}"
                );
            }

            if (
                apareceAreaPosterior
            )
            {
                string areasTexto =
                    string.Join(
                        ", ",
                        areasPosteriores.Select(
                            x =>
                            $"Área {x.Registro.NumeroAreaArchivo} " +
                            $"({x.Fecha!.Value:dd/MM/yyyy})"
                        )
                    );

                detalles.Add(
                    "Levantamiento posterior: " +
                    areasTexto
                );
            }

            if (
                apareceAreaMismoDia
            )
            {
                string areasTexto =
                    string.Join(
                        ", ",
                        areasMismoDia.Select(
                            x =>
                            $"Área {x.Registro.NumeroAreaArchivo}"
                        )
                    );

                detalles.Add(
                    "Levantamiento el mismo día de la baja: " +
                    areasTexto
                );
            }

            situacion_Ejemplos.Add(
                $"Placa: {placaSituacion}\n" +
                $"Cantidad de bajas: {grupoBajas.Count()}\n" +
                $"Última baja: {fechaUltimaBaja:dd/MM/yyyy}\n" +
                $"Motivo: {ultimaBaja.MotivoNormalizado}\n" +
                string.Join(
                    "\n",
                    detalles
                )
            );
        }
    }

    // ============================================
    // RESULTADOS
    // ============================================

    Console.WriteLine(
        $"Placas distintas con baja histórica: {situacion_PlacasConBaja}"
    );

    Console.WriteLine(
        $"Con una sola baja histórica: {situacion_ConUnaBaja}"
    );

    Console.WriteLine(
        $"Con múltiples bajas históricas: {situacion_ConMultiplesBajas}"
    );

    Console.WriteLine(
        $"Sin fecha de baja utilizable: {situacion_SinFechaBaja}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Sin señales posteriores a la última baja: {situacion_SinSenalesPosteriores}"
    );

    Console.WriteLine(
        $"Con al menos una señal posterior o del mismo día: {situacion_ConSenalesPosteriores}"
    );

    Console.WriteLine();

    Console.WriteLine(
        $"Con traslado posterior: {situacion_ConTrasladoPosterior}"
    );

    Console.WriteLine(
        $"Con cambio de estado posterior: {situacion_ConCambioEstadoPosterior}"
    );

    Console.WriteLine(
        $"Con modificación posterior: {situacion_ConModificacionPosterior}"
    );

    Console.WriteLine(
        $"Presentes en levantamiento posterior a la baja: {situacion_EnAreaPosterior}"
    );

    Console.WriteLine(
        $"Presentes en levantamiento el mismo día de la baja: {situacion_EnAreaMismoDia}"
    );

    Console.WriteLine();
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine(
        "   EJEMPLOS DE BAJAS CON SEÑALES POSTERIORES"
    );
    Console.WriteLine(
        "============================================"
    );
    Console.WriteLine();

    if (
        situacion_Ejemplos.Count == 0
    )
    {
        Console.WriteLine(
            "No se encontraron casos con señales posteriores."
        );
    }
    else
    {
        foreach (
            string ejemplo
            in situacion_Ejemplos
        )
        {
            Console.WriteLine(
                ejemplo
            );

            Console.WriteLine(
                "--------------------------------------------"
            );
        }
    }




    Console.WriteLine();

    Console.ForegroundColor =
        ConsoleColor.Green;

    Console.WriteLine(
        "Proceso de análisis completado."
    );

    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor =
        ConsoleColor.Red;

    Console.WriteLine(
        "Ocurrió un error:"
    );

    Console.ResetColor();

    Console.WriteLine(
        ex.Message
    );
}

Console.WriteLine();

Console.WriteLine(
    "Presione una tecla para finalizar..."
);

Console.ReadKey();


// ============================================
// FUNCIONES AUXILIARES
// ============================================

static bool EsFechaSinDato(
    string valor)
{
    if (string.IsNullOrWhiteSpace(valor))
        return true;

    string texto =
        valor.Trim().ToLowerInvariant();

    return texto == "ni" ||
           texto == "n/i" ||
           texto == "n.a" ||
           texto == "n/a" ||
           texto == "n";
}

static bool EsFechaParcial(
    string valor)
{
    if (string.IsNullOrWhiteSpace(valor))
        return false;

    string texto =
        valor.Trim();

    if (texto.Length == 4 &&
        int.TryParse(texto, out int anio) &&
        anio >= 1900 &&
        anio <= 2100)
    {
        return true;
    }

    return false;
}

static bool EsReferenciaHistorica(
    string valor)
{
    if (string.IsNullOrWhiteSpace(valor))
        return false;

    string texto =
        valor.Trim();

    return texto.StartsWith(
        "Ver ",
        StringComparison.OrdinalIgnoreCase
    );
}

static IEnumerable<List<T>> GroupByConsecutivos<T>(
    IEnumerable<T> elementos,
    Func<T, int> selector)
{
    var ordenados =
        elementos
            .OrderBy(selector)
            .ToList();

    if (ordenados.Count == 0)
    {
        yield break;
    }

    var grupoActual =
        new List<T>
        {
            ordenados[0]
        };

    for (int i = 1; i < ordenados.Count; i++)
    {
        int anterior =
            selector(
                ordenados[i - 1]
            );

        int actual =
            selector(
                ordenados[i]
            );

        if (actual == anterior + 1)
        {
            grupoActual.Add(
                ordenados[i]
            );
        }
        else
        {
            yield return grupoActual;

            grupoActual =
                new List<T>
                {
                    ordenados[i]
                };
        }
    }

    yield return grupoActual;
}




