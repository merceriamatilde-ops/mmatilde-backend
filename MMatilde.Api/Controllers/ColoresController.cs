using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.Models;

namespace MMatilde.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ColoresController : ControllerBase
{
    private readonly AppDbContext _db;

    public ColoresController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Color>>> Get()
    {
        return await _db.Colores.OrderBy(c => c.Nombre).ToListAsync();
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<Color>> Create([FromBody] ColorDto dto)
    {
        if (await _db.Colores.AnyAsync(c => c.Nombre.ToLower() == dto.Nombre.ToLower()))
            return BadRequest(new { message = "Ya existe un color con ese nombre." });

        var slug = MMatilde.Api.Helpers.SlugHelper.Slugify(dto.Nombre);
        
        var color = new Color
        {
            Nombre = dto.Nombre,
            CodigoHex = dto.CodigoHex,
            Slug = slug
        };

        _db.Colores.Add(color);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = color.Id }, color);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(int id)
    {
        var color = await _db.Colores.FindAsync(id);
        if (color == null) return NotFound();

        // Verificar si está en uso
        if (await _db.ProductoVariantes.AnyAsync(v => v.ColorId == id))
        {
            return BadRequest(new { message = "No se puede eliminar el color porque está siendo usado por uno o más productos." });
        }

        _db.Colores.Remove(color);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(int id, [FromBody] ColorDto dto)
    {
        var color = await _db.Colores.FindAsync(id);
        if (color == null) return NotFound();

        // Check if new name exists in another color
        if (await _db.Colores.AnyAsync(c => c.Id != id && c.Nombre.ToLower() == dto.Nombre.ToLower()))
            return BadRequest(new { message = "Ya existe otro color con ese nombre." });

        color.Nombre = dto.Nombre;
        color.CodigoHex = dto.CodigoHex;
        color.Slug = MMatilde.Api.Helpers.SlugHelper.Slugify(dto.Nombre);

        await _db.SaveChangesAsync();

        return Ok(color);
    }
}

public class ColorDto
{
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoHex { get; set; }
}
