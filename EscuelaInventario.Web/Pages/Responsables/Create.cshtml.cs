using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EscuelaInventario.Web.Pages.Responsables;

public class CreateModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public CreateModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Responsable Responsable { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Responsable.Nombre = Responsable.Nombre.Trim();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Responsable.Activo = true;

        _context.Responsables.Add(Responsable);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] =
            "Responsable creado correctamente.";

        return RedirectToPage("Index");
    }
}