﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Diffen.Database.Clients
{
	using Contracts;
	using Entities.User;
	using Entities.Forum;
	using Entities.Squad;
	using Helpers.Extensions;

	public class DiffenDbClient : IDiffenDbClient
	{
		private readonly DiffenDbContext _dbContext;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public DiffenDbClient(DiffenDbContext dbContext, IHttpContextAccessor httpContextAccessor)
		{
			_dbContext = dbContext;
			_httpContextAccessor = httpContextAccessor;
		}

		public Task<int> CountPostsAsync()
		{
			return _dbContext.Posts.CountAsync();
		}

		public Task<List<Post>> GetPostsAsync()
		{
			return _dbContext.Posts.IncludeAll().ToListAsync();
		}

		public Task<List<Post>> GetPagedPostsAsync(int pageNumber, int pageSize)
		{
			return _dbContext.Posts.IncludeAll().ExceptScissored()
				.OrderByCreated().Skip(pageSize * (pageNumber - 1)).Take(pageSize).ToListAsync();
		}

		public Task<List<Post>> GetPostsOnFilterAsync(Models.Forum.Filter filter)
		{
			var posts = _dbContext.Posts.IncludeAll().ExceptScissored();
			if (filter == null)
			{
				return posts.ToListAsync();
			}
			if (filter.ExcludedUsers != null && filter.ExcludedUsers.Any())
			{
				posts = posts.Where(x => !filter.ExcludedUsers.Select(y => y.Key).Contains(x.CreatedByUserId));
			}
			if (filter.FromDate != null)
			{
				posts = posts.Where(p => p.Created.Date >= Convert.ToDateTime(filter.FromDate).Date);
			}
			if (filter.ToDate != null)
			{
				posts = posts.Where(p => p.Created.Date <= Convert.ToDateTime(filter.ToDate).Date);
			}
			switch (filter.StartingEleven)
			{
				case Models.Forum.StartingEleven.With:
					posts = posts.Where(x => x.Lineup != null);
					break;
				case Models.Forum.StartingEleven.Without:
					posts = posts.Where(x => x.Lineup == null);
					break;
				case Models.Forum.StartingEleven.All:
					break;
			}
			if (filter.IncludedUsers != null && filter.IncludedUsers.Any())
			{
				posts = posts.Where(x => filter.IncludedUsers.Select(y => y.Key).Contains(x.CreatedByUserId));
			}
			return posts.ToListAsync();
		}

		public Task<List<Post>> GetPostsOnUserIdAsync(string userId)
		{
			return _dbContext.Posts.IncludeAll().ExceptScissored()
				.Where(post => post.CreatedByUserId == userId).OrderByCreated().ToListAsync();
		}

		public async Task<List<Post>> GetSavedPostsOnUserIdAsync(string userId)
		{
			var savedPosts = await _dbContext.SavedPosts.IncludeAll().ToListAsync();
			return savedPosts.Where(post => post.SavedByUserId == userId).Select(x => x.Post).ToList();
		}

		public Task<Post> GetPostOnIdAsync(int postId)
		{
			return _dbContext.Posts.IncludeAll().FirstOrDefaultAsync(post => post.Id == postId);
		}

		public Task<List<UrlTip>> GetUrlTipsAsync()
		{
			return _dbContext.UrlTips.Include(x => x.Post).ToListAsync();
		}

		public Task<UrlTip> GetUrlTipOnIdAsync(int tipId)
		{
			return _dbContext.UrlTips.FindAsync(tipId);
		}

		public Task<List<Vote>> GetVotesAsync()
		{
			return _dbContext.Votes.OrderByDescending(x => x.Created).ToListAsync();
		}

		public Task<List<Vote>> GetVotesOnPostIdAsync(int postId)
		{
			return _dbContext.Votes.Include(x => x.User).ThenInclude(x => x.NickNames)
				.Where(x => x.PostId == postId).OrderByDescending(x => x.Created).ToListAsync();
		}

		public Task<List<Vote>> GetVotesOnUserIdAsync(string userId)
		{
			return _dbContext.Votes.Where(x => x.CreatedByUserId == userId).OrderByDescending(x => x.Created).ToListAsync();
		}

		public Task<bool> CreatePostAsync(Post post)
		{
			_dbContext.Posts.Add(post);
			return CommitedResultIsSuccessfulAsync();
		}

		public async Task<bool> UpdatePostAsync(Post post)
		{
			_dbContext.Posts.Update(post);
			_dbContext.Entry(post).State = EntityState.Modified;
			_dbContext.Entry(post).Property(x => x.Created).IsModified = false;
			var result = await CommitedResultIsSuccessfulAsync();
			_dbContext.Entry(post).State = EntityState.Detached;
			return result;
		}

		public Task<bool> DeletePostAsync(int postId)
		{
			var entity = _dbContext.Posts.Find(postId);

			if (entity == null)
				return Task.FromResult(false);

			_dbContext.Posts.Remove(entity);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> ScissorPostAsync(Scissored scissoredPost)
		{
			_dbContext.ScissoredPosts.Add(scissoredPost);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> SavePostForUserAsync(SavedPost savedPost)
		{
			_dbContext.SavedPosts.Add(savedPost);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> ConnectLineupToPostAsync(PostToLineup postToLineup)
		{
			_dbContext.LineupsOnPosts.Add(postToLineup);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> DeleteLineupConnectionToPostAsync(int postId)
		{
			var entity = _dbContext.LineupsOnPosts.FirstOrDefault(e => e.PostId == postId);

			if (entity == null)
				return Task.FromResult(false);

			_dbContext.LineupsOnPosts.Remove(entity);
			return CommitedResultIsSuccessfulAsync();
		}

		public async Task<bool> PostHasALineupConnectedToItAsync(int postId)
		{
			return await _dbContext.LineupsOnPosts.CountAsync(x => x.PostId == postId) > 0;
		}

		public async Task<bool> PostHasAnUrlTipConnectedToItAsync(int postId)
		{
			return await _dbContext.UrlTips.CountAsync(x => x.PostId == postId) > 0;
		}

		public Task<bool> CreateUrlTipAsync(UrlTip urlTip)
		{
			_dbContext.UrlTips.Add(urlTip);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> UpdateUrlTipAsync(UrlTip urlTip)
		{
			_dbContext.UrlTips.Update(urlTip);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> DeleteUrlTipAsync(int postId)
		{
			var entity = _dbContext.UrlTips.FirstOrDefault(e => e.PostId == postId);

			if (entity == null)
				return Task.FromResult(false);

			_dbContext.UrlTips.Remove(entity);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> IncrementUrlTipClickCounterAsync(int postId)
		{
			var tip = _dbContext.UrlTips.FirstOrDefault(t => t.PostId == postId);

			if (tip == null)
				return Task.FromResult(false);

			tip.Clicks++;
			_dbContext.UrlTips.Update(tip);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> CreateVoteAsync(Vote vote)
		{
			_dbContext.Votes.Add(vote);
			return CommitedResultIsSuccessfulAsync();
		}

		public async Task<bool> UserHasAlreadyVotedOnPostAsync(int postId, string userId)
		{
			return await _dbContext.Votes.CountAsync(x => x.PostId == postId && x.CreatedByUserId == userId) > 0;
		}

		public Task<List<AppUser>> GetUsersExceptForLoggedInUserAsync()
		{
			return _dbContext.Users
				.Include(x => x.NickNames)
				.Where(x => x.UserName != _httpContextAccessor.HttpContext.User.Identity.Name)
				.OrderByDescending(x => x.Joined)
				.ToListAsync();
		}

		public Task<AppUser> GetUserOnIdAsync(string userId)
		{
			return _dbContext.Users.IncludeAll().FirstOrDefaultAsync(user => user.Id == userId);
		}

		public Task<AppUser> GetUserOnEmailAsync(string userEmail)
		{
			return _dbContext.Users.IncludeAll().FirstOrDefaultAsync(user => user.Email == userEmail);
		}

		public Task<FavoritePlayer> GetFavoritePlayerOnUserIdAsync(string userId)
		{
			return _dbContext.FavoritePlayers.Include(x => x.Player).FirstOrDefaultAsync(x => x.UserId == userId);
		}

		public Task<string> GetCurrentNickNameOnUserIdAsync(string userId)
		{
			return _dbContext.NickNames.Where(x => x.UserId == userId).OrderByDescending(x => x.Created).Select(x => x.Nick).FirstOrDefaultAsync();
		}

		public Task<Filter> GetBaseFilterForForumOnUserIdAsync(string userId)
		{
			return _dbContext.UserFilters.FirstOrDefaultAsync(u => u.UserId == userId);
		}

		public Task<List<Invite>> GetInvitesAsync()
		{
			return _dbContext.Invites.Include(x => x.InvitedByUser).ThenInclude(x => x.NickNames).OrderByDescending(x => x.InviteSent).ToListAsync();
		}

		public Task<Invite> GetInviteOnUserEmailAsync(string userEmail)
		{
			return _dbContext.Invites.FirstOrDefaultAsync(x => x.Email.Equals(userEmail));
		}

		public Task<List<PersonalMessage>> GetPmsSentFromUserToUserAsync(string fromUserId, string toUserId)
		{
			return _dbContext.PersonalMessages.IncludeAll()
				.Where(x => (x.FromUserId == fromUserId || x.FromUserId == toUserId) && (x.ToUserId == toUserId || x.ToUserId == fromUserId))
				.OrderByDescending(x => x.Created).ToListAsync();
		}

		public async Task<List<AppUser>> GetUsersThatUserHasOngoingConversationWithAsync(string userId)
		{
			var all = await _dbContext.PersonalMessages.IncludeAll()
				.Where(x => x.FromUserId == userId || x.ToUserId == userId).ToListAsync();
			var from = all.Select(x => x.FromUser);
			var to = all.Select(x => x.ToUser);

			var users = from.Distinct().Union(to.Distinct()).ToList();
			users.RemoveAll(x => x.Id == userId);
			return users;
		}

		public Task<bool> CreatePersonalMessageAsync(PersonalMessage personalMessage)
		{
			_dbContext.PersonalMessages.Add(personalMessage);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> UpdateUserAsync(AppUser user)
		{
			_dbContext.Users.Update(user);
			return CommitedResultIsSuccessfulAsync();
		}

		public async Task<bool> UpdateUserBioAsync(string userId, string newBio)
		{
			var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
			user.Bio = newBio;
			_dbContext.Users.Update(user);
			return await CommitedResultIsSuccessfulAsync();
		}

		public async Task<bool> UserHasAFavoritePlayerSelectedAsync(string userId)
		{
			return await _dbContext.FavoritePlayers.CountAsync(x => x.UserId == userId) > 0;
		}

		public Task<bool> ConnectFavoritePlayerToUserAsync(FavoritePlayer favoritePlayer)
		{
			_dbContext.FavoritePlayers.Add(favoritePlayer);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> DeleteFavoritePlayerConnectionToUserAsync(string userId)
		{
			var entity = _dbContext.FavoritePlayers.FirstOrDefault(e => e.UserId == userId);

			if (entity == null)
				return Task.FromResult(false);

			_dbContext.FavoritePlayers.Remove(entity);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> CreateNewNickNameForUserAsync(NickName nickName)
		{
			_dbContext.NickNames.Add(nickName);
			return CommitedResultIsSuccessfulAsync();
		}

		public async Task<bool> NickNameIsAlreadyTakenByOtherUserAsync(string nickName)
		{
			var activeNicks = _dbContext.NickNames.OrderByDescending(x => x.Created).GroupBy(x => x.UserId).Select(x => x.FirstOrDefault().Nick);
			return await activeNicks.CountAsync(x => x == nickName) > 0;
		}

		public async Task<bool> SetSelectedAvatarFileNameForUserAsync(string userId, string fileName)
		{
			var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
			user.AvatarFileName = fileName;
			_dbContext.Users.Update(user);
			return await _dbContext.SaveChangesAsync() >= 0;
		}

		public Task<bool> CreateBaseFilterForForumOnUserAsync(Filter filter)
		{
			_dbContext.UserFilters.Add(filter);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> UpdateBaseFilterForForumOnUserAsync(Filter filter)
		{
			_dbContext.UserFilters.Update(filter);
			return CommitedResultIsSuccessfulAsync();
		}

		public async Task<bool> AnActiveInviteExistsOnSelectedEmailAsync(string userEmail)
		{
			return await _dbContext.Invites.CountAsync(x => x.Email.Equals(userEmail)) > 0;
		}

		public Task<bool> CreateInviteAsync(Invite invite)
		{
			_dbContext.Invites.Add(invite);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> UpdateInviteAsync(Invite invite)
		{
			_dbContext.Invites.Update(invite);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> DeleteInviteAsync(int inviteId)
		{
			var entity = _dbContext.Invites.Find(inviteId);

			if (entity == null)
				return Task.FromResult(false);

			_dbContext.Invites.Remove(entity);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<Lineup> GetLineupOnIdAsync(int lineupId)
		{
			return _dbContext.Lineups.IncludeAll().FirstOrDefaultAsync(l => l.Id == lineupId);
		}

		public Task<Lineup> GetLineupOnPostIdAsync(int postId)
		{
			var lineupToPost = _dbContext.LineupsOnPosts.FirstOrDefault(x => x.PostId == postId);
			return _dbContext.Lineups.IncludeAll().FirstOrDefaultAsync(l => l.Id == lineupToPost.LineupId);
		}

		public Task<List<Lineup>> GetLineupsCreatedByUserIdAsync(string userId)
		{
			return _dbContext.Lineups.IncludeAll().Where(l => l.CreatedByUserId == userId).ToListAsync();
		}

		public Task<List<Player>> GetPlayersAsync()
		{
			return _dbContext.Players.Include(x => x.AvailablePositions).ThenInclude(x => x.Position).Include(x => x.InLineups).Where(x => !x.IsSold).OrderBy(x => x.LastName).ToListAsync();
		}

		public Task<Player> GetPlayerOnIdAsync(int playerId)
		{
			return _dbContext.Players.Include(x => x.AvailablePositions).ThenInclude(x => x.Position).FirstOrDefaultAsync(x => x.Id == playerId);
		}

		public Task<List<Position>> GetPositionsAsync()
		{
			return _dbContext.Positions.ToListAsync();
		}

		public Task<List<Formation>> GetFormationsAsync()
		{
			return _dbContext.Formations.ToListAsync();
		}

		public Task<bool> CreateLineupAsync(Lineup lineup)
		{
			_dbContext.Lineups.Add(lineup);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> CreatePlayerAsync(Player player)
		{
			_dbContext.Players.Add(player);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> UpdatePlayerAsync(Player player)
		{
			_dbContext.Players.Update(player);
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> DeleteFavoritePlayerRelationToUserForPlayerAsync(int playerId)
		{
			_dbContext.FavoritePlayers.RemoveRange(_dbContext.FavoritePlayers.Where(x => x.PlayerId == playerId));
			return CommitedResultIsSuccessfulAsync();
		}

		public Task<bool> UpdateAvailablePositionsForPlayerAsync(int playerId, IEnumerable<int> positionIds)
		{
			var relations = _dbContext.PlayersToPositions.Where(x => x.PlayerId == playerId).ToList();
			if (relations.Any())
			{
				foreach (var relation in relations)
				{
					_dbContext.PlayersToPositions.Remove(relation);
				}
			}
			foreach (var positionId in positionIds)
			{
				_dbContext.PlayersToPositions.Add(new PlayerToPosition
				{
					PlayerId = playerId,
					PositionId = positionId
				});
			}
			return CommitedResultIsSuccessfulAsync();
		}

		private async Task<bool> CommitedResultIsSuccessfulAsync()
		{
			return await _dbContext.SaveChangesAsync() >= 0;
		}
	}
}