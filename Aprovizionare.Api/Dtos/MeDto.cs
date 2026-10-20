namespace Aprovizionare.Api.Dtos;

public record MeDto(string Email, string FullName, IList<string> Roles);
