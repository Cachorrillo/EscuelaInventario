using EscuelaInventario.Migracion.Modelos;

namespace EscuelaInventario.Migracion.Servicios;

public class ValidadorInventarioService
{
    public List<IncidenciaMigracion> Validar(
        List<FilaInventario> registros)
    {
        var incidencias =
            new List<IncidenciaMigracion>();

        DetectarPlacasDuplicadas(
            registros,
            incidencias
        );

        DetectarEstados(
            registros,
            incidencias
        );

        DetectarModosAdquisicion(
            registros,
            incidencias
        );

        return incidencias;
    }

    private static void DetectarPlacasDuplicadas(
        List<FilaInventario> registros,
        List<IncidenciaMigracion> incidencias)
    {
        var gruposDuplicados = registros
            .GroupBy(
                x => x.Placa,
                StringComparer.OrdinalIgnoreCase
            )
            .Where(g => g.Count() > 1);

        foreach (var grupo in gruposDuplicados)
        {
            var filas = grupo
                .Select(x => x.NumeroFilaExcel)
                .OrderBy(x => x)
                .ToList();

            string filasTexto =
                string.Join(", ", filas);

            foreach (var registro in grupo)
            {
                incidencias.Add(
                    new IncidenciaMigracion
                    {
                        Tipo = "PLACA_DUPLICADA",
                        Severidad = "ERROR",
                        Placa = registro.Placa,
                        NumeroFilaExcel =
                            registro.NumeroFilaExcel,

                        Mensaje =
                            $"La placa {registro.Placa} " +
                            $"aparece más de una vez. " +
                            $"Filas involucradas: {filasTexto}."
                    }
                );
            }
        }
    }

    private static void DetectarEstados(
        List<FilaInventario> registros,
        List<IncidenciaMigracion> incidencias)
    {
        foreach (var registro in registros)
        {
            string estadoOriginal =
                registro.EstadoOriginal.Trim();

            if (string.IsNullOrWhiteSpace(estadoOriginal))
            {
                incidencias.Add(
                    new IncidenciaMigracion
                    {
                        Tipo = "ESTADO_VACIO",
                        Severidad = "ADVERTENCIA",
                        Placa = registro.Placa,
                        NumeroFilaExcel =
                            registro.NumeroFilaExcel,

                        Mensaje =
                            "El activo no tiene estado registrado " +
                            "y requiere revisión manual."
                    }
                );

                continue;
            }

            if (string.IsNullOrWhiteSpace(
                registro.EstadoNormalizado))
            {
                incidencias.Add(
                    new IncidenciaMigracion
                    {
                        Tipo = "ESTADO_NO_RESUELTO",
                        Severidad = "ADVERTENCIA",
                        Placa = registro.Placa,
                        NumeroFilaExcel =
                            registro.NumeroFilaExcel,

                        Mensaje =
                            $"No existe una regla de normalización " +
                            $"para el estado original " +
                            $"'{estadoOriginal}'."
                    }
                );
            }
        }
    }

    private static void DetectarModosAdquisicion(
        List<FilaInventario> registros,
        List<IncidenciaMigracion> incidencias)
    {
        foreach (var registro in registros)
        {
            if (!string.IsNullOrWhiteSpace(
                registro.ModoAdquisicionNormalizado))
            {
                continue;
            }

            string modoOriginal =
                registro.ModoAdquisicionOriginal.Trim();

            if (string.IsNullOrWhiteSpace(modoOriginal))
            {
                incidencias.Add(
                    new IncidenciaMigracion
                    {
                        Tipo = "MODO_ADQUISICION_VACIO",
                        Severidad = "ADVERTENCIA",
                        Placa = registro.Placa,
                        NumeroFilaExcel =
                            registro.NumeroFilaExcel,

                        Mensaje =
                            "El activo no tiene modo de adquisición registrado."
                    }
                );

                continue;
            }

            if (string.Equals(
                modoOriginal,
                "NI",
                StringComparison.OrdinalIgnoreCase))
            {
                incidencias.Add(
                    new IncidenciaMigracion
                    {
                        Tipo = "MODO_ADQUISICION_SIN_INFORMACION",
                        Severidad = "ADVERTENCIA",
                        Placa = registro.Placa,
                        NumeroFilaExcel =
                            registro.NumeroFilaExcel,

                        Mensaje =
                            "El modo de adquisición está registrado como 'NI'. " +
                            "Se conservará como dato histórico sin inventar una categoría."
                    }
                );

                continue;
            }

            incidencias.Add(
                new IncidenciaMigracion
                {
                    Tipo = "MODO_ADQUISICION_NO_RESUELTO",
                    Severidad = "ADVERTENCIA",
                    Placa = registro.Placa,
                    NumeroFilaExcel =
                        registro.NumeroFilaExcel,

                    Mensaje =
                        $"No existe una regla de normalización para " +
                        $"el modo de adquisición original '{modoOriginal}'."
                }
            );
        }
    }
}