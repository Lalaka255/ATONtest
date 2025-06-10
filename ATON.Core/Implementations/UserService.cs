using ATON.Core.Abstractions;
using ATON.DataAccess.Entities;

namespace ATON.Core.Implementations;

public class UserService : IUserService
{
    private readonly List<User> _users = new List<User>();

    public UserService()
    {
        if (!_users.Any(u => u.Login == "Admin" && u.Admin == true))
        {
            _users.Add(new User
            {
                Guid = Guid.NewGuid(),
                Login = "Admin",
                Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Name = "Administrator",
                Gender = 2,
                Birthday = null,
                Admin = true,
                CreatedOn = DateTime.Now,
                CreatedBy = "system"
            });
        }
    }

    void IUserService.AddUser(User user)
    {
        _users.Add(user);
    }
    
    User? IUserService.GetUserByLogin(string login)
    {
        return _users.FirstOrDefault(u => u.Login == login);
    }

    IEnumerable<User> IUserService.GetAllActiveUsers()
    {
        return _users
            .Where(u => u.RevokedOn == null)
            .OrderBy(u => u.CreatedOn)
            .ToList();
    } 
    
    IEnumerable<User> IUserService.GetUsersOlderThan(int age)
    {
        return _users
            .Where(u => u.RevokedOn == null && u.Birthday.HasValue && u.Birthday.Value.AddYears(age) <= DateTime.Now)
            .ToList();
    } 
    
    void IUserService.UpdateUser(User user)
    {
        var existing = _users.FirstOrDefault(u => u.Guid == user.Guid);
        
        if (existing != null)
        {
            _users.Remove(existing);
            _users.Add(user);
        }
    }

    void IUserService.RemoveUser(User user)
    {
        _users.Remove(user);
    } 
}