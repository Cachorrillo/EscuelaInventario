using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages;

public class PruebaConexionModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public PruebaConexionModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public List<EstadoActivo> Estados { get; set; } = new();

    public async Task OnGetAsync()
    {
        Estados = await _context.EstadoActivos
            .OrderBy(e => e.Orden)
            .ToListAsync();
    }
}