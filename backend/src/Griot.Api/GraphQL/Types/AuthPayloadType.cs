namespace Griot.Api.GraphQL.Types;

public class AuthPayloadType
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserType User { get; set; } = null!;
}
