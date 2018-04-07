﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

using Serilog;
using AutoMapper;
using Newtonsoft.Json;

namespace Diffen.Controllers.Api
{
	using Models;
	using Helpers;
	using Helpers.Extensions;
	using Repositories.Contracts;
	using Database.Entities.User;
	using Database.Entities.Forum;

	using User = Models.User.User;

	[Route("api/[controller]")]
	public class UsersController : Controller
	{
		private const int PageSize = 5;

		private readonly IMapper _mapper;
		private readonly IPmRepository _pmRepository;
		private readonly IPostRepository _postRepository;
		private readonly IUserRepository _userRepository;

		private readonly UserManager<AppUser> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;

		private readonly ILogger _logger = Log.ForContext<UsersController>();

		public UsersController(IMapper mapper, IPmRepository pmRepository, IPostRepository postRepository, IUserRepository userRepository, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
		{
			_mapper = mapper;
			_pmRepository = pmRepository;
			_postRepository = postRepository;
			_userRepository = userRepository;
			_userManager = userManager;
			_roleManager = roleManager;
		}

		[HttpGet]
		public async Task<IActionResult> Get()
		{
			try
			{
				return Json(await _userRepository.GetUsersAsync()
					.ContinueWith(t =>
					{
						return t.Result.Select(user =>
							new KeyValuePair<string, string>(user.Id,
								user.NickNames.OrderByDescending(x => x.Created).FirstOrDefault()?.Nick)).ToList();
					}));
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "Get: An unexpected error occured when fetching users");
				return BadRequest();
			}
		}

		[HttpGet("{userId}")]
		public async Task<IActionResult> Get(string userId)
		{
			try
			{
				return Json(_mapper.Map<User>(await _userRepository.GetUserOnIdAsync(userId)));
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "Get: An unexpected error occured when trying to fetch and map a user with id {userId}", userId);
				return BadRequest();
			}
		}

