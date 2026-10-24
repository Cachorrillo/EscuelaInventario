using EscuelaInventario.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Migracion.Servicios;

public class VerificadorBaseDatosService
{
    private readonly EscuelaInventarioContext _context;

    public VerificadorBaseDatosService(
        EscuelaInventarioContext context)
    {
        _context = context;
    }

    public async Task VerificarAsync()
    {
        Console.WriteLine();
        Console.WriteLine(
            "============================================"
        );
        Console.WriteLine(
            "   VERIFICACIÓN DE BASE DE DATOS"
        );
        Console.WriteLine(
            "============================================"
        );
        Console.WriteLine();

        bool puedeConectar =
            await _context.Database.CanConnectAsync();

        Console.WriteLine(
            $"Conexión SQL Server: " +
            $"{(puedeConectar ? "CORRECTA" : "FALLIDA")}"
        );

        if (!puedeConectar)
        {
            return;
        }

        int activos =
            await _context.Activos.CountAsync();

        int movimientos =
            await _context.MovimientoInventarios.CountAsync();

        int registrosLibro =
            await _context.RegistroLibros.CountAsync();

        int areas =
            await _context.Areas.CountAsync();

        int responsables =
            await _context.Responsables.CountAsync();

        Console.WriteLine();
        Console.WriteLine(
            $"Activos existentes: {activos}"
        );

        Console.WriteLine(
            $"Movimientos existentes: {movimientos}"
        );

        Console.WriteLine(
            $"Registros de libro existentes: {registrosLibro}"
        );

        Console.WriteLine(
            $"Áreas existentes: {areas}"
        );

        Console.WriteLine(
            $"Responsables existentes: {responsables}"
        );

        var configuracion =
            await _context.ConfiguracionInventarios
                .FirstOrDefaultAsync(
                    x => x.Id == 1
                );

        Console.WriteLine();
        Console.WriteLine(
            $"Configuración general: " +
            $"{(configuracion != null ? "OK" : "NO EXISTE")}"
        );

        if (configuracion != null)
        {
            Console.WriteLine(
                $"Prefijo: {configuracion.PrefijoPlaca}"
            );

            Console.WriteLine(
                $"Último consecutivo: " +
                $"{configuracion.UltimoConsecutivoPlaca}"
            );

            Console.WriteLine(
                $"Libro actual: " +
                $"T{configuracion.TomoActual}/" +
                $"F{configuracion.FolioActual}/" +
                $"A{configuracion.AsientoActual}"
            );
        }

        var situaciones =
            await _context.SituacionActivos
                .OrderBy(
                    x => x.Codigo
                )
                .ToListAsync();

        Console.WriteLine();
        Console.WriteLine(
            "Situaciones:"
        );

        foreach (
            var situacion
            in situaciones
        )
        {
            Console.WriteLine(
                $"  {situacion.Codigo} - {situacion.Nombre}"
            );
        }

        var tiposMovimiento =
            await _context.TipoMovimientos
                .OrderBy(
                    x => x.Codigo
                )
                .ToListAsync();

        Console.WriteLine();
        Console.WriteLine(
            "Tipos de movimiento:"
        );

        foreach (
            var tipo
            in tiposMovimiento
        )
        {
            Console.WriteLine(
                $"  {tipo.Codigo} - {tipo.Nombre}"
            );
        }

        bool baseLista =
            activos == 0 &&
            movimientos == 0 &&
            registrosLibro == 0 &&
            configuracion != null &&
            situaciones.Any(
                x => x.Codigo == "ACTIVO"
            ) &&
            situaciones.Any(
                x => x.Codigo == "BAJA"
            ) &&
            tiposMovimiento.Any(
                x => x.Codigo == "INCLUSION"
            ) &&
            tiposMovimiento.Any(
                x => x.Codigo == "TRASLADO"
            ) &&
            tiposMovimiento.Any(
                x => x.Codigo == "CAMBIO_ESTADO"
            ) &&
            tiposMovimiento.Any(
                x => x.Codigo == "MODIFICACION"
            ) &&
            tiposMovimiento.Any(
                x => x.Codigo == "BAJA"
            );

        Console.WriteLine();
        Console.WriteLine(
            "--------------------------------------------"
        );

        Console.WriteLine(
            $"¿Base lista para migración?: " +
            $"{(baseLista ? "SÍ" : "NO")}"
        );

        Console.WriteLine(
            "--------------------------------------------"
        );
    }
}