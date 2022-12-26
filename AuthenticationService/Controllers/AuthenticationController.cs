using AuthenticationService.Dtos;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace AuthenticationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {  //fields
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;

        //constructor
        public AuthenticationController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        //register
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register(UserRegistrationRequest dto)
        {
            //check validity of model state
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            //check if user exists
            Console.WriteLine(dto.Email);
            var user = await _userManager.FindByEmailAsync(dto.Email);
         
            if (user != null)
            {
                return BadRequest(new { message = "this email exists" });
            }

            //create new user
            User newUser = new User
            {
                Email = dto.Email,
                UserName = dto.Username
            };
            var result = await _userManager.CreateAsync(newUser, dto.Password);
            return result.Succeeded ? Ok(new { token = GenerateJwtToken(newUser) }) : BadRequest(new { message = "error" });
        }
        //login
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(UserLoginRequest dto)
        {
            //check validity of model state
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //get user
            var user = await _userManager.FindByNameAsync(dto.Username);

            //check if user is not found
            if (user == null)
            {
                return BadRequest(new { message = "invalid email" });
            }

            //sign in
            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);

            return result.Succeeded ? Ok(new { token = GenerateJwtToken(user) }) : Unauthorized();
        }

        //Generating JWT Token method
        private string GenerateJwtToken(IdentityUser user)
        {
            //token handler
            var tokenHandler = new JwtSecurityTokenHandler();

            //key
            var key = Encoding.ASCII.GetBytes(_configuration.GetSection("AppSetting:Secret").Value);

            //token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName)
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            //token
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
