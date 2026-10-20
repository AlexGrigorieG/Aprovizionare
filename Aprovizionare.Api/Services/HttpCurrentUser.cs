namespace Aprovizionare.Api.Services;
public class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string UserName => httpContextAccessor.HttpContext?.User.Identity?.Name ?? "System";
}
