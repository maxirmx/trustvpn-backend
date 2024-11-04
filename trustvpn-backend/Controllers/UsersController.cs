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

using TrustVpn.Authorization;
using TrustVpn.Data;
using TrustVpn.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrustVpn.Services;
using TrustVpn.Settings;
using System.Text.Json;

namespace TrustVpn.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrMessage))]

public class UsersController(
    IHttpContextAccessor httpContextAccessor,
    UserContext uContext,
    ProfileContext pContext,
    TrustVpnServiceContainer oContainer,
    ILogger<UsersController> logger) : TrustVpnControllerBase(httpContextAccessor, uContext, pContext)
{
    private readonly TrustVpnServiceContainer _oContainer = oContainer;
    private readonly ILogger<UsersController> _logger = logger;

    // GET: api/users
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserViewItem>))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    public async Task<ActionResult<IEnumerable<UserViewItem>>> GetUsers()
    {
        _logger.LogDebug("GetUsers");
        var ch = await userContext.CheckAdmin(curUserId);
        if (ch == null || !ch.Value)
        {
            _logger.LogDebug("GetUsers returning '403 Forbidden'");
            return _403();
        }

        var res = await userContext.UserViewItems();
        _logger.LogDebug("GetUsers returning:\n{items}\n", JsonSerializer.Serialize(res, JOptions.DefaultOptions));

        return res;
    }

    // GET: api/users/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserViewItem))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<ActionResult<UserViewItem>> GetUser(int id)
    {
        _logger.LogDebug("GetUser for id={id}", id);
        var ch = await userContext.CheckAdminOrSameUser(id, curUserId);
        if (ch == null || !ch.Value)
        {
            _logger.LogDebug("GetUser returning '403 Forbidden'");
            return _403();
        }

        var user = await userContext.UserViewItem(id);
        if (user == null)
        {
            _logger.LogDebug("GetUser returning '404 Not Found'");
            return _404User(id);
        }
        user.Config = await _oContainer.GetUserConfig(user.Email) ?? "";
        _logger.LogDebug("GetUser returning:\n{res}", user.ToString());
        return user;
    }

    // POST: api/users
    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Reference))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status409Conflict, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status418ImATeapot, Type = typeof(ErrMessage))]
    public async Task<ActionResult<Reference>> PostUser(User user)
    {
        _logger.LogDebug("PostUser (create) for {user}", user.ToString());
        var ch = await userContext.CheckAdmin(curUserId);
        if (ch == null || !ch.Value)
        {
            _logger.LogDebug("PostUser returning '403 Forbidden'");
            return _403();
        }

        if (userContext.Exists(user.Email))
        {
            _logger.LogDebug("PostUser returning '409 Conflict'");
            return _409Email(user.Email);
        }

        string hashToStoreInDb = BCrypt.Net.BCrypt.HashPassword(user.Password);
        user.Password = hashToStoreInDb;

        userContext.Users.Add(user);

        Profile? profile = await profileContext.Profiles.FindAsync(user.ProfileId);
        if (profile == null)
        {
            _logger.LogDebug("PostUser returning '404 Not Found'");
            return _404Profile(user.ProfileId);
        }

        string? output = await _oContainer.CreateUser(user.Email, profile.Prfile);
        if (output == null)
        {
            _logger.LogDebug("PostUser returning '418 I'm a teapot'");
            return _418IAmATeaPot();
        }

        await userContext.SaveChangesAsync();
        var reference = new Reference(user.Id) { Id = user.Id };
        _logger.LogDebug("PostUser returning:\n{res}", reference.ToString());
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, reference);
    }

    // PUT: api/users/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status418ImATeapot, Type = typeof(ErrMessage))]
    public async Task<IActionResult> PutUser(int id, UserUpdateItem update)
    {
        _logger.LogDebug("PutUser (update) for id={id} with {update}", id, update.ToString());
        var user = await userContext.Users.FindAsync(id);
        if (user == null)
        {
            _logger.LogDebug("PutUser returning '404 Not Found'");
            return _404User(id);
        }
        bool adminRequired = (user.ProfileId != update.ProfileId) || (user.IsAdmin != update.IsAdmin);

        ActionResult<bool> ch;
        ch = adminRequired ? await userContext.CheckAdmin(curUserId) :
                             await userContext.CheckAdminOrSameUser(id, curUserId);
        if (ch == null || !ch.Value)
        {
            _logger.LogDebug("PutUser returning '403 Forbidden'");
            return _403();
        }
        var crtProfile = user.ProfileId;
        Profile? profile = null;
        if (update.ProfileId != null && user.ProfileId != (int)update.ProfileId)
        {
            crtProfile = (int)update.ProfileId;
            profile = await profileContext.Profiles.FindAsync(crtProfile);
            if (profile == null)
            {
                _logger.LogDebug("PutUser returning '404 Not Found'");
                return _404Profile(crtProfile);
            }
        }

        if (update.Email != null && user.Email != update.Email)
        {
            if (userContext.Exists(update.Email)) return _409Email(update.Email);

            if (profile == null)
            {
                profile = await profileContext.Profiles.FindAsync(crtProfile);
                if (profile == null)
                {
                    _logger.LogDebug("PutUser returning '404 Not Found'");
                    return _404Profile(crtProfile);
                }
            }

            if (await _oContainer.RemoveUser(user.Email) == null)
            {
                _logger.LogDebug("PutUser returning '418 I'm a teapot'");  
                return _418IAmATeaPot();
            }
            if (await _oContainer.CreateUser(update.Email, profile.Prfile) == null)
            {
                _logger.LogDebug("PutUser returning '418 I'm a teapot'");
                return _418IAmATeaPot();
            }

            user.Email = update.Email;
        }
        else if (update.ProfileId != null && user.ProfileId != (int)update.ProfileId && profile != null)
        {
            if (await _oContainer.ModifyUser(user.Email, profile.Prfile) == null)
            {
                _logger.LogDebug("PutUser returning '418 I'm a teapot'");
                return _418IAmATeaPot();
            }
            user.ProfileId = (int)update.ProfileId;
        }

        if (update.FirstName != null) user.FirstName = update.FirstName;
        if (update.LastName != null) user.LastName = update.LastName;
        if (update.Patronimic != null) user.Patronimic = update.Patronimic;
        if (update.IsAdmin != null) user.IsAdmin = (bool)update.IsAdmin;
        if (update.Password != null) user.Password = BCrypt.Net.BCrypt.HashPassword(update.Password); ;

        userContext.Entry(user).State = EntityState.Modified;

        await userContext.SaveChangesAsync();
        _logger.LogDebug("PutUser returning '204 No content'");
        return NoContent();
    }

    // DELETE: api/users/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status418ImATeapot, Type = typeof(ErrMessage))]
    public async Task<IActionResult> DeleteUser(int id)
    {
        _logger.LogDebug("DeleteUser for id={id}", id);
        var ch = await userContext.CheckAdmin(curUserId);
        if (ch == null || !ch.Value)
        {
            _logger.LogDebug("DeleteUser returning '403 Forbidden'");
            return _403();
        }
        var user = await userContext.Users.FindAsync(id);
        if (user == null)
        {
            _logger.LogDebug("DeleteUser returning '404 Not Found'");
            return _404User(id);
        }
        string? output = await _oContainer.RemoveUser(user.Email);

        if (output == null)
        {
            _logger.LogDebug("DeleteUser returning '418 I'm a teapot'");
            return _418IAmATeaPot();
        }

        userContext.Users.Remove(user);
        await userContext.SaveChangesAsync();
        _logger.LogDebug("DeleteUser returning '204 No content'");
        return NoContent();
    }

}
