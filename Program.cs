using Microsoft.AspNetCore.Routing.Patterns;

var builder = WebApplication.CreateBuilder(args);


builder.Logging.ClearProviders();
builder.Logging.AddConsole();


//var connString = builder.Configuration.GetConnectionString("UniversityDB");
//if (string.IsNullOrEmpty(connString))
//{
//    throw new Exception("Connection string 'UniversityDB' not found in appsettings.json!");
//}

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
    context.Response.Redirect("/Login/Login");
    return Task.CompletedTask;
});



//MVC routes
app.MapControllerRoute(
    name: "default",
     //pattern: "{controller=Login}/{action=Login}/{id?}");
pattern: "{controller=Home}/{action=Index}/{id?}");

//Razor Pages routes
app.MapRazorPages();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();



