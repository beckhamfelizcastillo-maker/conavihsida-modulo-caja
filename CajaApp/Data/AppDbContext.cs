using CajaApp.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace CajaApp.Data;

public class AppDbContext : DbContext
{
    // ==========================================
    // TABLAS
    // ==========================================

    public DbSet<Paciente> Pacientes =>
        Set<Paciente>();

    public DbSet<Transaccion> Transacciones =>
        Set<Transaccion>();

    public DbSet<TurnoCaja> TurnosCaja =>
        Set<TurnoCaja>();

    public DbSet<MovimientoCaja> MovimientosCaja =>
        Set<MovimientoCaja>();

    public DbSet<Comprobante> Comprobantes =>
        Set<Comprobante>();

    public DbSet<FondoConvenio> FondosConvenios =>
        Set<FondoConvenio>();


    // ==========================================
    // SQLITE
    // ==========================================

    /// <summary>
    /// Cadena de conexión de SQLite. Por defecto el archivo cajaapp.db
    /// vive junto al ejecutable (bin/Debug/net8.0), sin depender de la
    /// carpeta desde la que se abra la aplicación. Las pruebas automáticas
    /// la reemplazan por una base temporal.
    /// </summary>
    public static string CadenaConexion { get; set; } =
        $"Data Source={System.IO.Path.Combine(AppContext.BaseDirectory, "cajaapp.db")}";

    protected override void OnConfiguring(
        DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(
            CadenaConexion);
    }


    /// <summary>
    /// EnsureCreated no modifica una base que ya existe. Este método agrega
    /// las columnas nuevas a un cajaapp.db creado con una versión anterior,
    /// sin borrar los datos.
    /// </summary>
    public void AsegurarEsquema()
    {
        var conexion = Database.GetDbConnection();

        bool abrir = conexion.State != System.Data.ConnectionState.Open;

        if (abrir)
        {
            conexion.Open();
        }

        try
        {
            using var consulta = conexion.CreateCommand();

            consulta.CommandText =
                "SELECT COUNT(*) FROM pragma_table_info('Transacciones') WHERE name = 'NumeroReceta';";

            bool existe = Convert.ToInt64(consulta.ExecuteScalar()) > 0;

            if (!existe)
            {
                using var alter = conexion.CreateCommand();

                alter.CommandText =
                    "ALTER TABLE \"Transacciones\" ADD COLUMN \"NumeroReceta\" TEXT NULL;";

                alter.ExecuteNonQuery();
            }
        }
        finally
        {
            if (abrir)
            {
                conexion.Close();
            }
        }
    }


    // ==========================================
    // MODELOS
    // ==========================================

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // ======================================
        // PACIENTE
        // ======================================

