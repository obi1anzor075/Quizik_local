using DataAccessLayer.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Services.Contracts
{
    public interface IAuthService
    {
        Task<ActionResult> Enable2FAAsync(string userId);
        Task<ActionResult> Disable2FAAsync(ClaimsPrincipal principal);

    }

}
