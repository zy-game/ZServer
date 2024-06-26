﻿using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ZGame;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ZGame.WebApi;

[DisableCors]
[Route("api")]
[ApiController]
public class GatewayApi : ControllerBase
{
    [HttpPost("startup")]
    public async Task<string> ServerStartup()
    {
        return default;
    }

    [HttpPost("shutdown")]
    public async Task<string> ServerCommand()
    {
        return default;
    }


}

