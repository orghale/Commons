﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Commons.Entities;
using Commons.Interceptors;
using FluentValidation;
using FluentValidation.AspNetCore;
using FundsTransfer.Entities;
using FundsTransfer.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NJsonSchema;
using NSwag.AspNetCore;
using NSwag.SwaggerGeneration.Processors.Security;
using Serilog;

namespace FundsTransfer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AccessAgencyBankingCorsPolicy",
                builder => builder.AllowAnyOrigin()
                             .AllowAnyMethod()
                             .AllowAnyHeader()
                             .AllowCredentials());
            });

            //Add AppSettings
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            services.AddAuthentication()
              .AddJwtBearer(cfg =>
              {
                  cfg.RequireHttpsMetadata = false;
                  cfg.SaveToken = true;
                  cfg.TokenValidationParameters = new TokenValidationParameters()
                  {
                      ValidIssuer = Configuration["AppSettings:Issuer"],
                      ValidAudience = Configuration["AppSettings:Audience"],
                      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["AppSettings:SecretKey"]))
                  };
                  cfg.Events = new JwtBearerEvents
                  {
                      OnChallenge = c =>
                      {
                          c.HandleResponse();
                          c.Response.StatusCode = 401;
                          c.Response.ContentType = "text/plain";

                          return c.Response.WriteAsync("You are not authorized to access this resource!");
                      }
                  };
              });

            //Add AuthSettings
            services.Configure<AuthSettings>(Configuration.GetSection("AuthSettings"));

            //Validators
            services.AddScoped<IValidator<FundsTransferRequest>, FundsTransferRequestValidator>();

            services.AddDataProtection()
                      .PersistKeysToFileSystem(new DirectoryInfo(@"C:\Server\Share\Keys\"));

            //Oracle  Repositories
            services.AddScoped<IFundsTransferRepository, FundsTransferRepository>();

            services.AddMvc().AddFluentValidation(fvc => { }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory
            , IOptions<AppSettings> options)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            loggerFactory.AddSerilog();
            app.UseRequestResponseLogger(options);
            app.UseMvc();

            app.UseSwaggerUi3WithApiExplorer(settings =>
            {
                settings.GeneratorSettings.DefaultPropertyNameHandling = PropertyNameHandling.CamelCase;
                settings.PostProcess = document =>
                {
                    document.Info.Version = "v1";
                    document.Info.Title = "Funds Transfer API";
                    document.Info.TermsOfService = "None";
                };
                settings.GeneratorSettings.OperationProcessors.Add(new OperationSecurityScopeProcessor("Authorization"));
                settings.GeneratorSettings.DocumentProcessors.Add(new SecurityDefinitionAppender("Authorization", new NSwag.SwaggerSecurityScheme()
                {
                    Type = NSwag.SwaggerSecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    In = NSwag.SwaggerSecurityApiKeyLocation.Header,
                    Description = "Bearer token"
                }));
            });
        }
    }
}
