using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MMatilde.Api.Controllers;

[Route("api/upload")]
[ApiController]
[Authorize(Roles = "ADMIN")]
public class UploadController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly Cloudinary _cloudinary;

    public UploadController(IConfiguration config)
    {
        _config = config;
        
        var cloudName = _config["Cloudinary:CloudName"];
        var apiKey = _config["Cloudinary:ApiKey"];
        var apiSecret = _config["Cloudinary:ApiSecret"];

        if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No se subió ningún archivo." });

        if (_cloudinary == null)
            return BadRequest(new { message = "Cloudinary no está configurado. Por favor agregá las credenciales en appsettings.json." });

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "mmatilde/productos",
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
            return BadRequest(new { message = uploadResult.Error.Message });

        return Ok(new { url = uploadResult.SecureUrl.ToString() });
    }
}
