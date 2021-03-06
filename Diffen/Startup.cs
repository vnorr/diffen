﻿using System;
using System.IO;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Serilog;
using Serilog.Events;
using AutoMapper;

namespace Diffen
{
	using Database;
	using Database.Entities.User;
	using Database.Clients;
	using Database.Clients.Contracts;
	using Repositories;
	using Repositories.Contracts;

	public class Startup
	{
		public IConfigurationRoot Configuration { get; }

		public Startup(IHostingEnvironment environment)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(environment.ContentRootPath)
				.AddJsonFile($"appsettings.{environment.EnvironmentName}.json")
				.AddEnvironmentVariables();

			Configuration = builder.Build();

			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(Configuration)
				.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
				.Enrich.FromLogContext()
				.CreateLogger();
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddAuthentication();

			services.AddAuthorization(options =>
			{
				options.AddPolicy("IsAdmin", policy =>
				{
					policy.RequireAssertion(ctx => ctx.User.IsInRole("Admin"));
				});
				options.AddPolicy("IsAuthor", policy =>
				{
					policy.RequireAssertion(ctx => ctx.User.IsInRole("Author") || ctx.User.IsInRole("Admin"));
				});
				options.AddPolicy("IsManager", policy =>
				{
					policy.RequireAssertion(ctx => ctx.User.IsInRole("Admin") || ctx.User.IsInRole("Scissor"));
				});
			});

			services.AddSingleton(Configuration);

			services.AddDbContext<DiffenDbContext>();

			services.AddIdentity<AppUser, IdentityRole>(options =>
				{
					options.User.RequireUniqueEmail = true;
					options.Password.RequiredLength = 8;
				})
				.AddEntityFrameworkStores<DiffenDbContext>()
				.AddDefaultTokenProviders();

			services.ConfigureApplicationCookie(options =>
			{
				options.AccessDeniedPath = "/auth/login";
				options.Cookie.Name = "DiffenKaka";
				options.Cookie.HttpOnly = true;
				options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
				options.LoginPath = "/auth/login";
				options.SlidingExpiration = true;
			});

			services.AddAutoMapper();

			services.AddCors(options =>
			{
				options.AddPolicy("CorsPolicy", builder => builder
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowAnyOrigin()
					.AllowCredentials());
			});

			services.AddAntiforgery(
				options =>
				{
					// Rename the form input name from "__RequestVerificationToken" to "f" for the same reason above
					// e.g. <input name="__RequestVerificationToken" type="hidden" value="..." />
					options.FormFieldName = "f";

					// Rename the Anti-Forgery HTTP header from RequestVerificationToken to X-XSRF-TOKEN. X-XSRF-TOKEN
					// is not a standard but a common name given to this HTTP header popularized by Angular.
					options.HeaderName = "X-XSRF-TOKEN";
				}
			);

			services.AddDataProtection()
				.SetApplicationName("app-blaranderna")
				.PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "key-ring\\keys")));

			services.AddMvc();

			services.AddScoped<IDiffenDbClient, DiffenDbClient>();
			services.AddScoped<IPmRepository, PmRepository>();
			services.AddScoped<IPostRepository, PostRepository>();
			services.AddScoped<IUserRepository, UserRepository>();
			services.AddScoped<ISquadRepository, SquadRepository>();
			services.AddScoped<IPollRepository, PollRepository>();
			services.AddScoped<IChronicleRepository, ChronicleRepository>();
			services.AddScoped<IUploadRepository, UploadRepository>();
			services.AddScoped<IRegionRepository, RegionRepository>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, IAntiforgery antiforgery)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Shared/_ErrorPage");
			}

			app.UseAuthentication();
			app.UseStaticFiles();
			app.UseCors("CorsPolicy");

			app.Use(next => context =>
			{
				var path = context.Request.Path.Value;
				if (
					string.Equals(path, "/", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(path, "/auth/login", StringComparison.OrdinalIgnoreCase))
				{
					// The request token can be sent as a JavaScript-readable cookie, 
					// and Angular uses it by default.
					var tokens = antiforgery.GetAndStoreTokens(context);
					context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken,
						new CookieOptions
						{
							HttpOnly = false
						});
				}
				return next(context);
			});

			app.UseMvc(r =>
			{
				r.MapRoute(
					name: "default", 
					template: "{controller=home}/{action=index}/{id?}"
				);
			});
		}
	}
}
