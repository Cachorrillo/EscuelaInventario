using System;
using System.Collections.Generic;
using EscuelaInventario.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace EscuelaInventario.Web.Data;

public partial class EscuelaInventarioContext : DbContext
{
    public EscuelaInventarioContext()
    {
    }

    public EscuelaInventarioContext(DbContextOptions<EscuelaInventarioContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Activo> Activos { get; set; }

    public virtual DbSet<Area> Areas { get; set; }

    public virtual DbSet<ConfiguracionInventario> ConfiguracionInventarios { get; set; }

    public virtual DbSet<EstadoActivo> EstadoActivos { get; set; }

    public virtual DbSet<ModoAdquisicion> ModoAdquisicions { get; set; }

    public virtual DbSet<MovimientoInventario> MovimientoInventarios { get; set; }

    public virtual DbSet<RegistroLibro> RegistroLibros { get; set; }

    public virtual DbSet<Responsable> Responsables { get; set; }

    public virtual DbSet<SituacionActivo> SituacionActivos { get; set; }

    public virtual DbSet<TipoMovimiento> TipoMovimientos { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activo>(entity =>
        {
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())", "DF_Activo_FechaCreacion");

            entity.HasOne(d => d.AreaActual).WithMany(p => p.Activos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Activo_Area");

            entity.HasOne(d => d.EstadoActivo).WithMany(p => p.Activos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Activo_EstadoActivo");

            entity.HasOne(d => d.ModoAdquisicion).WithMany(p => p.Activos).HasConstraintName("FK_Activo_ModoAdquisicion");

            entity.HasOne(d => d.SituacionActivo).WithMany(p => p.Activos)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Activo_SituacionActivo");
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.Property(e => e.Activa).HasDefaultValue(true, "DF_Area_Activa");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())", "DF_Area_FechaCreacion");
        });

        modelBuilder.Entity<ConfiguracionInventario>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.AsientosPorFolio).HasDefaultValue(32, "DF_ConfiguracionInventario_AsientosPorFolio");
            entity.Property(e => e.FechaModificacion).HasDefaultValueSql("(sysdatetime())", "DF_ConfiguracionInventario_FechaModificacion");
        });

        modelBuilder.Entity<EstadoActivo>(entity =>
        {
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_EstadoActivo_Activo");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())", "DF_EstadoActivo_FechaCreacion");
        });

        modelBuilder.Entity<ModoAdquisicion>(entity =>
        {
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_ModoAdquisicion_Activo");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())", "DF_ModoAdquisicion_FechaCreacion");
        });

        modelBuilder.Entity<MovimientoInventario>(entity =>
        {
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())", "DF_MovimientoInventario_FechaCreacion");

            entity.HasOne(d => d.Activo).WithMany(p => p.MovimientoInventarios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientoInventario_Activo");

            entity.HasOne(d => d.AreaAnterior).WithMany(p => p.MovimientoInventarioAreaAnteriors).HasConstraintName("FK_MovimientoInventario_AreaAnterior");

            entity.HasOne(d => d.AreaNueva).WithMany(p => p.MovimientoInventarioAreaNuevas).HasConstraintName("FK_MovimientoInventario_AreaNueva");

            entity.HasOne(d => d.EstadoAnterior).WithMany(p => p.MovimientoInventarioEstadoAnteriors).HasConstraintName("FK_MovimientoInventario_EstadoAnterior");

            entity.HasOne(d => d.EstadoNuevo).WithMany(p => p.MovimientoInventarioEstadoNuevos).HasConstraintName("FK_MovimientoInventario_EstadoNuevo");

            entity.HasOne(d => d.Responsable).WithMany(p => p.MovimientoInventarios).HasConstraintName("FK_MovimientoInventario_Responsable");

            entity.HasOne(d => d.SituacionAnterior).WithMany(p => p.MovimientoInventarioSituacionAnteriors).HasConstraintName("FK_MovimientoInventario_SituacionAnterior");

            entity.HasOne(d => d.SituacionNueva).WithMany(p => p.MovimientoInventarioSituacionNuevas).HasConstraintName("FK_MovimientoInventario_SituacionNueva");

            entity.HasOne(d => d.TipoMovimiento).WithMany(p => p.MovimientoInventarios)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientoInventario_TipoMovimiento");
        });

        modelBuilder.Entity<RegistroLibro>(entity =>
        {
            entity.Property(e => e.EsPrincipal).HasDefaultValue(true, "DF_RegistroLibro_EsPrincipal");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())", "DF_RegistroLibro_FechaCreacion");

            entity.HasOne(d => d.MovimientoInventario).WithMany(p => p.RegistroLibros)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RegistroLibro_MovimientoInventario");
        });

        modelBuilder.Entity<Responsable>(entity =>
        {
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_Responsable_Activo");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())", "DF_Responsable_FechaCreacion");
        });

        modelBuilder.Entity<SituacionActivo>(entity =>
        {
            entity.Property(e => e.Activa).HasDefaultValue(true, "DF_SituacionActivo_Activa");
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())", "DF_SituacionActivo_FechaCreacion");
        });

        modelBuilder.Entity<TipoMovimiento>(entity =>
        {
            entity.Property(e => e.Activo).HasDefaultValue(true, "DF_TipoMovimiento_Activo");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
