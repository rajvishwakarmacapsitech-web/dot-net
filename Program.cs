using MongoDB.Driver;
using api.Services;
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
builder.Services.AddSwaggerGen();

// Controllers
builder.Services.AddControllers();

// Cloudinary
builder.Services.AddSingleton<CloudinaryService>();

var app = builder.Build();

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

// CORS
app.UseCors("AllowAll");

//Authentication and Authorization
app.UseAuthorization();

// Controllers
app.MapControllers();

//Run
app.Run();