using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Areas;

public class IndexModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public IndexModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public List<Area> Areas { get; set; } = new();

    public async Task OnGetAsync()
    {
        Areas = await _context.Areas
            .OrderBy(a => a.Numero)
            .ToListAsync();
    }
}