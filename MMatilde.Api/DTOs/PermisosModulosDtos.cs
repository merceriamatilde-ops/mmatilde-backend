namespace MMatilde.Api.DTOs;

public record ModuloPermisoDto(bool Habilitado, List<string> Roles);

public record PermisosModulosDto(Dictionary<string, ModuloPermisoDto> Modulos);

public record PermisosModulosUpdateDto(Dictionary<string, ModuloPermisoDto> Modulos);

public record ModuloDefDto(string Key, string Label, bool Bloqueado);
