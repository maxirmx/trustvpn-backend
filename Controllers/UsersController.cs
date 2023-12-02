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
using TrustVpn.Service;

namespace TrustVpn.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class UsersController : TrustVpnControllerBase
{
  public UsersController(IHttpContextAccessor httpContextAccessor, UserContext uContext, ProfileContext pContext) :
         base(httpContextAccessor, uContext, pContext)
  {
  }

  // GET: api/users
  [HttpGet]
  public async Task<ActionResult<IEnumerable<UserViewItem>>> GetUsers()
  {
    var ch = await userContext.CheckAdmin(curUserId);
    if (ch == null || !ch.Value)  return _403();

    return await userContext.UserViewItems();
  }

  // GET: api/users/5
  [HttpGet("{id}")]
  public async Task<ActionResult<UserViewItem>> GetUser(int id)
  {
    var ch = await userContext.CheckAdminOrSameUser(id, curUserId);
    if (ch == null ||!ch.Value)  return _403();

    var user = await userContext.UserViewItem(id);
    if (user == null) return _404User(id);

    if (user.ProfileId != Profile.NoProfile) {
      var oContainer = new TrustVpnServiceContainer();
      var output = await oContainer.GetUserConfig(user.Email);
      if (output != null) {
        user.Config = output;
      }
    }
    return user;
  }

    // POST: api/users
  [HttpPost("add")]
  public async Task<ActionResult<Reference>> PostUser(User user)
  {
    var ch = await userContext.CheckAdmin(curUserId);
    if (ch == null ||!ch.Value)  return _403();

    if (userContext.Exists(user.Email)) return _409Email(user.Email) ;

    string hashToStoreInDb = BCrypt.Net.BCrypt.HashPassword(user.Password);
    user.Password = hashToStoreInDb;

    userContext.Users.Add(user);

    if (user.ProfileId != Profile.NoProfile) {
      Profile? profile = await profileContext.Profiles.FindAsync(user.ProfileId);
      if (profile == null) {
        return _404Profile(user.ProfileId);
      }

      string? output = await new TrustVpnServiceContainer().CreateUser(user.Email, profile.Prfile);
      if (output == null) {
        return _418IAmATeaPot();
      }
    }

    await userContext.SaveChangesAsync();
    var reference = new Reference(user.Id) { Id = user.Id };
    return CreatedAtAction(nameof(GetUser), new { id = user.Id }, reference);
  }

  // PUT: api/users/5
  [HttpPut("{id}")]
  public async Task<IActionResult> PutUser(int id, UserUpdateItem update)
  {
    var user = await userContext.Users.FindAsync(id);
    if (user == null) return _404User(id);

    bool adminRequired = (user.ProfileId != update.ProfileId) || (user.IsAdmin != update.IsAdmin);

    ActionResult<bool> ch;
    ch = adminRequired ? await userContext.CheckAdmin(curUserId) :
                         await userContext.CheckAdminOrSameUser(id, curUserId);
    if (ch == null ||!ch.Value)  return _403();

    if (update.ProfileId != null) {
      user.ProfileId = (int)update.ProfileId;
      var newEmail = update.Email ?? user.Email;
      Profile? profile = await profileContext.Profiles.FindAsync(update.ProfileId);
      if (profile == null) {
        return _404Profile((int)update.ProfileId);
      }

      if (user.Email != newEmail && userContext.Exists(newEmail)) return _409Email(newEmail);
      var oContainer = new TrustVpnServiceContainer();

      if (user.Email != newEmail || user.ProfileId != update.ProfileId) {
        if (user.ProfileId != Profile.NoProfile) {
          if (await oContainer.RemoveUser(user.Email) == null) {
            return _418IAmATeaPot();
          }
        }
        if (update.ProfileId != Profile.NoProfile) {
          if (await oContainer.CreateUser(newEmail, profile.Prfile) == null) {
            return _418IAmATeaPot();
          }
        }
      }
    }

    if (update.Email != null)  user.Email = update.Email;
    if (update.FirstName != null) user.FirstName = update.FirstName;
    if (update.LastName != null) user.LastName = update.LastName;
    if (update.Patronimic != null) user.Patronimic = update.Patronimic;
    if (update.IsAdmin != null) user.IsAdmin = (bool)update.IsAdmin;
    if (update.Password != null) user.Password =  BCrypt.Net.BCrypt.HashPassword(update.Password);;

    userContext.Entry(user).State = EntityState.Modified;

    await userContext.SaveChangesAsync();
    return NoContent();
  }

  // DELETE: api/users/5
  [HttpDelete("{id}")]
  public async Task<IActionResult> DeleteUser(int id)
  {
    var ch = await userContext.CheckAdmin(curUserId);
    if (ch == null ||!ch.Value)  return _403();

    var user = await userContext.Users.FindAsync(id);
    if (user == null) return _404User(id);

    TrustVpnServiceContainer container = new();
    string? output = await container.RemoveUser(user.Email);

    if (output == null) {
      return _418IAmATeaPot();
    }

    userContext.Users.Remove(user);
    await userContext.SaveChangesAsync();

    return NoContent();
  }

}
