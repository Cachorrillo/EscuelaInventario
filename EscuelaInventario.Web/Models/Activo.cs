using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Models;

[Table("Activo")]
[Index("Placa", Name = "UQ_Activo_Placa", IsUnique = true)]
public partial class Activo
{
    [Key]
    public int Id { get; set; }

    [StringLength(20)]
    [Unicode(false)]
    public string Placa { get; set; } = null!;

    [StringLength(250)]
    public string Descripcion { get; set; } = null!;

    [StringLength(100)]
    public string? Marca { get; set; }

    [StringLength(100)]
    public string? Modelo { get; set; }

    [StringLength(150)]
    public string? Serie { get; set; }

    public DateOnly? FechaAdquisicion { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Precio { get; set; }

    public int AreaActualId { get; set; }

    public int EstadoActivoId { get; set; }

    public int SituacionActivoId { get; set; }

    public int? ModoAdquisicionId { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    [ForeignKey("AreaActualId")]
    [InverseProperty("Activos")]
    public virtual Area AreaActual { get; set; } = null!;

    [ForeignKey("EstadoActivoId")]
    [InverseProperty("Activos")]
    public virtual EstadoActivo EstadoActivo { get; set; } = null!;

    [ForeignKey("ModoAdquisicionId")]
    [InverseProperty("Activos")]
    public virtual ModoAdquisicion? ModoAdquisicion { get; set; }

    [InverseProperty("Activo")]
    public virtual ICollection<MovimientoInventario> MovimientoInventarios { get; set; } = new List<MovimientoInventario>();

    [ForeignKey("SituacionActivoId")]
    [InverseProperty("Activos")]
    public virtual SituacionActivo SituacionActivo { get; set; } = null!;
}
