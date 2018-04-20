﻿using System;
using System.IO;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;

using AutoMapper;

using Serilog;

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
				.Enrich.FromLogContext()
				.CreateLogger();
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddAuthentication();

			services.AddSingleton(Configuration);

			services.AddDbContext<DiffenDbContext>();

			services.AddIdentity<AppUser, IdentityRole>(c =>
			{
				c.User.RequireUniqueEmail = true;
				c.Password.RequiredLength = 8;
			}).AddEntityFrameworkStores<DiffenDbContext>();

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

			services.AddMvc();

			services.AddScoped<IDiffenDbClient, DiffenDbClient>();
			services.AddScoped<IPmRepository, PmRepository>();
			services.AddScoped<IPostRepository, PostRepository>();
			services.AddScoped<IUserRepository, UserRepository>();
			services.AddScoped<ISquadRepository, SquadRepository>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("Home/Error");
			}

			app.UseStaticFiles();

			app.UseAuthentication();

			app.UseFileServer(new FileServerOptions
			{
				FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"dist")),
				RequestPath = new PathString("/dist"),
				EnableDirectoryBrowsing = true
			});

			app.UseMvc(r =>
			{
				r.MapRoute(name: "default", template: "{controller=home}/{action=index}/{id?}");
			});
		}
	}
}
