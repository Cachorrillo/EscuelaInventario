using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Responsables;

public class IndexModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public IndexModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public List<Responsable> Responsables { get; set; } = new();

    public async Task OnGetAsync()
    {
        Responsables = await _context.Responsables
            .OrderBy(r => r.Nombre)
            .ToListAsync();
    }
}