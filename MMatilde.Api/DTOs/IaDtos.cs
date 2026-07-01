namespace MMatilde.Api.DTOs;

public record IaConsultaDto(
    int Id,
    string Proyecto,
    string? Tecnica,
    string ContextoJson,
    string ResultadoJson,
    string? ProductosJson,
    string? Evaluacion,
    string? NotaCorreccion,
    string? CorreccionEsperada,
    DateTime CreadoEn,
    DateTime? RevisadoEn
);

public record CrearIaConsultaDto(
    string Proyecto,
    string? Tecnica,
    string ContextoJson,
    string ResultadoJson,
    string? ProductosJson
);

public record IaFeedbackDto(
    string Evaluacion,
    string? NotaCorreccion,
    string? CorreccionEsperada,
    bool CrearRegla,
    string? ReglaTitulo,
    string? ReglaDisparadores
);

public record IaReglaDto(
    int Id,
    string Titulo,
    string Disparadores,
    string Regla,
    bool Activa,
    int? ConsultaOrigenId,
    DateTime CreadoEn
);

public record CrearIaReglaDto(
    string Titulo,
    string Disparadores,
    string Regla,
    int? ConsultaOrigenId
);

public record ReglaActivaDto(string Regla, string? Disparadores = null);

public record EjemploActivoDto(
    string Titulo,
    string Descripcion,
    string RespuestaJson,
    string? ImagenUrl
);

public record ContextoAprendizajeDto(
    List<ReglaActivaDto> Reglas,
    List<EjemploActivoDto> Ejemplos
);

public record IaEjemploDto(
    int Id,
    string Titulo,
    string Disparadores,
    string Descripcion,
    string RespuestaJson,
    string? ImagenUrl,
    bool Activa,
    DateTime CreadoEn
);

public record CrearIaEjemploDto(
    string Titulo,
    string Disparadores,
    string Descripcion,
    string RespuestaJson,
    string? ImagenUrl
);
