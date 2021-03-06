﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

using AutoMapper;

namespace Diffen.Controllers.Pages
{
	using ViewModels.Pages;
	using Repositories.Contracts;

	[Route("omrostning")]
	public class PollController : CommonController<PollPageViewModel>
	{
		public PollController(IConfigurationRoot configuration, IMapper mapper, IUserRepository userRepository)
			: base(configuration, mapper, userRepository)
		{
		}

		[Authorize]
		public IActionResult Index()
		{
			return View("_Page", Model);
		}

		[Authorize]
		[Route("{slug}")]
		public IActionResult Index(string slug)
		{
			Model.SelectedPollSlug = slug;
			return View("_Page", Model);
		}
	}
}