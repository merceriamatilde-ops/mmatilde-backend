using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Helpers;
using MMatilde.Api.Models;
using MMatilde.Api.Services;

namespace MMatilde.Api.Controllers;

[ApiController]
[Route("api/precios")]
[Authorize]
public class PreciosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PricingService _pricing;

    public PreciosController(AppDbContext db, PricingService pricing)
    {
        _db = db;
        _pricing = pricing;
    }

    [HttpGet("config")]
    public async Task<ActionResult<PrecioConfigDto>> GetConfig()
    {
        return new PrecioConfigDto(
            await _pricing.GetIvaPorcentajeAsync(),
            await _pricing.GetMargenGlobalAsync()
        );
    }

    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] PrecioConfigDto dto)
    {
        await UpsertConfig(PricingService.ConfigIva, dto.IvaPorcentaje.ToString("0.##"));
        await UpsertConfig(PricingService.ConfigMargenGlobal, dto.MargenGlobal.ToString("0.##"));
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("reglas")]
    public async Task<ActionResult<List<ReglaPrecioDto>>> GetReglas()
    {
        var reglas = await _db.ReglasPrecio
            .OrderByDescending(r => r.Activo)
            .ThenBy(r => r.CategoriaId)
            .ToListAsync();

        var catIds = reglas.Where(r => r.CategoriaId.HasValue).Select(r => r.CategoriaId!.Value).Distinct().ToList();
        var subIds = reglas.Where(r => r.SubcategoriaId.HasValue).Select(r => r.SubcategoriaId!.Value).Distinct().ToList();

        var cats = await _db.Categorias.Where(c => catIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Nombre);
        var subs = await _db.Subcategorias.Where(s => subIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Nombre);

        return reglas.Select(r => new ReglaPrecioDto(
            r.Id,
            r.CategoriaId,
            r.CategoriaId.HasValue ? cats.GetValueOrDefault(r.CategoriaId.Value) : null,
            r.SubcategoriaId,
            r.SubcategoriaId.HasValue ? subs.GetValueOrDefault(r.SubcategoriaId.Value) : null,
            r.MarcaId,
            r.MargenPorcentaje,
            r.Tipo.ToString(),
            r.Activo
        )).ToList();
    }

    [HttpPost("reglas")]
    public async Task<ActionResult<ReglaPrecioDto>> CreateRegla([FromBody] ReglaPrecioCreateDto dto)
    {
        var regla = new ReglaPrecio
        {
            CategoriaId = dto.CategoriaId,
            SubcategoriaId = dto.SubcategoriaId,
            MarcaId = dto.MarcaId,
            MargenPorcentaje = dto.MargenPorcentaje,
            Tipo = dto.Tipo,
            Activo = true
        };
        _db.ReglasPrecio.Add(regla);
        await _db.SaveChangesAsync();

        string? catNombre = null;
        string? subNombre = null;
        if (regla.CategoriaId.HasValue)
            catNombre = await _db.Categorias.Where(c => c.Id == regla.CategoriaId).Select(c => c.Nombre).FirstOrDefaultAsync();
        if (regla.SubcategoriaId.HasValue)
            subNombre = await _db.Subcategorias.Where(s => s.Id == regla.SubcategoriaId).Select(s => s.Nombre).FirstOrDefaultAsync();

        return new ReglaPrecioDto(regla.Id, regla.CategoriaId, catNombre, regla.SubcategoriaId, subNombre,
            regla.MarcaId, regla.MargenPorcentaje, regla.Tipo.ToString(), regla.Activo);
    }

    [HttpPut("reglas/{id}")]
    public async Task<IActionResult> UpdateRegla(int id, [FromBody] ReglaPrecioCreateDto dto)
    {
        var regla = await _db.ReglasPrecio.FindAsync(id);
        if (regla == null) return NotFound();

        regla.CategoriaId = dto.CategoriaId;
        regla.SubcategoriaId = dto.SubcategoriaId;
        regla.MarcaId = dto.MarcaId;
        regla.MargenPorcentaje = dto.MargenPorcentaje;
        regla.Tipo = dto.Tipo;
        regla.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("reglas/{id}")]
    public async Task<IActionResult> DeleteRegla(int id)
    {
        var regla = await _db.ReglasPrecio.FindAsync(id);
        if (regla == null) return NotFound();
        _db.ReglasPrecio.Remove(regla);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("producto/{id}")]
    public async Task<ActionResult<ProductoUnidadesDto>> GetProductoUnidades(int id)
    {
        var prod = await _db.Productos
            .Include(p => p.Presentaciones.OrderBy(x => x.Orden))
            .FirstOrDefaultAsync(p => p.Id == id);
        if (prod == null) return NotFound();

        var changed = false;
        if (prod.UnidadCompraAutoDetectada && UnidadParser.TryApplyTo(prod, prod.Nombre))
            changed = true;

        if (await _pricing.EnsurePresentacionVentaListaAsync(prod))
            changed = true;

        if (changed)
        {
            prod.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return await MapProductoUnidades(prod);
    }

    [HttpPut("producto/{id}")]
    public async Task<ActionResult<ProductoUnidadesDto>> UpdateProductoUnidades(int id, [FromBody] ProductoUnidadesUpdateDto dto)
    {
        var prod = await _db.Productos
            .Include(p => p.Presentaciones)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (prod == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.UnidadBase) && Enum.TryParse<UnidadMedida>(dto.UnidadBase, true, out var ub))
            prod.UnidadBase = ub;
        else if (string.IsNullOrWhiteSpace(dto.UnidadBase))
            prod.UnidadBase = null;

        prod.CantidadUnidadCompra = dto.CantidadUnidadCompra;
        prod.EtiquetaUnidadCompra = string.IsNullOrWhiteSpace(dto.EtiquetaUnidadCompra) ? null : dto.EtiquetaUnidadCompra.Trim();
        if (dto.UnidadCompraAutoDetectada.HasValue)
            prod.UnidadCompraAutoDetectada = dto.UnidadCompraAutoDetectada.Value;

        if (!string.IsNullOrWhiteSpace(dto.ModoPrecio) && Enum.TryParse<ModoPrecio>(dto.ModoPrecio, true, out var modo))
            prod.ModoPrecio = modo;

        prod.IvaPorcentajeProducto = dto.IvaPorcentajeProducto;
        prod.MargenPorcentajeProducto = dto.MargenPorcentajeProducto;

        if (!string.IsNullOrWhiteSpace(dto.ModoOrigenEconomico) &&
            Enum.TryParse<ModoOrigenEconomico>(dto.ModoOrigenEconomico, true, out var origen))
            prod.ModoOrigenEconomico = origen;

        prod.ComisionTiendaPorcentaje = dto.ComisionTiendaPorcentaje;
        prod.TitularConsignacion = string.IsNullOrWhiteSpace(dto.TitularConsignacion)
            ? null
            : dto.TitularConsignacion.Trim();
        prod.CostoMateriales = dto.CostoMateriales;
        prod.ManoObra = dto.ManoObra;
        prod.MargenElaboracionPorcentaje = dto.MargenElaboracionPorcentaje;
        prod.MargenElaboracionMonto = dto.MargenElaboracionMonto;

        if (dto.PrecioCompra.HasValue && dto.PrecioCompra.Value > 0)
            prod.PrecioMayorista = dto.PrecioCompra.Value;

        if (dto.Presentaciones != null)
        {
            var incomingIds = dto.Presentaciones.Where(p => p.Id.HasValue && p.Id > 0).Select(p => p.Id!.Value).ToList();
            var toRemove = prod.Presentaciones.Where(p => !incomingIds.Contains(p.Id)).ToList();
            _db.ProductoPresentaciones.RemoveRange(toRemove);

            foreach (var input in dto.Presentaciones)
            {
                if (input.Id.HasValue && input.Id > 0)
                {
                    var existing = prod.Presentaciones.FirstOrDefault(p => p.Id == input.Id.Value);
                    if (existing == null) continue;
                    existing.Nombre = input.Nombre.Trim();
                    existing.CantidadUnidadBase = input.CantidadUnidadBase;
                    existing.PrecioVenta = input.PrecioVenta;
                    existing.MargenPorcentaje = input.MargenPorcentaje;
                    existing.EsDefault = input.EsDefault;
                    existing.Activo = input.Activo;
                    existing.Orden = input.Orden;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    prod.Presentaciones.Add(new ProductoPresentacion
                    {
                        Nombre = input.Nombre.Trim(),
                        CantidadUnidadBase = input.CantidadUnidadBase,
                        PrecioVenta = input.PrecioVenta,
                        MargenPorcentaje = input.MargenPorcentaje,
                        EsDefault = input.EsDefault,
                        Activo = input.Activo,
                        Orden = input.Orden
                    });
                }
            }

            if (prod.Presentaciones.Any(p => p.EsDefault))
            {
                var defaultId = prod.Presentaciones.First(p => p.EsDefault).Id;
                foreach (var p in prod.Presentaciones.Where(p => p.Id != defaultId && p.Id != 0))
                    p.EsDefault = false;
            }
        }

        if (prod.ModoOrigenEconomico == ModoOrigenEconomico.ELABORACION_PROPIA)
        {
            var precioCalc = GananciaService.CalcularPrecioElaboracion(prod);
            if (precioCalc.HasValue)
            {
                var activas = prod.Presentaciones.Where(p => p.Activo).ToList();
                if (activas.Count == 0)
                {
                    prod.Presentaciones.Add(new ProductoPresentacion
                    {
                        Nombre = "Unidad",
                        CantidadUnidadBase = 1,
                        PrecioVenta = precioCalc,
                        EsDefault = true,
                        Activo = true,
                        Orden = 0,
                    });
                }
                else
                {
                    foreach (var p in activas)
                    {
                        p.PrecioVenta = precioCalc;
                        p.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
        }

        if (dto.RecalcularPrecios)
            await _pricing.RecalcularPresentacionesAsync(prod);

        prod.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await MapProductoUnidades(prod);
    }

    [HttpPost("producto/{id}/detectar-unidad")]
    public async Task<ActionResult<UnidadSugeridaDto>> DetectarUnidad(int id)
    {
        var prod = await _db.Productos.FindAsync(id);
        if (prod == null) return NotFound();

        var detected = UnidadParser.TryParse(prod.Nombre);
        if (detected == null) return NotFound(new { message = "No se detectó unidad en el título." });

        return new UnidadSugeridaDto(
            detected.UnidadBase.ToString(),
            detected.CantidadUnidadCompra,
            detected.Etiqueta,
            detected.Confiable
        );
    }

    [HttpPost("producto/{id}/recalcular")]
    public async Task<ActionResult<ProductoUnidadesDto>> Recalcular(int id)
    {
        var prod = await _db.Productos
            .Include(p => p.Presentaciones)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (prod == null) return NotFound();

        await _pricing.RecalcularPresentacionesAsync(prod);
        prod.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await MapProductoUnidades(prod);
    }

    private async Task<ProductoUnidadesDto> MapProductoUnidades(Producto prod)
    {
        var margen = await _pricing.ResolveMargenAsync(prod);
        var costoBase = _pricing.CostoPorUnidadBase(prod);
        var iva = await _pricing.ResolveIvaAsync(prod);
        var ivaGlobal = await _pricing.GetIvaPorcentajeAsync();
        var margenGlobal = await _pricing.GetMargenGlobalAsync();

        var presentaciones = new List<PresentacionDto>();
        GananciaEstimadaDto? ganancia = null;
        foreach (var p in prod.Presentaciones.OrderBy(x => x.Orden))
        {
            var calculado = await _pricing.CalcularPrecioVentaAsync(prod, p);
            presentaciones.Add(new PresentacionDto(
                p.Id,
                p.Nombre,
                p.CantidadUnidadBase,
                p.PrecioVenta,
                p.MargenPorcentaje,
                calculado,
                p.EsDefault,
                p.Activo,
                p.Orden
            ));

            if (p.EsDefault || ganancia == null)
            {
                var precioRef = p.PrecioVenta ?? calculado;
                if (precioRef.HasValue)
                {
                    decimal? costoCompra = null;
                    if (costoBase.HasValue)
                        costoCompra = costoBase.Value * p.CantidadUnidadBase;
                    ganancia = GananciaService.Estimar(prod, precioRef.Value, costoCompra, iva);
                }
            }
        }

        var venta = await _pricing.ResolverPrecioVentaDefaultAsync(prod);

        if (ganancia == null && venta.PrecioVentaFinal.HasValue)
        {
            decimal? costoCompra = null;
            if (costoBase.HasValue)
            {
                costoCompra = costoBase.Value * venta.CantidadReferencia;
            }
            ganancia = GananciaService.Estimar(prod, venta.PrecioVentaFinal.Value, costoCompra, iva);
        }

        return new ProductoUnidadesDto(
            prod.UnidadBase?.ToString(),
            prod.CantidadUnidadCompra,
            prod.EtiquetaUnidadCompra,
            prod.UnidadCompraAutoDetectada,
            prod.PrecioMayorista,
            costoBase,
            iva,
            margen,
            prod.ModoPrecio.ToString(),
            prod.IvaPorcentajeProducto,
            prod.MargenPorcentajeProducto,
            ivaGlobal,
            margenGlobal,
            prod.ModoOrigenEconomico.ToString(),
            prod.ComisionTiendaPorcentaje,
            prod.TitularConsignacion,
            prod.CostoMateriales,
            prod.ManoObra,
            prod.MargenElaboracionPorcentaje,
            prod.MargenElaboracionMonto,
            ganancia,
            venta.PrecioVentaFinal,
            venta.PrecioVentaPorUnidad,
            venta.CantidadReferencia,
            venta.PresentacionNombre,
            presentaciones
        );
    }

    private async Task UpsertConfig(string clave, string valor)
    {
        var cfg = await _db.ConfiguracionSitio.FirstOrDefaultAsync(c => c.Clave == clave);
        if (cfg == null)
        {
            _db.ConfiguracionSitio.Add(new ConfiguracionSitio
            {
                Clave = clave,
                Valor = valor,
                Tipo = "number",
                Grupo = "precios",
                Label = clave == PricingService.ConfigIva ? "IVA %" : "Margen global %"
            });
        }
        else
        {
            cfg.Valor = valor;
            cfg.UpdatedAt = DateTime.UtcNow;
        }
    }
}
