using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MVAManagement.Data;
using MVAManagement.Models;
using System.ComponentModel;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//var connectionString = builder.Configuration.GetConnectionString("MVA") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));
//builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>();
//builder.Services.AddControllersWithViews();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseMigrationsEndPoint();
//}
//else
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");
//app.MapRazorPages();

//app.Run();


// CORRECT for EPPlus 8.5.1 - Use the new License API
//ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var builder = WebApplication.CreateBuilder(args);

/*  DB  */
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MVA"),
        sqlOptions => sqlOptions.UseCompatibilityLevel(120)
    ));

///* Second DB: SHReportPortal (for stored procedure)  */
//builder.Services.AddDbContext<SHReportDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("SHReportPortalConnection")));

// ────────────────────────────────────────────────────────────────
// CHANGE #1 — Add IdentityCore (UserManager + RoleManager ONLY)
//
// WHY: We use an external API for authentication — we do NOT want
//      Identity's login/password/SignInManager pipeline.
//      AddIdentityCore gives us just the user+role storage layer
//      so we can do: _userManager.FindByNameAsync(username)
//                    _userManager.GetRolesAsync(user)
//
// DO NOT use AddDefaultIdentity or AddIdentity — those pull in
// SignInManager and cookie middleware that conflicts with our
// existing cookie auth setup below.
// ────────────────────────────────────────────────────────────────
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    // Password rules are irrelevant — we verify via CheckPasswordAsync.
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 1;
})
.AddRoles<IdentityRole>()                        // enables RoleManager<IdentityRole>
.AddEntityFrameworkStores<ApplicationDbContext>() // binds to YOUR DbContext
.AddDefaultTokenProviders();                      // needed if you ever send reset emails

/*  Cookie Auth  */
builder.Services.AddAuthentication(
    Microsoft.AspNetCore.Authentication.Cookies
        .CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy =
            Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
    });

// ────────────────────────────────────────────────────────────────
// CHANGE #2 — Replace AddAuthorization() with role-aware policy
//
// WHY: The plain AddAuthorization() still works for [Authorize],
//      but adding the role policies here lets you use the cleaner
//      [Authorize(Policy = "IGHOnly")] attribute anywhere, AND
//      gives you a single place to adjust role rules in future.
//
// [Authorize(Roles = "Admin,IGH")] still works as before —
// these policies are an optional convenience on top.
// ────────────────────────────────────────────────────────────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("IGHOnly", policy => policy.RequireRole("Admin", "IGH"));
    options.AddPolicy("HROnly", policy => policy.RequireRole("Admin", "HR"));
});

/*  Session  */
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

/* HttpClient  */
builder.Services.AddHttpClient();


/* Antiforgery — allow header-based token for JSON POST */
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");

/* MVC  */
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        /*
         * CRASH PREVENTION #1 — PropertyNamingPolicy = null
         *
         * WHY: .NET default serializer converts to camelCase.
         *      Kendo JS grid expects PascalCase (Data, Total, Errors).
         *      Without this: grid shows empty data even though
         *      controller returns correct data.
         */
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

/*
 * CRASH PREVENTION #2 — DO NOT ADD AddKendo()
 * CRASH PREVENTION #3 — DO NOT USE [DataSourceRequest]
 * (reasons unchanged — see original comments)
 */
// builder.Services.AddKendo();   ← NEVER uncomment for .NET 8 + Kendo 2019

// ────────────────────────────────────────────────────────────────

//builder.Services.AddHostedService<DownloadsCleanupService>();

var app = builder.Build();

// ────────────────────────────────────────────────────────────────
// CHANGE #3 — Seed roles on startup
//
// WHY: AspNetRoles table must have "Admin", "IGH", "HR" rows
//      before any user can be assigned to them. Running this on
//      every startup is safe — it only inserts if the role does
//      not already exist. No migration needed for role data.
// ────────────────────────────────────────────────────────────────
//using (var scope = app.Services.CreateScope())
//{
//    var roleManager = scope.ServiceProvider
//        .GetRequiredService<RoleManager<IdentityRole>>();

//    foreach (var roleName in new[] { "Admin", "IGH", "HR" })
//    {
//        if (!await roleManager.RoleExistsAsync(roleName))
//            await roleManager.CreateAsync(new IdentityRole(roleName));
//    }
//}

// Place this block AFTER var app = builder.Build();
// and BEFORE app.UseHttpsRedirection();
// ═══════════════════════════════════════════════════════════════

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // ── Ensure all required roles exist ──────────────────────────
    var requiredRoles = new[] { "Admin", "Solicitor", "Caseworker", "Finance" };
    foreach (var roleName in requiredRoles)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
            Console.WriteLine($"[Seed] Role '{roleName}' created.");
        }
    }

    // ── Assign Admin role to your user ────────────────────────────
    // CHANGE THIS to your actual username or email
    var adminUsername = "tancm.mi";   // ← replace with your username

    var adminUser = await userManager.FindByNameAsync(adminUsername)
                 ?? await userManager.FindByEmailAsync(adminUsername);

    if (adminUser != null)
    {
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine($"[Seed] Admin role assigned to '{adminUser.UserName}'.");
        }
        else
        {
            Console.WriteLine($"[Seed] '{adminUser.UserName}' already has Admin role.");
        }
    }
    else
    {
        Console.WriteLine($"[Seed] WARNING: User '{adminUsername}' not found. Check username.");
    }
}



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();        /* ← MUST be before UseAuthentication */
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
