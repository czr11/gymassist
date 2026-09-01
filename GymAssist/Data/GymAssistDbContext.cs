using GymAssist.Models;
using Microsoft.EntityFrameworkCore;

namespace GymAssist.Data;

public class GymAssistDbContext(DbContextOptions<GymAssistDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Membresia> Membresias => Set<Membresia>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Checkin> Checkins => Set<Checkin>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.Entity<Usuario>().HasKey(usuario => usuario.IdUsuario);
        modelBuilder.Entity<Cliente>().HasKey(cliente => cliente.IdCliente);
        modelBuilder.Entity<Membresia>().HasKey(membresia => membresia.IdMembresia);
        modelBuilder.Entity<Pago>().HasKey(pago => pago.IdPago);
        modelBuilder.Entity<Checkin>().HasKey(checkin => checkin.IdCheckin);
        modelBuilder.Entity<Notificacion>().HasKey(notificacion => notificacion.IdNotificacion);
    }
}
