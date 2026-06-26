using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;

namespace MMatilde.Api.Controllers;

[Route("api/productos")]
[ApiController]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductosController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ProductoAdminListResponse>> Get(
        [FromQuery] string? q, 
        [FromQuery] int? categoriaId, 
        [FromQuery] int? subcategoriaId,
        [FromQuery] int? proveedorId,
        [FromQuery] bool? activo, 
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var query = _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Subcategoria)
            .AsQueryable();

        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(p => 
                EF.Functions.ILike(p.Nombre, $"%{q}%") || 
                EF.Functions.ILike(p.CodigoMakor, $"%{q}%") ||
                (p.Categoria != null && EF.Functions.ILike(p.Categoria.Nombre, $"%{q}%")) ||
                (p.Subcategoria != null && EF.Functions.ILike(p.Subcategoria.Nombre, $"%{q}%")));
        }

        if (categoriaId.HasValue)
        {
            query = query.Where(p => p.CategoriaId == categoriaId);
        }
        if (subcategoriaId.HasValue)
        {
            query = query.Where(p => p.SubcategoriaId == subcategoriaId);
        }
        if (proveedorId.HasValue)
        {
            query = query.Where(p => p.ProveedorId == proveedorId);
        }
        if (activo.HasValue)
        {
            query = query.Where(p => p.Activo == activo);
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var items = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductoAdminDto(
                p.Id,
                p.CodigoMakor,
                p.Nombre,
                p.Categoria != null ? (p.Subcategoria != null ? p.Categoria.Nombre + " > " + p.Subcategoria.Nombre : p.Categoria.Nombre) : "",
                p.PrecioMayorista,
                p.PrecioMinorista,
                p.Activo,
                p.Destacado,
                p.UltimaSync
            ))
            .ToListAsync();

        return new ProductoAdminListResponse(items, total, page, pageSize, totalPages);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ProductoDetalleDto>> GetBySlug(string slug)
    {
        var prod = await _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Imagenes)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Activo);

        if (prod == null) return NotFound();

        var imgUrls = prod.Imagenes.OrderByDescending(i => i.EsPrincipal).ThenBy(i => i.Orden).Select(i => i.UrlOriginal!).ToList();

        return new ProductoDetalleDto(
            prod.Id,
            prod.Slug,
            prod.Nombre,
            prod.Descripcion,
            prod.Categoria?.Nombre ?? "",
            prod.Categoria?.Slug ?? "",
            imgUrls
        );
    }

    [HttpPut("{id}/toggle-activo")]
    [Authorize]
    public async Task<IActionResult> ToggleActivo(int id, [FromBody] ToggleRequest req)
    {
        var prod = await _db.Productos.FindAsync(id);
        if (prod == null) return NotFound();

        prod.Activo = req.Value;
        prod.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPut("{id}/toggle-destacado")]
    [Authorize]
    public async Task<IActionResult> ToggleDestacado(int id, [FromBody] ToggleRequest req)
    {
        var prod = await _db.Productos.FindAsync(id);
        if (prod == null) return NotFound();

        prod.Destacado = req.Value;
        prod.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPut("bulk-toggle")]
    [Authorize]
    public async Task<IActionResult> BulkToggle([FromBody] BulkToggleRequest req)
    {
        var prod = await _db.Productos.Where(p => req.Ids.Contains(p.Id)).ToListAsync();
        foreach (var p in prod)
        {
            p.Activo = req.Activo;
            p.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();

        return Ok(new { success = true, count = prod.Count });
    }

    [HttpGet("admin/{id}")]
    [Authorize]
    public async Task<ActionResult<Models.Producto>> GetById(int id)
    {
        var prod = await _db.Productos
            .Include(p => p.Imagenes)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (prod == null) return NotFound();
        return prod;
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult> Create([FromBody] ProductoCreateDto dto)
    {
        var prov = await _db.Proveedores.FirstOrDefaultAsync(p => p.Slug == "manual");
        if (prov == null) return BadRequest(new { message = "Proveedor 'Manual' no configurado en la BD." });

        var codigo = string.IsNullOrWhiteSpace(dto.Codigo) 
            ? "M-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper() 
            : dto.Codigo.Trim();

        if (await _db.Productos.AnyAsync(p => p.CodigoMakor == codigo))
            return BadRequest(new { message = "Ya existe un producto con ese código." });

        var slug = MMatilde.Api.Helpers.SlugHelper.Slugify(dto.Nombre + "-" + codigo);

        var p = new Models.Producto
        {
            Nombre = dto.Nombre,
            CodigoMakor = codigo,
            Slug = slug,
            CategoriaId = dto.CategoriaId,
            SubcategoriaId = dto.SubcategoriaId,
            Descripcion = dto.Descripcion,
            PrecioMayorista = dto.PrecioBase,
            PrecioMinorista = dto.PrecioBase * 1.21m * 1.70m, // Aprox
            Destacado = dto.Destacado,
            Activo = dto.Visible,
            ProveedorId = prov.Id
        };

        if (!string.IsNullOrEmpty(dto.ImagenUrl))
        {
            p.Imagenes.Add(new Models.ProductoImagen
            {
                UrlOriginal = dto.ImagenUrl,
                EsPrincipal = true,
                Orden = 0
            });
        }

        _db.Productos.Add(p);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult> Update(int id, [FromBody] ProductoUpdateDto dto)
    {
        var p = await _db.Productos.Include(pr => pr.Imagenes).FirstOrDefaultAsync(pr => pr.Id == id);
        if (p == null) return NotFound();

        var codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? p.CodigoMakor : dto.Codigo.Trim();
        
        if (codigo != p.CodigoMakor && await _db.Productos.AnyAsync(x => x.CodigoMakor == codigo))
            return BadRequest(new { message = "Ya existe un producto con ese código." });

        p.Nombre = dto.Nombre;
        p.CodigoMakor = codigo;
        p.CategoriaId = dto.CategoriaId;
        p.SubcategoriaId = dto.SubcategoriaId;
        p.Descripcion = dto.Descripcion;
        p.PrecioMayorista = dto.PrecioBase;
        p.PrecioMinorista = dto.PrecioBase * 1.21m * 1.70m;
        p.Destacado = dto.Destacado;
        p.Activo = dto.Visible;

        if (!string.IsNullOrEmpty(dto.ImagenUrl))
        {
            var img = p.Imagenes.FirstOrDefault(i => i.EsPrincipal);
            if (img == null)
            {
                p.Imagenes.Add(new Models.ProductoImagen { UrlOriginal = dto.ImagenUrl, EsPrincipal = true });
            }
            else
            {
                img.UrlOriginal = dto.ImagenUrl;
            }
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(int id)
    {
        var p = await _db.Productos.FindAsync(id);
        if (p == null) return NotFound();

        _db.Productos.Remove(p);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
