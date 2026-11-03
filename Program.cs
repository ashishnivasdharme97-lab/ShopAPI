// using Microsoft.EntityFrameworkCore;
// using ShopAPI.Data;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.IdentityModel.Tokens;
// using System.Text;

// var builder = WebApplication.CreateBuilder(args);

// // ========================================
// // CONTROLLERS
// // ========================================
// builder.Services.AddControllers();

// // ========================================
// // POSTGRESQL DATABASE
// // ========================================
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseNpgsql(
//         builder.Configuration.GetConnectionString("DefaultConnection")
//     )
// );

// // ========================================
// // CORS
// // ========================================
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowAll", policy =>
//     {
//         policy.AllowAnyOrigin()
//               .AllowAnyHeader()
//               .AllowAnyMethod();
//     });
// });

// // ========================================
// // JWT
// // ========================================
// builder.Services
//     .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = false,
//             ValidateAudience = false,
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,

//             IssuerSigningKey = new SymmetricSecurityKey(
//                 Encoding.UTF8.GetBytes(
//                     builder.Configuration["Jwt:Key"]!
//                 )
//             )
//         };
//     });

// builder.Services.AddAuthorization();

// // ========================================
// // SWAGGER
// // ========================================
// builder.Services.AddEndpointsApiExplorer();

// builder.Services.AddSwaggerGen(c =>
// {
//     c.AddSecurityDefinition(
//         "Bearer",
//         new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//         {
//             Name = "Authorization",
//             Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
//             Scheme = "Bearer",
//             BearerFormat = "JWT",
//             In = Microsoft.OpenApi.Models.ParameterLocation.Header
//         });

//     c.AddSecurityRequirement(
//         new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
//         {
//             {
//                 new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//                 {
//                     Reference =
//                         new Microsoft.OpenApi.Models.OpenApiReference
//                         {
//                             Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
//                             Id = "Bearer"
//                         }
//                 },
//                 Array.Empty<string>()
//             }
//         });
// });

// // ========================================
// // BUILD
// // ========================================
// var app = builder.Build();

// // ========================================
// // EXCEPTION HANDLER
// // ========================================
// app.UseExceptionHandler(errorApp =>
// {
//     errorApp.Run(async context =>
//     {
//         var exception =
//             context.Features.Get<
//                 Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

//         if (exception != null)
//         {
//             Console.WriteLine(exception.Error);
//         }

//         context.Response.StatusCode = 500;
//         context.Response.ContentType = "application/json";

//         await context.Response.WriteAsJsonAsync(new
//         {
//             message = "Internal Server Error"
//         });
//     });
// });

// // ========================================
// // MIDDLEWARE
// // ========================================
// app.UseRouting();

// app.UseCors("AllowAll");

// app.UseAuthentication();

// app.UseAuthorization();

// app.UseStaticFiles();

// // ========================================
// // SWAGGER
// // ========================================
// app.UseSwagger();

// app.UseSwaggerUI();

// // ========================================
// // CONTROLLERS
// // ========================================
// app.MapControllers();

// // ========================================
// // RUN
// // ========================================
// app.Run();




using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Npgsql;
using System.Diagnostics;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using ShopAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// CONTROLLERS
// ========================================
builder.Services.AddControllers();

var jwtSigningKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
if (string.IsNullOrWhiteSpace(jwtSigningKey) || Encoding.UTF8.GetByteCount(jwtSigningKey) < 32)
{
    throw new InvalidOperationException(
        "JWT_SIGNING_KEY must be configured as a secret of at least 32 bytes.");
}

// ========================================
// FIREBASE CLOUD MESSAGING
// ========================================
FirebaseMessaging? firebaseMessaging = null;
string? firebaseConfigurationMessage = null;

try
{
    var serviceAccountJson =
        Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_JSON");
    var credentialsFile =
        Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

    GoogleCredential? firebaseCredential = null;

    if (!string.IsNullOrWhiteSpace(serviceAccountJson))
    {
        firebaseCredential = GoogleCredential.FromJson(serviceAccountJson);
    }
    else if (!string.IsNullOrWhiteSpace(credentialsFile))
    {
        firebaseCredential = GoogleCredential.FromFile(credentialsFile);
    }

    if (firebaseCredential is null)
    {
        firebaseConfigurationMessage =
            "FCM is disabled: set FIREBASE_SERVICE_ACCOUNT_JSON or GOOGLE_APPLICATION_CREDENTIALS.";
    }
    else
    {
        var firebaseApp = FirebaseApp.Create(new AppOptions
        {
            Credential = firebaseCredential,
            ProjectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID") ??
                        "kirana-shop-app-be084"
        }, "ShopAPI");

        firebaseMessaging = FirebaseMessaging.GetMessaging(firebaseApp);
    }
}
catch (Exception exception)
{
    firebaseConfigurationMessage =
        $"FCM is disabled because Firebase credentials could not be initialized ({exception.GetType().Name}).";
}

builder.Services.AddSingleton<IFcmPushSender>(new FirebasePushSender(firebaseMessaging));
builder.Services.AddScoped<NotificationEventService>();
builder.Services.AddHostedService<NotificationOutboxWorker>();
builder.Services.AddHostedService<FestivalReminderWorker>();

// ========================================
// POSTGRESQL DATABASE
// ========================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ========================================
// CORS
// ========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ========================================
// JWT
// ========================================
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSigningKey)
            )
        };
    });

