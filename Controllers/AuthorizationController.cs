// Copyright (C) 2023 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of TrustVPN applcation
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TrustVpn.Authorization;
using TrustVpn.Data;
using TrustVpn.Models;
using TrustVpn.Service;

namespace TrustVpn.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
  private readonly UserContext _context;
  private readonly IJwtUtils _jwtUtils;


  public AuthController(UserContext context, IJwtUtils jwtUtils)
  {
    _context = context;
    _jwtUtils = jwtUtils;
  }

  // POST: api/auth/login
  [AllowAnonymous]
  [HttpPost("login")]
  [Produces("application/json")]
  [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserViewItemWithJWT))]
  [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrMessage))]
  public async Task<ActionResult<UserViewItem>> Login(Credentials crd)
  {
    User? user = await _context.Users.Where(u => u.Email == crd.Email).SingleOrDefaultAsync();

    if (user == null) return Unauthorized(new {message = "Неправильный адрес электронной почты или пароль" });

    if (!BCrypt.Net.BCrypt.Verify(crd.Password, user.Password)) return Unauthorized();
    UserViewItemWithJWT userViewItem = new(user) {
      Token = _jwtUtils.GenerateJwtToken(user)
    };

    var oContainer = new TrustVpnServiceContainer();
    userViewItem.Config = await oContainer.GetUserConfig(user.Email) ?? "";

    return userViewItem;
  }

  // GET: api/auth/check
  // Checks authorization status
  [HttpGet("check")]
  [Produces("application/json")]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrMessage))]
  public IActionResult Check()
  {
    return NoContent();
  }

  // GET: api/auth/status
  // Checks service status
  [HttpGet("status")]
  [AllowAnonymous]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [Produces("application/json", Type = typeof(Status))]
  public async Task<ActionResult<Status>> Status()
  {
    TrustVpnBaseContainer oContainer = new("trustvpn-container");
    string? serviceContainerId = await oContainer.GetContainerId();

    oContainer = new("trustvpn-db");
    string? dbContainerId = await oContainer.GetContainerId();

    var status = new Status(serviceContainerId, dbContainerId) {
      Message = "Hello, world!",
      ServiceContainerId = serviceContainerId ?? "not found",
      DBContainerId = dbContainerId ?? "not found"
    };

    return Ok(status);
  }
}
