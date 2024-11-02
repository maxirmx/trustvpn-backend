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

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using TrustVpn.Settings;

namespace TrustVpn.Models;

[Table("users")]
public class User
{
    [Column("id")]
    public int Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = "";

    [Column("last_name")]
    public string LastName { get; set; } = "";

    [Column("patronimic")]
    public string Patronimic { get; set; } = "";

    [Column("email")]
    public required string Email { get; set; }

    [Column("password")]
    public required string Password { get; set; }

    [Column("is_admin")]
    public required bool IsAdmin { get; set; }

    [Column("profile_id")]
    public required int ProfileId { get; set; }

    [ForeignKey("ProfileId")]
    public Profile? Profile { get; set; }
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}

public class UserViewItem(User user)
{
    public int Id { get; set; } = user.Id;
    public string FirstName { get; set; } = user.FirstName;
    public string LastName { get; set; } = user.LastName;
    public string Patronimic { get; set; } = user.Patronimic;
    public string Email { get; set; } = user.Email;
    public bool IsAdmin { get; set; } = user.IsAdmin;
    public int ProfileId { get; set; } = user.ProfileId;
    public string Config { get; set; } = "";
    public override string ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}

public class UserViewItemWithJWT(User user) :  UserViewItem(user)
{
    public string Token { get; set; } = "";
    public override string ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}

public class UserUpdateItem
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Patronimic { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool? IsAdmin { get; set; }
    public int? ProfileId { get; set; }
    public override string ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this, JOptions.DefaultOptions);
    }
}
