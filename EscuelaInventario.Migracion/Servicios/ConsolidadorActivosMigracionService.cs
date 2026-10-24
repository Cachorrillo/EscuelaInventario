using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class ConsolidadorActivosMigracionService
{
    private static readonly HashSet<string>
        PlacasConPosicionLibroConflictiva =
            new(
                new[]
                {
                    "4615-0793",
                    "4615-0800",
                    "4615-4472",
                    "4615-4474"
                },
                StringComparer.OrdinalIgnoreCase
            );

    public List<ActivoFinalMigracion> Consolidar(
        List<ActivoPreparadoMigracion> activosPreparados,
        List<ActivoHistoricoReconstruido> activosHistoricosReconstruidos)
    {
        var resultado =
            new List<ActivoFinalMigracion>();

        // ========================================================
        // 1. ACTIVOS DEL INVENTARIO GENERAL
        // ========================================================

        var gruposActuales =
            activosPreparados
                .GroupBy(
                    x => x.Placa,
                    StringComparer.OrdinalIgnoreCase
                )
                .OrderBy(
                    x => x.Key
                )
                .ToList();

        foreach (
            var grupo
            in gruposActuales
        )
        {
            // ====================================================
            // CASO CONTROLADO: 4615-4678
            //
            // La encargada confirmó que la placa duplicada
            // corresponde a un error del archivo.
            //
            // Ambas filas representan el mismo activo.
            //
            // Se conserva un solo Activo final.
            // No se decide entre Donación y JPS.
            // Tampoco se decide entre las dos posiciones de libro.
            // ====================================================

            if (
                grupo.Key.Equals(
                    "4615-4678",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                AgregarDuplicado4678(
                    resultado,
                    grupo.ToList()
                );

                continue;
            }

            // Fuera del caso 4678, ninguna placa debería
            // seguir apareciendo más de una vez.

            if (grupo.Count() != 1)
            {
                throw new InvalidOperationException(
                    $"La placa {grupo.Key} aparece " +
                    $"{grupo.Count()} veces después de la preparación " +
                    "y no posee una regla de consolidación definida."
                );
            }

            var activo =
                grupo.Single();

            resultado.Add(
                ConvertirActivoActual(
                    activo
                )
            );
        }

        // ========================================================
        // 2. ACTIVOS HISTÓRICOS RECONSTRUIDOS
        // ========================================================

        foreach (
            var historico
            in activosHistoricosReconstruidos
        )
        {
            resultado.Add(
                ConvertirActivoHistorico(
                    historico
                )
            );
        }

        // ========================================================
        // 3. VALIDACIÓN FINAL DE PLACAS
        // ========================================================

        var placasDuplicadas =
            resultado
                .GroupBy(
                    x => x.Placa,
                    StringComparer.OrdinalIgnoreCase
                )
                .Where(
                    x => x.Count() > 1
                )
                .Select(
                    x => x.Key
                )
                .ToList();

        if (placasDuplicadas.Count > 0)
        {
            throw new InvalidOperationException(
                "Después de la consolidación todavía existen " +
                "placas duplicadas: " +
                string.Join(
                    ", ",
                    placasDuplicadas
                )
            );
        }

        return resultado;
    }

    private static ActivoFinalMigracion ConvertirActivoActual(
        ActivoPreparadoMigracion activo)
    {
        bool posicionConflictiva =
            PlacasConPosicionLibroConflictiva.Contains(
                activo.Placa
            );

        var motivos =
            activo.MotivosRevision
                .ToList();

        var posicionesConflictivas =
            new List<string>();

        if (posicionConflictiva)
        {
            string posicion =
                FormatearPosicion(
                    activo.Tomo,
                    activo.Folio,
                    activo.Asiento
                );

            posicionesConflictivas.Add(
                posicion
            );

            AgregarMotivo(
                motivos,
                "La posición de libro de inclusión proviene de una " +
                "inconsistencia del archivo fuente. El activo se migra, " +
                "pero esa posición no se insertará como RegistroLibro " +
                "estructurado para evitar asignarla arbitrariamente."
            );
        }

        return new ActivoFinalMigracion
        {
            Placa =
                activo.Placa,

            FilasFuente =
                new List<int>
                {
                    activo.NumeroFilaExcel
                },

            Descripcion =
                activo.Descripcion,

            Marca =
                activo.Marca,

            Modelo =
                activo.Modelo,

            Serie =
                activo.Serie,

            EstadoOriginal =
                activo.EstadoOriginal,

            EstadoNormalizado =
                activo.EstadoNormalizado,

            SituacionPreparada =
                activo.SituacionPreparada,

            ClasificacionSituacion =
                activo.ClasificacionSituacion,

            UbicacionOriginal =
                activo.UbicacionOriginal,

            AreaActualNumero =
                activo.AreaActualNumero,

            UbicacionHistorica =
                activo.UbicacionHistorica,

            FuenteAreaSeleccionada =
                activo.FuenteAreaSeleccionada,

            ModoAdquisicionOriginal =
                activo.ModoAdquisicionOriginal,

            ModoAdquisicionNormalizado =
                activo.ModoAdquisicionNormalizado,

            PrecioOriginal =
                activo.PrecioOriginal,

            PrecioNormalizado =
                activo.PrecioNormalizado,

            FechaAdquisicionOriginal =
                activo.FechaAdquisicionOriginal,

            FechaAdquisicionNormalizada =
                activo.FechaAdquisicionNormalizada,

            ObservacionesOriginales =
                activo.ObservacionesOriginales,

            TomoOriginal =
                activo.Tomo,

            FolioOriginal =
                activo.Folio,

            AsientoOriginal =
                activo.Asiento,

            RegistrarPosicionLibroInclusion =
                !posicionConflictiva,

            PosicionesLibroConflictivas =
                posicionesConflictivas,

            RequiereRevision =
                activo.RequiereRevision ||
                posicionConflictiva,

            MotivosRevision =
                motivos,

            OrigenMigracion =
                "INVENTARIO_GENERAL"
        };
    }

    private static ActivoFinalMigracion ConvertirActivoHistorico(
        ActivoHistoricoReconstruido activo)
    {
        var motivos =
            new List<string>();

        if (
            !string.IsNullOrWhiteSpace(
                activo.MotivoRevision
            )
        )
        {
            motivos.Add(
                activo.MotivoRevision
            );
        }

        return new ActivoFinalMigracion
        {
            Placa =
                activo.Placa,

            FilasFuente =
                new List<int>
                {
                    activo.NumeroFilaExcel
                },

            Descripcion =
                activo.Descripcion,

            Marca =
                activo.Marca,

            Modelo =
                activo.Modelo,

            Serie =
                activo.Serie,

            EstadoOriginal =
                activo.EstadoOriginal,

            EstadoNormalizado =
                activo.EstadoNormalizado,

            SituacionPreparada =
                activo.SituacionPreparada,

            ClasificacionSituacion =
                activo.ClasificacionSituacion,

            UbicacionOriginal =
                activo.UbicacionOriginal,

            AreaActualNumero =
                activo.AreaActualNumero,

            UbicacionHistorica =
                activo.UbicacionHistorica,

            FuenteAreaSeleccionada =
                activo.FuenteAreaSeleccionada,

            ModoAdquisicionOriginal =
                activo.ModoAdquisicionOriginal,

            ModoAdquisicionNormalizado =
                activo.ModoAdquisicionNormalizado,

            PrecioOriginal =
                activo.PrecioOriginal,

            PrecioNormalizado =
                activo.PrecioNormalizado,

            FechaAdquisicionOriginal =
                activo.FechaAdquisicionOriginal,

            FechaAdquisicionNormalizada =
                activo.FechaAdquisicionNormalizada,

            ObservacionesOriginales =
                activo.ObservacionesOriginales,

            TomoOriginal =
                activo.TomoOriginal,

            FolioOriginal =
                activo.FolioOriginal,

            AsientoOriginal =
                activo.AsientoOriginal,

            RegistrarPosicionLibroInclusion =
                true,

            RequiereRevision =
                activo.RequiereRevision,

            MotivosRevision =
                motivos,

            OrigenMigracion =
                activo.OrigenReconstruccion
        };
    }

    private static void AgregarDuplicado4678(
        List<ActivoFinalMigracion> resultado,
        List<ActivoPreparadoMigracion> filas)
    {
        if (filas.Count != 2)
        {
            throw new InvalidOperationException(
                "Se esperaban exactamente dos filas para la placa " +
                "4615-4678, pero se encontraron " +
                filas.Count +
                "."
            );
        }

        var fila1 =
            filas
                .OrderBy(
                    x => x.NumeroFilaExcel
                )
                .First();

        var fila2 =
            filas
                .OrderBy(
                    x => x.NumeroFilaExcel
                )
                .Last();

        var motivos =
            new List<string>();

        AgregarMotivo(
            motivos,
            "La placa 4615-4678 aparece dos veces en el archivo " +
            $"de corte, en las filas {fila1.NumeroFilaExcel} y " +
            $"{fila2.NumeroFilaExcel}. La encargada confirmó que " +
            "la duplicación corresponde a un error del archivo."
        );

        AgregarMotivo(
            motivos,
            "Las dos filas coinciden en placa, descripción, activo, " +
            "estado, ubicación y fecha de adquisición, pero difieren " +
            "en modo de adquisición y posición de libro."
        );

        AgregarMotivo(
            motivos,
            "No existe evidencia suficiente para elegir entre " +
            "'Donación' y 'Junta Protección Social'. El modo de " +
            "adquisición se migra como NULL y se conservan ambos " +
            "valores como evidencia de la inconsistencia."
        );

        AgregarMotivo(
            motivos,
            "No existe evidencia suficiente para elegir entre " +
            "T5/F737/A6 y T5/F748/A7. Ninguna de las dos posiciones " +
            "se insertará como RegistroLibro de inclusión."
        );

        resultado.Add(
            new ActivoFinalMigracion
            {
                Placa =
                    "4615-4678",

                FilasFuente =
                    filas
                        .Select(
                            x => x.NumeroFilaExcel
                        )
                        .OrderBy(
                            x => x
                        )
                        .ToList(),

                Descripcion =
                    fila1.Descripcion,

                Marca =
                    "Telstar",

                Modelo =
                    fila1.Modelo,

                Serie =
                    fila1.Serie,

                EstadoOriginal =
                    fila1.EstadoOriginal,

                EstadoNormalizado =
                    fila1.EstadoNormalizado,

                SituacionPreparada =
                    "ACTIVO",

                ClasificacionSituacion =
                    "ACTIVO_DUPLICADO_FUENTE_CONSOLIDADO",

                UbicacionOriginal =
                    "AREA 4",

                AreaActualNumero =
                    4,

                UbicacionHistorica =
                    null,

                FuenteAreaSeleccionada =
                    "DUPLICADO_FUENTE_AREA_COMUN",

                ModoAdquisicionOriginal =
                    "DONACIÓN / JPS (CONFLICTO DE FUENTE)",

                ModoAdquisicionNormalizado =
                    null,

                PrecioOriginal =
                    fila1.PrecioOriginal,

                PrecioNormalizado =
                    fila1.PrecioNormalizado,

                FechaAdquisicionOriginal =
                    fila1.FechaAdquisicionOriginal,

                FechaAdquisicionNormalizada =
                    fila1.FechaAdquisicionNormalizada,

                ObservacionesOriginales =
                    "Registro duplicado en el archivo fuente. " +
                    "Fila " +
                    fila1.NumeroFilaExcel +
                    ": modo Donación, T5/F737/A6. " +
                    "Fila " +
                    fila2.NumeroFilaExcel +
                    ": modo JPS, T5/F748/A7.",

                TomoOriginal =
                    null,

                FolioOriginal =
                    null,

                AsientoOriginal =
                    null,

                RegistrarPosicionLibroInclusion =
                    false,

                PosicionesLibroConflictivas =
                    new List<string>
                    {
                        "T5/F737/A6",
                        "T5/F748/A7"
                    },

                RequiereRevision =
                    true,

                MotivosRevision =
                    motivos,

                OrigenMigracion =
                    "INVENTARIO_GENERAL_DUPLICADO_CONSOLIDADO"
            }
        );
    }

    private static string FormatearPosicion(
        int? tomo,
        int? folio,
        int? asiento)
    {
        if (
            !tomo.HasValue ||
            !folio.HasValue ||
            !asiento.HasValue
        )
        {
            return "[POSICIÓN INCOMPLETA]";
        }

        return
            $"T{tomo.Value}/" +
            $"F{folio.Value}/" +
            $"A{asiento.Value}";
    }

    private static void AgregarMotivo(
        List<string> motivos,
        string motivo)
    {
        if (
            motivos.Any(
                x =>
                x.Equals(
                    motivo,
                    StringComparison.OrdinalIgnoreCase
                )
            )
        )
        {
            return;
        }

        motivos.Add(
            motivo
        );
    }
}