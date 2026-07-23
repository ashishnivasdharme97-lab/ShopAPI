// using Microsoft.EntityFrameworkCore;
// using ShopAPI.Data;

// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.IdentityModel.Tokens;
// using System.Text;

// var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddControllers();

// // 🔗 DB
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
// );

// // 🔥 CORS (VERY IMPORTANT)
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAll",
//         policy =>
//         {
//             policy.AllowAnyOrigin()
//                   .AllowAnyHeader()
//                   .AllowAnyMethod();
//         });
// });

// // 🔐 JWT AUTH
// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
// .AddJwtBearer(options =>
// {
//     options.TokenValidationParameters = new TokenValidationParameters
//     {
//         ValidateIssuer = false,
//         ValidateAudience = false,
//         ValidateLifetime = true,
//         ValidateIssuerSigningKey = true,
//         IssuerSigningKey = new SymmetricSecurityKey(
//             Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
//     };
// });

// builder.Services.AddAuthorization();

// // 📄 Swagger + JWT support
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(c =>
// {
//     c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//     {
//         Name = "Authorization",
//         Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
//         Scheme = "Bearer",
//         BearerFormat = "JWT",
//         In = Microsoft.OpenApi.Models.ParameterLocation.Header
//     });

//     c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
//     {
//         {
//             new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//             {
//                 Reference = new Microsoft.OpenApi.Models.OpenApiReference
//                 {
//                     Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
//                     Id = "Bearer"
//                 }
//             },
//             new string[] {}
//         }
//     });
// });

// var app = builder.Build();


// // 🔥 MIDDLEWARE ORDER (VERY IMPORTANT)
// app.UseCors("AllowAll");   // 👈 MUST BE FIRST

// app.UseAuthentication();
// app.UseAuthorization();

// app.UseSwagger();
// app.UseSwaggerUI();

// app.MapControllers();


// // 👇 DEFAULT ADMIN CREATE
// // using (var scope = app.Services.CreateScope())
// // {
// //     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

// //     if (!db.Users.Any(u => u.Role == "Admin"))
// //     {
// //         db.Users.Add(new ShopAPI.Models.User
// //         {
// //             Name = "Admin",
// //             Email = "admin@shop.com",
// //             Password = "admin123",
// //             Role = "Admin"
// //         });

// //         db.SaveChanges();
// //     }
// // }

// app.Run();






using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5022");
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header
        });

    c.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
        });
});

var app = builder.Build();

    

app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.UseSwagger();

app.UseSwaggerUI();

app.MapControllers();

app.Run();