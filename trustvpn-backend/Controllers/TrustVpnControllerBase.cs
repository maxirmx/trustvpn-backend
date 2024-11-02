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

using TrustVpn.Data;
using Microsoft.AspNetCore.Mvc;
using TrustVpn.Models;

namespace TrustVpn.Controllers;
public class TrustVpnControllerPreBase : ControllerBase
{
    protected ObjectResult _400()
    {
        return StatusCode(StatusCodes.Status400BadRequest,
                          new ErrMessage() { Msg = "Нарушена целостность запроса." });
    }
    protected ObjectResult _401()
    {
        return StatusCode(StatusCodes.Status401Unauthorized,
                          new ErrMessage { Msg = "Неправильный адрес электронной почты или пароль" });
    }
    protected ObjectResult _403()
    {
        return StatusCode(StatusCodes.Status403Forbidden,
                          new ErrMessage { Msg = "Недостаточно прав для выполнения операции." });
    }
    protected ObjectResult _404User(int id)
    {
        return StatusCode(StatusCodes.Status404NotFound,
                          new ErrMessage { Msg = $"Не удалось найти пользователя [id={id}]." });
    }
    protected ObjectResult _404Profile(int id)
    {
        return StatusCode(StatusCodes.Status404NotFound,
                          new ErrMessage { Msg = $"Не удалось найти профиль [profile id={id}]." });
    }
    protected ObjectResult _409Email(string email)
    {
        return StatusCode(StatusCodes.Status409Conflict,
                          new ErrMessage { Msg = $"Пользователь с таким адресом электронной почты уже зарегистрирован [email = {email}]." });
    }
    protected ObjectResult _418IAmATeaPot()
    {
        return StatusCode(StatusCodes.Status418ImATeapot, new ErrMessage { Msg = "Ошибка TrustVPN" });
    }

}
public class TrustVpnControllerBase : TrustVpnControllerPreBase
{
  protected readonly UserContext userContext;
  protected readonly ProfileContext profileContext;

  protected readonly int curUserId;

  protected TrustVpnControllerBase(IHttpContextAccessor httpContextAccessor, UserContext uContext, ProfileContext pContext)
  {
    userContext = uContext;
    profileContext = pContext;
    curUserId = 0;
    var htc = httpContextAccessor.HttpContext;
    if (htc != null) {
      var uid = htc.Items["UserId"];
      if (uid != null) curUserId = (int)uid;
    }
  }
}
