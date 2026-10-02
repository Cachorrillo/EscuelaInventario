using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.ModosAdquisicion;

public class CreateModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public CreateModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    [BindProperty]
    public ModoAdquisicion ModoAdquisicion { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ModoAdquisicion.Nombre = ModoAdquisicion.Nombre.Trim();

        bool nombreExiste = await _context.ModoAdquisicions
            .AnyAsync(m => m.Nombre.ToLower() == ModoAdquisicion.Nombre.ToLower());

        if (nombreExiste)
        {
            ModelState.AddModelError(
                "ModoAdquisicion.Nombre",
                "Ya existe un modo de adquisición con ese nombre."
            );
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        ModoAdquisicion.Activo = true;

        _context.ModoAdquisicions.Add(ModoAdquisicion);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = "Modo de adquisición creado correctamente.";

        return RedirectToPage("Index");
    }
}