using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;

namespace MMatilde.Api.Controllers;

[Route("api/categorias")]
[ApiController]
public class CategoriasController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriasController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoriaConConteoDto>>> Get([FromQuery] bool includeEmpty = false)
    {
        var query = _db.Categorias
            .Where(c => c.Activo)
            .Select(c => new
            {
                Cat = c,
                Count = c.Productos.Count(p => p.Activo)
            });

        if (!includeEmpty)
        {
            query = query.Where(x => x.Count > 0);
        }

        var result = await query

            .OrderBy(x => x.Cat.Orden)
            .Select(x => new CategoriaConConteoDto(
                x.Cat.Id,
                x.Cat.Nombre,
                x.Cat.Slug,
                x.Cat.Icono,
                x.Count
            ))
            .ToListAsync();

        return result;
    }

    [HttpGet("{slug}/productos")]
    public async Task<ActionResult<CategoriaCatalogoResponseDto>> GetProductos(string slug, [FromQuery] string? sub)
    {
        var cat = await _db.Categorias
            .FirstOrDefaultAsync(c => c.Slug == slug && c.Activo);
            
        if (cat == null) return NotFound();

        var query = _db.Productos
            .Include(p => p.Imagenes)
            .Where(p => p.CategoriaId == cat.Id && p.Activo);

        Console.WriteLine($"\n--- GET PRODUCTOS --- SLUG: {slug}, SUB: '{sub}'\n");

        if (!string.IsNullOrWhiteSpace(sub))
        {
            Console.WriteLine($"--- APLICANDO FILTRO SUB: {sub}");
            query = query.Where(p => p.Subcategoria != null && p.Subcategoria.Slug == sub);
        }

        var prods = await query
            .OrderByDescending(p => p.Id)
            .Select(p => new ProductoCatalogoDto(
                p.Id,
                p.Slug,
                p.Nombre,
                cat.Nombre,
                p.Imagenes.OrderByDescending(i => i.EsPrincipal).ThenBy(i => i.Orden).Select(i => i.UrlOriginal).FirstOrDefault()
            ))
            .ToListAsync();

        var subs = await _db.Subcategorias
            .Where(s => s.CategoriaId == cat.Id && _db.Productos.Any(p => p.SubcategoriaId == s.Id && p.Activo))
            .OrderBy(s => s.Orden)
            .Select(s => new SubcategoriaCatalogoDto(s.Id, s.Nombre, s.Slug))
            .ToListAsync();

        return new CategoriaCatalogoResponseDto(
            cat.Id,
            cat.Nombre,
            cat.Slug,
            cat.Icono,
            subs,
            prods
        );
    }

    [HttpGet("admin")]
    public async Task<ActionResult<List<CategoriaAdminDto>>> GetAdmin()
    {
        var result = await _db.Categorias
            .Include(c => c.Subcategorias)
            .OrderBy(c => c.Orden)
            .Select(c => new CategoriaAdminDto(
                c.Id,
                c.Nombre,
                c.Slug,
                c.EsMakor,
                c.Productos.Count,
                c.Subcategorias.OrderBy(s => s.Orden).Select(s => new SubcategoriaAdminDto(
                    s.Id,
                    s.Nombre,
                    s.Slug,
                    s.EsMakor,
                    s.Productos.Count
                )).ToList()
            ))
            .ToListAsync();

        return result;
    }

    [HttpPost]
    public async Task<ActionResult> CreateCategoria([FromBody] CategoriaCreateDto dto)
    {
        var slug = MMatilde.Api.Helpers.SlugHelper.Slugify(dto.Nombre);
        if (await _db.Categorias.AnyAsync(c => c.Slug == slug))
            return BadRequest(new { message = "Ya existe una categoría con ese nombre." });

        var cat = new Models.Categoria
        {
            Nombre = dto.Nombre,
            Slug = slug,
            EsMakor = false
        };
        _db.Categorias.Add(cat);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateCategoria(int id, [FromBody] CategoriaUpdateDto dto)
    {
        var cat = await _db.Categorias.FindAsync(id);
        if (cat == null) return NotFound();

        var tempSlug = MMatilde.Api.Helpers.SlugHelper.Slugify(dto.Nombre);
        if (await _db.Categorias.AnyAsync(c => c.Slug == tempSlug && c.Id != id))
            return BadRequest(new { message = "Ya existe otra categoría con ese nombre." });

        cat.Nombre = dto.Nombre;
        // IMPORTANTE: NO actualizar el Slug para no romper la sincronización si es de Makor, 
        // y para no romper URLs si es manual.
        
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCategoria(int id)
    {
        var cat = await _db.Categorias.FindAsync(id);
        if (cat == null) return NotFound();

        if (cat.EsMakor)
            return BadRequest(new { message = "No se puede eliminar una categoría sincronizada desde Makor." });

        if (await _db.Productos.AnyAsync(p => p.CategoriaId == id))
            return BadRequest(new { message = "No se puede eliminar la categoría porque tiene productos asignados." });

        if (await _db.Subcategorias.AnyAsync(s => s.CategoriaId == id))
            return BadRequest(new { message = "No se puede eliminar la categoría porque tiene subcategorías." });

        _db.Categorias.Remove(cat);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("subcategorias")]
    public async Task<ActionResult> CreateSubcategoria([FromBody] SubcategoriaCreateDto dto)
    {
        var cat = await _db.Categorias.FindAsync(dto.CategoriaId);
        if (cat == null) return NotFound();

        var slug = MMatilde.Api.Helpers.SlugHelper.Slugify(dto.Nombre);
        if (await _db.Subcategorias.AnyAsync(s => s.Slug == slug && s.CategoriaId == dto.CategoriaId))
            return BadRequest(new { message = "Ya existe una subcategoría con ese nombre en esta categoría." });

        var sub = new Models.Subcategoria
        {
            Nombre = dto.Nombre,
            Slug = slug,
            CategoriaId = dto.CategoriaId,
            EsMakor = false
        };
        _db.Subcategorias.Add(sub);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("subcategorias/{id}")]
    public async Task<ActionResult> UpdateSubcategoria(int id, [FromBody] SubcategoriaUpdateDto dto)
    {
        var sub = await _db.Subcategorias.FindAsync(id);
        if (sub == null) return NotFound();

        var tempSlug = MMatilde.Api.Helpers.SlugHelper.Slugify(dto.Nombre);
        if (await _db.Subcategorias.AnyAsync(s => s.Slug == tempSlug && s.CategoriaId == sub.CategoriaId && s.Id != id))
            return BadRequest(new { message = "Ya existe otra subcategoría con ese nombre en esta categoría." });

        sub.Nombre = dto.Nombre;
        
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("subcategorias/{id}")]
    public async Task<ActionResult> DeleteSubcategoria(int id)
    {
        var sub = await _db.Subcategorias.FindAsync(id);
        if (sub == null) return NotFound();

        if (sub.EsMakor)
            return BadRequest(new { message = "No se puede eliminar una subcategoría sincronizada desde Makor." });

        if (await _db.Productos.AnyAsync(p => p.SubcategoriaId == id))
            return BadRequest(new { message = "No se puede eliminar la subcategoría porque tiene productos asignados." });

        _db.Subcategorias.Remove(sub);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
