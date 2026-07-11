using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Models;
using System.Security.Claims;

namespace MMatilde.Api.Controllers;

[Route("api/usuarios")]
[ApiController]
[Authorize(Roles = "ADMIN")]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public UsuariosController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioListDto>>> List()
    {
        var items = await _db.Usuarios
            .OrderBy(u => u.Nombre)
            .Select(u => new UsuarioListDto(
                u.Id,
                u.Email,
                u.Nombre,
                u.Rol.ToString(),
                u.Activo,
                u.CreatedAt))
            .ToListAsync();

        return items;
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioListDto>> Create([FromBody] UsuarioCreateDto dto)
    {
        var email = NormalizeEmail(dto.Email);
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "El email es obligatorio." });
        if (string.IsNullOrWhiteSpace(dto.Nombre?.Trim()))
            return BadRequest(new { message = "El nombre es obligatorio." });
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            return BadRequest(new { message = "La contraseña debe tener al menos 8 caracteres." });
        if (!TryParseRol(dto.Rol, out var rol))
            return BadRequest(new { message = "Rol inválido." });

        if (await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email))
            return BadRequest(new { message = "Ya existe un usuario con ese email." });

        var user = new Usuario
        {
            Email = email,
            Nombre = dto.Nombre.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 12),
            Rol = rol,
            Activo = true,
        };

        _db.Usuarios.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new UsuarioListDto(
            user.Id, user.Email, user.Nombre, user.Rol.ToString(), user.Activo, user.CreatedAt));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UsuarioListDto>> Update(Guid id, [FromBody] UsuarioUpdateDto dto)
    {
        var currentId = GetUserId();
        var user = await _db.Usuarios.FindAsync(id);
        if (user == null) return NotFound();

        var email = NormalizeEmail(dto.Email);
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "El email es obligatorio." });
        if (string.IsNullOrWhiteSpace(dto.Nombre?.Trim()))
            return BadRequest(new { message = "El nombre es obligatorio." });
        if (!TryParseRol(dto.Rol, out var rol))
            return BadRequest(new { message = "Rol inválido." });

        if (await _db.Usuarios.AnyAsync(u => u.Id != id && u.Email.ToLower() == email))
            return BadRequest(new { message = "Ya existe otro usuario con ese email." });

        if (IsSuperAdmin(user.Email))
        {
            if (email != NormalizeEmail(user.Email))
                return BadRequest(new { message = "No se puede cambiar el email del superadmin." });
            if (!dto.Activo)
                return BadRequest(new { message = "No se puede desactivar el superadmin." });
            if (rol != RolUsuario.ADMIN)
                return BadRequest(new { message = "El superadmin debe mantener rol ADMIN." });
        }

        if (currentId == id)
        {
            if (!dto.Activo)
                return BadRequest(new { message = "No podés desactivar tu propia cuenta." });
            if (user.Rol == RolUsuario.ADMIN && rol != RolUsuario.ADMIN)
                return BadRequest(new { message = "No podés quitarte el rol de administrador a vos mismo." });
        }

        if (user.Rol == RolUsuario.ADMIN && (rol != RolUsuario.ADMIN || !dto.Activo))
        {
            var otrosAdminsActivos = await _db.Usuarios.CountAsync(u =>
                u.Id != id && u.Rol == RolUsuario.ADMIN && u.Activo);
            if (otrosAdminsActivos == 0)
                return BadRequest(new { message = "Debe quedar al menos un administrador activo." });
        }

        user.Email = email;
        user.Nombre = dto.Nombre.Trim();
        user.Rol = rol;
        user.Activo = dto.Activo;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new UsuarioListDto(user.Id, user.Email, user.Nombre, user.Rol.ToString(), user.Activo, user.CreatedAt);
    }

    [HttpPut("{id:guid}/password")]
    public async Task<IActionResult> SetPassword(Guid id, [FromBody] UsuarioPasswordDto dto)
    {
        var user = await _db.Usuarios.FindAsync(id);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            return BadRequest(new { message = "La contraseña debe tener al menos 8 caracteres." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 12);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var currentId = GetUserId();
        var user = await _db.Usuarios.FindAsync(id);
        if (user == null) return NotFound();

        if (IsSuperAdmin(user.Email))
            return BadRequest(new { message = "No se puede eliminar el superadmin." });

        if (currentId == id)
            return BadRequest(new { message = "No podés eliminar tu propia cuenta." });

        _db.Usuarios.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private bool IsSuperAdmin(string email) =>
        NormalizeEmail(email) == NormalizeEmail(_config["AdminEmail"] ?? "admin@mmatilde.com");

    private static string NormalizeEmail(string? email) =>
        email?.Trim().ToLowerInvariant() ?? string.Empty;

    private static bool TryParseRol(string? raw, out RolUsuario rol)
    {
        rol = RolUsuario.VIEWER;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return Enum.TryParse(raw.Trim().ToUpperInvariant(), out rol);
    }

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
