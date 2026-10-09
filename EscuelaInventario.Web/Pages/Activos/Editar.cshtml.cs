using System.ComponentModel.DataAnnotations;
using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Activos;

public class EditarModel : PageModel
{
    private readonly EscuelaInventarioContext _context;
    private readonly InventarioService _inventarioService;

    public EditarModel(
        EscuelaInventarioContext context,
        InventarioService inventarioService)
    {
        _context = context;
        _inventarioService = inventarioService;
    }

    public string Placa { get; set; } = string.Empty;
    public string AreaActual { get; set; } = string.Empty;
    public string EstadoActual { get; set; } = string.Empty;
    public string SituacionActual { get; set; } = string.Empty;

    public List<SelectListItem> ModosAdquisicion { get; set; } = new();
    public List<SelectListItem> Responsables { get; set; } = new();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public int ActivoId { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        [StringLength(250)]
        public string Descripcion { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Marca { get; set; }

        [StringLength(100)]
        public string? Modelo { get; set; }

        [StringLength(150)]
        public string? Serie { get; set; }

        [DataType(DataType.Date)]
        public DateOnly? FechaAdquisicion { get; set; }

        [Range(0, double.MaxValue,
            ErrorMessage = "El precio no puede ser negativo.")]
        public decimal? Precio { get; set; }

        public int? ModoAdquisicionId { get; set; }

        public string? ObservacionesActivo { get; set; }

        [Required(ErrorMessage = "La fecha de modificación es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly FechaMovimiento { get; set; }
            = DateOnly.FromDateTime(DateTime.Today);

        public int? ResponsableId { get; set; }

        [StringLength(500)]
        public string? Motivo { get; set; }

        public string? ObservacionMovimiento { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        bool cargado = await CargarDatosAsync(id, cargarInput: true);

        if (!cargado)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        bool cargado = await CargarDatosAsync(
            Input.ActivoId,
            cargarInput: false);

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
                await _inventarioService.ModificarActivoAsync(
                    Input.ActivoId,
                    Input.Descripcion,
                    Input.Marca,
                    Input.Modelo,
                    Input.Serie,
                    Input.FechaAdquisicion,
                    Input.Precio,
                    Input.ModoAdquisicionId,
                    Input.ObservacionesActivo,
                    Input.FechaMovimiento,
                    Input.ResponsableId,
                    Input.Motivo,
                    Input.ObservacionMovimiento);

            TempData["MensajeExito"] =
                $"Información actualizada correctamente. " +
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

    private async Task<bool> CargarDatosAsync(
        int activoId,
        bool cargarInput)
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

        AreaActual =
            $"Área {activo.AreaActual.Numero}";

        EstadoActual = activo.EstadoActivo.Nombre;
        SituacionActual = activo.SituacionActivo.Nombre;

        if (cargarInput)
        {
            Input.ActivoId = activo.Id;
            Input.Descripcion = activo.Descripcion;
            Input.Marca = activo.Marca;
            Input.Modelo = activo.Modelo;
            Input.Serie = activo.Serie;
            Input.FechaAdquisicion = activo.FechaAdquisicion;
            Input.Precio = activo.Precio;
            Input.ModoAdquisicionId = activo.ModoAdquisicionId;
            Input.ObservacionesActivo = activo.Observaciones;
        }

        ModosAdquisicion = await _context.ModoAdquisicions
            .Where(m => m.Activo)
            .OrderBy(m => m.Nombre)
            .Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = m.Nombre
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
}