		[HttpGet("{userId}/posts/{pageId}")]
		public async Task<IActionResult> GetPosts(string userId, int pageId)
		{
			try
			{
				var allPosts = await _postRepository.GetPostsOnUserIdAsync(userId);
				var posts = allPosts as IList<Post> ?? allPosts.ToList();

				return Json(new Paging<Post>
				{
					Data = _mapper.Map<List<Post>>(posts.Skip(PageSize * (pageId - 1)).Take(PageSize)),
					CurrentPage = pageId,
					NumberOfPages = Convert.ToInt32(Math.Ceiling((double)posts.Count / PageSize)),
					Total = posts.Count
				});
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "GetPosts: An unexpected error occured when trying to fetch posts created by user with id {userId}", userId);
				return BadRequest();
			}
		}

		[HttpGet("{userId}/posts/saved/{pageId}")]
		public async Task<IActionResult> GetSavedPosts(string userId, int pageId)
		{
			try
			{
				var allPosts = await _postRepository.GetSavedPosts(userId);
				var posts = allPosts as IList<Post> ?? allPosts.ToList();

				return Json(new Paging<Post>
				{
					Data = _mapper.Map<List<Post>>(posts.Skip(PageSize * (pageId - 1)).Take(PageSize)),
					CurrentPage = pageId,
					NumberOfPages = Convert.ToInt32(Math.Ceiling((double)posts.Count / PageSize)),
					Total = posts.Count
				});
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "GetSavedPosts: An unexpected error occured when trying to fetch posts saved by user with id {userId}", userId);
				return BadRequest();
			}
		}

		[HttpPost("{userId}/player/add")]
		public async Task<IActionResult> AddFavoritePlayer(string userId, int playerId)
		{
			try
			{
				if (await _userRepository.FavoritePlayerExistsAsync(userId))
				{
					await _userRepository.RemovePlayerToUserAsync(userId);
				}

				var results = new List<Result>();
				await _userRepository.AddFavoritePlayerAsync(new FavoritePlayer
				{
					PlayerId = playerId,
					UserId = userId
				}).ContinueWith(task => task.UpdateResults(ResultMessages.CreateFavoritePlayer, results));
				return Json(results);
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "AddFavoritePlayer: An unexpected error occured when adding a favorite player for user with id {userId}", userId);
				return BadRequest();
			}
		}

		[HttpPost("{userId}/player/remove")]
		public async Task<IActionResult> RemovePlayerToUser(string userId)
		{
			try
			{
				return Json(await _userRepository.RemovePlayerToUserAsync(userId));
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "RemovePlayerToUser: An unexpected error occured when removing a favorite player for user with id {userId}", userId);
				return BadRequest();
			}
		}

		[HttpPost("{userId}/seclude")]
		public async Task<IActionResult> Seclude(string userId, string to)
		{
			try
			{
				var user = await _userRepository.GetUserOnIdAsync(userId);
				user.SecludedUntil = Convert.ToDateTime(to);
				return Json(await _userRepository.UpdateUserAsync(user));
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "Seclude: An unexpected error occured when secluding user with id {userId}", userId);
				return BadRequest();
			}
		}

		[HttpPost("{userId}/nick/add")]
		public async Task<IActionResult> AddNickToUser(string userId, string nick)
		{
			try
			{
				if (await _userRepository.NickExistsAsync(nick))
					return NoContent();

				var results = new List<Result>();
				await _userRepository.AddNickNameAsync(new NickName()
				{
					UserId = userId,
					Nick = nick,
					Created = DateTime.Now
				}).ContinueWith(task => task.UpdateResults(ResultMessages.CreateNick, results));
				return Json(results);
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "AddNickToUser: An unexpected error occured when adding a new nick to user with id {userId}", userId);
				return BadRequest();
			}
		}

		[HttpPost("{userId}/bio")]
		public async Task<IActionResult> UpdateUserBio(string userId, string bio)
		{
			try
			{
				var user = await _userRepository.GetUserOnIdAsync(userId);
				user.Bio = bio;

				var results = new List<Result>();
				await _userRepository.UpdateUserAsync(user)
					.ContinueWith(task => task.UpdateResults(ResultMessages.UpdateBio, results));
				return Json(results);
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "UpdateUserBio: An unexpected error occured when updating the bio for user with id {userId}", userId);
				return BadRequest();
			}
		}

		[HttpGet("roles")]
		public IActionResult GetRoles() => Json(_roleManager.Roles.Select(x => x.Name).ToList());

		[HttpPost("{userId}/roles")]
		public async Task<IActionResult> UpdateUserRoles(string userId, string roles)
		{
			try
			{
				var user = await _userRepository.GetUserOnIdAsync(userId);
				var mappedUser = _mapper.Map<User>(user);
				foreach (var role in mappedUser.InRoles)
				{
					await _userManager.RemoveFromRoleAsync(user, role);
				}
				foreach (var role in JsonConvert.DeserializeObject<List<string>>(roles))
				{
					await _userManager.AddToRoleAsync(user, role);
				}
				return Ok();
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "UpdateUserRoles: An unexpected error occured when updating roles for user with id {userId}", userId);
				return BadRequest();
			}
		}

		[HttpGet("role")]
		public async Task<IActionResult> GetUsersInRole(string name)
		{
			try
			{
				return Json(await _userManager.GetUsersInRoleAsync(name)
					.ContinueWith(t =>
					{
						return t.Result.Select(user =>
							new KeyValuePair<string, string>(user.Id,
								user.NickNames.OrderByDescending(x => x.Created).FirstOrDefault()?.Nick)).ToList();
					}));
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "GetUsersInRole: An unexpected error occured when fetching users in role {role}", name);
				return BadRequest();
			}
		}

		[HttpGet("invites")]
		public async Task<IActionResult> GetInvites() => Json(_mapper.Map<List<Invite>>(await _userRepository.GetInvitesAsync()));

		[HttpGet, Route("invites/exist")]
		public async Task<IActionResult> InviteExists(string email) => Ok(await _userRepository.EmailHasInvite(email));

		[HttpPost("{userId}/invites/add")]
		public async Task<IActionResult> AddInvite(string userId, [FromBody] Models.User.CRUD.Invite invite)
		{
			try
			{
				if (invite == null)
					return BadRequest();

				if (await _userRepository.EmailHasInvite(invite.Email))
				{
					return BadRequest($"det finns redan en inbjudan på email {invite.Email}");
				}
				var results = new List<Result>();
				await _userRepository.AddInviteAsync(new Invite
				{
					Email = invite.Email,
					InvitedByUserId = invite.InvitedByUserId,
					InviteSent = DateTime.Now
				}).ContinueWith(task => task.UpdateResults(ResultMessages.CreateInvite, results));
				return Json(results);
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "AddInvite: An unexpected error occured when adding an invite to email {email}", invite?.Email);
				return BadRequest();
			}
		}

		[HttpPost("{userId}/filter")]
		public async Task<IActionResult> ChangeFilter(string userId, [FromBody] Models.User.Filter filter)
		{
			try
			{
				if (filter == null)
					return BadRequest();

				var currentFilter = await _userRepository.GetFiltersOnUserIdAsync(userId);
				if (currentFilter == null)
				{
					return Json(await _userRepository.AddUserFilterAsync(new Filter
					{
						UserId = userId,
						PostsPerPage = filter.PostsPerPage,
						ExcludedUserIds = string.Join(",", filter.ExcludedUsers.Select(x => x.Key))
					}));
				}
				currentFilter.PostsPerPage = filter.PostsPerPage;
				currentFilter.ExcludedUserIds = string.Join(",", filter.ExcludedUsers.Select(x => x.Key));
				return Json(await _userRepository.UpdateUserFilterAsync(currentFilter));
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "AddInvite: An unexpected error occured when changing the forum filters for user with id {userid}", userId);
				return BadRequest();
			}
		}

		[HttpGet, Route("{userId}/pm")]
		public async Task<IActionResult> GetPmOnUser(string userId, string to)
		{
			try
			{
				if (!userId.Equals(_userManager.GetUserId(User)))
				{
					return Forbid();
				}
				return Json(_mapper.Map<List<Models.User.PersonalMessage>>(await _pmRepository.GetPmsSentFromUserToUserAsync(userId, to)));
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "GetPmOnUser: An unexpected error occured when fetching pm for user with id {userid}", userId);
				return BadRequest();
			}
		}

		[HttpPost, Route("{userId}/pm/create")]
		public async Task<IActionResult> CreatePm(string id, [FromBody] Models.User.CRUD.PersonalMessage pm)
		{
			try
			{
				if (pm == null)
					return BadRequest();

				if (string.IsNullOrEmpty(pm.Message))
				{
					return BadRequest("pm får inte vara tomt");
				}
				var results = new List<Result>();
				await _pmRepository.AddPmAsync(new PersonalMessage
				{
					FromUserId = pm.FromUserId,
					ToUserId = pm.ToUserId,
					Message = pm.Message,
					Created = DateTime.Now
				}).ContinueWith(task => task.UpdateResults(ResultMessages.CreatePm, results));
				return Json(results);
			}
			catch (Exception e)
			{
				_logger.Warning(e.Message, "CreatePm: An unexpected error occured when creating pm for user with id {userid}", pm?.FromUserId);
				return BadRequest();
			}
		}
	}
}