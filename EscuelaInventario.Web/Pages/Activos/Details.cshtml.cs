using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Activos;

public class DetailsModel : PageModel
{
    private readonly EscuelaInventarioContext _context;

    public DetailsModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public Activo Activo { get; set; } = null!;

    public List<MovimientoInventario> Movimientos { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Activo = await _context.Activos
            .Include(a => a.AreaActual)
            .Include(a => a.EstadoActivo)
            .Include(a => a.SituacionActivo)
            .Include(a => a.ModoAdquisicion)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (Activo == null)
        {
            return NotFound();
        }

        Movimientos = await _context.MovimientoInventarios
            .Where(m => m.ActivoId == id)
            .Include(m => m.TipoMovimiento)
            .Include(m => m.AreaAnterior)
            .Include(m => m.AreaNueva)
            .Include(m => m.EstadoAnterior)
            .Include(m => m.EstadoNuevo)
            .Include(m => m.SituacionAnterior)
            .Include(m => m.SituacionNueva)
            .Include(m => m.Responsable)
            .Include(m => m.RegistroLibros)
            .OrderByDescending(m => m.FechaMovimiento)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        return Page();
    }
}