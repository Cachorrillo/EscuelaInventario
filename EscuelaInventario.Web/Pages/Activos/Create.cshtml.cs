using System.ComponentModel.DataAnnotations;
using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using EscuelaInventario.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Pages.Activos;

public class CreateModel : PageModel
{
    private readonly EscuelaInventarioContext _context;
    private readonly InventarioService _inventarioService;

    public CreateModel(
        EscuelaInventarioContext context,
        InventarioService inventarioService)
    {
        _context = context;
        _inventarioService = inventarioService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> Areas { get; set; } = new();
    public List<SelectListItem> Estados { get; set; } = new();
    public List<SelectListItem> ModosAdquisicion { get; set; } = new();
    public List<SelectListItem> Responsables { get; set; } = new();

    public class InputModel
    {
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

        [Required(ErrorMessage = "Debe seleccionar un área.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un área.")]
        public int AreaActualId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un estado.")]
        [Range(1, int.MaxValue,
            ErrorMessage = "Debe seleccionar un estado.")]
        public int EstadoActivoId { get; set; }

        public int? ModoAdquisicionId { get; set; }

        public string? Observaciones { get; set; }

        [Required(ErrorMessage = "La fecha del movimiento es obligatoria.")]
        [DataType(DataType.Date)]
        public DateOnly FechaMovimiento { get; set; }
            = DateOnly.FromDateTime(DateTime.Today);

        public int? ResponsableId { get; set; }

        public string? ObservacionMovimiento { get; set; }
    }

    public async Task OnGetAsync()
    {
        await CargarCatalogosAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await CargarCatalogosAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        bool areaValida = await _context.Areas
            .AnyAsync(a =>
                a.Id == Input.AreaActualId &&
                a.Activa);

        if (!areaValida)
        {
            ModelState.AddModelError(
                "Input.AreaActualId",
                "El área seleccionada no es válida o se encuentra inactiva.");
        }

        bool estadoValido = await _context.EstadoActivos
            .AnyAsync(e =>
                e.Id == Input.EstadoActivoId &&
                e.Activo);

        if (!estadoValido)
        {
            ModelState.AddModelError(
                "Input.EstadoActivoId",
                "El estado seleccionado no es válido.");
        }

        if (Input.ModoAdquisicionId.HasValue)
        {
            bool modoValido = await _context.ModoAdquisicions
                .AnyAsync(m =>
                    m.Id == Input.ModoAdquisicionId.Value &&
                    m.Activo);

            if (!modoValido)
            {
                ModelState.AddModelError(
                    "Input.ModoAdquisicionId",
                    "El modo de adquisición seleccionado no es válido.");
            }
        }

        if (Input.ResponsableId.HasValue)
        {
            bool responsableValido = await _context.Responsables
                .AnyAsync(r =>
                    r.Id == Input.ResponsableId.Value &&
                    r.Activo);

            if (!responsableValido)
            {
                ModelState.AddModelError(
                    "Input.ResponsableId",
                    "El responsable seleccionado no es válido.");
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var activo = new Activo
        {
            Placa = string.Empty,

            Descripcion = Input.Descripcion.Trim(),

            Marca = LimpiarTexto(Input.Marca),
            Modelo = LimpiarTexto(Input.Modelo),
            Serie = LimpiarTexto(Input.Serie),

            FechaAdquisicion = Input.FechaAdquisicion,
            Precio = Input.Precio,

            AreaActualId = Input.AreaActualId,
            EstadoActivoId = Input.EstadoActivoId,

            SituacionActivoId = 0,

            ModoAdquisicionId = Input.ModoAdquisicionId,

            Observaciones = LimpiarTexto(Input.Observaciones)
        };

        try
        {
            var resultado =
                await _inventarioService.RegistrarActivoAsync(
                    activo,
                    Input.FechaMovimiento,
                    Input.ResponsableId,
                    LimpiarTexto(Input.ObservacionMovimiento));

            TempData["MensajeExito"] =
                $"Activo {resultado.Placa} registrado correctamente. " +
                $"Tomo {resultado.Tomo}, Folio {resultado.Folio}, " +
                $"Asiento {resultado.Asiento}.";

            return RedirectToPage("Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }

    private async Task CargarCatalogosAsync()
    {
        Areas = await _context.Areas
            .Where(a => a.Activa)
            .OrderBy(a => a.Numero)
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"Área {a.Numero}"
            })
            .ToListAsync();

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

        Responsables = await _context.Responsables
            .Where(r => r.Activo)
            .OrderBy(r => r.Nombre)
            .Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Nombre
            })
            .ToListAsync();
    }

    private static string? LimpiarTexto(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        return texto.Trim();
    }
}