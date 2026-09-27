using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Models;

[Table("ConfiguracionInventario")]
public partial class ConfiguracionInventario
{
    [Key]
    public int Id { get; set; }

    [StringLength(4)]
    [Unicode(false)]
    public string PrefijoPlaca { get; set; } = null!;

    public int UltimoConsecutivoPlaca { get; set; }

    public int TomoActual { get; set; }

    public int FolioActual { get; set; }

    public int AsientoActual { get; set; }

    public int AsientosPorFolio { get; set; }

    public DateTime FechaModificacion { get; set; }
}
