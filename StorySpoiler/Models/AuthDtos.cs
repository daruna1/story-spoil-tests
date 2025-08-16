namespace StorySpoiler.Models;

public class AuthRequestDto
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthResponseDto
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string AccessToken { get; set; } = "";
}
