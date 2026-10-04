using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Areas;

public class DetailsModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public DetailsModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public Area Area { get; set; } = null!;

    public List<Activo> Activos { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Area = await _context.Areas
            .FirstOrDefaultAsync(a => a.Id == id);

        if (Area == null)
        {
            return NotFound();
        }

        Activos = await _context.Activos
            .Where(a => a.AreaActualId == id)
            .Include(a => a.EstadoActivo)
            .Include(a => a.SituacionActivo)
            .OrderBy(a => a.Placa)
            .ToListAsync();

        return Page();
    }
}