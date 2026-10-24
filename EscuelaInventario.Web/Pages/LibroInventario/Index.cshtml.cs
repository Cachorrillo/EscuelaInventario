using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.LibroInventario;

public class IndexModel : PageModel
{
    private readonly EscuelaInventarioContext _context;
    private const int TamanoPagina = 50;

    public IndexModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public List<RegistroLibro> Registros { get; set; } = new();
    public List<SelectListItem> TiposMovimiento { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Placa { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TipoMovimiento { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }

    public int? UltimoTomo { get; set; }
    public int? UltimoFolio { get; set; }
    public int? UltimoAsiento { get; set; }

    public async Task OnGetAsync()
    {
        await CargarTiposMovimientoAsync();

        var consulta = _context.RegistroLibros
            .AsNoTracking()
            .Include(r => r.MovimientoInventario)
                .ThenInclude(m => m.Activo)
            .Include(r => r.MovimientoInventario)
                .ThenInclude(m => m.TipoMovimiento)
            .AsQueryable();

        var ultimoRegistro = await _context.RegistroLibros
            .AsNoTracking()
            .OrderByDescending(r => r.Tomo)
            .ThenByDescending(r => r.Folio)
            .ThenByDescending(r => r.Asiento)
            .Select(r => new
            {
                r.Tomo,
                r.Folio,
                r.Asiento
            })
            .FirstOrDefaultAsync();

        if (ultimoRegistro != null)
        {
            UltimoTomo = ultimoRegistro.Tomo;
            UltimoFolio = ultimoRegistro.Folio;
            UltimoAsiento = ultimoRegistro.Asiento;
        }

        if (!string.IsNullOrWhiteSpace(Placa))
        {
            string placaBuscada = Placa.Trim();

            consulta = consulta.Where(r =>
                r.MovimientoInventario.Activo.Placa
                    .EndsWith(placaBuscada));
        }

        if (!string.IsNullOrWhiteSpace(TipoMovimiento))
        {
            string codigo = TipoMovimiento.Trim();

            consulta = consulta.Where(r =>
                r.MovimientoInventario.TipoMovimiento.Codigo == codigo);
        }

        TotalRegistros = await consulta.CountAsync();

        TotalPaginas = (int)Math.Ceiling(
            TotalRegistros / (double)TamanoPagina);

        if (Pagina < 1)
        {
            Pagina = 1;
        }

        if (TotalPaginas > 0 && Pagina > TotalPaginas)
        {
            Pagina = TotalPaginas;
        }

        Registros = await consulta
            .OrderByDescending(r => r.Tomo)
            .ThenByDescending(r => r.Folio)
            .ThenByDescending(r => r.Asiento)
            .Skip((Pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .ToListAsync();
    }

    private async Task CargarTiposMovimientoAsync()
    {
        TiposMovimiento = await _context.TipoMovimientos
            .AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .Select(t => new SelectListItem
            {
                Value = t.Codigo,
                Text = t.Nombre
            })
            .ToListAsync();
    }
}
