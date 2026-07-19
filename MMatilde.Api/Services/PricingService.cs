using Microsoft.EntityFrameworkCore;

using MMatilde.Api.Data;

using MMatilde.Api.DTOs;

using MMatilde.Api.Helpers;

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

        if (!costoBase.HasValue || presentacion.CantidadUnidadBase <= 0)

            return presentacion.PrecioVenta;



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

    public static string DefaultPresentacionNombre(UnidadMedida? unidad) => unidad switch
    {
        UnidadMedida.g => "1 g",
        UnidadMedida.cm => "1 cm",
        UnidadMedida.m => "1 m",
        UnidadMedida.ml => "1 ml",
        _ => "Unidad",
    };

    private static (string Nombre, decimal CantidadUnidadBase) GetPresentacionVentaDefault(Producto producto)
    {
        if (UnidadParser.EsProductoEnMetros(producto))
            return producto.UnidadBase == UnidadMedida.m ? ("1 m", 1m) : ("1 m", 100m);

        return (DefaultPresentacionNombre(producto.UnidadBase), 1m);
    }

    public static string PaqueteCerradoNombre(Producto producto)
    {
        var detalle = string.IsNullOrWhiteSpace(producto.EtiquetaUnidadCompra)
            ? null
            : producto.EtiquetaUnidadCompra.Trim();
        return string.IsNullOrWhiteSpace(detalle)
            ? "Paquete cerrado"
            : $"Paquete cerrado ({detalle})";
    }

    public bool EnsurePresentacionVentaDefault(Producto producto)
    {
        if (producto.ModoOrigenEconomico == ModoOrigenEconomico.ELABORACION_PROPIA)
            return false;

        var changed = false;
        var activas = producto.Presentaciones.Where(p => p.Activo).ToList();

        if (activas.Count == 0)
        {
            var defaultPresentacion = GetPresentacionVentaDefault(producto);
            producto.Presentaciones.Add(new ProductoPresentacion
            {
                Nombre = defaultPresentacion.Nombre,
                CantidadUnidadBase = defaultPresentacion.CantidadUnidadBase,
                EsDefault = true,
                Activo = true,
                Orden = 0,
            });
            changed = true;
            activas = producto.Presentaciones.Where(p => p.Activo).ToList();
        }
        else if (UnidadParser.EsProductoEnMetros(producto))
        {
            var expected = GetPresentacionVentaDefault(producto);
            var correcta = activas.FirstOrDefault(p => p.Nombre == expected.Nombre);
            var defaultActual = activas.FirstOrDefault(p => p.EsDefault);

            if (correcta != null && defaultActual != null && !ReferenceEquals(defaultActual, correcta))
            {
                defaultActual.EsDefault = false;
                correcta.EsDefault = true;
                correcta.CantidadUnidadBase = expected.CantidadUnidadBase;
                correcta.UpdatedAt = DateTime.UtcNow;
                changed = true;
                activas = producto.Presentaciones.Where(p => p.Activo).ToList();
            }
            else
            {
                var wrongDefault = activas.FirstOrDefault(p =>
                    p.EsDefault &&
                    p.Nombre is "1 cm" or "Unidad");
                if (wrongDefault != null)
                {
                    wrongDefault.Nombre = expected.Nombre;
                    wrongDefault.CantidadUnidadBase = expected.CantidadUnidadBase;
                    wrongDefault.UpdatedAt = DateTime.UtcNow;
                    changed = true;
                    activas = producto.Presentaciones.Where(p => p.Activo).ToList();
                }
            }
        }

        if (producto.CantidadUnidadCompra.HasValue && producto.CantidadUnidadCompra.Value > 1m)
        {
            var cantidadPaquete = producto.CantidadUnidadCompra.Value;
            var yaExistePaquete = activas.Any(p => p.CantidadUnidadBase == cantidadPaquete);
            if (!yaExistePaquete)
            {
                var nextOrder = activas.Count == 0 ? 1 : activas.Max(p => p.Orden) + 1;
                producto.Presentaciones.Add(new ProductoPresentacion
                {
                    Nombre = PaqueteCerradoNombre(producto),
                    CantidadUnidadBase = cantidadPaquete,
                    EsDefault = false,
                    Activo = true,
                    Orden = nextOrder,
                });
                changed = true;
            }
        }

        return changed;
    }

    public async Task<bool> EnsurePresentacionVentaListaAsync(Producto producto)
    {
        var changed = false;

        // Productos viejos sin unidad: default Unidad × 1 para poder calcular costo/margen.
        if (producto.UnidadBase == null || !producto.CantidadUnidadCompra.HasValue || producto.CantidadUnidadCompra <= 0)
        {
            UnidadParser.ApplyDetectedOrDefault(producto, producto.Nombre);
            changed = true;
        }

        if (EnsurePresentacionVentaDefault(producto))
            changed = true;

        if (!changed)
            return false;

        if (producto.ModoPrecio == ModoPrecio.PRECIO_FIJO)
        {
            if (producto.PrecioMinorista.HasValue)
            {
                foreach (var p in producto.Presentaciones.Where(x => x.Activo))
                    p.PrecioVenta = producto.PrecioMinorista;
            }
        }
        else if (CostoPorUnidadBase(producto).HasValue)
        {
            await RecalcularPresentacionesAsync(producto);
        }

        producto.UpdatedAt = DateTime.UtcNow;
        return true;
    }

    private static ProductoPresentacion SeleccionarPresentacionReferencia(
        Producto producto,
        List<ProductoPresentacion> activas)
    {
        if (UnidadParser.EsProductoEnMetros(producto))
        {
            var expected = GetPresentacionVentaDefault(producto);
            var porNombre = activas.FirstOrDefault(p => p.Nombre == expected.Nombre);
            if (porNombre != null) return porNombre;
        }

        return activas[0];
    }

    public async Task<PrecioVentaResumenDto> ResolverPrecioVentaDefaultAsync(Producto producto)
    {
        var activas = producto.Presentaciones
            .Where(x => x.Activo)
            .OrderByDescending(x => x.EsDefault)
            .ThenBy(x => x.Orden)
            .ToList();

        ProductoPresentacion presRef;
        string? nombre;

        if (activas.Count > 0)
        {
            presRef = SeleccionarPresentacionReferencia(producto, activas);
            nombre = string.IsNullOrWhiteSpace(presRef.Nombre) ? null : presRef.Nombre;
        }
        else if (producto.ModoPrecio == ModoPrecio.PRECIO_FIJO)
        {
            var precioFijo = producto.PrecioMinorista;
            return new PrecioVentaResumenDto(precioFijo, precioFijo, 1m, null);
        }
        else
        {
            var defaultPresentacion = GetPresentacionVentaDefault(producto);
            nombre = defaultPresentacion.Nombre;
            presRef = new ProductoPresentacion
            {
                CantidadUnidadBase = defaultPresentacion.CantidadUnidadBase,
                Nombre = nombre
            };
        }

        var calculado = await CalcularPrecioVentaAsync(producto, presRef);
        var precioTotal = presRef.PrecioVenta ?? calculado ?? producto.PrecioMinorista;
        var cantidadRef = presRef.CantidadUnidadBase > 0 ? presRef.CantidadUnidadBase : 1m;
        decimal? precioPorUnidad = null;
        if (precioTotal.HasValue && cantidadRef > 0)
        {
            precioPorUnidad = Math.Round(precioTotal.Value / cantidadRef, 2, MidpointRounding.AwayFromZero);
        }

        return new PrecioVentaResumenDto(precioTotal, precioPorUnidad, cantidadRef, nombre);
    }

}


