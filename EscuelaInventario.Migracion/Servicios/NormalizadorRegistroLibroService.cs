using EscuelaInventario.Migracion.Modelos;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class NormalizadorRegistroLibroService
{
    public void Normalizar(
        List<FilaInventario> registros)
    {
        foreach (var registro in registros)
        {
            NormalizarRegistro(registro);
        }
    }

    private static void NormalizarRegistro(
        FilaInventario registro)
    {
        registro.Tomo = null;
        registro.Folio = null;
        registro.Asiento = null;

        if (string.IsNullOrWhiteSpace(
            registro.RegistradoEn))
        {
            return;
        }

        string valor =
            registro.RegistradoEn.Trim();

        // ============================================
        // FORMATO NORMAL
        //
        // Ejemplos:
        //
        // 5,246,30
        // 5/246/30
        // 5-246-30
        // ============================================

        var coincidenciaNormal =
            Regex.Match(
                valor,
                @"^\s*(\d+)\s*[,/-]\s*(\d+)\s*[,/-]\s*(\d+)\s*$"
            );

        if (coincidenciaNormal.Success)
        {
            if (IntentarAsignar(
                registro,
                coincidenciaNormal.Groups[1].Value,
                coincidenciaNormal.Groups[2].Value,
                coincidenciaNormal.Groups[3].Value))
            {
                return;
            }
        }

        // ============================================
        // FORMATO HISTÓRICO CON PUNTO
        //
        // Encontrado en el Excel:
        //
        // 5. 213, 1
        // 5. 213, 2
        // ...
        // 5. 213, 32
        //
        // Se interpreta como:
        //
        // Tomo 5
        // Folio 213
        // Asiento correspondiente
        // ============================================

        var coincidenciaConPunto =
            Regex.Match(
                valor,
                @"^\s*(\d+)\s*\.\s*(\d+)\s*,\s*(\d+)\s*$"
            );

        if (coincidenciaConPunto.Success)
        {
            IntentarAsignar(
                registro,
                coincidenciaConPunto.Groups[1].Value,
                coincidenciaConPunto.Groups[2].Value,
                coincidenciaConPunto.Groups[3].Value
            );
        }
    }

    private static bool IntentarAsignar(
        FilaInventario registro,
        string tomoTexto,
        string folioTexto,
        string asientoTexto)
    {
        if (!int.TryParse(
            tomoTexto,
            out int tomo))
        {
            return false;
        }

        if (!int.TryParse(
            folioTexto,
            out int folio))
        {
            return false;
        }

        if (!int.TryParse(
            asientoTexto,
            out int asiento))
        {
            return false;
        }

        // ============================================
        // VALIDACIONES
        // ============================================

        if (tomo <= 0)
            return false;

        if (folio <= 0)
            return false;

        if (asiento <= 0)
            return false;

        // Regla institucional:
        // cada folio posee hasta 32 asientos.
        if (asiento > 32)
            return false;

        registro.Tomo = tomo;
        registro.Folio = folio;
        registro.Asiento = asiento;

        return true;
    }
}