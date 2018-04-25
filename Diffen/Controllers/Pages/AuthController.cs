﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;

using Serilog;

namespace Diffen.Controllers.Pages
{
	using ViewModels;
	using Repositories.Contracts;
	using Database.Entities.User;

	public class AuthController : Controller
	{
		private readonly IUserRepository _userRepository;
		private readonly UserManager<AppUser> _userManager;
		private readonly SignInManager<AppUser> _signInManager;
		private readonly IUploadRepository _uploadRepository;

		private readonly IHostingEnvironment _environment;

		private readonly ILogger _logger = Log.ForContext<AuthController>();

		public AuthController(IUserRepository userRepository, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IHostingEnvironment environment, IUploadRepository uploadRepository)
		{
			_userRepository = userRepository;
			_userManager = userManager;
			_signInManager = signInManager;
			_environment = environment;
			_uploadRepository = uploadRepository;
		}

		public IActionResult Login()
		{
			if (User.Identity.IsAuthenticated)
			{
				return RedirectToAction("index", "forum");
			}
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginViewModel vm, string returnUrl)
		{
			if (!ModelState.IsValid)
			{
				return View();
			}

			var attempt = await _userRepository.GetUserOnEmailAsync(vm.Email);
			if (attempt == null)
			{
				ModelState.AddModelError("All", "kontot existerar inte. var god skapa ett nytt!");
				return View();
			}
			if (!string.IsNullOrEmpty(attempt.SecludedUntil) && Convert.ToDateTime(attempt.SecludedUntil) > DateTime.Now)
			{
				_logger.Information(
					"Login: User with email {userEmail} tried to login even though he or she is secluded until {secludedUntil}",
					attempt.Email, attempt.SecludedUntil);
				ModelState.AddModelError("All", $"du är spärrad till och med {attempt.SecludedUntil}");
				return View();
			}

			var result = await _signInManager.PasswordSignInAsync(
				vm.Email,
				vm.Password,
				true, false);

			if (result.Succeeded)
			{
				if (string.IsNullOrWhiteSpace(returnUrl))
				{
					return RedirectToAction("index", "forum");
				}
				return Redirect(returnUrl);
			}

			ModelState.AddModelError("All", "felaktiga inloggningsuppgifter");
			return View();
		}

		public IActionResult Register()
		{
			if (User.Identity.IsAuthenticated)
			{
				return RedirectToAction("index", "forum");
			}
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Register(RegisterViewModel vm, string returnUrl)
		{
			if (!ModelState.IsValid)
			{
				return View(vm);
			}
			if (!await _userRepository.EmailHasInvite(vm.Email))
			{
				ModelState.AddModelError("", "hittade ingen inbjudan på den valda emailen");
				return View(vm);
			}
			if (await _userRepository.NickExistsAsync(vm.NickName))
			{
				ModelState.AddModelError("", "nicket finns redan registrerat");
				return View(vm);
			}
			var user = new AppUser
			{
				UserName = vm.Email,
				Email = vm.Email,
				Bio = vm.Bio,
				Joined = DateTime.Now
			};
			if (string.Equals(vm.Password, vm.ConfirmPassword))
			{
				var result = await _userManager.CreateAsync(user, vm.Password);

				if (result.Succeeded)
				{
					await _userRepository.CreateNewNickNameAsync(user.Id, vm.NickName);
					await _userRepository.SetInviteAsAccountCreatedAsync(vm.Email);

					if (vm.Avatar != null)
					{
						var fileName = await _uploadRepository.UploadFileAsync("avatars", vm.Avatar, user.Id);
						await _userRepository.UpdateAvatarFileNameForUserWithIdAsync(user.Id, fileName);
					}

					await _signInManager.SignInAsync(user, isPersistent: false);

					if (string.IsNullOrWhiteSpace(returnUrl))
					{
						return RedirectToAction("index", "forum");
					}

					return Redirect(returnUrl);
				}
				if (result.Errors.Any(x => x.Code == "DuplicateUserName"))
				{
					ModelState.AddModelError("", "användarnamnet (email) finns redan registrerad");
				}
			}
			else
			{
				ModelState.AddModelError("", "lösenorden matchar inte");
			}
			return View(vm);
		}

		public async Task<IActionResult> Logout()
		{
			if (User.Identity.IsAuthenticated)
			{
				await _signInManager.SignOutAsync();
			}
			return RedirectToAction("index", "forum");
		}
	}
}