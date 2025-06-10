using ATON.DataAccess.Entities;

namespace ATON.Core.Abstractions;

public interface IUserService
{
    public void AddUser(User user);
    
    public User? GetUserByLogin(string login);
    
    public IEnumerable<User> GetAllActiveUsers();
    
    public IEnumerable<User> GetUsersOlderThan(int age);
    
    public void UpdateUser(User user);
    
    public void RemoveUser(User user);
}