using Microsoft.AspNetCore.Routing.Patterns;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();


// Redirect root URL to Login page
app.MapGet("/", context =>
{
    context.Response.Redirect("/LogIn");
    return Task.CompletedTask;
});



//MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//Razor Pages routes
app.MapRazorPages();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();



