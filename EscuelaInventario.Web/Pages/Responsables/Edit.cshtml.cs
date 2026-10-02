using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Responsables;

public class EditModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public EditModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Responsable Responsable { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Responsable = await _context.Responsables
            .FirstOrDefaultAsync(r => r.Id == id);

        if (Responsable == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Responsable.Nombre = Responsable.Nombre.Trim();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var responsableDb = await _context.Responsables
            .FirstOrDefaultAsync(r => r.Id == Responsable.Id);

        if (responsableDb == null)
        {
            return NotFound();
        }

        responsableDb.Nombre = Responsable.Nombre;
        responsableDb.Observaciones = Responsable.Observaciones;
        responsableDb.Activo = Responsable.Activo;
        responsableDb.FechaModificacion = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["MensajeExito"] =
            "Responsable actualizado correctamente.";

        return RedirectToPage("Index");
    }
}