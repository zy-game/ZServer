using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ZServer;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApi;

[Route("api/")]
[ApiController]
public class APIReceiver : APIBase
{
    [HttpPost("startup")]
    public async Task<string> ServerStartup()
    {
        return JsonConvert.SerializeObject(IServerResult.Create(Status.Success, ""));
    }

    [HttpPost("shutdown")]
    public async Task<string> ServerCommand()
    {
        return JsonConvert.SerializeObject(IServerResult.Create(Status.Success, ""));
    }
}