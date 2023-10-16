using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.LocalUsers;

public class LocalUserController
{

    [HttpPost]
    public async Task<string> Authenticate([FromForm] string username, [FromForm] string password)
    {
        return "";
    }
}