using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.ModosAdquisicion;

public class EditModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public EditModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ModoAdquisicion ModoAdquisicion { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ModoAdquisicion = await _context.ModoAdquisicions
            .FirstOrDefaultAsync(m => m.Id == id);

        if (ModoAdquisicion == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Quitamos espacios al inicio y al final
        ModoAdquisicion.Nombre = ModoAdquisicion.Nombre.Trim();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var modoDb = await _context.ModoAdquisicions
            .FirstOrDefaultAsync(m => m.Id == ModoAdquisicion.Id);

        if (modoDb == null)
        {
            return NotFound();
        }

        // Evita duplicados ignorando mayúsculas/minúsculas
        bool nombreDuplicado = await _context.ModoAdquisicions
            .AnyAsync(m =>
                m.Id != ModoAdquisicion.Id &&
                m.Nombre.ToLower() == ModoAdquisicion.Nombre.ToLower());

        if (nombreDuplicado)
        {
            ModelState.AddModelError(
                "ModoAdquisicion.Nombre",
                "Ya existe otro modo de adquisición con ese nombre."
            );

            return Page();
        }

        modoDb.Nombre = ModoAdquisicion.Nombre;
        modoDb.Observaciones = ModoAdquisicion.Observaciones;
        modoDb.Activo = ModoAdquisicion.Activo;
        modoDb.FechaModificacion = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["MensajeExito"] =
            "Modo de adquisición actualizado correctamente.";

        return RedirectToPage("Index");
    }
}