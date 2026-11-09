using EscuelaInventario.Migracion.Modelos;
using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace EscuelaInventario.Migracion.Servicios;

public class ImportadorActivosMigracionService
{
    private readonly EscuelaInventarioContext _context;

    public ImportadorActivosMigracionService(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public Task ProbarImportacionAsync(List<ActivoFinalMigracion> activosFinales)
    {
        return EjecutarMigracionAsync(
            activosFinales,
            new List<MovimientoPreparadoMigracion>(),
            incluirMovimientos: false,
            confirmarCambios: false
        );
    }

    public Task ProbarImportacionCompletaAsync(
        List<ActivoFinalMigracion> activosFinales,
        List<MovimientoPreparadoMigracion> movimientosHistoricos)
    {
        return EjecutarMigracionAsync(
            activosFinales,
            movimientosHistoricos,
            incluirMovimientos: true,
            confirmarCambios: false
        );
    }

    public Task MigrarDefinitivamenteAsync(
        List<ActivoFinalMigracion> activosFinales,
        List<MovimientoPreparadoMigracion> movimientosHistoricos)
    {
        return EjecutarMigracionAsync(
            activosFinales,
            movimientosHistoricos,
            incluirMovimientos: true,
            confirmarCambios: true
        );
    }

    private async Task EjecutarMigracionAsync(
        List<ActivoFinalMigracion> activosFinales,
        List<MovimientoPreparadoMigracion> movimientosHistoricos,
        bool incluirMovimientos,
        bool confirmarCambios)
    {
        Console.WriteLine();
        Console.WriteLine("============================================");
        Console.WriteLine(
            incluirMovimientos
                ? (confirmarCambios
                    ? "   MIGRACIÓN DEFINITIVA DE INVENTARIO"
                    : "   PRUEBA INTEGRAL DE MIGRACIÓN")
                : "   PRUEBA TRANSACCIONAL DE IMPORTACIÓN"
        );
        Console.WriteLine("============================================");
        Console.WriteLine();

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        bool transaccionFinalizada = false;

        try
        {
            int activosAntes = await _context.Activos.CountAsync();
            int movimientosAntes = await _context.MovimientoInventarios.CountAsync();
            int registrosAntes = await _context.RegistroLibros.CountAsync();
            int areasAntes = await _context.Areas.CountAsync();

            if (
                activosAntes != 0 ||
                movimientosAntes != 0 ||
                registrosAntes != 0 ||
                areasAntes != 0
            )
            {
                throw new InvalidOperationException(
                    "La base ya contiene información operativa. " +
                    "La prueba de migración fue cancelada."
                );
            }

            DateTime ahora = DateTime.Now;

            var estados =
                await _context.EstadoActivos.ToListAsync();

            var situaciones =
                await _context.SituacionActivos.ToListAsync();

            var tiposMovimiento =
                await _context.TipoMovimientos.ToListAsync();

            var estadoPorNombre =
                estados.ToDictionary(
                    x => NormalizarClave(x.Nombre),
                    x => x
                );

            var tipoPorCodigo =
                tiposMovimiento.ToDictionary(
                    x => x.Codigo.Trim().ToUpperInvariant(),
                    x => x
                );

            var situacionActiva =
                situaciones.FirstOrDefault(
                    x =>
                    x.Codigo.Equals(
                        "ACTIVO",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                ?? throw new InvalidOperationException(
                    "No existe la situación ACTIVO."
                );

            var situacionBaja =
                situaciones.FirstOrDefault(
                    x =>
                    x.Codigo.Equals(
                        "BAJA",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                ?? throw new InvalidOperationException(
                    "No existe la situación BAJA."
                );

            string[] tiposNecesarios =
            {
                "INCLUSION",
                "TRASLADO",
                "BAJA",
                "CAMBIO_ESTADO",
                "MODIFICACION"
            };

            foreach (string codigo in tiposNecesarios)
            {
                if (!tipoPorCodigo.ContainsKey(codigo))
                {
                    throw new InvalidOperationException(
                        $"No existe el tipo de movimiento {codigo}."
                    );
                }
            }

            var areas =
                Enumerable
                    .Range(1, 71)
                    .Select(
                        numero =>
                            new Area
                            {
                                Numero = numero,
                                Nombre = null,
                                Activa = true,
                                Observaciones = null,
                                FechaCreacion = ahora,
                                FechaModificacion = null
                            }
                    )
                    .ToList();

            _context.Areas.AddRange(areas);
            await _context.SaveChangesAsync();

            var areaPorNumero =
                areas.ToDictionary(
                    x => x.Numero,
                    x => x
                );

            var modosExistentes =
                await _context
                    .Set<ModoAdquisicion>()
                    .ToListAsync();

            var modoPorClave =
                modosExistentes.ToDictionary(
                    x => NormalizarClave(x.Nombre),
                    x => x
                );

            var modosNecesarios =
                activosFinales
                    .Where(
                        x =>
                        !string.IsNullOrWhiteSpace(
                            x.ModoAdquisicionNormalizado
                        )
                    )
                    .Select(
                        x =>
                        CanonicalizarModo(
                            x.ModoAdquisicionNormalizado!
                        )
                    )
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
                    )
                    .OrderBy(x => x)
                    .ToList();

            foreach (string nombreModo in modosNecesarios)
            {
                string clave =
                    NormalizarClave(nombreModo);

                if (modoPorClave.ContainsKey(clave))
                {
                    continue;
                }

                var nuevoModo =
                    new ModoAdquisicion
                    {
                        Nombre = nombreModo,
                        Activo = true,
                        Observaciones =
                            "Creado durante la migración inicial " +
                            "a partir del catálogo oficial.",
                        FechaCreacion = ahora,
                        FechaModificacion = null
                    };

                _context
                    .Set<ModoAdquisicion>()
                    .Add(nuevoModo);

                modosExistentes.Add(nuevoModo);
                modoPorClave[clave] = nuevoModo;
            }

            await _context.SaveChangesAsync();

            var activosSql =
                new List<Activo>();

            int sinEstado = 0;
            int sinModo = 0;
            int sinArea = 0;

            foreach (var origen in activosFinales)
            {
                int? estadoActivoId = null;

                if (
                    !string.IsNullOrWhiteSpace(
                        origen.EstadoNormalizado
                    )
                )
                {
                    string claveEstado =
                        NormalizarClave(
                            origen.EstadoNormalizado
                        );

                    if (
                        !estadoPorNombre.TryGetValue(
                            claveEstado,
                            out var estado
                        )
                    )
                    {
                        throw new InvalidOperationException(
                            $"No existe el estado normalizado " +
                            $"'{origen.EstadoNormalizado}' " +
                            $"para la placa {origen.Placa}."
                        );
                    }

                    estadoActivoId = estado.Id;
                }
                else
                {
                    sinEstado++;
                }

                int? modoAdquisicionId = null;

                if (
                    !string.IsNullOrWhiteSpace(
                        origen.ModoAdquisicionNormalizado
                    )
                )
                {
                    string modoCanonico =
                        CanonicalizarModo(
                            origen.ModoAdquisicionNormalizado
                        );

                    string claveModo =
                        NormalizarClave(modoCanonico);

                    if (
                        !modoPorClave.TryGetValue(
                            claveModo,
                            out var modo
                        )
                    )
                    {
                        throw new InvalidOperationException(
                            $"No existe el modo de adquisición " +
                            $"'{modoCanonico}' " +
                            $"para la placa {origen.Placa}."
                        );
                    }

                    modoAdquisicionId = modo.Id;
                }
                else
                {
                    sinModo++;
                }

                int? areaActualId = null;

                if (origen.AreaActualNumero.HasValue)
                {
                    if (
                        !areaPorNumero.TryGetValue(
                            origen.AreaActualNumero.Value,
                            out var area
                        )
                    )
                    {
                        throw new InvalidOperationException(
                            $"No existe el Área " +
                            $"{origen.AreaActualNumero.Value} " +
                            $"para la placa {origen.Placa}."
                        );
                    }

                    areaActualId = area.Id;
                }
                else
                {
                    sinArea++;
                }

                int situacionActivoId =
                    origen.SituacionPreparada.Equals(
                        "DADO_DE_BAJA",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? situacionBaja.Id
                        : situacionActiva.Id;

                var activo =
                    new Activo
                    {
                        Placa = origen.Placa,
                        Descripcion = origen.Descripcion,
                        Marca = LimpiarTexto(origen.Marca),
                        Modelo = LimpiarTexto(origen.Modelo),
                        Serie = LimpiarTexto(origen.Serie),

                        FechaAdquisicion =
                            origen.FechaAdquisicionNormalizada
                                .HasValue
                                    ? DateOnly.FromDateTime(
                                        origen.FechaAdquisicionNormalizada
                                            .Value
                                    )
                                    : null,

                        Precio = origen.PrecioNormalizado,
                        EstadoActivoId = estadoActivoId,
                        SituacionActivoId = situacionActivoId,
                        AreaActualId = areaActualId,
                        ModoAdquisicionId = modoAdquisicionId,
                        UbicacionHistorica =
                            LimpiarTexto(
                                origen.UbicacionHistorica
                            ),
                        Observaciones =
                            ConstruirObservacionesActivo(origen),
                        FechaCreacion = ahora,
                        FechaModificacion = null
                    };

                activosSql.Add(activo);
            }

            int placasUnicas =
                activosSql
                    .Select(x => x.Placa)
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase
                    )
                    .Count();

            if (
                activosSql.Count != 5356 ||
                placasUnicas != 5356
            )
            {
                throw new InvalidOperationException(
                    $"La colección SQL no contiene exactamente " +
                    $"5356 activos únicos. " +
                    $"Total: {activosSql.Count}. " +
                    $"Únicos: {placasUnicas}."
                );
            }

            _context.Activos.AddRange(activosSql);
            await _context.SaveChangesAsync();

            var activoPorPlaca =
                activosSql.ToDictionary(
                    x => x.Placa,
                    x => x,
                    StringComparer.OrdinalIgnoreCase
                );

            if (!incluirMovimientos)
            {
                int activosInsertados =
                    await _context.Activos.CountAsync();

                int areasInsertadas =
                    await _context.Areas.CountAsync();

                int activosSituacionActivo =
                    await _context.Activos.CountAsync(
                        x =>
                        x.SituacionActivoId ==
                        situacionActiva.Id
                    );

                int activosSituacionBaja =
                    await _context.Activos.CountAsync(
                        x =>
                        x.SituacionActivoId ==
                        situacionBaja.Id
                    );

                int activosSinEstado =
                    await _context.Activos.CountAsync(
                        x =>
                        x.EstadoActivoId == null
                    );

                int activosSinArea =
                    await _context.Activos.CountAsync(
                        x =>
                        x.AreaActualId == null
                    );

                int activosSinModo =
                    await _context.Activos.CountAsync(
                        x =>
                        x.ModoAdquisicionId == null
                    );

                bool resultadoValido =
                    activosInsertados == 5356 &&
                    areasInsertadas == 71 &&
                    activosSituacionActivo == 4184 &&
                    activosSituacionBaja == 1172 &&
                    activosSinEstado == sinEstado &&
                    activosSinArea == sinArea &&
                    activosSinModo == sinModo;

                Console.WriteLine(
                    $"Áreas insertadas temporalmente: " +
                    $"{areasInsertadas}"
                );

                Console.WriteLine(
                    $"Activos insertados temporalmente: " +
                    $"{activosInsertados}"
                );

                Console.WriteLine();

                Console.WriteLine(
                    $"ACTIVO: {activosSituacionActivo}"
                );

                Console.WriteLine(
                    $"DADO_DE_BAJA: {activosSituacionBaja}"
                );

                Console.WriteLine();

                Console.WriteLine(
                    $"Sin estado: {activosSinEstado}"
                );

                Console.WriteLine(
                    $"Sin área oficial: {activosSinArea}"
                );

                Console.WriteLine(
                    $"Sin modo de adquisición normalizado: " +
                    $"{activosSinModo}"
                );

                Console.WriteLine();

                Console.WriteLine(
                    $"¿Prueba de activos válida?: " +
                    $"{(resultadoValido ? "SÍ" : "NO")}"
                );

                await transaction.RollbackAsync();
                transaccionFinalizada = true;

                Console.WriteLine();
                Console.WriteLine(
                    "ROLLBACK EJECUTADO INTENCIONALMENTE."
                );

                Console.WriteLine(
                    "No se conservaron cambios en SQL Server."
                );

                return;
            }

            if (movimientosHistoricos.Count != 5295)
            {
                throw new InvalidOperationException(
                    $"Se esperaban 5295 movimientos históricos " +
                    $"preparados y se recibieron " +
                    $"{movimientosHistoricos.Count}."
                );
            }

            int placasHistoricasNoEncontradas =
                movimientosHistoricos.Count(
                    x =>
                    !activoPorPlaca.ContainsKey(
                        x.Placa
                    )
                );

            if (placasHistoricasNoEncontradas != 0)
            {
                throw new InvalidOperationException(
                    $"Hay {placasHistoricasNoEncontradas} " +
                    $"movimientos históricos cuya placa " +
                    $"no existe entre los activos finales."
                );
            }

            var tipoInclusion =
                tipoPorCodigo["INCLUSION"];

            // ============================================================
            // RECONSTRUCCIÓN DEL ÁREA ORIGINAL DE INCLUSIÓN
            // ============================================================
            // Regla conservadora:
            // 1) Si el activo tiene traslados históricos, el área original
            //    será el ÁreaAnterior del PRIMER traslado conocido.
            // 2) Si nunca tuvo traslados, su área actual oficial puede
            //    tomarse como área de inclusión.
            // 3) Si existe traslado pero su primera área anterior no está
            //    determinada, NO se infiere desde el área actual: queda NULL.

            var primerosTrasladosPorPlaca =
                movimientosHistoricos
                    .Where(
                        x =>
                            x.TipoMovimientoCodigo
                                .Trim()
                                .Equals(
                                    "TRASLADO",
                                    StringComparison.OrdinalIgnoreCase
                                )
                    )
                    .GroupBy(
                        x => x.Placa,
                        StringComparer.OrdinalIgnoreCase
                    )
                    .ToDictionary(
                        g => g.Key,
                        g => g.First(),
                        StringComparer.OrdinalIgnoreCase
                    );

            var areaInclusionPorPlaca =
                new Dictionary<string, int?>(
                    StringComparer.OrdinalIgnoreCase
                );

            int areaInclusionDesdePrimerTraslado = 0;
            int areaInclusionDesdeAreaActual = 0;
            int areaInclusionSinEvidencia = 0;

            foreach (var fuente in activosFinales)
            {
                int? areaInclusionNumero;

                if (
                    primerosTrasladosPorPlaca.TryGetValue(
                        fuente.Placa,
                        out var primerTraslado
                    )
                )
                {
                    areaInclusionNumero =
                        primerTraslado.AreaAnteriorNumero;

                    if (areaInclusionNumero.HasValue)
                    {
                        areaInclusionDesdePrimerTraslado++;
                    }
                    else
                    {
                        areaInclusionSinEvidencia++;
                    }
                }
                else
                {
                    areaInclusionNumero =
                        fuente.RegularizadoArea71
                            ? fuente.AreaAntesRegularizacionArea71
                            : fuente.AreaActualNumero;

                    if (areaInclusionNumero.HasValue)
                    {
                        areaInclusionDesdeAreaActual++;
                    }
                    else
                    {
                        areaInclusionSinEvidencia++;
                    }
                }

                if (
                    areaInclusionNumero.HasValue &&
                    !areaPorNumero.ContainsKey(
                        areaInclusionNumero.Value
                    )
                )
                {
                    throw new InvalidOperationException(
                        $"El área original reconstruida " +
                        $"{areaInclusionNumero.Value} para la placa " +
                        $"{fuente.Placa} no existe entre las áreas 1-71."
                    );
                }

                areaInclusionPorPlaca[fuente.Placa] =
                    areaInclusionNumero;
            }

            if (areaInclusionPorPlaca.Count != 5356)
            {
                throw new InvalidOperationException(
                    $"Se esperaban áreas de inclusión evaluadas para " +
                    $"5356 activos y se obtuvieron " +
                    $"{areaInclusionPorPlaca.Count}."
                );
            }

            var movimientosInclusion =
                new List<(
                    ActivoFinalMigracion Fuente,
                    MovimientoInventario Movimiento
                )>();

            foreach (var fuente in activosFinales)
            {
                var activo =
                    activoPorPlaca[
                        fuente.Placa
                    ];

                var movimiento =
                    new MovimientoInventario
                    {
                        ActivoId = activo.Id,
                        TipoMovimientoId = tipoInclusion.Id,

                        FechaMovimiento =
                            fuente.FechaAdquisicionNormalizada
                                .HasValue
                                    ? DateOnly.FromDateTime(
                                        fuente.FechaAdquisicionNormalizada
                                            .Value
                                    )
                                    : null,

                        AreaAnteriorId = null,
                        AreaNuevaId =
                            ObtenerAreaId(
                                areaInclusionPorPlaca[fuente.Placa],
                                areaPorNumero,
                                fuente.Placa,
                                "original de inclusión"
                            ),
                        EstadoAnteriorId = null,
                        EstadoNuevoId = null,
                        SituacionAnteriorId = null,
                        SituacionNuevaId = null,
                        ResponsableId = null,
                        Motivo = "Inclusión histórica",
                        Observacion =
                            ConstruirObservacionInclusion(
                                fuente
                            ),
                        FechaCreacion = ahora
                    };

                _context
                    .MovimientoInventarios
                    .Add(movimiento);

                movimientosInclusion.Add(
                    (
                        fuente,
                        movimiento
                    )
                );
            }

            await _context.SaveChangesAsync();

            var registrosInclusion =
                new List<RegistroLibro>();

            foreach (var item in movimientosInclusion)
            {
                var fuente = item.Fuente;

                if (!fuente.RegistrarPosicionLibroInclusion)
                {
                    continue;
                }

                if (
                    !fuente.TomoOriginal.HasValue ||
                    !fuente.FolioOriginal.HasValue ||
                    !fuente.AsientoOriginal.HasValue
                )
                {
                    throw new InvalidOperationException(
                        $"La placa {fuente.Placa} está marcada " +
                        $"para registrar inclusión en libro, " +
                        $"pero no tiene Tomo/Folio/Asiento completos."
                    );
                }

                registrosInclusion.Add(
                    new RegistroLibro
                    {
                        MovimientoInventarioId =
                            item.Movimiento.Id,
                        Tomo =
                            fuente.TomoOriginal.Value,
                        Folio =
                            fuente.FolioOriginal.Value,
                        Asiento =
                            fuente.AsientoOriginal.Value,
                        EsPrincipal = true,
                        EsHistorico = true,
                        TextoOriginal =
                            ConstruirTextoOriginalInclusion(
                                fuente
                            ),
                        FechaCreacion = ahora
                    }
                );
            }

            if (registrosInclusion.Count != 5352)
            {
                throw new InvalidOperationException(
                    $"Se esperaban 5352 registros de libro " +
                    $"de inclusión y se prepararon " +
                    $"{registrosInclusion.Count}."
                );
            }

            var movimientosHistoricosSql =
                new List<(
                    MovimientoPreparadoMigracion Fuente,
                    MovimientoInventario Movimiento
                )>();

            foreach (var fuente in movimientosHistoricos)
            {
                var activo =
                    activoPorPlaca[
                        fuente.Placa
                    ];

                string codigoTipo =
                    fuente.TipoMovimientoCodigo
                        .Trim()
                        .ToUpperInvariant();

                if (
                    !tipoPorCodigo.TryGetValue(
                        codigoTipo,
                        out var tipoMovimiento
                    )
                )
                {
                    throw new InvalidOperationException(
                        $"No existe el tipo de movimiento " +
                        $"{codigoTipo} para la placa " +
                        $"{fuente.Placa}."
                    );
                }

                int? areaAnteriorId =
                    ObtenerAreaId(
                        fuente.AreaAnteriorNumero,
                        areaPorNumero,
                        fuente.Placa,
                        "anterior"
                    );

                int? areaNuevaId =
                    ObtenerAreaId(
                        fuente.AreaNuevaNumero,
                        areaPorNumero,
                        fuente.Placa,
                        "nueva"
                    );

                int? estadoAnteriorId =
                    ObtenerEstadoId(
                        fuente.EstadoAnteriorNombre,
                        estadoPorNombre,
                        fuente.Placa,
                        "anterior"
                    );

                int? estadoNuevoId =
                    ObtenerEstadoId(
                        fuente.EstadoNuevoNombre,
                        estadoPorNombre,
                        fuente.Placa,
                        "nuevo"
                    );

                int? situacionAnteriorId = null;
                int? situacionNuevaId = null;

                if (codigoTipo == "BAJA")
                {
                    situacionAnteriorId = situacionActiva.Id;
                    situacionNuevaId = situacionBaja.Id;
                }

                var movimiento =
                    new MovimientoInventario
                    {
                        ActivoId = activo.Id,
                        TipoMovimientoId =
                            tipoMovimiento.Id,

                        FechaMovimiento =
                            fuente.FechaMovimiento
                                .HasValue
                                    ? DateOnly.FromDateTime(
                                        fuente.FechaMovimiento.Value
                                    )
                                    : null,

                        AreaAnteriorId = areaAnteriorId,
                        AreaNuevaId = areaNuevaId,
                        EstadoAnteriorId = estadoAnteriorId,
                        EstadoNuevoId = estadoNuevoId,
                        SituacionAnteriorId =
                            situacionAnteriorId,
                        SituacionNuevaId =
                            situacionNuevaId,
                        ResponsableId = null,
                        Motivo =
                            LimpiarTexto(
                                fuente.Motivo
                            ),
                        Observacion =
                            ConstruirObservacionMovimientoHistorico(
                                fuente
                            ),
                        FechaCreacion = ahora
                    };

                _context
                    .MovimientoInventarios
                    .Add(movimiento);

                movimientosHistoricosSql.Add(
                    (
                        fuente,
                        movimiento
                    )
                );
            }

            await _context.SaveChangesAsync();

            var registrosHistoricosSql =
                new List<RegistroLibro>();

            foreach (var item in movimientosHistoricosSql)
            {
                if (!item.Fuente.RecibeRegistroLibro)
                {
                    continue;
                }

                foreach (
                    var posicion
                    in item.Fuente.PosicionesLibro
                )
                {
                    registrosHistoricosSql.Add(
                        new RegistroLibro
                        {
                            MovimientoInventarioId =
                                item.Movimiento.Id,
                            Tomo = posicion.Tomo,
                            Folio = posicion.Folio,
                            Asiento = posicion.Asiento,
                            EsPrincipal =
                                posicion.EsPrincipal,
                            EsHistorico = true,
                            TextoOriginal =
                                LimpiarTexto(
                                    posicion.TextoOriginal
                                ),
                            FechaCreacion = ahora
                        }
                    );
                }
            }

            if (registrosHistoricosSql.Count != 11244)
            {
                throw new InvalidOperationException(
                    $"Se esperaban 11244 posiciones históricas " +
                    $"y se prepararon " +
                    $"{registrosHistoricosSql.Count}."
                );
            }

            var todosRegistrosLibro =
                registrosInclusion
                    .Concat(registrosHistoricosSql)
                    .ToList();

            var posicionesDuplicadas =
                todosRegistrosLibro
                    .GroupBy(
                        x =>
                            $"{x.Tomo}/{x.Folio}/{x.Asiento}",
                        StringComparer.OrdinalIgnoreCase
                    )
                    .Where(g => g.Count() > 1)
                    .ToList();

            if (posicionesDuplicadas.Count != 0)
            {
                string ejemplo =
                    posicionesDuplicadas
                        .First()
                        .Key;

                throw new InvalidOperationException(
                    $"Se detectaron " +
                    $"{posicionesDuplicadas.Count} posiciones " +
                    $"de libro duplicadas antes de insertar. " +
                    $"Ejemplo: {ejemplo}."
                );
            }

            _context
                .RegistroLibros
                .AddRange(
                    todosRegistrosLibro
                );

            await _context.SaveChangesAsync();

            var configuracion =
                await _context
                    .ConfiguracionInventarios
                    .FirstOrDefaultAsync(
                        x => x.Id == 1
                    )
                ?? throw new InvalidOperationException(
                    "No existe la configuración general Id=1."
                );

            configuracion.PrefijoPlaca = "4615";
            configuracion.UltimoConsecutivoPlaca = 5376;
            configuracion.TomoActual = 5;
            configuracion.FolioActual = 810;
            configuracion.AsientoActual = 13;
            configuracion.AsientosPorFolio = 32;
            configuracion.FechaModificacion = ahora;

            await _context.SaveChangesAsync();

            int totalAreas =
                await _context.Areas.CountAsync();

            int totalActivos =
                await _context.Activos.CountAsync();

            int totalMovimientos =
                await _context
                    .MovimientoInventarios
                    .CountAsync();

            int totalRegistrosLibro =
                await _context
                    .RegistroLibros
                    .CountAsync();

            int totalInclusion =
                await _context
                    .MovimientoInventarios
                    .CountAsync(
                        x =>
                        x.TipoMovimientoId ==
                        tipoPorCodigo["INCLUSION"].Id
                    );

            int totalTraslado =
                await _context
                    .MovimientoInventarios
                    .CountAsync(
                        x =>
                        x.TipoMovimientoId ==
                        tipoPorCodigo["TRASLADO"].Id
                    );

            int totalBaja =
                await _context
                    .MovimientoInventarios
                    .CountAsync(
                        x =>
                        x.TipoMovimientoId ==
                        tipoPorCodigo["BAJA"].Id
                    );

            int totalCambioEstado =
                await _context
                    .MovimientoInventarios
                    .CountAsync(
                        x =>
                        x.TipoMovimientoId ==
                        tipoPorCodigo["CAMBIO_ESTADO"].Id
                    );

            int totalModificacion =
                await _context
                    .MovimientoInventarios
                    .CountAsync(
                        x =>
                        x.TipoMovimientoId ==
                        tipoPorCodigo["MODIFICACION"].Id
                    );

            int totalSinFechaHistorica =
                movimientosHistoricos.Count(
                    x =>
                    !x.FechaMovimiento.HasValue
                );

            int inclusionesConAreaOriginal =
                await _context
                    .MovimientoInventarios
                    .CountAsync(
                        x =>
                        x.TipoMovimientoId ==
                            tipoPorCodigo["INCLUSION"].Id &&
                        x.AreaNuevaId != null
                    );

            int inclusionesSinAreaOriginal =
                totalInclusion - inclusionesConAreaOriginal;

            bool reconstruccionAreaOriginalCorrecta =
                areaInclusionDesdePrimerTraslado +
                areaInclusionDesdeAreaActual +
                areaInclusionSinEvidencia == 5356 &&
                inclusionesConAreaOriginal ==
                    areaInclusionDesdePrimerTraslado +
                    areaInclusionDesdeAreaActual &&
                inclusionesSinAreaOriginal ==
                    areaInclusionSinEvidencia;

            bool configuracionCorrecta =
                    configuracion.PrefijoPlaca == "4615" &&
                    configuracion.UltimoConsecutivoPlaca == 5376 &&
                    configuracion.TomoActual == 5 &&
                    configuracion.FolioActual == 810 &&
                    configuracion.AsientoActual == 13 &&
                    configuracion.AsientosPorFolio == 32;

            bool resultadoCompletoValido =
                totalAreas == 71 &&
                totalActivos == 5356 &&
                 totalMovimientos == 10651 &&
                totalRegistrosLibro == 16596 &&
                 totalInclusion == 5356 &&
                 totalTraslado == 3819 &&
                 totalBaja == 1239 &&
                totalCambioEstado == 166 &&
                totalModificacion == 71 &&
                totalSinFechaHistorica == 24 &&
                reconstruccionAreaOriginalCorrecta &&
                 configuracionCorrecta;

            Console.WriteLine(
                $"Áreas: {totalAreas}"
            );

            Console.WriteLine(
                $"Activos: {totalActivos}"
            );

            Console.WriteLine();

            Console.WriteLine(
                $"INCLUSION: {totalInclusion}"
            );

            Console.WriteLine(
                $"TRASLADO: {totalTraslado}"
            );

            Console.WriteLine(
                $"BAJA: {totalBaja}"
            );

            Console.WriteLine(
                $"CAMBIO_ESTADO: {totalCambioEstado}"
            );

            Console.WriteLine(
                $"MODIFICACION: {totalModificacion}"
            );

            Console.WriteLine(
                $"TOTAL MovimientoInventario: " +
                $"{totalMovimientos}"
            );

            Console.WriteLine();

            Console.WriteLine(
                $"RegistroLibro inclusión: " +
                $"{registrosInclusion.Count}"
            );

            Console.WriteLine(
                $"RegistroLibro históricos: " +
                $"{registrosHistoricosSql.Count}"
            );

            Console.WriteLine(
                $"TOTAL RegistroLibro: " +
                $"{totalRegistrosLibro}"
            );

            Console.WriteLine();

            Console.WriteLine(
                $"Históricos sin fecha: " +
                $"{totalSinFechaHistorica}"
            );

            Console.WriteLine();

            Console.WriteLine(
                "Reconstrucción del área original:"
            );

            Console.WriteLine(
                $"  Desde primer traslado: " +
                $"{areaInclusionDesdePrimerTraslado}"
            );

            Console.WriteLine(
                $"  Desde área actual (sin traslados): " +
                $"{areaInclusionDesdeAreaActual}"
            );

            Console.WriteLine(
                $"  Sin evidencia suficiente: " +
                $"{areaInclusionSinEvidencia}"
            );

            Console.WriteLine(
                $"  INCLUSION con área original: " +
                $"{inclusionesConAreaOriginal}"
            );

            Console.WriteLine(
                $"  INCLUSION sin área original: " +
                $"{inclusionesSinAreaOriginal}"
            );

            Console.WriteLine(
                $"¿Reconstrucción coherente?: " +
                $"{(reconstruccionAreaOriginalCorrecta ? "SÍ" : "NO")}"
            );

            Console.WriteLine();

            Console.WriteLine(
                $"Configuración placa: " +
                $"{configuracion.PrefijoPlaca}-" +
                $"{configuracion.UltimoConsecutivoPlaca:D4}"
            );

            Console.WriteLine(
                $"Configuración libro: " +
                $"T{configuracion.TomoActual}/" +
                $"F{configuracion.FolioActual}/" +
                $"A{configuracion.AsientoActual}"
            );

            Console.WriteLine(
                $"Asientos por folio: " +
                $"{configuracion.AsientosPorFolio}"
            );

            Console.WriteLine();

            Console.WriteLine(
                $"¿Prueba integral válida?: " +
                $"{(resultadoCompletoValido ? "SÍ" : "NO")}"
            );

            if (!resultadoCompletoValido)
            {
                throw new InvalidOperationException(
                    "La prueba integral no alcanzó " +
                    "los conteos esperados."
                );
            }

            if (confirmarCambios)
            {
                await transaction.CommitAsync();
                transaccionFinalizada = true;

                _context.ChangeTracker.Clear();

                int activosConfirmados =
                    await _context.Activos.CountAsync();

                int movimientosConfirmados =
                    await _context.MovimientoInventarios.CountAsync();

                int registrosConfirmados =
                    await _context.RegistroLibros.CountAsync();

                int areasConfirmadas =
                    await _context.Areas.CountAsync();

                bool commitConfirmado =
                    activosConfirmados == 5356 &&
                    movimientosConfirmados == 10651 &&
                    registrosConfirmados == 16596 &&
                    areasConfirmadas == 71;

                Console.WriteLine();
                Console.WriteLine(
                    "COMMIT EJECUTADO. LOS CAMBIOS FUERON GUARDADOS."
                );

                Console.WriteLine();
                Console.WriteLine(
                    "Verificación posterior al COMMIT:"
                );
                Console.WriteLine(
                    $"  Áreas: {areasConfirmadas}"
                );
                Console.WriteLine(
                    $"  Activos: {activosConfirmados}"
                );
                Console.WriteLine(
                    $"  Movimientos: {movimientosConfirmados}"
                );
                Console.WriteLine(
                    $"  Registros de libro: {registrosConfirmados}"
                );
                Console.WriteLine(
                    $"¿COMMIT verificado?: " +
                    $"{(commitConfirmado ? "SÍ" : "NO")}"
                );

                if (!commitConfirmado)
                {
                    throw new InvalidOperationException(
                        "La transacción fue confirmada, pero la " +
                        "verificación posterior al COMMIT no coincide " +
                        "con los conteos esperados. No ejecute la " +
                        "migración nuevamente; revise la base actual."
                    );
                }
            }
            else
            {
                await transaction.RollbackAsync();
                transaccionFinalizada = true;

                Console.WriteLine();
                Console.WriteLine(
                    "ROLLBACK EJECUTADO INTENCIONALMENTE."
                );

                Console.WriteLine(
                    "La prueba integral fue válida, pero " +
                    "NO se conservaron cambios en SQL Server."
                );
            }
        }
        catch
        {
            if (!transaccionFinalizada)
            {
                await transaction.RollbackAsync();
                transaccionFinalizada = true;

                Console.WriteLine();
                Console.WriteLine(
                    "ERROR DURANTE LA MIGRACIÓN."
                );

                Console.WriteLine(
                    "ROLLBACK EJECUTADO. No se conservaron cambios."
                );
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine(
                    "ERROR DESPUÉS DE FINALIZAR LA TRANSACCIÓN."
                );
            }

            throw;
        }
    }

    private static int? ObtenerAreaId(
        int? numeroArea,
        Dictionary<int, Area> areaPorNumero,
        string placa,
        string etiqueta)
    {
        if (!numeroArea.HasValue)
        {
            return null;
        }

        if (
            !areaPorNumero.TryGetValue(
                numeroArea.Value,
                out var area
            )
        )
        {
            throw new InvalidOperationException(
                $"No existe el Área {numeroArea.Value} " +
                $"({etiqueta}) para la placa {placa}."
            );
        }

        return area.Id;
    }

    private static int? ObtenerEstadoId(
        string? nombre,
        Dictionary<string, EstadoActivo> estadoPorNombre,
        string placa,
        string etiqueta)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        string clave =
            NormalizarClave(nombre);

        if (
            !estadoPorNombre.TryGetValue(
                clave,
                out var estado
            )
        )
        {
            throw new InvalidOperationException(
                $"No existe el estado '{nombre}' " +
                $"({etiqueta}) para la placa {placa}."
            );
        }

        return estado.Id;
    }

    private static string? ConstruirObservacionesActivo(
        ActivoFinalMigracion activo)
    {
        var partes = new List<string>();

        if (
            !string.IsNullOrWhiteSpace(
                activo.ObservacionesOriginales
            )
        )
        {
            partes.Add(
                activo.ObservacionesOriginales.Trim()
            );
        }

        if (activo.RequiereRevision)
        {
            string motivos =
                activo.MotivosRevision.Count > 0
                    ? string.Join(
                        " | ",
                        activo.MotivosRevision
                    )
                    : "Registro de migración que requiere revisión.";

            partes.Add(
                "[MIGRACIÓN - REQUIERE REVISIÓN] " +
                motivos
            );
        }

        if (
            activo.PosicionesLibroConflictivas.Count > 0
        )
        {
            partes.Add(
                "[POSICIÓN DE LIBRO EN FUENTE] " +
                string.Join(
                    ", ",
                    activo.PosicionesLibroConflictivas
                )
            );
        }

        return partes.Count == 0
            ? null
            : string.Join(
                Environment.NewLine,
                partes
            );
    }

    private static string ConstruirObservacionInclusion(
        ActivoFinalMigracion activo)
    {
        var partes =
            new List<string>
            {
                "Inclusión histórica reconstruida durante " +
                "la migración inicial del inventario."
            };

        if (!activo.RegistrarPosicionLibroInclusion)
        {
            partes.Add(
                "No se creó RegistroLibro estructurado " +
                "porque la posición original presenta " +
                "un conflicto documentado en la fuente."
            );
        }

        if (
            activo.PosicionesLibroConflictivas.Count > 0
        )
        {
            partes.Add(
                "Posición(es) fuente: " +
                string.Join(
                    ", ",
                    activo.PosicionesLibroConflictivas
                )
            );
        }

        return string.Join(
            " ",
            partes
        );
    }

    private static string ConstruirTextoOriginalInclusion(
        ActivoFinalMigracion activo)
    {
        return
            $"Placa {activo.Placa}. " +
            $"Descripción: {activo.Descripcion}. " +
            $"Registro original de inclusión preservado " +
            $"durante la migración.";
    }

    private static string? ConstruirObservacionMovimientoHistorico(
        MovimientoPreparadoMigracion movimiento)
    {
        var partes = new List<string>();

        if (
            !string.IsNullOrWhiteSpace(
                movimiento.Observacion
            )
        )
        {
            partes.Add(
                movimiento.Observacion.Trim()
            );
        }

        if (
            !string.IsNullOrWhiteSpace(
                movimiento.UbicacionAnteriorOriginal
            ) ||
            !string.IsNullOrWhiteSpace(
                movimiento.UbicacionNuevaOriginal
            )
        )
        {
            partes.Add(
                "[UBICACIÓN ORIGINAL] " +
                $"{movimiento.UbicacionAnteriorOriginal ?? "[SIN DATO]"} " +
                "→ " +
                $"{movimiento.UbicacionNuevaOriginal ?? "[SIN DATO]"}"
            );
        }

        if (
            movimiento.EsAnotacionCompartida &&
            !movimiento.RecibeRegistroLibro
        )
        {
            partes.Add(
                "[ANOTACIÓN COMPARTIDA] La posición física " +
                "del libro se conserva únicamente en el " +
                "movimiento principal de la anotación."
            );
        }

        if (movimiento.RequiereRevision)
        {
            partes.Add(
                "[REQUIERE REVISIÓN] " +
                (
                    string.IsNullOrWhiteSpace(
                        movimiento.MotivoRevision
                    )
                        ? "Movimiento histórico marcado para revisión."
                        : movimiento.MotivoRevision.Trim()
                )
            );
        }

        return partes.Count == 0
            ? null
            : string.Join(
                Environment.NewLine,
                partes
            );
    }

    private static string CanonicalizarModo(
        string valor)
    {
        string clave =
            NormalizarClave(valor);

        return clave switch
        {
            "JUNTA PROTECCION SOCIAL" =>
                "Junta Protección Social",

            "JUNTA ADMINISTRATIVA" =>
                "Junta Administrativa",

            "LEY8283" or "LEY 8283" =>
                "Ley8283",

            "LEY7552" or "LEY 7552" =>
                "Ley7552",

            "DONACION" =>
                "Donación",

            "OTROS" =>
                "Otros",

            _ =>
                throw new InvalidOperationException(
                    $"Modo de adquisición normalizado no reconocido: " +
                    $"'{valor}'."
                )
        };
    }

    private static string NormalizarClave(
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return string.Empty;
        }

        string texto =
            valor.Trim()
                .ToUpperInvariant()
                .Normalize(
                    NormalizationForm.FormD
                );

        var builder = new StringBuilder();

        foreach (char caracter in texto)
        {
            var categoria =
                CharUnicodeInfo.GetUnicodeCategory(
                    caracter
                );

            if (
                categoria !=
                UnicodeCategory.NonSpacingMark
            )
            {
                builder.Append(caracter);
            }
        }

        return string.Join(
            " ",
            builder
                .ToString()
                .Normalize(
                    NormalizationForm.FormC
                )
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                )
        );
    }

    private static string? LimpiarTexto(
        string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        string limpio = valor.Trim();

        if (
            limpio.Equals(
                "NI",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return null;
        }

        return limpio;
    }
}
