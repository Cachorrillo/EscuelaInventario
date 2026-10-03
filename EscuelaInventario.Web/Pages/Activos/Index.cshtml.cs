using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Activos;

public class IndexModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public IndexModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public List<Activo> Activos { get; set; } = new();

    public async Task OnGetAsync()
    {
        Activos = await _context.Activos
            .Include(a => a.AreaActual)
            .Include(a => a.EstadoActivo)
            .Include(a => a.SituacionActivo)
            .OrderBy(a => a.Placa)
            .ToListAsync();
    }
}