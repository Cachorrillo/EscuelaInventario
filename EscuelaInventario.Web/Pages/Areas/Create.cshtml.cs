using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Areas;

public class CreateModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public CreateModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Area Area { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Validación manual: evitar números de área duplicados
        bool numeroExiste = await _context.Areas
            .AnyAsync(a => a.Numero == Area.Numero);

        if (numeroExiste)
        {
            ModelState.AddModelError(
                "Area.Numero",
                "Ya existe un área con ese número."
            );
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Area.Activa = true;

        _context.Areas.Add(Area);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = "Área creada correctamente.";

        return RedirectToPage("Index");
    }
}