        modelBuilder.Entity<Paciente>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Cedula)
                .IsRequired();

            entity.HasIndex(x => x.Cedula)
                .IsUnique();

            entity.Property(x => x.NombreMinimo)
                .IsRequired();

            entity.Property(x => x.PorcentajeSubsidio)
                .HasPrecision(5, 2);
        });


        // ======================================
        // TRANSACCIÓN
        // ======================================

        modelBuilder.Entity<Transaccion>(entity =>
        {
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Transacciones_Monto", "CAST(\"Monto\" AS REAL) >= 0");
                t.HasCheckConstraint("CK_Transacciones_FormaPago", "\"FormaPago\" IN (0, 1, 2)");
                t.HasCheckConstraint("CK_Transacciones_Estado", "\"Estado\" IN (0, 1, 2)");
                t.HasCheckConstraint("CK_Transacciones_Subsidio", "\"EsSubsidiado\" = 0 OR CAST(\"Monto\" AS REAL) = 0");
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.NumeroTransaccion)
                .IsRequired();

            entity.HasIndex(x => x.NumeroTransaccion)
                .IsUnique();

            entity.Property(x => x.CedulaPaciente)
                .IsRequired();

            entity.Property(x => x.TipoServicio)
                .IsRequired();

            entity.Property(x => x.Monto)
                .HasPrecision(18, 2);

            entity.Property(x => x.UsuarioCajero)
                .IsRequired();

            entity.Property(x => x.NumeroReceta)
                .HasMaxLength(50);
        });


        // ======================================
        // TURNO DE CAJA
        // ======================================

        modelBuilder.Entity<TurnoCaja>(entity =>
        {
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_TurnosCaja_BalanceInicial", "CAST(\"BalanceInicial\" AS REAL) >= 0");
                t.HasCheckConstraint("CK_TurnosCaja_SaldoEsperado", "CAST(\"SaldoEsperado\" AS REAL) >= 0");
                t.HasCheckConstraint("CK_TurnosCaja_EfectivoDeclarado", "\"EfectivoDeclarado\" IS NULL OR CAST(\"EfectivoDeclarado\" AS REAL) >= 0");
                t.HasCheckConstraint("CK_TurnosCaja_Estado", "\"Estado\" IN (1, 2)");
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.NumeroTurno)
                .IsRequired();

            entity.HasIndex(x => x.NumeroTurno)
                .IsUnique();

            entity.Property(x => x.UsuarioCajero)
                .IsRequired();

            entity.Property(x => x.NombreCajero)
                .IsRequired();

            entity.Property(x => x.Caja)
                .IsRequired();

            entity.Property(x => x.BalanceInicial)
                .HasPrecision(18, 2);

            entity.Property(x => x.SaldoEsperado)
                .HasPrecision(18, 2);

            entity.Property(x => x.EfectivoDeclarado)
                .HasPrecision(18, 2);

            entity.Property(x => x.Diferencia)
                .HasPrecision(18, 2);

            entity.Property(x => x.Observacion)
                .HasMaxLength(500);
        });


        // ======================================
        // MOVIMIENTO
        // ======================================

        modelBuilder.Entity<MovimientoCaja>(entity =>
        {
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_MovimientosCaja_Monto", "CAST(\"Monto\" AS REAL) >= 0");
                t.HasCheckConstraint("CK_MovimientosCaja_Tipo", "\"Tipo\" BETWEEN 1 AND 5");
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.NumeroMovimiento)
                .IsRequired();

            entity.HasIndex(x => x.NumeroMovimiento)
                .IsUnique();

            entity.Property(x => x.Monto)
                .HasPrecision(18, 2);

            entity.Property(x => x.Concepto)
                .IsRequired();

            entity.Property(x => x.UsuarioCajero)
                .IsRequired();

            entity.HasOne(x => x.TurnoCaja)
                .WithMany()
                .HasForeignKey(x => x.TurnoCajaId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ======================================
        // COMPROBANTE
        // ======================================

        modelBuilder.Entity<Comprobante>(entity =>
        {
            entity.ToTable(t =>
                t.HasCheckConstraint("CK_Comprobantes_Reimpresiones", "\"CantidadReimpresiones\" >= 0"));

            entity.HasKey(x => x.Id);

            entity.Property(x => x.NumeroComprobante)
                .IsRequired();

            entity.HasIndex(x => x.NumeroComprobante)
                .IsUnique();

            entity.Property(x => x.RutaArchivo)
                .IsRequired();

            entity.Property(x => x.TipoDocumento)
                .IsRequired();

            entity.HasOne(x => x.Transaccion)
                .WithMany()
                .HasForeignKey(x => x.TransaccionId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ======================================
        // FONDO / CONVENIO
        // ======================================

        modelBuilder.Entity<FondoConvenio>(entity =>
        {
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_FondosConvenios_MontoAsignado", "CAST(\"MontoAsignado\" AS REAL) >= 0");
                t.HasCheckConstraint("CK_FondosConvenios_MontoUtilizado", "CAST(\"MontoUtilizado\" AS REAL) >= 0");
                t.HasCheckConstraint("CK_FondosConvenios_Tipo", "\"TipoFuente\" IN (1, 2)");
            });

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Codigo)
                .IsRequired();

            entity.HasIndex(x => x.Codigo)
                .IsUnique();

            entity.Property(x => x.Nombre)
                .IsRequired();

            entity.Property(x => x.Organismo)
                .HasMaxLength(250);

            entity.Property(x => x.MontoAsignado)
                .HasPrecision(18, 2);

            entity.Property(x => x.MontoUtilizado)
                .HasPrecision(18, 2);
        });


        // ======================================
        // PACIENTES DE PRUEBA
        // ======================================

        modelBuilder.Entity<Paciente>().HasData(

            new Paciente
            {
                Id = 1,

                Cedula = "001-1234567-8",

                NombreMinimo =
                    "Paciente Prueba 1",

                TieneSubsidio = true,

                PorcentajeSubsidio = 100,

                VigenciaHasta =
                    new DateTime(
                        2027,
                        12,
                        31)
            },

            new Paciente
            {
                Id = 2,

                Cedula = "001-9999999-9",

                NombreMinimo =
                    "Paciente Prueba 2",

                TieneSubsidio = true,

                PorcentajeSubsidio = 100,

                VigenciaHasta =
                    new DateTime(
                        2025,
                        12,
                        31)
            }
        );


        // ======================================
        // FONDOS DE PRUEBA
        // ======================================

        modelBuilder.Entity<FondoConvenio>().HasData(

            new FondoConvenio
            {
                Id = 1,

                Codigo =
                    "FONDO-EST-2026",

                Nombre =
                    "Presupuesto Estatal 2026",

                TipoFuente =
                    TipoFuenteFondo.PresupuestoEstatal,

                Organismo =
                    "Gobierno Dominicano",

                MontoAsignado =
                    15000000m,

                MontoUtilizado =
                    0m,

                FechaInicio =
                    new DateTime(
                        2026,
                        1,
                        1),

                FechaFin =
                    new DateTime(
                        2026,
                        12,
                        31),

                Activo =
                    true,

                Sincronizado =
                    false
            },

            new FondoConvenio
            {
                Id = 2,

                Codigo =
                    "DON-INT-2026",

                Nombre =
                    "Programa de Cooperación Internacional",

                TipoFuente =
                    TipoFuenteFondo.DonacionInternacional,

                Organismo =
                    "Cooperación Internacional",

                MontoAsignado =
                    5000000m,

                MontoUtilizado =
                    0m,

                FechaInicio =
                    new DateTime(
                        2026,
                        1,
                        1),

                FechaFin =
                    new DateTime(
                        2026,
                        12,
                        31),

                Activo =
                    true,

                Sincronizado =
                    false
            }
        );
    }
}
