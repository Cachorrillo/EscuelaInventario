using EscuelaInventario.Migracion.Modelos;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class NormalizadorInventarioService
{
    public void Normalizar(
        List<FilaInventario> registros)
    {
        foreach (var registro in registros)
        {
            registro.EstadoNormalizado =
                NormalizarEstado(
                    registro.EstadoOriginal
                );

            registro.ModoAdquisicionNormalizado =
                NormalizarModoAdquisicion(
                    registro.ModoAdquisicionOriginal
                );

            registro.PrecioNormalizado =
                NormalizarPrecio(
                    registro.PrecioOriginal
                );

            registro.FechaAdquisicionNormalizada =
                NormalizarFechaAdquisicion(
                    registro.FechaAdquisicionOriginal
                );
        }
    }

    private static string? NormalizarEstado(
        string estadoOriginal)
    {
        if (string.IsNullOrWhiteSpace(estadoOriginal))
            return null;

        string estado =
            estadoOriginal
                .Trim()
                .ToLowerInvariant();

        return estado switch
        {
            "excelente" => "Excelente",

            "bueno" => "Bueno",
            "bien" => "Bueno",
            "buena" => "Bueno",
            "nueva" => "Bueno",

            "regular" => "Regular",
            "regulular" => "Regular",

            "malo" => "Malo",
            "mala" => "Malo",

            _ => null
        };
    }

    private static string? NormalizarModoAdquisicion(
        string modoOriginal)
    {
        if (string.IsNullOrWhiteSpace(modoOriginal))
            return null;

        string modo =
            modoOriginal
                .Trim()
                .ToLowerInvariant();

        return modo switch
        {
            "junta protección social" =>
                "Junta Protección Social",

            "junta proteccion social" =>
                "Junta Protección Social",

            "jps" =>
                "Junta Protección Social",

            "j.p.s" =>
                "Junta Protección Social",

            "jsp" =>
                "Junta Protección Social",

            "junta administrativa" =>
                "Junta Administrativa",

            "ja" =>
                "Junta Administrativa",

            "j.a" =>
                "Junta Administrativa",

            "junta adm." =>
                "Junta Administrativa",

            "junta adm" =>
                "Junta Administrativa",

            "junta admnistrativa" =>
                "Junta Administrativa",

            "junta adminsitrativa" =>
                "Junta Administrativa",

            "junta administrativa (donación)" =>
                "Junta Administrativa",

            "junta administrativa (donacion)" =>
                "Junta Administrativa",

            "junta adm. (donacion)" =>
                "Junta Administrativa",

            "junta adm. (donación)" =>
                "Junta Administrativa",

            "junta adm.(donacion)" =>
                "Junta Administrativa",

            "junta  adm (donacion)" =>
                "Junta Administrativa",

            "ley 8283" =>
                "Ley 8283",

            "ley8283" =>
                "Ley 8283",

            "ley 7552" =>
                "Ley 7552",

            "ley7552" =>
                "Ley 7552",

            "donación" =>
                "Donación",

            "donacion" =>
                "Donación",

            "docación" =>
                "Donación",

            "donación de municipalidad" =>
                "Donación",

            "donacion de municipalidad" =>
                "Donación",

            "donacion de padres de familia" =>
                "Donación",

            "donación de padres de familia" =>
                "Donación",

            "otros" =>
                "Otros",

            "otro" =>
                "Otros",

            "fondos propios" =>
                "Otros",

            "gobierno de costa rica" =>
                "Otros",

            "poder judicial" =>
                "Otros",

            "venta de bienes y servicios" =>
                "Otros",

            "coopenae" =>
                "Otros",

            "esph" =>
                "Otros",

            "inbiopaque" =>
                "Otros",

            "prod, agroind, del caribe" =>
                "Otros",

            "ni" =>
                null,

            _ => null
        };
    }

    private static decimal? NormalizarPrecio(
        string precioOriginal)
    {
        if (string.IsNullOrWhiteSpace(precioOriginal))
            return null;

        string precio =
            precioOriginal
                .Trim()
                .ToLowerInvariant();

        if (precio == "ni" ||
            precio == ",ni" ||
            precio == "n" ||
            precio == "n>i" ||
            precio == "n/i" ||
            precio == "n.a" ||
            precio == "n/a" ||
            precio == "donación" ||
            precio == "donacion")
        {
            return null;
        }

        if (precio.StartsWith("ver "))
            return null;

        precio = precio
            .Replace("₡", "")
            .Replace("₵", "")
            .Replace("¢", "")
            .Replace("$", "");

        precio =
            Regex.Replace(precio, @"\s+", "");

        if (Regex.IsMatch(
            precio,
            @"^\d{1,3}(\.\d{3})+\.\d{2}$"))
        {
            int ultimoPunto =
                precio.LastIndexOf('.');

            string parteEntera =
                precio[..ultimoPunto]
                    .Replace(".", "");

            string parteDecimal =
                precio[(ultimoPunto + 1)..];

            string limpio =
                $"{parteEntera}.{parteDecimal}";

            if (decimal.TryParse(
                limpio,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal valorHistoricoPuntos))
            {
                return valorHistoricoPuntos;
            }
        }

        if (Regex.IsMatch(
            precio,
            @"^\d{1,3}(,\d{3})+,\d{1,2}$"))
        {
            int ultimaComa =
                precio.LastIndexOf(',');

            string parteEntera =
                precio[..ultimaComa]
                    .Replace(",", "");

            string parteDecimal =
                precio[(ultimaComa + 1)..];

            string limpio =
                $"{parteEntera}.{parteDecimal}";

            if (decimal.TryParse(
                limpio,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal valorHistoricoComas))
            {
                return valorHistoricoComas;
            }
        }

        if (Regex.IsMatch(
            precio,
            @"^\d{1,3}(\.\d{3})+(,\d{1,2})?$"))
        {
            string limpio =
                precio.Replace(".", "");

            limpio =
                limpio.Replace(",", ".");

            if (decimal.TryParse(
                limpio,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal valorCostaRica))
            {
                return valorCostaRica;
            }
        }

        if (Regex.IsMatch(
            precio,
            @"^\d{1,3}(,\d{3})+(\.\d{1,2})?$"))
        {
            string limpio =
                precio.Replace(",", "");

            if (decimal.TryParse(
                limpio,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal valorInternacional))
            {
                return valorInternacional;
            }
        }

        if (Regex.IsMatch(
            precio,
            @"^\d+,\d{1,2}$"))
        {
            string limpio =
                precio.Replace(",", ".");

            if (decimal.TryParse(
                limpio,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal valorDecimalComa))
            {
                return valorDecimalComa;
            }
        }

        if (Regex.IsMatch(
            precio,
            @"^\d+\.\d{1,2}$"))
        {
            if (decimal.TryParse(
                precio,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal valorDecimalPunto))
            {
                return valorDecimalPunto;
            }
        }

        if (Regex.IsMatch(
            precio,
            @"^\d+$"))
        {
            if (decimal.TryParse(
                precio,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out decimal valorEntero))
            {
                return valorEntero;
            }
        }

        return null;
    }

    private static DateTime? NormalizarFechaAdquisicion(
        string fechaOriginal)
    {
        if (string.IsNullOrWhiteSpace(fechaOriginal))
            return null;

        string fecha =
            fechaOriginal.Trim();

        string fechaMinuscula =
            fecha.ToLowerInvariant();

        // ============================================
        // SIN INFORMACIÓN
        // ============================================

        if (fechaMinuscula == "ni" ||
            fechaMinuscula == "n/i" ||
            fechaMinuscula == "n.a" ||
            fechaMinuscula == "n/a" ||
            fechaMinuscula == "n")
        {
            return null;
        }

        // ============================================
        // REFERENCIA HISTÓRICA
        // ============================================

        if (fechaMinuscula.StartsWith("ver "))
            return null;

        // ============================================
        // FECHA PARCIAL
        //
        // Ejemplo:
        // 2022
        //
        // No inventamos día ni mes.
        // ============================================

        if (Regex.IsMatch(
            fecha,
            @"^\d{4}$"))
        {
            return null;
        }

        // ============================================
        // FORMATO ESTADOUNIDENSE
        //
        // El Excel contiene muchos registros así:
        //
        // M/d/yyyy
        //
        // 7/31/2026  -> 31 julio 2026
        // 8/23/2024  -> 23 agosto 2024
        // 11/30/2023 -> 30 noviembre 2023
        // ============================================

        // ============================================
        // CORRECCIONES HISTÓRICAS DOCUMENTADAS
        //
        // Estas reglas corresponden exclusivamente a
        // valores encontrados en Inventario 2026.xlsx.
        // El valor original siempre se conserva en
        // FechaAdquisicionOriginal.
        // ============================================

        if (fecha == "/3/2024")
        {
            // Registros 4615-4300 a 4615-4307.
            // Ubicados entre 21/02/2024 y 04/04/2024.
            return new DateTime(2024, 3, 3);
        }

        if (fecha == "06/12/20204")
        {
            // Registros 4615-4655 a 4615-4661.
            // Ubicados entre 28/11/2024 y 09/12/2024.
            // Año con cero adicional.
            return new DateTime(2024, 12, 6);
        }

        if (fecha == "25/02025")
        {
            // Registro 4615-4757.
            // Corresponde al bloque del 25/02/2025.
            return new DateTime(2025, 2, 25);
        }

        if (fecha == "3107/2026")
        {
            // Registro 4615-5283.
            // Rodeado por registros del 31/07/2026.
            return new DateTime(2026, 7, 31);
        }

        string[] formatosEstadosUnidos =
        {
            "M/d/yyyy",
            "MM/dd/yyyy",
            "M/dd/yyyy",
            "MM/d/yyyy",

            "M/d/yy",
            "MM/dd/yy",
            "M/dd/yy",
            "MM/d/yy"
        };

        if (DateTime.TryParseExact(
            fecha,
            formatosEstadosUnidos,
            CultureInfo.GetCultureInfo("en-US"),
            DateTimeStyles.None,
            out DateTime fechaEstadosUnidos))
        {
            return fechaEstadosUnidos.Date;
        }

        // ============================================
        // FORMATO DÍA/MES/AÑO
        // ============================================

        string[] formatosCostaRica =
        {
            "d/M/yyyy",
            "dd/MM/yyyy",
            "d/MM/yyyy",
            "dd/M/yyyy",

            "d-M-yyyy",
            "dd-MM-yyyy",
            "d-MM-yyyy",
            "dd-M-yyyy",

            "d/M/yy",
            "dd/MM/yy",
            "d/MM/yy",
            "dd/M/yy",

            "d-M-yy",
            "dd-MM-yy",
            "d-MM-yy",
            "dd-M-yy",

            "yyyy-MM-dd"
        };

        if (DateTime.TryParseExact(
            fecha,
            formatosCostaRica,
            CultureInfo.GetCultureInfo("es-CR"),
            DateTimeStyles.None,
            out DateTime fechaCostaRica))
        {
            return fechaCostaRica.Date;
        }

        // ============================================
        // NO INTERPRETABLE CON SEGURIDAD
        // ============================================

        return null;
    }
}