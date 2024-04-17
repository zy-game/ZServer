using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace WebApi;

public class APIBase : ControllerBase
{
    public async Task<T> ReadRequestDataAsync<T>()
    {
        using (var reader = new StreamReader(Request.Body))
        {
            return JsonConvert.DeserializeObject<T>(await reader.ReadToEndAsync());
        }
    }
}