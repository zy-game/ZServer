using Microsoft.AspNetCore.Authentication.Negotiate;
using WebApi;
using ZGame;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate();
builder.Services.AddAuthorization(options => { options.FallbackPolicy = options.DefaultPolicy; });
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
// app.UseHttpsRedirection();
//app.UseAuthentication();
//app.UseAuthorization();
app.MapControllers();
app.Run();