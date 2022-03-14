using Microsoft.AspNetCore.Mvc;

namespace UserApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {

        private readonly ILogger<UserController> _logger;
        private static List<User> _users = new List<User>
        {
            new User{Name="Emirhan"},
            new User{Name="Diren"},
            new User{Name="Javi"}
        };

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetUsers")]
        public IEnumerable<User> Get()
        {
            return _users;
        }
        [HttpPost(Name = "AddUser")]
        public IActionResult AddUser([FromBody] User user)
        {
            _users.Add(user);
            return Ok("Successfully");
        }
    }
}