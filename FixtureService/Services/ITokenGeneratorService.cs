using FixtureService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FixtureService.Services
{
    public interface ITokenGeneratorService
    {
        string GenerateToken(LoginModel login);
    }
}
