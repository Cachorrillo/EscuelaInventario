using ClosedXML.Excel;
using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    public List<SelectListItem> Estados { get; set; } = new();

    public List<SelectListItem> ModosAdquisicion { get; set; } = new();


    [BindProperty(SupportsGet = true)]
    public string? Placa { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? EstadoId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ModoAdquisicionId { get; set; }


    // =========================================================
    // MOSTRAR ACTIVOS DEL ÁREA
    // =========================================================

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var area = await _context.Areas
            .FirstOrDefaultAsync(a => a.Id == id);

        if (area == null)
        {
            return NotFound();
        }

        Area = area;

        await CargarCatalogosAsync();

        var consulta = _context.Activos
            .Include(a => a.EstadoActivo)
            .Include(a => a.SituacionActivo)
            .Include(a => a.ModoAdquisicion)
            .Where(a =>
                a.AreaActualId == id &&
                a.SituacionActivo.Codigo == "ACTIVO")
            .AsQueryable();


        // Filtro por últimos números de placa
        if (!string.IsNullOrWhiteSpace(Placa))
        {
            string placaBuscada = Placa.Trim();

            consulta = consulta.Where(a =>
                a.Placa.EndsWith(placaBuscada));
        }


        // Búsqueda general
        if (!string.IsNullOrWhiteSpace(Buscar))
        {
            string texto = Buscar.Trim();

            consulta = consulta.Where(a =>
                a.Descripcion.Contains(texto) ||
                (a.Marca != null && a.Marca.Contains(texto)) ||
                (a.Modelo != null && a.Modelo.Contains(texto)) ||
                (a.Serie != null && a.Serie.Contains(texto)));
        }


        // Estado físico
        if (EstadoId.HasValue)
        {
            consulta = consulta.Where(a =>
                a.EstadoActivoId == EstadoId.Value);
        }


        // Modo de adquisición
        if (ModoAdquisicionId.HasValue)
        {
            consulta = consulta.Where(a =>
                a.ModoAdquisicionId == ModoAdquisicionId.Value);
        }


        Activos = await consulta
            .OrderBy(a => a.Placa)
            .ToListAsync();

        return Page();
    }


    // =========================================================
    // EXPORTAR ACTIVOS JPS DEL ÁREA
    // =========================================================

    public async Task<IActionResult> OnGetExportarJpsAsync(int id)
    {
        var area = await _context.Areas
            .FirstOrDefaultAsync(a => a.Id == id);

        if (area == null)
        {
            return NotFound();
        }


        // Solo activos vigentes del área y de Junta Protección Social
        var activos = await _context.Activos
            .Include(a => a.EstadoActivo)
            .Include(a => a.SituacionActivo)
            .Include(a => a.ModoAdquisicion)
            .Where(a =>
                a.AreaActualId == id &&
                a.SituacionActivo.Codigo == "ACTIVO" &&
                a.ModoAdquisicion != null &&
                a.ModoAdquisicion.Nombre == "Junta Protección Social")
            .OrderBy(a => a.Placa)
            .ToListAsync();


        // =====================================================
        // OBTENER TOMO, FOLIO Y ASIENTO MÁS RECIENTE
        // =====================================================

        var idsActivos = activos
            .Select(a => a.Id)
            .ToList();


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
            $"Área {area.Numero}");


        // =====================================================
        // TÍTULO
        // =====================================================

        string fechaActual =
            DateTime.Now.ToString("dd/MM/yyyy");

        string titulo =
            $"LEVANTAMIENTO DE INVENTARIO GENERAL #{area.Numero} {fechaActual}";


        hoja.Cell(1, 1).Value = titulo;

        hoja.Range(1, 1, 1, 8)
            .Merge();

        hoja.Cell(1, 1)
            .Style.Font.Bold = true;

        hoja.Cell(1, 1)
            .Style.Font.FontSize = 14;

        hoja.Cell(1, 1)
            .Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;


        // =====================================================
        // ENCABEZADOS
        // =====================================================

        hoja.Cell(3, 1).Value = "Tomo";
        hoja.Cell(3, 2).Value = "Folio";
        hoja.Cell(3, 3).Value = "Asiento";
        hoja.Cell(3, 4).Value = "Nombre";
        hoja.Cell(3, 5).Value = "Serie";
        hoja.Cell(3, 6).Value = "Modelo";
        hoja.Cell(3, 7).Value = "Estado";
        hoja.Cell(3, 8).Value = "Modo de adquisición";


        var encabezados =
            hoja.Range(3, 1, 3, 8);

        encabezados.Style.Font.Bold = true;

        encabezados.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;


        // =====================================================
        // LLENAR DATOS
        // =====================================================

        int fila = 4;


        foreach (var activo in activos)
        {
            ultimoRegistroPorActivo.TryGetValue(
                activo.Id,
                out var registroLibro);


            if (registroLibro != null)
            {
                hoja.Cell(fila, 1).Value =
                    registroLibro.Tomo;

                hoja.Cell(fila, 2).Value =
                    registroLibro.Folio;

                hoja.Cell(fila, 3).Value =
                    registroLibro.Asiento;
            }


            hoja.Cell(fila, 4).Value =
                activo.Descripcion;


            hoja.Cell(fila, 5).Value =
                activo.Serie ?? "";


            hoja.Cell(fila, 6).Value =
                activo.Modelo ?? "";


            hoja.Cell(fila, 7).Value =
                activo.EstadoActivo?.Nombre ?? "";


            hoja.Cell(fila, 8).Value =
                activo.ModoAdquisicion?.Nombre ?? "";


            fila++;
        }


        // =====================================================
        // FORMATO
        // =====================================================

        hoja.Columns().AdjustToContents();

        hoja.SheetView.FreezeRows(3);


        if (fila > 4)
        {
            hoja.Range(
                    3,
                    1,
                    fila - 1,
                    8)
                .SetAutoFilter();
        }


        // =====================================================
        // DESCARGAR ARCHIVO
        // =====================================================

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        var contenido = stream.ToArray();


        string fechaArchivo =
            DateTime.Now.ToString("yyyy-MM-dd");


        string nombreArchivo =
            $"LEVANTAMIENTO_INVENTARIO_AREA_{area.Numero}_{fechaArchivo}.xlsx";


        return File(
            contenido,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nombreArchivo);
    }


    // =========================================================
    // CARGAR CATÁLOGOS
    // =========================================================

    private async Task CargarCatalogosAsync()
    {
        Estados = await _context.EstadoActivos
            .Where(e => e.Activo)
            .OrderBy(e => e.Orden)
            .Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = e.Nombre
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
}