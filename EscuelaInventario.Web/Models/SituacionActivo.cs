using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Models;

[Table("SituacionActivo")]
[Index("Codigo", Name = "UQ_SituacionActivo_Codigo", IsUnique = true)]
[Index("Nombre", Name = "UQ_SituacionActivo_Nombre", IsUnique = true)]
public partial class SituacionActivo
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Codigo { get; set; } = null!;

    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    public bool Activa { get; set; }

    public int Orden { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    [InverseProperty("SituacionActivo")]
    public virtual ICollection<Activo> Activos { get; set; } = new List<Activo>();

    [InverseProperty("SituacionAnterior")]
    public virtual ICollection<MovimientoInventario> MovimientoInventarioSituacionAnteriors { get; set; } = new List<MovimientoInventario>();

    [InverseProperty("SituacionNueva")]
    public virtual ICollection<MovimientoInventario> MovimientoInventarioSituacionNuevas { get; set; } = new List<MovimientoInventario>();
}
