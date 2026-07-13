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

            // Home y páginas estáticas
            AddUrl(writer, $"{baseUrl}/", "daily", "1.0");
            AddUrl(writer, $"{baseUrl}/categorias", "weekly", "0.8");
            AddUrl(writer, $"{baseUrl}/buscar", "weekly", "0.8");

            // Categorías
            var categorias = await _db.Categorias.Where(c => c.Activo).ToListAsync();
            foreach (var cat in categorias)
            {
                AddUrl(writer, $"{baseUrl}/categorias/{cat.Slug}", "weekly", "0.9");
            }

            // Productos
            var productos = await _db.Productos.Where(p => p.Activo && !p.EsVentaLibre).ToListAsync();
            foreach (var prod in productos)
            {
                AddUrl(writer, $"{baseUrl}/producto/{prod.Slug}", "weekly", "0.7");
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        return File(memoryStream.ToArray(), "application/xml; charset=utf-8");
    }

    private void AddUrl(XmlWriter writer, string loc, string changefreq, string priority)
    {
        writer.WriteStartElement("url");
        writer.WriteElementString("loc", loc);
        writer.WriteElementString("changefreq", changefreq);
        writer.WriteElementString("priority", priority);
        writer.WriteEndElement();
    }
}
