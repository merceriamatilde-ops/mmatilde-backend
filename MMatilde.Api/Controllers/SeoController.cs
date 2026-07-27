using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using System.Text;
using System.Xml;

namespace MMatilde.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeoController : ControllerBase
{
    private readonly AppDbContext _db;

    public SeoController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("sitemap")]
    public async Task<IActionResult> GetSitemap()
    {
        var baseUrl = "https://www.merceriamatilde.com";
        
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true
        };

        using var memoryStream = new MemoryStream();
        using (var writer = XmlWriter.Create(memoryStream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            AddUrl(writer, $"{baseUrl}/", "daily", "1.0", DateTime.UtcNow);
            AddUrl(writer, $"{baseUrl}/categorias", "weekly", "0.9", DateTime.UtcNow);
            AddUrl(writer, $"{baseUrl}/contacto", "monthly", "0.8", DateTime.UtcNow);
            AddUrl(writer, $"{baseUrl}/buscar", "weekly", "0.5", DateTime.UtcNow);

            var categorias = await _db.Categorias.Where(c => c.Activo).ToListAsync();
            foreach (var cat in categorias)
            {
                AddUrl(writer, $"{baseUrl}/categorias/{cat.Slug}", "weekly", "0.9", cat.CreatedAt);
            }

            var colecciones = await _db.Tags
                .Where(t => t.Activo && t.VisibleEnCatalogo)
                .Where(t => t.Productos.Any(pt => pt.Producto.Activo))
                .ToListAsync();
            foreach (var col in colecciones)
            {
                AddUrl(writer, $"{baseUrl}/colecciones/{col.Slug}", "weekly", "0.8", col.UpdatedAt);
            }

            var productos = await _db.Productos.Where(p => p.Activo && !p.EsVentaLibre).ToListAsync();
            foreach (var prod in productos)
            {
                AddUrl(writer, $"{baseUrl}/producto/{prod.Slug}", "weekly", "0.7", prod.UpdatedAt);
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return File(memoryStream.ToArray(), "application/xml; charset=utf-8");
    }

    private static void AddUrl(XmlWriter writer, string loc, string changefreq, string priority, DateTime? lastmod)
    {
        writer.WriteStartElement("url");
        writer.WriteElementString("loc", loc);
        if (lastmod.HasValue)
        {
            writer.WriteElementString("lastmod", lastmod.Value.ToUniversalTime().ToString("yyyy-MM-dd"));
        }
        writer.WriteElementString("changefreq", changefreq);
        writer.WriteElementString("priority", priority);
        writer.WriteEndElement();
    }
}