builder.Services.AddAuthorization();

// ========================================
// SWAGGER
// ========================================
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition(
        "Bearer",
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

// ========================================
// BUILD
// ========================================
var app = builder.Build();

if (firebaseConfigurationMessage is not null)
{
    app.Logger.LogWarning("{FirebaseConfigurationMessage}", firebaseConfigurationMessage);
}
else
{
    app.Logger.LogInformation("Firebase Cloud Messaging initialized successfully.");
}

var apiRequestLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("ApiRequests");

// ========================================
// DATABASE MIGRATION
// ========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.Migrate();
}
// ========================================
// TEMPORARY POSTGRESQL CONNECTION TEST
// ========================================
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

try
{
    using var connection = new NpgsqlConnection(connectionString);

    Console.WriteLine("========================================");
    Console.WriteLine("DB CONNECTION TEST START...");
    Console.WriteLine("========================================");

    await connection.OpenAsync();

    Console.WriteLine("========================================");
    Console.WriteLine("DB CONNECTION SUCCESS!");
    Console.WriteLine($"Database : {connection.Database}");
    Console.WriteLine($"Host     : {connection.Host}");
    Console.WriteLine($"Port     : {connection.Port}");
    Console.WriteLine("========================================");
}
catch (Exception ex)
{
    Console.WriteLine("========================================");
    Console.WriteLine("DB CONNECTION FAILED!");
    Console.WriteLine("========================================");
    Console.WriteLine(ex.ToString());
    Console.WriteLine("========================================");
}

// ========================================
// EXCEPTION HANDLER
// ========================================
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception =
            context.Features.Get<
                Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

        if (exception != null)
        {
            Console.WriteLine(exception.Error);
        }

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            message = "Internal Server Error"
        });
    });
});

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    var startedAt = Stopwatch.GetTimestamp();

    try
    {
        await next();

        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        var statusCode = context.Response.StatusCode;

        if (statusCode < StatusCodes.Status400BadRequest)
        {
            apiRequestLogger.LogInformation(
                "API request succeeded: {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds:F0} ms.",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                elapsedMilliseconds);
        }
        else
        {
            apiRequestLogger.LogWarning(
                "API request failed: {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds:F0} ms.",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                elapsedMilliseconds);
        }
    }
    catch (Exception ex)
    {
        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
        apiRequestLogger.LogError(
            ex,
            "API request failed: {Method} {Path} after {ElapsedMilliseconds:F0} ms.",
            context.Request.Method,
            context.Request.Path,
            elapsedMilliseconds);
        throw;
    }
});

// ========================================
// MIDDLEWARE
// ========================================
app.UseRouting();

app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

app.UseStaticFiles();

// ========================================
// SWAGGER
// ========================================
app.UseSwagger();

app.UseSwaggerUI();

// ========================================
// CONTROLLERS
// ========================================
app.MapControllers();

// ========================================
// RUN
// ========================================
app.Run();