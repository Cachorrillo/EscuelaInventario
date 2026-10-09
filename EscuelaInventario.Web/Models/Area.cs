using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Models;

[Table("Area")]
[Index("Numero", Name = "UQ_Area_Numero", IsUnique = true)]
public partial class Area
{
    [Key]
    public int Id { get; set; }

    public int Numero { get; set; }

    [StringLength(150)]
    public string? Nombre { get; set; }

    public bool Activa { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    [InverseProperty("AreaActual")]
    public virtual ICollection<Activo> Activos { get; set; } = new List<Activo>();

    [InverseProperty("AreaAnterior")]
    public virtual ICollection<MovimientoInventario> MovimientoInventarioAreaAnteriors { get; set; } = new List<MovimientoInventario>();

    [InverseProperty("AreaNueva")]
    public virtual ICollection<MovimientoInventario> MovimientoInventarioAreaNuevas { get; set; } = new List<MovimientoInventario>();
}
