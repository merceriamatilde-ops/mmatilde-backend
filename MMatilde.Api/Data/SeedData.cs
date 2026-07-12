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

        await EnsureColoresAsync(db);
        await EnsureProductoVariosAsync(db);
        await RecalcularTitulosMakorCortadosAsync(db);
    }

    /// <summary>Paleta base de colores. Idempotente: no duplica (compara por nombre sin acentos/mayúsculas ni por slug)
    /// y respeta los que ya estén cargados a mano.</summary>
    private static async Task EnsureColoresAsync(AppDbContext db)
    {
        var paleta = new (string Nombre, string Hex)[]
        {
            ("Blanco", "#FFFFFF"), ("Gris Perla", "#C0C5C8"), ("Gris Medio", "#8E9396"),
            ("Gris Topo", "#66696C"), ("Negro", "#1A1A1A"), ("Celeste Bebé", "#A9D5ED"),
            ("Celeste Pastel", "#6097CE"), ("Turquesa", "#007AAB"), ("Azul Francia", "#22529A"),
            ("Rojo", "#E42A25"), ("Bordó", "#641F2B"), ("Amarillo Patito", "#FEF9B6"),
            ("Amarillo Oro", "#FBA044"), ("Naranja", "#FD5634"), ("Natural", "#FFF8EA"),
            ("Beige", "#F5CDA8"), ("Habano", "#7B4C38"), ("Marrón Claro", "#773B28"),
            ("Verde Oscuro", "#1F3628"), ("Verde Militar", "#4B4F3B"), ("Verde Agua", "#C6F4EA"),
            ("Mostaza", "#E5973A"), ("Violeta", "#52357A"), ("Salmón", "#FFB39E"),
            ("Rosa Dior", "#F98FB5"), ("Rosa Cristal", "#FDE8F1"), ("Celeste Claro", "#CBEAEF"),
            ("Yute", "#BBA999"), ("Pedrejón", "#E2CEB4"), ("Arena", "#EFE0CE"),
            ("Gris Pluma", "#C5BAAD"), ("L. Aceitunado", "#9B8C7A"), ("Verde Secreto", "#96937C"),
            ("Nuez", "#786C5F"), ("Beige Gamo", "#DEBA9F"), ("Violeta Pastel", "#D0C3EA"),
            ("Coral", "#FF5E74"), ("A. Indigo", "#324C53"), ("A. Navy", "#151D28"),
            ("Verde Atlantis", "#42C79E"), ("Maíz", "#FED192"), ("Naranja de Jaffa", "#FF6A39"),
            ("Ocre Quemado", "#D85237"), ("Petróleo", "#215551"), ("Fresa", "#FFAED2"),
            ("Teja", "#8A2D3A"), ("Hortensia", "#5E3A60"), ("Turquesa Claro", "#1EC9E8"),
            ("Rosa Plateado", "#F7B5BA"), ("Sandía", "#FF3B5D"), ("Suela (texturado)", "#A05F1D"),
            ("Mandarina", "#FF7B39"), ("Rosa Viejo (texturado)", "#8E4964"),
        };

        var existentes = await db.Colores.Select(c => new { c.Nombre, c.Slug }).ToListAsync();
        var nombresExistentes = existentes.Select(c => (c.Nombre ?? "").Trim().ToLowerInvariant()).ToHashSet();
        var slugsExistentes = existentes.Select(c => c.Slug).ToHashSet();

        foreach (var (nombre, hex) in paleta)
        {
            var slug = SlugHelper.Slugify(nombre);
            if (nombresExistentes.Contains(nombre.ToLowerInvariant()) || slugsExistentes.Contains(slug))
                continue;

            db.Colores.Add(new Color { Nombre = nombre, CodigoHex = hex, Slug = slug });
            nombresExistentes.Add(nombre.ToLowerInvariant());
            slugsExistentes.Add(slug);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Repara nombres públicos de productos Makor que quedaron cortados a mitad de palabra
    /// por el bug viejo del parser de unidades (ej "...CBX 20,5CM" → "...CB"). Idempotente.</summary>
    private static async Task RecalcularTitulosMakorCortadosAsync(AppDbContext db)
    {
        var makor = await db.Proveedores.FirstOrDefaultAsync(p => p.Slug == "makor");
        if (makor == null) return;

        var candidatos = await db.Productos
            .Where(p => p.ProveedorId == makor.Id && p.NombrePublico != null && p.NombrePublico != "")
            .ToListAsync();

        var cambios = 0;
        foreach (var p in candidatos)
        {
            var raw = (p.Nombre ?? "").Trim();
            var stored = p.NombrePublico!.Trim();

            var cortadoAMitadDePalabra =
                stored.Length > 0 &&
                stored.Length < raw.Length &&
                raw.StartsWith(stored, StringComparison.Ordinal) &&
                !char.IsWhiteSpace(raw[stored.Length]);

            if (cortadoAMitadDePalabra)
            {
                p.NombrePublico = null;
                cambios++;
            }
        }

        if (cambios > 0)
            await db.SaveChangesAsync();
    }

    private static async Task EnsureProductoVariosAsync(AppDbContext db)
    {
        if (await db.Productos.AnyAsync(p => p.EsVentaLibre))
            return;

        var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.Slug == "manual")
            ?? await db.Proveedores.FirstAsync();
        var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Nombre == "Complementos")
            ?? await db.Categorias.FirstAsync();

        db.Productos.Add(new Producto
        {
            CodigoMakor = "VARIOS",
            Nombre = "Varios",
            Slug = "varios",
            Descripcion = "Venta de ítems no catalogados. Solo uso interno en mostrador.",
            ModoPrecio = ModoPrecio.PRECIO_FIJO,
            ModoOrigenEconomico = ModoOrigenEconomico.SIN_COSTO,
            Activo = false,
            EsVentaLibre = true,
            CategoriaId = categoria.Id,
            ProveedorId = proveedor.Id,
        });
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
        var exists = await db.Usuarios.AnyAsync(u => u.Email.ToLower() == normalized && u.EliminadoEn == null);
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
        var user = await db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == normalized && u.EliminadoEn == null);
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
