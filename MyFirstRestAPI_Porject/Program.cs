using System.Text;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StudentApi.Authorization;
using StudentApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.WebHost.UseKestrel(options =>
{
	options.AddServerHeader = false;
});

var keyVaultUrl = builder.Configuration["KeyVault:Url"];
if (!string.IsNullOrEmpty(keyVaultUrl))
{
	builder.Configuration.AddAzureKeyVault(
		new Uri(keyVaultUrl),
		new DefaultAzureCredential());
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters()
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = "StudentApi",
			ValidAudience = "StudentApiUsers",
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(builder.Configuration["JwtSigningKey"]
					?? throw new KeyNotFoundException("'JWT_SECRET_KEY' key is not found in Environment Variables.")))
		};
	});

builder.Services.AddAuthorization(options =>
	{
		options.AddPolicy("StudentOwnerOrAdmin", policy =>
			policy.Requirements.Add(new StudentOwnerOrAdminRequirement()));
	});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
	{
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		Scheme = "Bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "JWT Authorization header using the Bearer scheme."
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			[]
		}
	});
});

builder.Services.AddCors(options =>
{
	options.AddPolicy("StudentsApiCorsPolicy", policy =>
	{
		policy.WithOrigins(
			"https://localhost:7217",
			"http://localhost:5215"
			)
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IAuthorizationHandler, StudentOwnerOrAdminHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("StudentsApiCorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
