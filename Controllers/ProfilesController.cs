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

namespace TrustVpn.Controllers;


[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProfilesController : TrustVpnControllerBase
{
  public ProfilesController(IHttpContextAccessor httpContextAccessor, UserContext uContext, ProfileContext pContext):
         base(httpContextAccessor, uContext, pContext)
  {
  }

  // GET: api/profiles
  [HttpGet]
  public async Task<ActionResult<IEnumerable<Profile>>> GetProfiles()
  {

    var res = await profileContext.Profiles.ToListAsync();
    return res;
  }

  // GET: api/profiles/5
  [HttpGet("{id}")]
  public async Task<ActionResult<Profile>> GetProfile(int id)
  {
    var profile = await profileContext.Profiles.FindAsync(id);
    if (profile == null)  return NotFound();
    return profile;
  }

  // POST: api/profiles/add
  [HttpPost("add")]
  public async Task<ActionResult<Profile>> AddProfile(Profile profile)
  {
    var ch = await userContext.CheckAdmin(curUserId);
    if (ch == null || !ch.Value)  return _403();

    profileContext.Profiles.Add(profile);
    await profileContext.SaveChangesAsync();

    var reference = new Reference(profile.Id) { Id = profile.Id };
    return CreatedAtAction(nameof(GetProfile), new { id = profile.Id }, reference);
  }


  // PUT: api/profiles/5
  [HttpPut("{id}")]
  public async Task<ActionResult<Profile>> UpdateProfile(int id, Profile profile)
  {
    if (id != profile.Id) return BadRequest();

    var ch = await userContext.CheckAdmin(curUserId);
    if (ch == null || !ch.Value)  return _403();

    profileContext.Entry(profile).State = EntityState.Modified;
    try {
      await profileContext.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)  {
      if (!profileContext.Exists(id)) {
        return _404Profile(id);
      }
      else {
        throw;
      }
    }
    return NoContent();
  }

  // DELETE: api/profiles/5
  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteProfile(int id)
  {
    var profile = await profileContext.Profiles.FindAsync(id);
    if (profile == null) return _404Profile(id);

    var ch = await userContext.CheckAdmin(curUserId);
    if (ch == null || !ch.Value)  return _403();

    profileContext.Profiles.Remove(profile);
    await profileContext.SaveChangesAsync();

    return NoContent();
  }
}
