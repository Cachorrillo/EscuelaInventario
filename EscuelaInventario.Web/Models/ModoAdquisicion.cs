using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Models;

[Table("ModoAdquisicion")]
[Index("Nombre", Name = "UQ_ModoAdquisicion_Nombre", IsUnique = true)]
public partial class ModoAdquisicion
{
    [Key]
    public int Id { get; set; }

    [StringLength(120)]
    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    [InverseProperty("ModoAdquisicion")]
    public virtual ICollection<Activo> Activos { get; set; } = new List<Activo>();
}
