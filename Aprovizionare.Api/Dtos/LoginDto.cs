using System.ComponentModel.DataAnnotations;

namespace Aprovizionare.Api.Dtos;

public record LoginDto([Required, EmailAddress] string Email, [Required] string Password);