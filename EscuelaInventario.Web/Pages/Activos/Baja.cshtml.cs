using System.ComponentModel.DataAnnotations;
using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Activos;

public class BajaModel : PageModel
{
    private readonly EscuelaInventarioContext _context;
    private readonly InventarioService _inventarioService;

    public BajaModel(
        EscuelaInventarioContext context,
        InventarioService inventarioService)
    {
        _context = context;
        _inventarioService = inventarioService;
    }

    public string Placa { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string AreaActual { get; set; } = string.Empty;
    public string EstadoActual { get; set; } = string.Empty;
    public string SituacionActual { get; set; } = string.Empty;

    public List<SelectListItem> Responsables { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int ActivoId { get; set; }

        [Required(ErrorMessage = "La fecha de baja es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly FechaMovimiento { get; set; }
            = DateOnly.FromDateTime(DateTime.Today);

        public int? ResponsableId { get; set; }

        [Required(ErrorMessage = "Debe indicar el motivo de la baja.")]
        [StringLength(500)]
        public string Motivo { get; set; } = string.Empty;

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
                await _inventarioService.DarBajaActivoAsync(
                    Input.ActivoId,
                    Input.FechaMovimiento,
                    Input.ResponsableId,
                    Input.Motivo.Trim(),
                    LimpiarTexto(Input.Observacion));

            TempData["MensajeExito"] =
                $"Activo dado de baja correctamente. " +
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
            .Include(a => a.EstadoActivo)
            .Include(a => a.SituacionActivo)
            .FirstOrDefaultAsync(a => a.Id == activoId);

        if (activo == null)
        {
            return false;
        }

        Placa = activo.Placa;
        Descripcion = activo.Descripcion;

        AreaActual =
            $"Área {activo.AreaActual.Numero} - {activo.AreaActual.Nombre}";

        EstadoActual = activo.EstadoActivo.Nombre;
        SituacionActual = activo.SituacionActivo.Nombre;

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