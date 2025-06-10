namespace ATON.WebApi.Models.UserModels;

public class UpdateProfileRequestModel
{
    public string Name { get; set; }
    
    public int Gender { get; set; }
    
    public DateTime? Birthday { get; set; }
}