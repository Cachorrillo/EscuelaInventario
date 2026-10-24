using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class ArchivoAreaDetectado
{
    public string NombreArchivo { get; set; } = string.Empty;

    public string RutaCompleta { get; set; } = string.Empty;

    public int? NumeroAreaDetectado { get; set; }

    public bool RequiereRevision { get; set; }

    public string MotivoRevision { get; set; } = string.Empty;
}

public class DetectorArchivosAreaService
{
    private const string NombreInventarioGeneral =
        "Inventario 2026.xlsx";

    public List<ArchivoAreaDetectado> Detectar(
        string carpetaDatos)
    {
        var resultados =
            new List<ArchivoAreaDetectado>();

        if (!Directory.Exists(carpetaDatos))
        {
            throw new DirectoryNotFoundException(
                $"No existe la carpeta de datos: {carpetaDatos}"
            );
        }

        var archivos =
            Directory
                .GetFiles(
                    carpetaDatos,
                    "*.*",
                    SearchOption.TopDirectoryOnly
                )
                .Where(
                    ruta =>
                    {
                        string extension =
                            Path.GetExtension(ruta);

                        return
                            extension.Equals(
                                ".xlsx",
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            extension.Equals(
                                ".xls",
                                StringComparison.OrdinalIgnoreCase
                            );
                    }
                )
                .Where(
                    ruta =>
                    !Path.GetFileName(ruta).Equals(
                        NombreInventarioGeneral,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .OrderBy(
                    ruta =>
                    Path.GetFileName(ruta)
                )
                .ToList();

        foreach (var ruta in archivos)
        {
            string nombreArchivo =
                Path.GetFileName(ruta);

            var resultado =
                new ArchivoAreaDetectado
                {
                    NombreArchivo =
                        nombreArchivo,

                    RutaCompleta =
                        ruta
                };

            resultado.NumeroAreaDetectado =
                ExtraerNumeroArea(
                    nombreArchivo
                );

            EvaluarRevision(
                resultado
            );

            resultados.Add(
                resultado
            );
        }

        return resultados;
    }

    private static int? ExtraerNumeroArea(
        string nombreArchivo)
    {
        string nombreSinExtension =
            Path.GetFileNameWithoutExtension(
                nombreArchivo
            );

        var coincidencias =
            Regex.Matches(
                nombreSinExtension,
                @"(?<!\d)(\d{1,3})(?!\d)"
            );

        if (coincidencias.Count == 0)
        {
            return null;
        }

        foreach (Match coincidencia in coincidencias)
        {
            if (
                int.TryParse(
                    coincidencia.Value,
                    out int numero
                )
                &&
                numero >= 1
                &&
                numero <= 70
            )
            {
                return numero;
            }
        }

        return null;
    }

    private static void EvaluarRevision(
        ArchivoAreaDetectado archivo)
    {
        var motivos =
            new List<string>();

        if (
            !archivo.NumeroAreaDetectado.HasValue
        )
        {
            motivos.Add(
                "No se pudo identificar automáticamente un número de área entre 1 y 70 en el nombre del archivo."
            );
        }

        archivo.RequiereRevision =
            motivos.Count > 0;

        archivo.MotivoRevision =
            string.Join(
                " ",
                motivos
            );
    }
}