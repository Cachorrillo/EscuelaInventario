using System.ComponentModel.DataAnnotations;
using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Activos;

public class TrasladarModel : PageModel
{
    private readonly EscuelaInventarioContext _context;
    private readonly InventarioService _inventarioService;

    public TrasladarModel(
        EscuelaInventarioContext context,
        InventarioService inventarioService)
    {
        _context = context;
        _inventarioService = inventarioService;
    }

    public string Placa { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string AreaActual { get; set; } = string.Empty;

    public List<SelectListItem> Areas { get; set; } = new();
    public List<SelectListItem> Responsables { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int ActivoId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar el área de destino.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar el área de destino.")]
        public int NuevaAreaId { get; set; }

        [Required(ErrorMessage = "La fecha del traslado es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly FechaMovimiento { get; set; }
            = DateOnly.FromDateTime(DateTime.Today);

        public int? ResponsableId { get; set; }

        [StringLength(500)]
        public string? Motivo { get; set; }

        public string? Observacion { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Input.ActivoId = id;

        bool cargado = await CargarDatosAsync(id);

        if (!cargado)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        bool cargado = await CargarDatosAsync(Input.ActivoId);

        if (!cargado)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var resultado =
                await _inventarioService.TrasladarActivoAsync(
                    Input.ActivoId,
                    Input.NuevaAreaId,
                    Input.FechaMovimiento,
                    Input.ResponsableId,
                    LimpiarTexto(Input.Motivo),
                    LimpiarTexto(Input.Observacion));

            TempData["MensajeExito"] =
                $"Activo trasladado correctamente. " +
                $"Tomo {resultado.Tomo}, Folio {resultado.Folio}, " +
                $"Asiento {resultado.Asiento}.";

            return RedirectToPage(
                "Details",
                new { id = resultado.ActivoId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private async Task<bool> CargarDatosAsync(int activoId)
    {
        var activo = await _context.Activos
            .Include(a => a.AreaActual)
            .FirstOrDefaultAsync(a => a.Id == activoId);

        if (activo == null)
        {
            return false;
        }

        Placa = activo.Placa;
        Descripcion = activo.Descripcion;
        AreaActual =
            $"Área {activo.AreaActual.Numero} - {activo.AreaActual.Nombre}";

        Areas = await _context.Areas
            .Where(a =>
                a.Activa &&
                a.Id != activo.AreaActualId)
            .OrderBy(a => a.Numero)
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"Área {a.Numero} - {a.Nombre}"
            })
            .ToListAsync();

        Responsables = await _context.Responsables
            .Where(r => r.Activo)
            .OrderBy(r => r.Nombre)
            .Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Nombre
            })
            .ToListAsync();

        return true;
    }

    private static string? LimpiarTexto(string? texto)
    {
        return string.IsNullOrWhiteSpace(texto)
            ? null
            : texto.Trim();
    }
}