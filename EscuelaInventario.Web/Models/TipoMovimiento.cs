using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Models;

[Table("TipoMovimiento")]
[Index("Codigo", Name = "UQ_TipoMovimiento_Codigo", IsUnique = true)]
[Index("Nombre", Name = "UQ_TipoMovimiento_Nombre", IsUnique = true)]
public partial class TipoMovimiento
{
    [Key]
    public int Id { get; set; }

    [StringLength(50)]
    [Unicode(false)]
    public string Codigo { get; set; } = null!;

    [StringLength(120)]
    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; }

    [InverseProperty("TipoMovimiento")]
    public virtual ICollection<MovimientoInventario> MovimientoInventarios { get; set; } = new List<MovimientoInventario>();
}
