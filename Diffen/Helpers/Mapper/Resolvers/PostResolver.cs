﻿using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

using AutoMapper;

namespace Diffen.Helpers.Mapper.Resolvers
{
	using Extensions;

	public class PostResolver : 
		ITypeConverter<Database.Entities.Forum.Post, Models.Forum.Post>,
		ITypeConverter<Database.Entities.Forum.Post, Models.Forum.ParentPost>,
		ITypeConverter<Database.Entities.Forum.Vote, Models.Forum.Vote>, 
		ITypeConverter<Models.Forum.CRUD.Post, Database.Entities.Forum.Post>, 
		ITypeConverter<Models.Forum.CRUD.Vote, Database.Entities.Forum.Vote>,
		ITypeConverter<Database.Entities.Forum.UrlTip, string>
	{
		private readonly string _loggedInUserId;

		private static readonly string[] NoClicksForTheseSites = {
			"fotbolldirekt.se",
			"aftonbladet.se",
			"expressen.se",
			"aik.se",
			"aikfotboll.se",
			"hammarbyfotboll.se"
		};

		public PostResolver(UserManager<Database.Entities.User.AppUser> userManager, IHttpContextAccessor httpContextAccessor)
		{
			_loggedInUserId = userManager.GetUserId(httpContextAccessor.HttpContext.User);
		}

		public Models.Forum.Post Convert(Database.Entities.Forum.Post source, Models.Forum.Post destination, ResolutionContext context)
		{
			return new Models.Forum.Post
			{
				Id = source.Id,
				Message = source.Message,
				User = context.Mapper.Map<Models.Forum.User>(source.User),
				UrlTipHref = context.Mapper.Map<string>(source.UrlTips.Current()),
				Votes = context.Mapper.Map<IEnumerable<Models.Forum.Vote>>(source.Votes),
				ParentPost = source.ParentPost != null ? context.Mapper.Map<Models.Forum.ParentPost>(source.ParentPost) : null,
				LineupId = source.Lineups.Current()?.LineupId,
				Since = source.Created.GetSinceStamp(),
				Updated = source.Updated.GetSinceStamp(),
				IsScissored = source.Scissored != null,
				LoggedInUserCanVote = source.User.Id != _loggedInUserId && source.Votes.All(v => v.CreatedByUserId != _loggedInUserId)
			};
		}

		public Models.Forum.ParentPost Convert(Database.Entities.Forum.Post source, Models.Forum.ParentPost destination, ResolutionContext context)
		{
			var parent = source.ParentPost;
			return new Models.Forum.ParentPost
			{
				Id = source.Id,
				Message = source.Message,
				User = context.Mapper.Map<Models.Forum.User>(source.User),
				Parent = parent != null ? context.Mapper.Map<Models.Forum.ParentPost>(parent) : null,
				Since = source.Created.GetSinceStamp()
			};
		}

		public Models.Forum.Vote Convert(Database.Entities.Forum.Vote source, Models.Forum.Vote destination, ResolutionContext context)
		{
			return new Models.Forum.Vote
			{
				Type = source.Type,
				ByNickName = source.User.NickNames.Current() ?? "anonymous"
			};
		}

		public Database.Entities.Forum.Post Convert(Models.Forum.CRUD.Post source, Database.Entities.Forum.Post destination, ResolutionContext context)
		{
			return new Database.Entities.Forum.Post
			{
				Id = source.Id,
				Message = source.Message,
				ParentPostId = source.ParentPostId,
				CreatedByUserId = source.CreatedByUserId,
			};
		}

		public Database.Entities.Forum.Vote Convert(Models.Forum.CRUD.Vote source, Database.Entities.Forum.Vote destination, ResolutionContext context)
		{
			return new Database.Entities.Forum.Vote
			{
				Type = source.Type,
				PostId = source.PostId,
				CreatedByUserId = source.CreatedByUserId,
				Created = DateTime.Now
			};
		}

		public string Convert(Database.Entities.Forum.UrlTip source, string destination, ResolutionContext context)
		{
			if (string.IsNullOrEmpty(source?.Href))
			{
				return null;
			}
			if (!NoClicksForTheseSites.Any(source.Href.Contains))
			{
				return source.Href;
			}
			var urlStart = source.Href.StartsWith("http:") ? "http:" : "https:";
			return source.Href.Replace($"{urlStart}//www.", "https://unv.is/");
		}
	}
}
