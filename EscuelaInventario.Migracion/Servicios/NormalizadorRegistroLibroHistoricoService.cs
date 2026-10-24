using EscuelaInventario.Migracion.Modelos;
using System.Text.RegularExpressions;

namespace EscuelaInventario.Migracion.Servicios;

public class NormalizadorRegistroLibroHistoricoService
{
    public void Normalizar(
        List<FilaHistoricaInventario> registros)
    {
        foreach (var registro in registros)
        {
            NormalizarRegistro(registro);
        }
    }

    private static void NormalizarRegistro(
        FilaHistoricaInventario registro)
    {
        registro.Tomo = null;
        registro.Folio = null;
        registro.Asiento = null;

        if (string.IsNullOrWhiteSpace(
            registro.RegistradoEnOriginal))
        {
            return;
        }

        string valor =
            registro.RegistradoEnOriginal.Trim();

        // ============================================
        // FORMATO NORMAL
        //
        // 5,312,1
        // 5, 312, 1
        // 5/312/1
        // 5-312-1
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
        // 5. 213, 1
        // ============================================

        var coincidenciaConPunto =
            Regex.Match(
                valor,
                @"^\s*(\d+)\s*\.\s*(\d+)\s*,\s*(\d+)\s*$"
            );

        if (coincidenciaConPunto.Success)
        {
            if (IntentarAsignar(
                registro,
                coincidenciaConPunto.Groups[1].Value,
                coincidenciaConPunto.Groups[2].Value,
                coincidenciaConPunto.Groups[3].Value))
            {
                return;
            }
        }

        // ============================================
        // FORMATO HISTÓRICO SIN SEGUNDO SEPARADOR
        //
        // Encontrado en el Excel:
        //
        // 5,776 1
        // 5,776 2
        // ...
        // 5,776 32
        //
        // Se interpreta como:
        //
        // Tomo 5
        // Folio 776
        // Asiento correspondiente
        // ============================================

        var coincidenciaSinSegundoSeparador =
            Regex.Match(
                valor,
                @"^\s*(\d+)\s*,\s*(\d+)\s+(\d+)\s*$"
            );

        if (coincidenciaSinSegundoSeparador.Success)
        {
            IntentarAsignar(
                registro,
                coincidenciaSinSegundoSeparador.Groups[1].Value,
                coincidenciaSinSegundoSeparador.Groups[2].Value,
                coincidenciaSinSegundoSeparador.Groups[3].Value
            );
        }
    }

    private static bool IntentarAsignar(
        FilaHistoricaInventario registro,
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

        if (tomo <= 0)
            return false;

        if (folio <= 0)
            return false;

        if (asiento <= 0)
            return false;

        if (asiento > 32)
            return false;

        registro.Tomo = tomo;
        registro.Folio = folio;
        registro.Asiento = asiento;

        return true;
    }
}