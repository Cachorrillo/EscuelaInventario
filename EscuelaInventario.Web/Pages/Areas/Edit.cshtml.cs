using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Areas;

public class EditModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public EditModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Area Area { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Area = await _context.Areas
            .FirstOrDefaultAsync(a => a.Id == id);

        if (Area == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var areaDb = await _context.Areas
            .FirstOrDefaultAsync(a => a.Id == Area.Id);

        if (areaDb == null)
        {
            return NotFound();
        }

        areaDb.Nombre = Area.Nombre;
        areaDb.Observaciones = Area.Observaciones;
        areaDb.Activa = Area.Activa;
        areaDb.FechaModificacion = DateTime.Now;

        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = "Área actualizada correctamente.";

        return RedirectToPage("Index");
    }
}