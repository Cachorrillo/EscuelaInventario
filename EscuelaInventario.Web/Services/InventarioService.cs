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

    public async Task<ResultadoCambioEstado> CambiarEstadoActivoAsync(
    int activoId,
    int nuevoEstadoId,
    DateOnly fechaMovimiento,
    int? responsableId,
    string? motivo,
    string? observacion)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Obtener activo
            var activo = await _context.Activos
                .FirstOrDefaultAsync(a => a.Id == activoId);

            if (activo == null)
            {
                throw new InvalidOperationException(
                    "El activo seleccionado no existe.");
            }

            int estadoAnteriorId = activo.EstadoActivoId;

            // 2. Validar nuevo estado
            var nuevoEstado = await _context.EstadoActivos
                .FirstOrDefaultAsync(e =>
                    e.Id == nuevoEstadoId &&
                    e.Activo);

            if (nuevoEstado == null)
            {
                throw new InvalidOperationException(
                    "El estado seleccionado no existe o está inactivo.");
            }

            if (estadoAnteriorId == nuevoEstadoId)
            {
                throw new InvalidOperationException(
                    "El activo ya tiene asignado ese estado físico.");
            }

            // 3. Obtener configuración
            var configuracion = await _context.ConfiguracionInventarios
                .FirstOrDefaultAsync(c => c.Id == 1);

            if (configuracion == null)
            {
                throw new InvalidOperationException(
                    "No existe la configuración general del inventario.");
            }

            // 4. Obtener tipo de movimiento
            var tipoCambioEstado = await _context.TipoMovimientos
                .FirstOrDefaultAsync(t => t.Codigo == "CAMBIO_ESTADO");

            if (tipoCambioEstado == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el tipo de movimiento CAMBIO_ESTADO.");
            }

            // 5. Validar responsable
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

            // 6. Calcular siguiente posición del libro
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

            // Cambio automático de tomo:
            // pendiente hasta confirmar regla institucional.

            // 7. Crear movimiento
            var movimiento = new MovimientoInventario
            {
                ActivoId = activo.Id,

                TipoMovimientoId = tipoCambioEstado.Id,

                FechaMovimiento = fechaMovimiento,

                AreaAnteriorId = null,
                AreaNuevaId = null,

                EstadoAnteriorId = estadoAnteriorId,
                EstadoNuevoId = nuevoEstadoId,

                SituacionAnteriorId = null,
                SituacionNuevaId = null,

                Motivo = motivo,
                Observacion = observacion,

                ResponsableId = responsableId
            };

            _context.MovimientoInventarios.Add(movimiento);

            await _context.SaveChangesAsync();

            // 8. Crear registro del libro
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

            // 9. Actualizar estado vigente
            activo.EstadoActivoId = nuevoEstadoId;
            activo.FechaModificacion = DateTime.Now;

            // 10. Actualizar configuración
            configuracion.TomoActual = nuevoTomo;
            configuracion.FolioActual = nuevoFolio;
            configuracion.AsientoActual = nuevoAsiento;
            configuracion.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            // 11. Confirmar transacción
            await transaction.CommitAsync();

            return new ResultadoCambioEstado(
                activo.Id,
                estadoAnteriorId,
                nuevoEstadoId,
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

    public async Task<ResultadoModificacionActivo> ModificarActivoAsync(
    int activoId,
    string descripcion,
    string? marca,
    string? modelo,
    string? serie,
    DateOnly? fechaAdquisicion,
    decimal? precio,
    int? modoAdquisicionId,
    string? observacionesActivo,
    DateOnly fechaMovimiento,
    int? responsableId,
    string? motivo,
    string? observacionMovimiento)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Obtener el activo
            var activo = await _context.Activos
                .FirstOrDefaultAsync(a => a.Id == activoId);

            if (activo == null)
            {
                throw new InvalidOperationException(
                    "El activo seleccionado no existe.");
            }

            // 2. Validar modo de adquisición si se seleccionó
            if (modoAdquisicionId.HasValue)
            {
                bool modoValido = await _context.ModoAdquisicions
                    .AnyAsync(m =>
                        m.Id == modoAdquisicionId.Value &&
                        m.Activo);

                if (!modoValido)
                {
                    throw new InvalidOperationException(
                        "El modo de adquisición seleccionado no es válido.");
                }
            }

            // 3. Validar responsable
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

            // 4. Obtener configuración
            var configuracion = await _context.ConfiguracionInventarios
                .FirstOrDefaultAsync(c => c.Id == 1);

            if (configuracion == null)
            {
                throw new InvalidOperationException(
                    "No existe la configuración general del inventario.");
            }

            // 5. Obtener tipo MODIFICACION
            var tipoModificacion = await _context.TipoMovimientos
                .FirstOrDefaultAsync(t => t.Codigo == "MODIFICACION");

            if (tipoModificacion == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el tipo de movimiento MODIFICACION.");
            }

            // 6. Calcular siguiente posición del libro
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

            // Cambio automático de tomo sigue pendiente de confirmar.

            // 7. Aplicar modificaciones permitidas
            activo.Descripcion = descripcion.Trim();
            activo.Marca = LimpiarTextoInterno(marca);
            activo.Modelo = LimpiarTextoInterno(modelo);
            activo.Serie = LimpiarTextoInterno(serie);
            activo.FechaAdquisicion = fechaAdquisicion;
            activo.Precio = precio;
            activo.ModoAdquisicionId = modoAdquisicionId;
            activo.Observaciones = LimpiarTextoInterno(observacionesActivo);
            activo.FechaModificacion = DateTime.Now;

            // 8. Crear movimiento histórico
            var movimiento = new MovimientoInventario
            {
                ActivoId = activo.Id,
                TipoMovimientoId = tipoModificacion.Id,
                FechaMovimiento = fechaMovimiento,

                AreaAnteriorId = null,
                AreaNuevaId = null,
                EstadoAnteriorId = null,
                EstadoNuevoId = null,
                SituacionAnteriorId = null,
                SituacionNuevaId = null,

                Motivo = LimpiarTextoInterno(motivo),
                Observacion = LimpiarTextoInterno(observacionMovimiento),
                ResponsableId = responsableId
            };

            _context.MovimientoInventarios.Add(movimiento);

            await _context.SaveChangesAsync();

            // 9. Crear registro del libro
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

            // 10. Actualizar configuración
            configuracion.TomoActual = nuevoTomo;
            configuracion.FolioActual = nuevoFolio;
            configuracion.AsientoActual = nuevoAsiento;
            configuracion.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new ResultadoModificacionActivo(
                activo.Id,
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

    private static string? LimpiarTextoInterno(string? texto)
    {
        return string.IsNullOrWhiteSpace(texto)
            ? null
            : texto.Trim();
    }

    public async Task<ResultadoBajaActivo> DarBajaActivoAsync(
    int activoId,
    DateOnly fechaMovimiento,
    int? responsableId,
    string? motivo,
    string? observacion)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Obtener activo
            var activo = await _context.Activos
                .FirstOrDefaultAsync(a => a.Id == activoId);

            if (activo == null)
            {
                throw new InvalidOperationException(
                    "El activo seleccionado no existe.");
            }

            // 2. Obtener situación ACTIVO
            var situacionActiva = await _context.SituacionActivos
                .FirstOrDefaultAsync(s => s.Codigo == "ACTIVO");

            if (situacionActiva == null)
            {
                throw new InvalidOperationException(
                    "No se encontró la situación administrativa ACTIVO.");
            }

            // 3. Obtener situación BAJA
            var situacionBaja = await _context.SituacionActivos
                .FirstOrDefaultAsync(s => s.Codigo == "BAJA");

            if (situacionBaja == null)
            {
                throw new InvalidOperationException(
                    "No se encontró la situación administrativa BAJA.");
            }

            // 4. Evitar dar de baja algo que ya está de baja
            if (activo.SituacionActivoId == situacionBaja.Id)
            {
                throw new InvalidOperationException(
                    "El activo ya se encuentra dado de baja.");
            }

            // 5. Validar responsable si fue seleccionado
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

            // 6. Obtener configuración
            var configuracion = await _context.ConfiguracionInventarios
                .FirstOrDefaultAsync(c => c.Id == 1);

            if (configuracion == null)
            {
                throw new InvalidOperationException(
                    "No existe la configuración general del inventario.");
            }

            // 7. Obtener tipo de movimiento BAJA
            var tipoBaja = await _context.TipoMovimientos
                .FirstOrDefaultAsync(t => t.Codigo == "BAJA");

            if (tipoBaja == null)
            {
                throw new InvalidOperationException(
                    "No se encontró el tipo de movimiento BAJA.");
            }

            // 8. Calcular siguiente posición del libro
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

            // Cambio automático de tomo:
            // pendiente hasta confirmar la regla institucional.

            // 9. Crear movimiento histórico
            var movimiento = new MovimientoInventario
            {
                ActivoId = activo.Id,

                TipoMovimientoId = tipoBaja.Id,

                FechaMovimiento = fechaMovimiento,

                AreaAnteriorId = null,
                AreaNuevaId = null,

                EstadoAnteriorId = null,
                EstadoNuevoId = null,

                SituacionAnteriorId = activo.SituacionActivoId,
                SituacionNuevaId = situacionBaja.Id,

                Motivo = LimpiarTextoInterno(motivo),
                Observacion = LimpiarTextoInterno(observacion),

                ResponsableId = responsableId
            };

            _context.MovimientoInventarios.Add(movimiento);

            await _context.SaveChangesAsync();

            // 10. Crear registro del libro
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

            // 11. Actualizar situación vigente del activo
            activo.SituacionActivoId = situacionBaja.Id;
            activo.FechaModificacion = DateTime.Now;

            // 12. Actualizar configuración
            configuracion.TomoActual = nuevoTomo;
            configuracion.FolioActual = nuevoFolio;
            configuracion.AsientoActual = nuevoAsiento;
            configuracion.FechaModificacion = DateTime.Now;

            await _context.SaveChangesAsync();

            // 13. Confirmar
            await transaction.CommitAsync();

            return new ResultadoBajaActivo(
                activo.Id,
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

public record ResultadoCambioEstado(
    int ActivoId,
    int EstadoAnteriorId,
    int EstadoNuevoId,
    int Tomo,
    int Folio,
    int Asiento
);

public record ResultadoModificacionActivo(
    int ActivoId,
    int Tomo,
    int Folio,
    int Asiento
);

public record ResultadoBajaActivo(
    int ActivoId,
    int Tomo,
    int Folio,
    int Asiento
);