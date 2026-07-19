using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Models;

namespace MMatilde.Api.Services;

public class PermisosModulosService
{
    public const string ConfigClave = "bo_permisos_modulos";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static readonly string[] RolesValidos = ["ADMIN", "VIEWER"];

    public static readonly ModuloDefDto[] Definiciones =
    [
        new("dashboard", "Dashboard", false),
        new("ventas", "Ventas", false),
        new("productos", "Productos", false),
        new("categorias", "Categorías", false),
        new("banners", "Banners", false),
        new("tags", "Tags", false),
        new("colores", "Colores", false),
        new("precios", "Precios", false),
        new("sync", "Sincronización Makor", false),
        new("estadisticas", "Estadísticas", false),
        new("ia", "Asistente IA", false),
        new("configuracion", "Configuración", true),
        new("usuarios", "Usuarios", true),
    ];

    private static readonly HashSet<string> ModulosBloqueados = Definiciones
        .Where(d => d.Bloqueado)
        .Select(d => d.Key)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly AppDbContext _db;

    public PermisosModulosService(AppDbContext db) => _db = db;

    public static Dictionary<string, ModuloPermisoDto> Defaults() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["dashboard"] = new(true, ["ADMIN", "VIEWER"]),
            ["ventas"] = new(true, ["ADMIN", "VIEWER"]),
            ["productos"] = new(true, ["ADMIN", "VIEWER"]),
            ["categorias"] = new(true, ["ADMIN"]),
            ["tags"] = new(true, ["ADMIN"]),
            ["colores"] = new(true, ["ADMIN"]),
            ["precios"] = new(true, ["ADMIN"]),
            ["sync"] = new(true, ["ADMIN"]),
            ["estadisticas"] = new(true, ["ADMIN"]),
            ["ia"] = new(true, ["ADMIN"]),
            ["configuracion"] = new(true, ["ADMIN"]),
            ["usuarios"] = new(true, ["ADMIN"]),
        };

    public async Task<PermisosModulosDto> GetAsync()
    {
        var merged = Defaults();
        var raw = await _db.ConfiguracionSitio
            .Where(c => c.Clave == ConfigClave)
            .Select(c => c.Valor)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                var saved = JsonSerializer.Deserialize<Dictionary<string, ModuloPermisoDto>>(raw, JsonOpts);
                if (saved != null)
                {
                    foreach (var (key, value) in saved)
                    {
                        if (merged.ContainsKey(key))
                            merged[key] = Sanitize(key, value);
                    }
                }
            }
            catch
            {
                // ignore corrupt JSON, use defaults
            }
        }

        EnforceLocked(merged);
        return new PermisosModulosDto(merged);
    }

    public async Task<PermisosModulosDto> SaveAsync(PermisosModulosUpdateDto dto)
    {
        var merged = Defaults();
        foreach (var (key, value) in dto.Modulos)
        {
            if (merged.ContainsKey(key))
                merged[key] = Sanitize(key, value);
        }

        EnforceLocked(merged);

        var json = JsonSerializer.Serialize(merged, JsonOpts);
        var row = await _db.ConfiguracionSitio.FirstOrDefaultAsync(c => c.Clave == ConfigClave);
        if (row == null)
        {
            _db.ConfiguracionSitio.Add(new ConfiguracionSitio
            {
                Clave = ConfigClave,
                Valor = json,
                Grupo = "Backoffice",
                Label = "Permisos por módulo",
                Tipo = "json",
            });
        }
        else
        {
            row.Valor = json;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return new PermisosModulosDto(merged);
    }

    public async Task<bool> CanAccessAsync(string? rol, string moduloKey)
    {
        if (string.Equals(rol, "ADMIN", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(rol))
            return false;

        var config = await GetAsync();
        if (!config.Modulos.TryGetValue(moduloKey, out var mod) || !mod.Habilitado)
            return false;

        return mod.Roles.Any(r => string.Equals(r, rol, StringComparison.OrdinalIgnoreCase));
    }

    private static ModuloPermisoDto Sanitize(string key, ModuloPermisoDto raw)
    {
        if (ModulosBloqueados.Contains(key))
            return new ModuloPermisoDto(true, ["ADMIN"]);

        var roles = raw.Roles?
            .Where(r => RolesValidos.Contains(r.ToUpperInvariant()))
            .Select(r => r.ToUpperInvariant())
            .Distinct()
            .ToList() ?? [];

        if (roles.Count == 0)
            roles = ["ADMIN"];

        return new ModuloPermisoDto(raw.Habilitado, roles);
    }

    private static void EnforceLocked(Dictionary<string, ModuloPermisoDto> modulos)
    {
        foreach (var key in ModulosBloqueados)
            modulos[key] = new ModuloPermisoDto(true, ["ADMIN"]);
    }
}
