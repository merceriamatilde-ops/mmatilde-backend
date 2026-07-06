using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Helpers;
using MMatilde.Api.Models;

namespace MMatilde.Api.Controllers;

[ApiController]
[Route("api/tags")]
public class TagsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TagsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<TagDto>>> GetAll()
    {
        return await MapTags(_db.Tags.AsQueryable());
    }

    [HttpGet("activos")]
    public async Task<ActionResult<List<TagDto>>> GetActivos()
    {
        return await MapTags(_db.Tags.Where(t => t.Activo).OrderBy(t => t.Orden).ThenBy(t => t.Nombre));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TagDto>> Create([FromBody] TagCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { message = "El nombre es obligatorio." });

        var nombre = dto.Nombre.Trim();
        if (await _db.Tags.AnyAsync(t => t.Nombre.ToLower() == nombre.ToLower()))
            return BadRequest(new { message = "Ya existe un tag con ese nombre." });

        var tag = new Tag
        {
            Nombre = nombre,
            Slug = SlugHelper.Slugify(nombre),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? null : dto.ColorHex.Trim(),
            VisibleEnCatalogo = dto.VisibleEnCatalogo,
            Orden = dto.Orden,
            Activo = dto.Activo
        };

        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();

        return (await MapTags(_db.Tags.Where(t => t.Id == tag.Id))).First();
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<TagDto>> Update(int id, [FromBody] TagUpdateDto dto)
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag == null) return NotFound();

        var nombre = dto.Nombre.Trim();
        if (await _db.Tags.AnyAsync(t => t.Id != id && t.Nombre.ToLower() == nombre.ToLower()))
            return BadRequest(new { message = "Ya existe otro tag con ese nombre." });

        tag.Nombre = nombre;
        tag.Slug = SlugHelper.Slugify(nombre);
        tag.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
        tag.ColorHex = string.IsNullOrWhiteSpace(dto.ColorHex) ? null : dto.ColorHex.Trim();
        tag.VisibleEnCatalogo = dto.VisibleEnCatalogo;
        tag.Orden = dto.Orden;
        tag.Activo = dto.Activo;
        tag.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (await MapTags(_db.Tags.Where(t => t.Id == id))).First();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag == null) return NotFound();

        _db.Tags.Remove(tag);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<List<TagDto>> MapTags(IQueryable<Tag> query)
    {
        return await query
            .OrderBy(t => t.Orden)
            .ThenBy(t => t.Nombre)
            .Select(t => new TagDto(
                t.Id,
                t.Nombre,
                t.Slug,
                t.Descripcion,
                t.ColorHex,
                t.VisibleEnCatalogo,
                t.Orden,
                t.Activo,
                t.Productos.Count(pt => pt.Producto.Activo)
            ))
            .ToListAsync();
    }
}
