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
            //Console.WriteLine(dto.Username,dto.Password,dto.Email);
            //return null;
            //check validity of model state
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            //check if user exists
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
        [HttpPost]
        [Route("login2")]
        public void login2(string Username, string Surname)
        {
            string sql = "insert into UserTable(Username, Surname) values (@Username,@Surname)";
            SqlConnection con = new SqlConnection("Server=LAPTOP-BA51GN1C\\SQLEXPRESS;Database=AuthServiceDB;Integrated Security=true;");
            con.Open();
            con.Execute(sql, new[]
            {
new {Username=Username, Surname = Surname}
});
        }
        public class data
        {
            public string Username { get; set; }
            public string Surname { get; set; }
        }
        [HttpGet]
        [Route("getUser")]
        public List<data> GetAllSummary()
        {
            string connectionString = "Server=LAPTOP-BA51GN1C\\SQLEXPRESS;Database=AuthServiceDB;Integrated Security=true;";
            string query = "SELECT * from UserTable";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(query, conn);


                conn.Open();
                SqlDataReader reader = command.ExecuteReader();

                //In this part below, I want the SqlDataReader to
                //     read all of the records from database returned,
                //     and I want the result to be returned as Array or
                //     Json type, but I don't know how to write this part.

                List<data> result = new List<data>();
                if (reader.HasRows)
                {

                    while (reader.Read())
                    {
                        var d = new data();
                        d.Username = (string)reader[1]; // Probably needs fixing
                        d.Surname = (string)reader[2]; // Probably needs fixing
                        result.Add(d);
                    }
                }

                reader.Close();
                return result;


            }
        }

    }
}
