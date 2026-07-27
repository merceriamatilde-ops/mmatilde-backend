namespace MMatilde.Api.DTOs;

public record SyncRequest(List<string> Terms);
public record SyncResponse(bool Success, int Count);
public record CleanupFakeSubsResult(int SubcategoriasEliminadas, int ProductosDesvinculados);
public record SyncLogDto(
    int Id,
    string Estado,
    int ProductosNuevos,
    int ProductosActualizados,
    int Errores,
    string? TermsJson,
    string? CategoriasJson,
    string? ResumenJson,
    string? DetalleErrores,
    DateTime IniciadoEn,
    DateTime? FinalizadoEn
);
