using EscuelaInventario.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EscuelaInventario.Web.Pages.Respaldos;

public class IndexModel : PageModel
{
    private readonly BackupService _backupService;

    public IndexModel(BackupService backupService)
    {
        _backupService = backupService;
    }

    [TempData(Key = "RespaldoMensajeExito")]
    public string? MensajeExito { get; set; }

    [TempData(Key = "RespaldoMensajeError")]
    public string? MensajeError { get; set; }

    [TempData(Key = "RespaldoNombreArchivo")]
    public string? NombreArchivo { get; set; }

    [TempData(Key = "RespaldoRuta")]
    public string? RutaRespaldo { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostCrearAsync()
    {
        try
        {
            var resultado =
                await _backupService.CrearBackupAsync();

            MensajeExito =
                "El respaldo de la base de datos se creó y verificó correctamente.";

            NombreArchivo =
                resultado.NombreArchivo;

            RutaRespaldo =
                resultado.RutaCompleta;
        }
        catch (Exception ex)
        {
            MensajeError =
                "No fue posible crear el respaldo. " +
                "Detalle: " +
                ex.Message;
        }

        return RedirectToPage();
    }
}