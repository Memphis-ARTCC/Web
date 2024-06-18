using Memphis.API.Extensions;
using Memphis.API.Services;
using Memphis.Shared.Data;
using Memphis.Shared.Dtos.auth;
using Memphis.Shared.Dtos.Auth;
using Memphis.Shared.Enums;
using Memphis.Shared.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Memphis.API.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly DatabaseContext _context;
    private readonly RedisService _redisService;
    private readonly ISentryClient _sentryHub;
    private readonly ILogger<AuthController> _logger;

    public AuthController(DatabaseContext context, RedisService redisService, ISentryClient sentryHub, ILogger<AuthController> logger)
    {
        _context = context;
        _redisService = redisService;
        _sentryHub = sentryHub;
        _logger = logger;
    }


    [HttpGet("redirect")]
    [ProducesResponseType(301)]
    [ProducesResponseType(typeof(Response<Guid>), 500)]
    public async Task<IActionResult> RedirectToVatsim()
    {
        try
        {
            var authUrl = Environment.GetEnvironmentVariable("CONNECT_AUTH_URL") ??
                          throw new ArgumentNullException("CONNECT_AUTH_URL env variable not found");
            var clientId = Environment.GetEnvironmentVariable("CONNECT_CLIENT_ID") ??
                           throw new ArgumentNullException("CONNECT_CLIENT_ID env variable not found");
            var redirectUrl = Environment.GetEnvironmentVariable("CONNECT_REDIRECT_URL") ??
                              throw new ArgumentNullException("CONNECT_REDIRECT_URL env variable not found");
            var url =
                $"{authUrl}/oauth/authorize?client_id={clientId}&redirect_uri={redirectUrl}&response_type=code&scope=full_name+vatsim_details+email";
            await Task.CompletedTask;
            return RedirectPreserveMethod(url);
        }
        catch (Exception ex)
        {
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }

    [HttpPost("callback")]
    [ProducesResponseType(typeof(Response<string>), 200)]
    [ProducesResponseType(typeof(Response<Guid>), 500)]
    public async Task<ActionResult<Response<string>>> ProcessCallback(CodeDto payload)
    {
        try
        {
            var authUrl = Environment.GetEnvironmentVariable("CONNECT_AUTH_URL") ??
                          throw new ArgumentNullException("CONNECT_AUTH_URL env variable not found");
            var clientId = Environment.GetEnvironmentVariable("CONNECT_CLIENT_ID") ??
                           throw new ArgumentNullException("CONNECT_CLIENT_ID env variable not found");
            var clientSecret = Environment.GetEnvironmentVariable("CONNECT_CLIENT_SECRET") ??
                               throw new ArgumentNullException("CONNECT_CLIENT_SECRET env variable not found");
            var redirectUrl = Environment.GetEnvironmentVariable("CONNECT_REDIRECT_URL") ??
                              throw new ArgumentNullException("CONNECT_REDIRECT_URL env variable not found");
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ??
                         throw new ArgumentNullException("JWT_ISSUER env variable not found");
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ??
                           throw new ArgumentNullException("JWT_AUDIENCE env variable not found");
            var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ??
                         throw new ArgumentNullException("JWT_SECRET env variable not found");
            var expirationDays = int.Parse(Environment.GetEnvironmentVariable("JWT_ACCESS_EXPIRATION") ??
                                           throw new ArgumentNullException(
                                               "JWT_ACCESS_EXPIRATION env variable not found"));

            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = payload.Code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUrl
            });

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await client.PostAsync($"{authUrl}/oauth/token", tokenRequest);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var token = JsonConvert.DeserializeObject<VatsimTokenDto>(content);
            if (token == null)
            {
                _logger.LogError("Invalid VATSIM token response:\nconnect response code: {Code}\ncontent: {Content}",
                    response.StatusCode, content);
                throw new InvalidDataException("Invalid VATSIM token response");
            }

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");
            var data = await client.GetFromJsonAsync<VatsimUserResponseDto>($"{authUrl}/api/user");

            if (data == null)
            {
                _logger.LogError("Invalid VATSIM data response:\nconnect response code: {Code}\ndata: {Data}",
                    response.StatusCode, data);
                throw new InvalidDataException("Invalid VATSIM data response");
            }

            var claims = new List<Claim>()
            {
                new("cid", $"{data.Data.Cid}"),
                new("email", data.Data.PersonalDetails.Email),
                new("fullName", data.Data.PersonalDetails.NameFull),
                new("firstName", data.Data.PersonalDetails.NameFirst),
                new("lastName", data.Data.PersonalDetails.NameLast),
                new("rating", $"{data.Data.VatsimDetails.ControllerRating.Id}"),
                new("ratingLong", data.Data.VatsimDetails.ControllerRating.Long),
                new("region", data.Data.VatsimDetails.Region.Id),
                new("division", data.Data.VatsimDetails.Division.Id),
            };
            var user = await _context.Users.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == data.Data.Cid);

            if (user == null || user.Status == UserStatus.REMOVED)
            {
                claims.Add(new Claim("isMember", $"{false}"));
                claims.Add(new Claim("roles", string.Empty));
                var jwtNone = new JwtSecurityToken(
                    issuer,
                    audience,
                    claims,
                    expires: DateTime.UtcNow.AddDays(expirationDays),
                    signingCredentials: new SigningCredentials(
                        new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
                        SecurityAlgorithms.HmacSha256Signature
                    )
                );
                var accessTokenNone = new JwtSecurityTokenHandler().WriteToken(jwtNone);
                return Ok(new Response<string>
                {
                    StatusCode = 200,
                    Message = "Logged in",
                    Data = accessTokenNone
                });
            }

            claims.Add(new Claim("isMember", $"{true}"));

            var roles = new List<string>();

            if (user.CanRegisterForEvents)
                roles.Add("CanRegisterForEvents");
            if (user.CanRequestTraining)
                roles.Add("CanRequestTraining");
            if (user.Roles != null)
                roles.AddRange(user.Roles.Select(x => x.NameShort).ToList());
            claims.AddRange(roles.Select(x => new Claim("roles", x)));

            await _redisService.SetRoles(roles, user.Id);

            var jwt = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddDays(expirationDays),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)), SecurityAlgorithms.HmacSha256Signature
                )
            );
            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
            return Ok(new Response<string>
            {
                StatusCode = 200,
                Message = "Logged in",
                Data = accessToken
            });
        }
        catch (Exception ex)
        {
            return _sentryHub.CaptureException(ex).ReturnActionResult();
        }
    }
}