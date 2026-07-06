using Microsoft.EntityFrameworkCore;

using MMatilde.Api.Data;

using MMatilde.Api.Models;



namespace MMatilde.Api.Services;



public class PricingService

{

    private readonly AppDbContext _db;



    public const string ConfigIva = "precio_iva_porcentaje";

    public const string ConfigMargenGlobal = "precio_margen_global";



    public PricingService(AppDbContext db) => _db = db;



    public async Task<decimal> GetIvaPorcentajeAsync()

    {

        var cfg = await _db.ConfiguracionSitio.FirstOrDefaultAsync(c => c.Clave == ConfigIva);

        return decimal.TryParse(cfg?.Valor, out var n) ? n : 21m;

    }



    public async Task<decimal> GetMargenGlobalAsync()

    {

        var cfg = await _db.ConfiguracionSitio.FirstOrDefaultAsync(c => c.Clave == ConfigMargenGlobal);

        return decimal.TryParse(cfg?.Valor, out var n) ? n : 115m;

    }



    public async Task<decimal> ResolveIvaAsync(Producto producto)

    {

        if (producto.ModoPrecio == ModoPrecio.PRECIO_FIJO)

            return producto.IvaPorcentajeProducto ?? 0m;



        if (producto.ModoPrecio == ModoPrecio.EXCEPCION && producto.IvaPorcentajeProducto.HasValue)

            return producto.IvaPorcentajeProducto.Value;



        return await GetIvaPorcentajeAsync();

    }



    public async Task<decimal> ResolveMargenAsync(Producto producto, decimal? margenPresentacion = null)

    {

        if (margenPresentacion.HasValue) return margenPresentacion.Value;



        if (producto.ModoPrecio == ModoPrecio.PRECIO_FIJO)

            return 0m;



        if (producto.ModoPrecio == ModoPrecio.EXCEPCION && producto.MargenPorcentajeProducto.HasValue)

            return producto.MargenPorcentajeProducto.Value;



        var regla = await _db.ReglasPrecio

            .Where(r => r.Activo)

            .Where(r =>

                (r.SubcategoriaId != null && r.SubcategoriaId == producto.SubcategoriaId) ||

                (r.SubcategoriaId == null && r.CategoriaId != null && r.CategoriaId == producto.CategoriaId) ||

                (r.SubcategoriaId == null && r.CategoriaId == null && r.MarcaId != null && r.MarcaId == producto.MarcaId))

            .OrderByDescending(r => r.SubcategoriaId != null)

            .ThenByDescending(r => r.CategoriaId != null)

            .FirstOrDefaultAsync();



        if (regla != null && regla.Tipo != TipoPrecio.DESCUENTO)

            return regla.MargenPorcentaje;



        return await GetMargenGlobalAsync();

    }



    public decimal? CostoPorUnidadBase(Producto producto)

    {

        if (!producto.PrecioMayorista.HasValue || !producto.CantidadUnidadCompra.HasValue || producto.CantidadUnidadCompra <= 0)

            return null;



        return producto.PrecioMayorista.Value / producto.CantidadUnidadCompra.Value;

    }



    public async Task<decimal?> CalcularPrecioVentaAsync(Producto producto, ProductoPresentacion presentacion)

    {

        if (producto.ModoPrecio == ModoPrecio.PRECIO_FIJO)

            return presentacion.PrecioVenta;



        var costoBase = CostoPorUnidadBase(producto);

        if (!costoBase.HasValue || presentacion.CantidadUnidadBase <= 0) return null;



        var costoPresentacion = costoBase.Value * presentacion.CantidadUnidadBase;

        var iva = await ResolveIvaAsync(producto);

        var margen = await ResolveMargenAsync(producto, presentacion.MargenPorcentaje);

        var conIva = costoPresentacion * (1 + iva / 100m);

        var precio = conIva * (1 + margen / 100m);



        if (producto.DescuentoPorcentaje > 0)

            precio *= (1 - producto.DescuentoPorcentaje / 100m);



        return Math.Round(precio, 2, MidpointRounding.AwayFromZero);

    }



    public async Task RecalcularPresentacionesAsync(Producto producto)

    {

        if (producto.ModoPrecio == ModoPrecio.PRECIO_FIJO)

        {

            ActualizarPrecioMinoristaDesdePresentaciones(producto);

            return;

        }



        foreach (var p in producto.Presentaciones.Where(x => x.Activo))

        {

            var calculado = await CalcularPrecioVentaAsync(producto, p);

            if (calculado.HasValue)

            {

                p.PrecioVenta = calculado;

                p.UpdatedAt = DateTime.UtcNow;

            }

        }



        ActualizarPrecioMinoristaDesdePresentaciones(producto);

    }



    private static void ActualizarPrecioMinoristaDesdePresentaciones(Producto producto)

    {

        var defaultPres = producto.Presentaciones.FirstOrDefault(p => p.EsDefault && p.Activo)

            ?? producto.Presentaciones.FirstOrDefault(p => p.Activo);

        if (defaultPres?.PrecioVenta != null)

            producto.PrecioMinorista = defaultPres.PrecioVenta;

    }

}


