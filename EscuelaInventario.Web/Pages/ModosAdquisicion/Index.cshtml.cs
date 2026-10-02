using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.ModosAdquisicion;

public class IndexModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public IndexModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public List<ModoAdquisicion> ModosAdquisicion { get; set; } = new();

    public async Task OnGetAsync()
    {
        ModosAdquisicion = await _context.ModoAdquisicions
            .OrderBy(m => m.Nombre)
            .ToListAsync();
    }
}