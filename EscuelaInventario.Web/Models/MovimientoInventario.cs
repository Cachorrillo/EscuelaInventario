using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Models;

[Table("MovimientoInventario")]
public partial class MovimientoInventario
{
    [Key]
    public long Id { get; set; }

    public int ActivoId { get; set; }

    public int TipoMovimientoId { get; set; }

    public DateOnly FechaMovimiento { get; set; }

    public int? AreaAnteriorId { get; set; }

    public int? AreaNuevaId { get; set; }

    public int? EstadoAnteriorId { get; set; }

    public int? EstadoNuevoId { get; set; }

    public int? SituacionAnteriorId { get; set; }

    public int? SituacionNuevaId { get; set; }

    [StringLength(500)]
    public string? Motivo { get; set; }

    public string? Observacion { get; set; }

    public int? ResponsableId { get; set; }

    public DateTime FechaCreacion { get; set; }

    [ForeignKey("ActivoId")]
    [InverseProperty("MovimientoInventarios")]
    public virtual Activo Activo { get; set; } = null!;

    [ForeignKey("AreaAnteriorId")]
    [InverseProperty("MovimientoInventarioAreaAnteriors")]
    public virtual Area? AreaAnterior { get; set; }

    [ForeignKey("AreaNuevaId")]
    [InverseProperty("MovimientoInventarioAreaNuevas")]
    public virtual Area? AreaNueva { get; set; }

    [ForeignKey("EstadoAnteriorId")]
    [InverseProperty("MovimientoInventarioEstadoAnteriors")]
    public virtual EstadoActivo? EstadoAnterior { get; set; }

    [ForeignKey("EstadoNuevoId")]
    [InverseProperty("MovimientoInventarioEstadoNuevos")]
    public virtual EstadoActivo? EstadoNuevo { get; set; }

    [InverseProperty("MovimientoInventario")]
    public virtual ICollection<RegistroLibro> RegistroLibros { get; set; } = new List<RegistroLibro>();

    [ForeignKey("ResponsableId")]
    [InverseProperty("MovimientoInventarios")]
    public virtual Responsable? Responsable { get; set; }

    [ForeignKey("SituacionAnteriorId")]
    [InverseProperty("MovimientoInventarioSituacionAnteriors")]
    public virtual SituacionActivo? SituacionAnterior { get; set; }

    [ForeignKey("SituacionNuevaId")]
    [InverseProperty("MovimientoInventarioSituacionNuevas")]
    public virtual SituacionActivo? SituacionNueva { get; set; }

    [ForeignKey("TipoMovimientoId")]
    [InverseProperty("MovimientoInventarios")]
    public virtual TipoMovimiento TipoMovimiento { get; set; } = null!;
}
