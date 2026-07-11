using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Models;
using MMatilde.Api.Helpers;
using System.Text.Json;

namespace MMatilde.Api.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration config)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var adminEmail = string.IsNullOrWhiteSpace(config["AdminEmail"]) ? "admin@mmatilde.com" : config["AdminEmail"];
        var adminPassword = string.IsNullOrWhiteSpace(config["AdminPassword"]) ? "Admin123!" : config["AdminPassword"];

        await EnsureUsuarioAsync(db, adminEmail, "Administrador", adminPassword, RolUsuario.ADMIN);

        var staffEmail = config["StaffEmail"]?.Trim();
        if (!string.IsNullOrWhiteSpace(staffEmail))
        {
            var staffNombre = string.IsNullOrWhiteSpace(config["StaffNombre"]) ? "Staff" : config["StaffNombre"]!.Trim();
            var staffPassword = string.IsNullOrWhiteSpace(config["StaffPassword"]) ? "Staff123!" : config["StaffPassword"]!;
            var staffRol = ParseRol(config["StaffRol"], RolUsuario.VIEWER);
            await EnsureUsuarioAsync(db, staffEmail, staffNombre, staffPassword, staffRol);
            await SyncStaffUsuarioAsync(db, staffEmail, staffNombre, staffRol);
        }

        var makor = await db.Proveedores.FirstOrDefaultAsync(p => p.Slug == "makor");
        if (makor == null)
        {
            db.Proveedores.Add(new Proveedor
            {
                Nombre = "Makor",
                Slug = "makor",
                UrlBase = "https://makorsa.com.ar"
            });
        }

        var manual = await db.Proveedores.FirstOrDefaultAsync(p => p.Slug == "manual");
        if (manual == null)
        {
            db.Proveedores.Add(new Proveedor
            {
                Nombre = "Manual / Otros",
                Slug = "manual",
                UrlBase = ""
            });
        }

        await db.SaveChangesAsync();

        if (!await db.MediosPago.AnyAsync())
        {
            db.MediosPago.AddRange(
                new MedioPago { Nombre = "Efectivo", Slug = "efectivo", EsDefault = true, Orden = 1 },
                new MedioPago { Nombre = "Transferencia", Slug = "transferencia", Orden = 2 },
                new MedioPago { Nombre = "Mixto", Slug = "mixto", Orden = 3 }
            );
            await db.SaveChangesAsync();
        }

        var categoriasNombres = new[] {
            "Abrojos", "Agujas y Alfileres", "Anilinas y Quitamanchas", "Apliques", "Bies",
            "Bijou", "Botones y Broches", "Cierres y Deslizadores", "Cintas", "Complementos",
            "Cordones y Cuerdas", "Elásticos", "Fliselinas", "Galones y Pasamanerías", "Hilos",
            "Indumentaria", "Lanas e Hilos de Tejer", "Lencería", "Librería", "Manualidades",
            "Pegamentos y Adhesivos", "Pitucones y Reparadores", "Puntillas y Broderies", "Telas, Totoras y Rellenos"
        };

        var currentCats = await db.Categorias.Select(c => c.Nombre).ToListAsync();
        foreach (var name in categoriasNombres)
        {
            if (!currentCats.Contains(name))
            {
                db.Categorias.Add(new Categoria
                {
                    Nombre = name,
                    Slug = SlugHelper.Slugify(name)
                });
            }
        }

        var configs = new Dictionary<string, (string valor, string grupo, string label)>
        {
            { "telefono", ("03435190082", "contacto", "Teléfono Fijo") },
            { "whatsapp", ("+5434351900082", "contacto", "WhatsApp") },
            { "direccion", ("Av. Francisco Ramírez 1883, Paraná, Entre Ríos", "contacto", "Dirección") },
            { "google_maps_url", ("", "contacto", "URL Google Maps") },
            { "nombre_negocio", ("Matilde Mercería", "general", "Nombre del Negocio") },
            { "slogan", ("Tu mercería de confianza en Paraná", "general", "Slogan") },
            { "instagram_url", ("", "redes", "URL Instagram") },
            { "facebook_url", ("", "redes", "URL Facebook") },
            { "horarios", (JsonSerializer.Serialize(new { lunes_viernes = "08:30 a 12:30 y de 16:30 a 20:30", sabados = "08:30 a 13:00", domingos = (string)null }), "horarios", "Horarios") }
        };

        var currentConfigs = await db.ConfiguracionSitio.Select(c => c.Clave).ToListAsync();
        foreach (var kvp in configs)
        {
            if (!currentConfigs.Contains(kvp.Key))
            {
                db.ConfiguracionSitio.Add(new ConfiguracionSitio
                {
                    Clave = kvp.Key,
                    Valor = kvp.Value.valor,
                    Grupo = kvp.Value.grupo,
                    Label = kvp.Value.label
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureUsuarioAsync(
        AppDbContext db,
        string email,
        string nombre,
        string password,
        RolUsuario rol)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var exists = await db.Usuarios.AnyAsync(u => u.Email.ToLower() == normalized);
        if (exists) return;

        db.Usuarios.Add(new Usuario
        {
            Email = normalized,
            Nombre = nombre.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
            Rol = rol,
            Activo = true,
        });
    }

    private static async Task SyncStaffUsuarioAsync(
        AppDbContext db,
        string email,
        string nombre,
        RolUsuario rol)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);
        if (user == null) return;

        user.Nombre = nombre.Trim();
        user.Rol = rol;
        user.Activo = true;
        await db.SaveChangesAsync();
    }

    private static RolUsuario ParseRol(string? raw, RolUsuario fallback)
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        return Enum.TryParse(raw.Trim(), true, out RolUsuario rol) ? rol : fallback;
    }
}
