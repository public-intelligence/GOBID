using Core;
using Exceptionless;
using GOBID.Api.Infrastructure;
using GOBID.Api.Middlewares;
using GOBID.Infrastructure;
using GOBID.Infrastructure.Configuration;
using GOBID.Infrastructure.Security;
using GOBID.Infrastructure.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GOBID.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var base64UrlSecret = Configuration.GetRequired("Jwt:AppKey");
            var keyBytes = Base64UrlEncoder.DecodeBytes(base64UrlSecret);
            var signingKey = new SymmetricSecurityKey(keyBytes);

            // Resolver de clave para tokens M2M (kid = "app:{applicationId}").
            // Se registra ESTA instancia para que el contenedor y la validación de tokens compartan
            // una sola caché de claves derivadas.
            var appKeyResolver = new AppJwtSigningKeyResolver(Configuration);
            services.AddSingleton(appKeyResolver);

            services.AddAuthentication()
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = Configuration["Jwt:Audience"],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = signingKey,
                        IssuerSigningKeyResolver = appKeyResolver.ResolveKey
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = ctx =>
                        {
                            Console.WriteLine($"JWT Auth failed: {ctx.Exception.GetType().Name} - {ctx.Exception.Message}");
                            return Task.CompletedTask;
                        }
                    };
                });
            services.AddAuthorization();
            services.AddHttpContextAccessor();

            var graphMailSettings = Configuration.GetRequiredSection("GraphMailSettings").Get<GraphMailSettings>()!;
            services.AddEmailService(graphMailSettings);

            services.AddCors(opt =>
              {
                  opt.AddDefaultPolicy(policy =>
                  {
                       var allowedOrigins = Configuration.GetRequiredSection("AllowedClientOrigins").Get<List<string>>();
                      policy.WithOrigins((allowedOrigins ?? []).ToArray());
                      policy.AllowAnyHeader();
                      policy.AllowAnyMethod();
                      policy.AllowCredentials();
                  });
              });

            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            });

            services.AddVersionedApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
           {
               new OpenApiSecurityScheme {
                   Reference = new OpenApiReference {
                       Type = ReferenceType.SecurityScheme,
                       Id = "Bearer"
                   }
               },
               new string[] {}
           }
    });
            });

            services.AddApplicationLayer();
            services.AddPersistenceInfraestructure(Configuration);
            services.AddControllers().AddNewtonsoftJson();
            services.AddExceptionless(Configuration);

        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.  
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    c.SwaggerEndpoint(
                        $"/swagger/{description.GroupName}/swagger.json",
                        $"Api GOBID {description.GroupName.ToUpperInvariant()}");
                }
            });
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<PolicyEnforcementMiddleware>();
            app.UseMiddleware<LogsAndErrorHandlerMiddleware>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
        }
    }
}
