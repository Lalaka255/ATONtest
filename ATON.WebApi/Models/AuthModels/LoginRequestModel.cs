namespace ATON.WebApi.Models.AuthModels;

public class LoginRequestModel
{
    public string Login { get; set; } = null!;
    
    public string Password { get; set; } = null!;
}