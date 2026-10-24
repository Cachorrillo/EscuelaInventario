using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Activos;

public class IndexModel : PageModel
{
    private readonly EscuelaInventarioContext _context;
    private const int TamanoPagina = 50;

    public IndexModel(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public List<Activo> Activos { get; set; } = new();
    public Dictionary<int, int?> AreasOriginales { get; set; } = new();
    public List<SelectListItem> Areas { get; set; } = new();
    public List<SelectListItem> Estados { get; set; } = new();
    public List<SelectListItem> Situaciones { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Placa { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? AreaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EstadoId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? SituacionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }

    public async Task OnGetAsync()
    {
        await CargarCatalogosAsync();

        var consulta = _context.Activos
            .Include(a => a.AreaActual)
            .Include(a => a.EstadoActivo)
            .Include(a => a.SituacionActivo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Placa))
        {
            string placaBuscada = Placa.Trim();
            consulta = consulta.Where(a => a.Placa.EndsWith(placaBuscada));
        }

        if (!string.IsNullOrWhiteSpace(Buscar))
        {
            string texto = Buscar.Trim();

            consulta = consulta.Where(a =>
                a.Descripcion.Contains(texto) ||
                (a.Marca != null && a.Marca.Contains(texto)) ||
                (a.Modelo != null && a.Modelo.Contains(texto)) ||
                (a.Serie != null && a.Serie.Contains(texto)));
        }

        if (AreaId.HasValue)
        {
            consulta = consulta.Where(a => a.AreaActualId == AreaId.Value);
        }

        if (EstadoId.HasValue)
        {
            consulta = consulta.Where(a => a.EstadoActivoId == EstadoId.Value);
        }

        if (SituacionId.HasValue)
        {
            consulta = consulta.Where(a => a.SituacionActivoId == SituacionId.Value);
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

        Activos = await consulta
            .OrderByDescending(a => a.Placa)
            .Skip((Pagina - 1) * TamanoPagina)
            .Take(TamanoPagina)
            .ToListAsync();

        var idsActivos = Activos
            .Select(a => a.Id)
            .ToList();

        if (idsActivos.Count > 0)
        {
            var movimientosInclusion =
                await _context.MovimientoInventarios
                    .Where(m =>
                        idsActivos.Contains(m.ActivoId) &&
                        m.TipoMovimiento.Codigo == "INCLUSION")
                    .Include(m => m.AreaNueva)
                    .OrderBy(m => m.Id)
                    .ToListAsync();

            AreasOriginales = movimientosInclusion
                .GroupBy(m => m.ActivoId)
                .ToDictionary(
                    g => g.Key,
                    g => g
                        .OrderBy(m => m.Id)
                        .Select(m => m.AreaNueva?.Numero)
                        .FirstOrDefault()
                );
        }
    }

    private async Task CargarCatalogosAsync()
    {
        Areas = await _context.Areas
            .OrderBy(a => a.Numero)
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"Área {a.Numero}"
            })
            .ToListAsync();

        Estados = await _context.EstadoActivos
            .OrderBy(e => e.Orden)
            .Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = e.Nombre
            })
            .ToListAsync();

        Situaciones = await _context.SituacionActivos
            .OrderBy(s => s.Orden)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Nombre
            })
            .ToListAsync();
    }
}
