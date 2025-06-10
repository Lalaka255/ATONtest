namespace ATON.WebApi.Models.UserModels;

public class AuthenticateResponseModel
{
    public Guid Guid { get; set; }
    
    public string Login { get; set; } = string.Empty;
    
    public string Name { get; set; } = string.Empty;
    
    public int Gender { get; set; }
    
    public DateTime? Birthday { get; set; }
    
    public bool Admin { get; set; }
    
    public bool IsActive { get; set; }
    
    
}