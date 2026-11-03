using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.Auth;
using GOBID.Application.DTOs.Personnel;
using GOBID.Application.Interfaces.Services;
using GOBID.Application.Wrappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

[Route("api/v{version:apiVersion}/public")]
[AllowAnonymous]
public class PublicController : OwnBaseController
{
    private readonly IUserService _userService;
    private readonly IPersonApiService _personApiService;

    public PublicController(IUserService userService, IAuthService authService, IPersonApiService personApiService) : base(authService)
    {
        _userService = userService;
        _personApiService = personApiService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<Response<string>>> Register([FromBody] PublicRegisterDto dto)
    {
        var response = await _userService.RegisterPublicAsync(dto);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    /// <summary>
    /// Crea una persona en la API externa y registra su cuenta de usuario
    /// </summary>
    [HttpPost("register-person-user")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<ActionResult<Response<string>>> RegisterPersonAndUserAsync([FromBody] RegisterPersonAndUserDto dto)
    {
        var response = await _userService.RegisterPersonAndUserAsync(dto);

        if (!response.Succeeded)
            return BadRequest(response);

        return StatusCode(201, response);
    }
}
