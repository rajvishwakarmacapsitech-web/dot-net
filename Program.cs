using MongoDB.Driver;
using api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using api.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// MongoDB
var connectionString = builder.Configuration["MongoDB:ConnectionString"];
var databaseName = builder.Configuration["MongoDB:DatabaseName"];

var mongoClient = new MongoClient(connectionString);

// Test MongoDB connection
try
{
    await mongoClient
        .GetDatabase(databaseName)
        .RunCommandAsync<MongoDB.Bson.BsonDocument>(
            new MongoDB.Bson.BsonDocument("ping", 1)
        );

    Console.WriteLine("MongoDB connected successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"MongoDB connection failed: {ex.Message}");
}

// Register MongoDB
builder.Services.AddSingleton<IMongoClient>(mongoClient);

builder.Services.AddSingleton<IMongoDatabase>(
    mongoClient.GetDatabase(databaseName)
);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] =
                new List<string>()
        });
});

// Controllers
builder.Services.AddControllers();

// Cloudinary
builder.Services.AddScoped<CloudinaryService>();

// Jwt Service
builder.Services.AddScoped<JwtService>();

// Authentication and Authorization
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Secret"]!
                )
            )
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var database = context.HttpContext.RequestServices
                    .GetRequiredService<IMongoDatabase>();

                var users = database.GetCollection<User>("users");

                var userId = context.Principal?
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    context.Fail("Invalid user.");
                    return;
                }

                var user = await users
                    .Find(x => x.Id == userId)
                    .FirstOrDefaultAsync();

                Console.WriteLine($"User found---->>>>: {user}");

                if (user == null)
                {
                    context.Fail("User not found.");
                    return;
                }

                if (user.Status == UserStatus.Inactive)
                {
                    context.Fail("User is inactive.");
                    return;
                }

                if (!user.isEmailVerified)
                {
                    context.Fail("Email is not verified.");
                    return;
                }

                var identity = context.Principal?.Identity as ClaimsIdentity;

                identity?.AddClaim(
                    new Claim(
                        ClaimTypes.Role,
                        user.Role.ToString()
                    )
                );
            }
        };
    });
// Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// CORS
app.UseCors("AllowAll");

//Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Controllers
app.MapControllers();

//Run
app.Run();