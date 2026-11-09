using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

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

    public List<SelectListItem> ModosAdquisicion { get; set; } = new();


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
    public int? ModoAdquisicionId { get; set; }

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
            .Include(a => a.ModoAdquisicion)
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

        if (ModoAdquisicionId.HasValue)
        {
            consulta = consulta.Where(a =>
                a.ModoAdquisicionId == ModoAdquisicionId.Value);
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

        ModosAdquisicion = await _context.ModoAdquisicions
            .Where(m => m.Activo)
            .OrderBy(m => m.Nombre)
            .Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = m.Nombre
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnGetExportarExcelAsync()
    {
        var consulta = _context.Activos
            .Include(a => a.AreaActual)
            .Include(a => a.EstadoActivo)
            .Include(a => a.SituacionActivo)
            .Include(a => a.ModoAdquisicion)
            .AsQueryable();

        // Aplicar los mismos filtros de la pantalla
        if (!string.IsNullOrWhiteSpace(Placa))
        {
            string placaBuscada = Placa.Trim();

            consulta = consulta.Where(a =>
                a.Placa.EndsWith(placaBuscada));
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
            consulta = consulta.Where(a =>
                a.AreaActualId == AreaId.Value);
        }

        if (EstadoId.HasValue)
        {
            consulta = consulta.Where(a =>
                a.EstadoActivoId == EstadoId.Value);
        }

        if (SituacionId.HasValue)
        {
            consulta = consulta.Where(a =>
                a.SituacionActivoId == SituacionId.Value);
        }

        if (ModoAdquisicionId.HasValue)
        {
            consulta = consulta.Where(a =>
                a.ModoAdquisicionId == ModoAdquisicionId.Value);
        }

        // Se exportan todos los resultados del filtro.
        // No usamos Skip ni Take.
        var activos = await consulta
            .OrderBy(a => a.Placa)
            .ToListAsync();

        var idsActivos = activos
            .Select(a => a.Id)
            .ToList();

        // =====================================================
        // OBTENER EL ÚLTIMO REGISTRO DE LIBRO DE CADA ACTIVO
        // =====================================================

        var movimientos = await _context.MovimientoInventarios
            .Where(m => idsActivos.Contains(m.ActivoId))
            .Include(m => m.RegistroLibros)
            .OrderByDescending(m => m.FechaMovimiento)
            .ThenByDescending(m => m.Id)
            .ToListAsync();

        var ultimoRegistroPorActivo = movimientos
            .GroupBy(m => m.ActivoId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(m => m.FechaMovimiento)
                    .ThenByDescending(m => m.Id)
                    .SelectMany(m => m.RegistroLibros
                        .OrderByDescending(r => r.EsPrincipal)
                        .ThenByDescending(r => r.Id))
                    .FirstOrDefault()
            );

        // =====================================================
        // CREAR EXCEL
        // =====================================================

        using var workbook = new XLWorkbook();

        var hoja = workbook.Worksheets.Add(
            "Inventario General");

        // ENCABEZADOS
        hoja.Cell(1, 1).Value = "Id";
        hoja.Cell(1, 2).Value = "Placa";
        hoja.Cell(1, 3).Value = "Tomo";
        hoja.Cell(1, 4).Value = "Folio";
        hoja.Cell(1, 5).Value = "Asiento";
        hoja.Cell(1, 6).Value = "Descripción";
        hoja.Cell(1, 7).Value = "Marca";
        hoja.Cell(1, 8).Value = "Modelo";
        hoja.Cell(1, 9).Value = "Serie";
        hoja.Cell(1, 10).Value = "Fecha de adquisición";
        hoja.Cell(1, 11).Value = "Precio";
        hoja.Cell(1, 12).Value = "Área actual";
        hoja.Cell(1, 13).Value = "Estado físico";
        hoja.Cell(1, 14).Value = "Situación";
        hoja.Cell(1, 15).Value = "Modo de adquisición";
        hoja.Cell(1, 16).Value = "Observaciones";
        hoja.Cell(1, 17).Value = "Ubicación histórica";
        hoja.Cell(1, 18).Value = "Fecha de creación";
        hoja.Cell(1, 19).Value = "Fecha de modificación";

        int fila = 2;

        foreach (var activo in activos)
        {
            ultimoRegistroPorActivo.TryGetValue(
                activo.Id,
                out var registroLibro);

            hoja.Cell(fila, 1).Value = activo.Id;
            hoja.Cell(fila, 2).Value = activo.Placa;

            if (registroLibro != null)
            {
                hoja.Cell(fila, 3).Value = registroLibro.Tomo;
                hoja.Cell(fila, 4).Value = registroLibro.Folio;
                hoja.Cell(fila, 5).Value = registroLibro.Asiento;
            }

            hoja.Cell(fila, 6).Value =
                activo.Descripcion;

            hoja.Cell(fila, 7).Value =
                activo.Marca ?? "";

            hoja.Cell(fila, 8).Value =
                activo.Modelo ?? "";

            hoja.Cell(fila, 9).Value =
                activo.Serie ?? "";

            if (activo.FechaAdquisicion.HasValue)
            {
                hoja.Cell(fila, 10).Value =
                    activo.FechaAdquisicion.Value
                        .ToDateTime(TimeOnly.MinValue);
            }

            if (activo.Precio.HasValue)
            {
                hoja.Cell(fila, 11).Value =
                    activo.Precio.Value;
            }

            hoja.Cell(fila, 12).Value =
                activo.AreaActual != null
                    ? $"Área {activo.AreaActual.Numero}"
                    : "";

            hoja.Cell(fila, 13).Value =
                activo.EstadoActivo?.Nombre ?? "";

            hoja.Cell(fila, 14).Value =
                activo.SituacionActivo?.Nombre ?? "";

            hoja.Cell(fila, 15).Value =
                activo.ModoAdquisicion?.Nombre ?? "";

            hoja.Cell(fila, 16).Value =
                activo.Observaciones ?? "";

            hoja.Cell(fila, 17).Value =
                activo.UbicacionHistorica ?? "";

            hoja.Cell(fila, 18).Value =
                activo.FechaCreacion;

            if (activo.FechaModificacion.HasValue)
            {
                hoja.Cell(fila, 19).Value =
                    activo.FechaModificacion.Value;
            }

            fila++;
        }

        // =====================================================
        // FORMATO
        // =====================================================

        var encabezados =
            hoja.Range(1, 1, 1, 19);

        encabezados.Style.Font.Bold = true;

        encabezados.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        hoja.Column(10)
            .Style
            .DateFormat
            .Format = "dd/MM/yyyy";

        hoja.Column(11)
            .Style
            .NumberFormat
            .Format = "#,##0.00";

        hoja.Column(18)
            .Style
            .DateFormat
            .Format = "dd/MM/yyyy HH:mm";

        hoja.Column(19)
            .Style
            .DateFormat
            .Format = "dd/MM/yyyy HH:mm";

        hoja.Columns().AdjustToContents();

        if (fila > 2)
        {
            hoja.Range(
                    1,
                    1,
                    fila - 1,
                    19)
                .SetAutoFilter();
        }

        hoja.SheetView.FreezeRows(1);

        // =====================================================
        // DESCARGAR ARCHIVO
        // =====================================================

        using var stream =
            new MemoryStream();

        workbook.SaveAs(stream);

        var contenido =
            stream.ToArray();

        var nombreArchivo =
            $"Inventario_General_{DateTime.Now:yyyy-MM-dd}.xlsx";

        return File(
            contenido,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nombreArchivo);
    }

}
