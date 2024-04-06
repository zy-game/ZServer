using Newtonsoft.Json;

namespace ZServer;



public static class MessageExtension
{
    private static HttpClient httpClient = new();

    public static async Task<IServerResult> POST(this IMessaged messaged, string url)
    {
        //todo 使用http请求
        var response = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(messaged)));
        var message = response.EnsureSuccessStatusCode();
        return JsonConvert.DeserializeObject<IServerResult.DefaultServerResult>(await message.Content.ReadAsStringAsync());
    }

    // public static async Task<IServerResult> GET(this IMessaged messaged, string url)
    // {
    //     //todo 使用http请求
    //     var response = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(messaged)));
    //     var message = response.EnsureSuccessStatusCode();
    //     return JsonConvert.DeserializeObject<IServerResult.DefaultServerResult>(await message.Content.ReadAsStringAsync());
    // }

    public static async Task<T> POST<T>(this IMessaged messaged, string url) where T : IServerResult, new()
    {
        //todo 使用http请求
        var response = await httpClient.PostAsync(url, new StringContent(JsonConvert.SerializeObject(messaged)));
        var message = response.EnsureSuccessStatusCode();
        return JsonConvert.DeserializeObject<T>(await message.Content.ReadAsStringAsync());
    }

    // public static async Task<T> GET<T>(this IMessaged messaged, string url) where T : IServerResult, new()
    // {
    //     //todo 使用http请求
    //     var response = await httpClient.GetAsync(url);
    //     var message = response.EnsureSuccessStatusCode();
    //     return JsonConvert.DeserializeObject<T>(await message.Content.ReadAsStringAsync());
    // }
}