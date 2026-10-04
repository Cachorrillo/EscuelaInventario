using EscuelaInventario.Web.Data;
using EscuelaInventario.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Services;

public class InventarioService
{
    private readonly EscuelaInventarioContext _context;

    public InventarioService(EscuelaInventarioContext context)
    {
        _context = context;
    }

    public async Task<ResultadoRegistroActivo> RegistrarActivoAsync(
        Activo activo,
        DateOnly fechaMovimiento,
        int? responsableId,
        string? observacion)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // ----------------------------------------------------
            // 1. Obtener configuración general del inventario
            // ----------------------------------------------------

            var configuracion = await _context.ConfiguracionInventarios
                .FirstOrDefaultAsync(c => c.Id == 1);

            if (configuracion == null)
            {
                throw new InvalidOperationException(
                    "No existe la configuración general del inventario.");
            }

            // ----------------------------------------------------
            // 2. Obtener la situación administrativa ACTIVO
            // ----------------------------------------------------

            var situacionActiva = await _context.SituacionActivos
                .FirstOrDefaultAsync(s => s.Codigo == "ACTIVO");

            if (situacionActiva == null)
            {
                throw new InvalidOperationException(
                    "No se encontró la situación administrativa ACTIVO.");
            }

            // ----------------------------------------------------
            // 3. Obtener el tipo de movimiento INCLUSION
            // ----------------------------------------------------

            var tipoInclusion = await _context.TipoMovimientos
                .FirstOrDefaultAsync(t => t.Codigo == "INCLUSION");

            if (tipoInclusion == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el tipo de movimiento INCLUSION.");
            }

            // ----------------------------------------------------
            // 4. Calcular la siguiente placa
            // ----------------------------------------------------

            int siguienteConsecutivo =
                configuracion.UltimoConsecutivoPlaca + 1;

            if (siguienteConsecutivo > 9999)
            {
                throw new InvalidOperationException(
                    "Se agotó el consecutivo disponible de cuatro dígitos para las placas.");
            }

            string nuevaPlaca =
                $"{configuracion.PrefijoPlaca}-{siguienteConsecutivo:D4}";

            bool placaExiste = await _context.Activos
                .AnyAsync(a => a.Placa == nuevaPlaca);

            if (placaExiste)
            {
                throw new InvalidOperationException(
                    $"La placa {nuevaPlaca} ya existe.");
            }

            // ----------------------------------------------------
            // 5. Calcular siguiente tomo / folio / asiento
            // ----------------------------------------------------

            int nuevoTomo = configuracion.TomoActual;
            int nuevoFolio = configuracion.FolioActual;
            int nuevoAsiento;

            if (configuracion.AsientoActual == 0)
            {
                nuevoAsiento = 1;
            }
            else if (configuracion.AsientoActual <
                     configuracion.AsientosPorFolio)
            {
                nuevoAsiento = configuracion.AsientoActual + 1;
            }
            else
            {
                nuevoFolio++;
                nuevoAsiento = 1;
            }

            // IMPORTANTE:
            // Todavía no programamos el cambio automático de tomo,
            // porque esa regla está pendiente de confirmación.

            // ----------------------------------------------------
            // 6. Completar y registrar el activo
            // ----------------------------------------------------

            activo.Placa = nuevaPlaca;
            activo.SituacionActivoId = situacionActiva.Id;

            _context.Activos.Add(activo);

            // Necesitamos guardar para obtener el Id Identity del activo.
            await _context.SaveChangesAsync();

            // ----------------------------------------------------
            // 7. Crear el movimiento de inclusión
            // ----------------------------------------------------

            var movimiento = new MovimientoInventario
            {
                ActivoId = activo.Id,

                TipoMovimientoId = tipoInclusion.Id,

                FechaMovimiento = fechaMovimiento,

                AreaAnteriorId = null,
                AreaNuevaId = activo.AreaActualId,

                EstadoAnteriorId = null,
                EstadoNuevoId = activo.EstadoActivoId,

                SituacionAnteriorId = null,
                SituacionNuevaId = situacionActiva.Id,

                ResponsableId = responsableId,

                Observacion = observacion
            };

            _context.MovimientoInventarios.Add(movimiento);

            // Necesitamos el Id Identity del movimiento.
            await _context.SaveChangesAsync();

            // ----------------------------------------------------
            // 8. Crear el registro del libro
            // ----------------------------------------------------

            var registroLibro = new RegistroLibro
            {
                MovimientoInventarioId = movimiento.Id,

                Tomo = nuevoTomo,
                Folio = nuevoFolio,
                Asiento = nuevoAsiento,

                EsPrincipal = true,
                EsHistorico = false
            };

            _context.RegistroLibros.Add(registroLibro);

            // ----------------------------------------------------
            // 9. Actualizar las secuencias
            // ----------------------------------------------------

            configuracion.UltimoConsecutivoPlaca =
                siguienteConsecutivo;

            configuracion.TomoActual =
                nuevoTomo;

            configuracion.FolioActual =
                nuevoFolio;

            configuracion.AsientoActual =
                nuevoAsiento;

            configuracion.FechaModificacion =
                DateTime.Now;

            await _context.SaveChangesAsync();

            // ----------------------------------------------------
            // 10. Confirmar toda la operación
            // ----------------------------------------------------

            await transaction.CommitAsync();

