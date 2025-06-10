using System.Text.RegularExpressions;
using ATON.Core.Abstractions;
using ATON.DataAccess.Entities;
using ATON.WebApi.Models.AuthModels;
using ATON.WebApi.Models.UserModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ATON.WebApi.Controllers;

/// <summary>
/// Пользователи
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    
    /// <summary>
    /// Создание пользователя
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult CreateUser([FromBody] CreateUserRequestModel model)
    {
        if (_userService.GetUserByLogin(model.Login) != null)
            return BadRequest("Логин уже существует.");

        if (!IsValidLogin(model.Login) || !IsValidPassword(model.Password) || !IsValidName(model.Name))
            return BadRequest("Недопустимые символы в логине, пароле или имени.");

        var currentUserLogin = User.Identity.Name;
        
        var newUser = new User
        {
            Guid = Guid.NewGuid(),
            Login = model.Login,
            Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Name = model.Name,
            Gender = model.Gender,
            Birthday = model.Birthday,
            Admin = model.Admin,
            CreatedOn = DateTime.Now,
            CreatedBy = currentUserLogin
        };

        _userService.AddUser(newUser);
        
        return NoContent();
    }
    
    /// <summary>
    /// Изменение имени, пола и даты рождения пользователя
    /// </summary>
    /// <returns></returns>
    [HttpPut("{login}/profile")]
    public IActionResult UpdateProfile(string login, [FromBody] UpdateProfileRequestModel model)
    {
        var user = _userService.GetUserByLogin(login);
        if (user == null || user.RevokedOn != null)
            return NotFound();

        var currentUserLogin = User.Identity.Name;
        if (!User.IsInRole("Admin") && currentUserLogin != login)
            return Forbid();

        if (!IsValidName(model.Name))
            return BadRequest("Недопустимые символы в имени.");

        user.Name = model.Name;
        user.Gender = model.Gender;
        user.Birthday = model.Birthday;
        user.ModifiedOn = DateTime.Now;
        user.ModifiedBy = currentUserLogin;

        _userService.UpdateUser(user);
        
        return NoContent();
    }
    
    /// <summary>
    /// Изменение пароля пользователя
    /// </summary>
    /// <returns></returns>
    [HttpPut("{login}/password")]
    public IActionResult ChangePassword(string login, [FromBody] ChangePasswordRequestModel model)
    {
        var user = _userService.GetUserByLogin(login);
        if (user == null || user.RevokedOn != null)
            return NotFound();

        var currentUserLogin = User.Identity.Name;
        if (!User.IsInRole("Admin") && currentUserLogin != login)
            return Forbid();

        if (!IsValidPassword(model.NewPassword))
            return BadRequest("Недопустимые символы в пароле.");

        user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        user.ModifiedOn = DateTime.Now;
        user.ModifiedBy = currentUserLogin;

        _userService.UpdateUser(user);
        
        return NoContent();
    }
    
    /// <summary>
    /// Изменение логина пользователя
    /// </summary>
    /// <returns></returns>
    [HttpPut("{login}/login")]
    public IActionResult ChangeLogin(string login, [FromBody] ChangeLoginRequestModel model)
    {
        var user = _userService.GetUserByLogin(login);
        if (user == null || user.RevokedOn != null)
            return NotFound();

        var currentUserLogin = User.Identity.Name;
        if (!User.IsInRole("Admin") && currentUserLogin != login)
            return Forbid();

        if (_userService.GetUserByLogin(model.NewLogin) != null)
            return BadRequest("Новый логин уже существует.");

        if (!IsValidLogin(model.NewLogin))
            return BadRequest("Недопустимые символы в логине.");

        user.Login = model.NewLogin;
        user.ModifiedOn = DateTime.Now;
        user.ModifiedBy = currentUserLogin;

        _userService.UpdateUser(user);
        
        return NoContent();
    }
    
    /// <summary>
    /// Запрос списка всех активных пользователей
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAllActiveUsers()
    {
        var users = _userService.GetAllActiveUsers();
        
        return Ok(users);
    }
    
    /// <summary>
    /// Запрос пользователя по логину
    /// </summary>
    /// <returns></returns>
    [HttpGet("{login}")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetUserByLogin(string login)
    {
        var user = _userService.GetUserByLogin(login);
        if (user == null)
            return NotFound();

        var getUserByLoginResponseModel = new GetUserByLoginResponseModel()
        {
            Name = user.Name,
            Gender = user.Gender,
            Birthday = user.Birthday,
            IsActive = user.RevokedBy == null,
        };
        
        return Ok(getUserByLoginResponseModel);
    }
    
    /// <summary>
    /// Запрос пользователя по логину и паролю
    /// </summary>
    /// <returns></returns>
    [HttpPost("authenticate")]
    public IActionResult Authenticate([FromBody] LoginRequestModel model)
    {
        var user = _userService.GetUserByLogin(model.Login);
        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password) || user.RevokedOn != null)
            return Unauthorized();

        var authenticateResponseModel = new AuthenticateResponseModel()
        {
            Guid = user.Guid,
            Login = user.Login,
            Name = user.Name,
            Gender = user.Gender,
            Birthday = user.Birthday,
            Admin = user.Admin,
            IsActive = user.RevokedBy == null,
        };
        
        return Ok(authenticateResponseModel);
    }

    /// <summary>
    /// Запрос всех пользователей старше определённого возраста
    /// </summary>
    /// <returns></returns>
    [HttpGet("olderthan/{age}")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetUsersOlderThan(int age)
    {
        var users = _userService.GetUsersOlderThan(age);
        
        return Ok(users);
    }

    /// <summary>
    /// Удаление пользователя по логину полное или мягкое
    /// </summary>
    /// <returns></returns>
    [HttpDelete("{login}")]
    [Authorize(Roles = "Admin")]
    public IActionResult DeleteUser(string login, [FromQuery] bool hard = false)
    {
        var user = _userService.GetUserByLogin(login);
        if (user == null)
            return NotFound();

        var currentUserLogin = User.Identity.Name;
        if (hard)
        {
            _userService.RemoveUser(user);
        }
        else
        {
            user.RevokedOn = DateTime.Now;
            user.RevokedBy = currentUserLogin;
            _userService.UpdateUser(user);
        }

        return NoContent();
    }
    
    /// <summary>
    /// Восстановление пользователя
    /// </summary>
    /// <returns></returns>
    [HttpPut("{login}/restore")]
    [Authorize(Roles = "Admin")]
    public IActionResult RestoreUser(string login)
    {
        var user = _userService.GetUserByLogin(login);
        if (user == null || user.RevokedOn == null)
            return NotFound();

        user.RevokedOn = null;
        user.RevokedBy = null;
        _userService.UpdateUser(user);
        
        return NoContent();
    }

    private bool IsValidLogin(string login)
    {
        return Regex.IsMatch(login, @"^[a-zA-Z0-9]+$");
    }

    private bool IsValidPassword(string password)
    {
        return Regex.IsMatch(password, @"^[a-zA-Z0-9]+$");
    }

    private bool IsValidName(string name)
    {
        return Regex.IsMatch(name, @"^[a-zA-Zа-яА-Я]+$");
    } 
    
}







