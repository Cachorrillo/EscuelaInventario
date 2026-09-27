using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Models;

[Table("EstadoActivo")]
[Index("Nombre", Name = "UQ_EstadoActivo_Nombre", IsUnique = true)]
public partial class EstadoActivo
{
    [Key]
    public int Id { get; set; }

    [StringLength(100)]
    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; }

    public int Orden { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    [InverseProperty("EstadoActivo")]
    public virtual ICollection<Activo> Activos { get; set; } = new List<Activo>();

    [InverseProperty("EstadoAnterior")]
    public virtual ICollection<MovimientoInventario> MovimientoInventarioEstadoAnteriors { get; set; } = new List<MovimientoInventario>();

    [InverseProperty("EstadoNuevo")]
    public virtual ICollection<MovimientoInventario> MovimientoInventarioEstadoNuevos { get; set; } = new List<MovimientoInventario>();
}