            return new ResultadoRegistroActivo(
                activo.Id,
                nuevaPlaca,
                nuevoTomo,
                nuevoFolio,
                nuevoAsiento
            );
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<ResultadoTraslado> TrasladarActivoAsync(
    int activoId,
    int nuevaAreaId,
    DateOnly fechaMovimiento,
    int? responsableId,
    string? motivo,
    string? observacion)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // ----------------------------------------------------
            // 1. Obtener el activo
            // ----------------------------------------------------

            var activo = await _context.Activos
                .FirstOrDefaultAsync(a => a.Id == activoId);

            if (activo == null)
            {
                throw new InvalidOperationException(
                    "El activo seleccionado no existe.");
            }

            // ----------------------------------------------------
            // 2. Validar la nueva área
            // ----------------------------------------------------

            var nuevaArea = await _context.Areas
                .FirstOrDefaultAsync(a =>
                    a.Id == nuevaAreaId &&
                    a.Activa);

            if (nuevaArea == null)
            {
                throw new InvalidOperationException(
                    "El área de destino no existe o se encuentra inactiva.");
            }

            int areaAnteriorId = activo.AreaActualId;

            if (areaAnteriorId == nuevaAreaId)
            {
                throw new InvalidOperationException(
                    "El activo ya se encuentra asignado a esa área.");
            }

            // ----------------------------------------------------
            // 3. Obtener configuración del inventario
            // ----------------------------------------------------

            var configuracion = await _context.ConfiguracionInventarios
                .FirstOrDefaultAsync(c => c.Id == 1);

            if (configuracion == null)
            {
                throw new InvalidOperationException(
                    "No existe la configuración general del inventario.");
            }

            // ----------------------------------------------------
            // 4. Obtener tipo de movimiento TRASLADO
            // ----------------------------------------------------

            var tipoTraslado = await _context.TipoMovimientos
                .FirstOrDefaultAsync(t => t.Codigo == "TRASLADO");

            if (tipoTraslado == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el tipo de movimiento TRASLADO.");
            }

            // ----------------------------------------------------
            // 5. Validar responsable si fue seleccionado
            // ----------------------------------------------------

            if (responsableId.HasValue)
            {
                bool responsableValido = await _context.Responsables
                    .AnyAsync(r =>
                        r.Id == responsableId.Value &&
                        r.Activo);

                if (!responsableValido)
                {
                    throw new InvalidOperationException(
                        "El responsable seleccionado no es válido.");
                }
            }

            // ----------------------------------------------------
            // 6. Calcular siguiente posición del libro
            // ----------------------------------------------------

            int nuevoTomo = configuracion.TomoActual;
            int nuevoFolio = configuracion.FolioActual;
            int nuevoAsiento;

            if (configuracion.AsientoActual == 0)
            {
                nuevoAsiento = 1;
            }
            else if (configuracion.AsientoActual <
                     configuracion.AsientosPorFolio)
            {
                nuevoAsiento = configuracion.AsientoActual + 1;
            }
            else
            {
                nuevoFolio++;
                nuevoAsiento = 1;
            }

            // El cambio automático de tomo sigue pendiente
            // hasta confirmar la regla institucional.

            // ----------------------------------------------------
            // 7. Crear movimiento histórico
            // ----------------------------------------------------

            var movimiento = new MovimientoInventario
            {
                ActivoId = activo.Id,

                TipoMovimientoId = tipoTraslado.Id,

                FechaMovimiento = fechaMovimiento,

                AreaAnteriorId = areaAnteriorId,
                AreaNuevaId = nuevaAreaId,

                EstadoAnteriorId = null,
                EstadoNuevoId = null,

                SituacionAnteriorId = null,
                SituacionNuevaId = null,

                Motivo = motivo,
                Observacion = observacion,

                ResponsableId = responsableId
            };

            _context.MovimientoInventarios.Add(movimiento);

            await _context.SaveChangesAsync();

            // ----------------------------------------------------
            // 8. Crear registro del libro
            // ----------------------------------------------------

            var registroLibro = new RegistroLibro
            {
                MovimientoInventarioId = movimiento.Id,

                Tomo = nuevoTomo,
                Folio = nuevoFolio,
                Asiento = nuevoAsiento,

                EsPrincipal = true,
                EsHistorico = false
            };

            _context.RegistroLibros.Add(registroLibro);

            // ----------------------------------------------------
            // 9. Actualizar área vigente del activo
            // ----------------------------------------------------

            activo.AreaActualId = nuevaAreaId;
            activo.FechaModificacion = DateTime.Now;

            // ----------------------------------------------------
            // 10. Actualizar posición actual del libro
            // ----------------------------------------------------

            configuracion.TomoActual = nuevoTomo;
            configuracion.FolioActual = nuevoFolio;
            configuracion.AsientoActual = nuevoAsiento;
            configuracion.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            // ----------------------------------------------------
            // 11. Confirmar transacción
            // ----------------------------------------------------

            await transaction.CommitAsync();

            return new ResultadoTraslado(
                activo.Id,
                areaAnteriorId,
                nuevaAreaId,
                nuevoTomo,
                nuevoFolio,
                nuevoAsiento
            );
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

}


public record ResultadoRegistroActivo(
    int ActivoId,
    string Placa,
    int Tomo,
    int Folio,
    int Asiento
);

public record ResultadoTraslado(
    int ActivoId,
    int AreaAnteriorId,
    int AreaNuevaId,
    int Tomo,
    int Folio,
    int Asiento
);