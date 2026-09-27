using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Models;

[Table("RegistroLibro")]
[Index("Tomo", "Folio", "Asiento", Name = "UQ_RegistroLibro_Posicion", IsUnique = true)]
public partial class RegistroLibro
{
    [Key]
    public long Id { get; set; }

    public long MovimientoInventarioId { get; set; }

    public int Tomo { get; set; }

    public int Folio { get; set; }

    public int Asiento { get; set; }

    public bool EsPrincipal { get; set; }

    public bool EsHistorico { get; set; }

    public string? TextoOriginal { get; set; }

    public DateTime FechaCreacion { get; set; }

    [ForeignKey("MovimientoInventarioId")]
    [InverseProperty("RegistroLibros")]
    public virtual MovimientoInventario MovimientoInventario { get; set; } = null!;
}
