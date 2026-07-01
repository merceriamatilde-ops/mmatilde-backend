using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Models;
using MMatilde.Api.Services;

namespace MMatilde.Api.Controllers;

[Route("api/ia")]
[ApiController]
public class IaController : ControllerBase
{
    private readonly AppDbContext _db;

    public IaController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("consultas")]
    [AllowAnonymous]
    public async Task<ActionResult<IaConsultaDto>> CrearConsulta(
        [FromBody] CrearIaConsultaDto dto,
        [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey = null)
    {
        var key = idempotencyKey?.Trim();
        if (!string.IsNullOrEmpty(key))
        {
            var existing = await _db.IaConsultas
                .FirstOrDefaultAsync(c => c.IdempotencyKey == key);
            if (existing != null)
                return Ok(ToDto(existing));
        }

        var consulta = new IaConsulta
        {
            Proyecto = dto.Proyecto.Trim(),
            Tecnica = dto.Tecnica?.Trim(),
            ContextoJson = dto.ContextoJson,
            ResultadoJson = dto.ResultadoJson,
            ProductosJson = dto.ProductosJson,
            IdempotencyKey = string.IsNullOrEmpty(key) ? null : key,
        };

        _db.IaConsultas.Add(consulta);
        await _db.SaveChangesAsync();

        return Ok(ToDto(consulta));
    }

    [HttpGet("consultas")]
    [Authorize]
    public async Task<ActionResult<List<IaConsultaDto>>> ListarConsultas(
        [FromQuery] string? evaluacion = null,
        [FromQuery] bool pendientes = false,
        [FromQuery] int limit = 50)
    {
        limit = Math.Clamp(limit, 1, 200);

        var query = _db.IaConsultas.AsQueryable();
        if (pendientes)
            query = query.Where(c => c.Evaluacion == null);
        else if (!string.IsNullOrWhiteSpace(evaluacion))
            query = query.Where(c => c.Evaluacion == evaluacion);

        var items = await query
            .OrderByDescending(c => c.CreadoEn)
            .Take(limit)
            .ToListAsync();

        return Ok(items.Select(ToDto).ToList());
    }

    [HttpGet("consultas/{id:int}")]
    [Authorize]
    public async Task<ActionResult<IaConsultaDto>> ObtenerConsulta(int id)
    {
        var consulta = await _db.IaConsultas.FindAsync(id);
        if (consulta == null) return NotFound();
        return Ok(ToDto(consulta));
    }

    [HttpPut("consultas/{id:int}/feedback")]
    [Authorize]
    public async Task<ActionResult<IaConsultaDto>> EnviarFeedback(int id, [FromBody] IaFeedbackDto dto)
    {
        var consulta = await _db.IaConsultas.FindAsync(id);
        if (consulta == null) return NotFound();

        var eval = dto.Evaluacion.Trim().ToLowerInvariant();
        if (eval is not ("bien" or "mal"))
            return BadRequest("Evaluación debe ser 'bien' o 'mal'");

        consulta.Evaluacion = eval;
        consulta.NotaCorreccion = dto.NotaCorreccion?.Trim();
        consulta.CorreccionEsperada = dto.CorreccionEsperada?.Trim();
        consulta.RevisadoEn = DateTime.UtcNow;

        if (dto.CrearRegla && eval == "mal" && !string.IsNullOrWhiteSpace(dto.CorreccionEsperada))
        {
            var titulo = dto.ReglaTitulo?.Trim();
            if (string.IsNullOrWhiteSpace(titulo))
                titulo = $"Corrección: {consulta.Proyecto}".Trim();

            var disparadores = dto.ReglaDisparadores?.Trim();
            if (string.IsNullOrWhiteSpace(disparadores))
                disparadores = consulta.Proyecto.ToLowerInvariant();

            _db.IaReglasAprendidas.Add(new IaReglaAprendida
            {
                Titulo = titulo,
                Disparadores = disparadores,
                Regla = dto.CorreccionEsperada.Trim(),
                ConsultaOrigenId = consulta.Id,
            });
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(consulta));
    }

    [HttpGet("contexto-aprendizaje")]
    [AllowAnonymous]
    public async Task<ActionResult<ContextoAprendizajeDto>> ContextoAprendizaje([FromQuery] string? q = null)
    {
        return Ok(await BuildContextoAprendizaje(q));
    }

    [HttpGet("reglas-activas")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ReglaActivaDto>>> ReglasActivas([FromQuery] string? q = null)
    {
        var ctx = await BuildContextoAprendizaje(q);
        return Ok(ctx.Reglas);
    }

    private async Task<ContextoAprendizajeDto> BuildContextoAprendizaje(string? q)
    {
        var texto = IaMatching.NormalizeText(q ?? "");

        var reglasDb = await _db.IaReglasAprendidas.Where(r => r.Activa).ToListAsync();
        var reglas = reglasDb
            .Select(r => new { r.Regla, r.Disparadores, Score = IaMatching.ScoreDisparadores(r.Disparadores, texto) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Disparadores.Length == 0)
            .Take(8)
            .Select(x => new ReglaActivaDto(x.Regla, x.Disparadores))
            .ToList();

        var ejemplosDb = await _db.IaEjemplos.Where(e => e.Activa).ToListAsync();
        var ejemplos = ejemplosDb
            .Select(e => new
            {
                e.Titulo,
                e.Descripcion,
                e.RespuestaJson,
                e.ImagenUrl,
                Score = IaMatching.ScoreEjemplo(e.Disparadores, e.Descripcion, e.Titulo, texto),
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(3)
            .Select(x => new EjemploActivoDto(x.Titulo, x.Descripcion, x.RespuestaJson, x.ImagenUrl))
            .ToList();

        return new ContextoAprendizajeDto(reglas, ejemplos);
    }

    [HttpGet("reglas")]
    [Authorize]
    public async Task<ActionResult<List<IaReglaDto>>> ListarReglas()
    {
        var reglas = await _db.IaReglasAprendidas
            .OrderByDescending(r => r.CreadoEn)
            .ToListAsync();

        return Ok(reglas.Select(ToReglaDto).ToList());
    }

    [HttpPost("reglas")]
    [Authorize]
    public async Task<ActionResult<IaReglaDto>> CrearRegla([FromBody] CrearIaReglaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo) || string.IsNullOrWhiteSpace(dto.Regla))
            return BadRequest("Título y regla son obligatorios");

        var regla = new IaReglaAprendida
        {
            Titulo = dto.Titulo.Trim(),
            Disparadores = dto.Disparadores?.Trim() ?? "",
            Regla = dto.Regla.Trim(),
            ConsultaOrigenId = dto.ConsultaOrigenId,
        };

        _db.IaReglasAprendidas.Add(regla);
        await _db.SaveChangesAsync();

        return Ok(ToReglaDto(regla));
    }

    [HttpPut("reglas/{id:int}/toggle")]
    [Authorize]
    public async Task<ActionResult<IaReglaDto>> ToggleRegla(int id)
    {
        var regla = await _db.IaReglasAprendidas.FindAsync(id);
        if (regla == null) return NotFound();

        regla.Activa = !regla.Activa;
        await _db.SaveChangesAsync();

        return Ok(ToReglaDto(regla));
    }

    [HttpDelete("reglas/{id:int}")]
    [Authorize]
    public async Task<IActionResult> EliminarRegla(int id)
    {
        var regla = await _db.IaReglasAprendidas.FindAsync(id);
        if (regla == null) return NotFound();

        _db.IaReglasAprendidas.Remove(regla);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("ejemplos")]
    [Authorize]
    public async Task<ActionResult<List<IaEjemploDto>>> ListarEjemplos()
    {
        var items = await _db.IaEjemplos
            .OrderByDescending(e => e.CreadoEn)
            .ToListAsync();

        return Ok(items.Select(ToEjemploDto).ToList());
    }

    [HttpPost("ejemplos")]
    [Authorize]
    public async Task<ActionResult<IaEjemploDto>> CrearEjemplo([FromBody] CrearIaEjemploDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo) || string.IsNullOrWhiteSpace(dto.Descripcion))
            return BadRequest("Título y descripción son obligatorios");

        if (string.IsNullOrWhiteSpace(dto.RespuestaJson))
            return BadRequest("La respuesta correcta es obligatoria");

        var ejemplo = new IaEjemplo
        {
            Titulo = dto.Titulo.Trim(),
            Disparadores = dto.Disparadores?.Trim() ?? "",
            Descripcion = dto.Descripcion.Trim(),
            RespuestaJson = dto.RespuestaJson.Trim(),
            ImagenUrl = dto.ImagenUrl?.Trim(),
        };

        _db.IaEjemplos.Add(ejemplo);
        await _db.SaveChangesAsync();

        return Ok(ToEjemploDto(ejemplo));
    }

    [HttpPut("ejemplos/{id:int}/toggle")]
    [Authorize]
    public async Task<ActionResult<IaEjemploDto>> ToggleEjemplo(int id)
    {
        var ejemplo = await _db.IaEjemplos.FindAsync(id);
        if (ejemplo == null) return NotFound();

        ejemplo.Activa = !ejemplo.Activa;
        await _db.SaveChangesAsync();

        return Ok(ToEjemploDto(ejemplo));
    }

    [HttpDelete("ejemplos/{id:int}")]
    [Authorize]
    public async Task<IActionResult> EliminarEjemplo(int id)
    {
        var ejemplo = await _db.IaEjemplos.FindAsync(id);
        if (ejemplo == null) return NotFound();

        _db.IaEjemplos.Remove(ejemplo);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static IaConsultaDto ToDto(IaConsulta c) => new(
        c.Id,
        c.Proyecto,
        c.Tecnica,
        c.ContextoJson,
        c.ResultadoJson,
        c.ProductosJson,
        c.Evaluacion,
        c.NotaCorreccion,
        c.CorreccionEsperada,
        c.CreadoEn,
        c.RevisadoEn
    );

    private static IaReglaDto ToReglaDto(IaReglaAprendida r) => new(
        r.Id,
        r.Titulo,
        r.Disparadores,
        r.Regla,
        r.Activa,
        r.ConsultaOrigenId,
        r.CreadoEn
    );

    private static IaEjemploDto ToEjemploDto(IaEjemplo e) => new(
        e.Id,
        e.Titulo,
        e.Disparadores,
        e.Descripcion,
        e.RespuestaJson,
        e.ImagenUrl,
        e.Activa,
        e.CreadoEn
    );
}
