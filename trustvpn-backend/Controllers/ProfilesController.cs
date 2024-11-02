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
using System.Text.Json;
using TrustVpn.Settings;

namespace TrustVpn.Controllers;


[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ErrMessage))]

public class ProfilesController(
    IHttpContextAccessor httpContextAccessor,
    UserContext uContext,
    ProfileContext pContext,
    ILogger<ProfilesController> logger) : TrustVpnControllerBase(httpContextAccessor, uContext, pContext)
{
    private readonly ILogger<ProfilesController> _logger = logger;

    // GET: api/profiles
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Profile>))]
    public async Task<ActionResult<IEnumerable<Profile>>> GetProfiles()
    {
        _logger.LogDebug("Get all profiles");
        var res = await profileContext.Profiles.ToListAsync();
        _logger.LogDebug("Get all profiles:\n {res}\n", JsonSerializer.Serialize(res, JOptions.DefaultOptions));
        return res;
    }

    // GET: api/profiles/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Profile))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<ActionResult<Profile>> GetProfile(int id)
    {
        _logger.LogDebug("GetProfile {id}", id);
        var profile = await profileContext.Profiles.FindAsync(id);
        if (profile == null)
        {
            _logger.LogDebug("GetProfile returning '404 Not found'");
            return _404Profile(id);
        }
        _logger.LogDebug("GetProfile returning {profile}", profile);
        return profile;
    }

    // POST: api/profiles/add
    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Reference))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    public async Task<ActionResult<Profile>> AddProfile(Profile profile)
    {
        _logger.LogDebug("PostProfile (create) for {profile}", profile);
        var ch = await userContext.CheckAdmin(curUserId);
        if (ch == null || !ch.Value)
        {
            _logger.LogDebug("PostProfile returning '403 Forbidden'");
            return _403();
        }
        profileContext.Profiles.Add(profile);
        await profileContext.SaveChangesAsync();

        var reference = new Reference(profile.Id) { Id = profile.Id };
        _logger.LogDebug("PostProfile returning {reference}", reference);
        return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, reference);
    }


    // PUT: api/profiles/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<ActionResult<Profile>> UpdateProfile(int id, Profile profile)
    {
        _logger.LogDebug("PutProfile (update) for {id} with {profile}", id, profile.ToString());
        if (id != profile.Id)
        {
            _logger.LogDebug("PutProfile returning '400 Bad Request'");
            return _400();
        }
        var ch = await userContext.CheckAdmin(curUserId);
        if (ch == null || !ch.Value)
        {
            _logger.LogDebug("PutProfile returning '403 Forbidden'");
            return _403();
        }
        profileContext.Entry(profile).State = EntityState.Modified;
        try
        {
            await profileContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!profileContext.Exists(id))
            {
                _logger.LogDebug("PutProfile returning '404 Not found'");
                return _404Profile(id);
            }
            else
            {
                throw;
            }
        }
        _logger.LogDebug("PutProfile returning '204 No Content'");
        return NoContent();
    }

    // DELETE: api/profiles/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ErrMessage))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrMessage))]
    public async Task<IActionResult> DeleteProfile(int id)
    {
        _logger.LogDebug("DeleteProfile for {id}", id);
        var profile = await profileContext.Profiles.FindAsync(id);
        if (profile == null)
        {
            _logger.LogDebug("DeleteProfile returning '404 Not found'");
            return _404Profile(id);
        }
        var ch = await userContext.CheckAdmin(curUserId);
        if (ch == null || !ch.Value)
        {
            _logger.LogDebug("DeleteProfile returning '403 Forbidden'");
            return _403();
        }
        profileContext.Profiles.Remove(profile);
        await profileContext.SaveChangesAsync();

        _logger.LogDebug("DeleteProfile returning '204 No Content'");
        return NoContent();
    }
}
