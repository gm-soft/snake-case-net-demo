using Microsoft.AspNetCore.Mvc;
using SnakeCaseDemo.Models;

namespace SnakeCaseDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountController
    {
        [HttpPost("register")]
        public FormData Register([FromBody] FormData data)
        {
            return data;
        }
    }
}