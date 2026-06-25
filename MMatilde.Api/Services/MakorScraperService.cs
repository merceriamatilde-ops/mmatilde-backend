using HtmlAgilityPack;

namespace MMatilde.Api.Services;

public record MakorProductScraped(string CodigoMakor, string Nombre, string? ImagenUrl, string CategoriaSlug, string? SubcategoriaSlug, decimal? Precio);

public class MakorScraperService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://www.makorsa.com.ar";

    public MakorScraperService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0 Safari/537.36");
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("action", "user/login"),
            new KeyValuePair<string, string>("next_action", $"{_baseUrl}/login"),
            new KeyValuePair<string, string>("user", username),
            new KeyValuePair<string, string>("pass", password)
        });

        // Use a temporary handler to prevent auto-redirects so we can catch the cookie
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var tempClient = new HttpClient(handler);
        tempClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0.0.0 Safari/537.36");

        var response = await tempClient.PostAsync($"{_baseUrl}/index.php", content);
        
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                if (cookie.Contains("PHPSESSID"))
                {
                    var phpsessid = cookie.Split(';')[0];
                    if (_httpClient.DefaultRequestHeaders.Contains("Cookie"))
                        _httpClient.DefaultRequestHeaders.Remove("Cookie");
                    _httpClient.DefaultRequestHeaders.Add("Cookie", phpsessid);
                    break;
                }
            }
        }
        
        // Verificamos si estamos logueados intentando ver si un request a home nos trae logout o mi cuenta
        var homeHtml = await _httpClient.GetStringAsync(_baseUrl);
        return homeHtml.Contains("Mi Cuenta") || homeHtml.Contains("Cerrar sesi");
    }

    public async Task<List<MakorProductScraped>> SearchProducts(string searchTerm)
    {
        var url = $"{_baseUrl}/index.php?action=portal/search&advanced=1&fulltext=on&str={Uri.EscapeDataString(searchTerm)}";
        return await ParseProductsFromUrl(url);
    }

    public async Task<List<MakorProductScraped>> GetProductsByCategory(string categorySlug)
    {
        var url = $"{_baseUrl}/{categorySlug}";
        return await ParseProductsFromUrl(url);
    }

    private async Task<List<MakorProductScraped>> ParseProductsFromUrl(string url, HashSet<string>? visitedUrls = null)
    {
        visitedUrls ??= new HashSet<string>();
        // Remove trailing slashes and pag query to avoid infinite loops on same URL
        var cleanUrl = url.Split('?')[0].TrimEnd('/');
        if (!visitedUrls.Add(cleanUrl)) return new List<MakorProductScraped>();

        var allProducts = new List<MakorProductScraped>();
        var seenCodes = new HashSet<string>();
        int page = 1;
        bool hasMore = true;

        while (hasMore)
        {
            var separator = url.Contains("?") ? "&" : "?";
            var pagedUrl = $"{url}{separator}pag={page}";
            
            var html = await _httpClient.GetStringAsync(pagedUrl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var articles = doc.DocumentNode.SelectNodes("//div[contains(@class,'home_destacados')]//article");
            if (articles == null || articles.Count == 0)
            {
                // Si estamos en la página 1 y no hay artículos, podría ser una categoría padre
                if (page == 1)
                {
                    var subcategoryLinks = doc.DocumentNode.SelectNodes("//div[contains(@class,'secciones_dest')]//div[contains(@class,'list-card')]//a");
                    if (subcategoryLinks != null && subcategoryLinks.Count > 0)
                    {
                        foreach (var subcat in subcategoryLinks)
                        {
                            var subUrl = subcat.GetAttributeValue("href", "");
                            if (!string.IsNullOrEmpty(subUrl))
                            {
                                if (!subUrl.StartsWith("http")) subUrl = _baseUrl + "/" + subUrl.TrimStart('/');
                                var subProducts = await ParseProductsFromUrl(subUrl, visitedUrls);
                                allProducts.AddRange(subProducts);
                            }
                        }
                    }
                }

                // Salimos del loop de paginación
                break;
            }

            int newProductsOnPage = 0;

            foreach (var article in articles)
            {
                var headerNode = article.SelectSingleNode(".//div[@class='text']//header//h1");
                var nombre = headerNode?.InnerText?.Trim() ?? string.Empty;

                var codeNode = article.SelectSingleNode(".//div[@class='text']//*[contains(@class,'codigo')]");
                var codigoFull = codeNode?.InnerText?.Trim() ?? string.Empty;
                var codigoMakor = string.Empty;
                if (!string.IsNullOrEmpty(codigoFull) && codigoFull.Contains(":"))
                {
                    codigoMakor = codigoFull.Split(':').Last().Trim();
                }

                if (string.IsNullOrEmpty(codigoMakor)) continue;

                // Si ya vimos este código, significa que la paginación no funcionó y nos devolvió la misma página
                if (!seenCodes.Add(codigoMakor)) continue;

                newProductsOnPage++;

                var imgNode = article.SelectSingleNode(".//figure//img");
                var imagenUrl = imgNode?.GetAttributeValue("src", null);
                if (!string.IsNullOrEmpty(imagenUrl) && !imagenUrl.StartsWith("http"))
                {
                    imagenUrl = _baseUrl + "/" + imagenUrl.TrimStart('/');
                }

                decimal? precio = null;
                var priceNode = article.SelectSingleNode(".//div[@class='text']//*[contains(@class,'precio')]");
                if (priceNode != null)
                {
                    var priceText = priceNode.InnerText.Replace("$", "").Replace(".", "").Replace(",", ".").Trim();
                    // Extraer solo la parte numérica (ej: "87846.14No incluye IVA" -> "87846.14")
                    var match = System.Text.RegularExpressions.Regex.Match(priceText, @"[\d\.]+");
                    if (match.Success && decimal.TryParse(match.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var p))
                    {
                        precio = p;
                    }
                }

                var linkNode = article.SelectSingleNode(".//a");
                var link = linkNode?.GetAttributeValue("href", "");
                
                var categoriaSlug = "otros";
                string? subcategoriaSlug = null;

                if (!string.IsNullOrEmpty(link))
                {
                    try
                    {
                        var uri = new Uri(link.StartsWith("http") ? link : $"{_baseUrl}/{link.TrimStart('/')}");
                        var segments = uri.Segments.Select(s => s.TrimEnd('/')).Where(s => !string.IsNullOrEmpty(s) && s != "index.php").ToList();

                        if (segments.Count > 0)
                        {
                            categoriaSlug = segments[0].ToLowerInvariant();
                            if (segments.Count > 1 && !segments[1].StartsWith("action="))
                            {
                                subcategoriaSlug = segments[1].ToLowerInvariant();
                            }
                        }
                    }
                    catch { }
                }

                allProducts.Add(new MakorProductScraped(codigoMakor, nombre, imagenUrl, categoriaSlug, subcategoriaSlug, precio));
            }

            // Si no obtuvimos ningún producto nuevo, salimos del loop para no quedar infinitos
            if (newProductsOnPage == 0)
            {
                break;
            }

            page++;
            // Limitar a 50 páginas máximo por seguridad
            if (page > 50) break;
        }

        return allProducts;
    }
}
