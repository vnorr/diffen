﻿using System.Threading.Tasks;
using System.Collections.Generic;

namespace Diffen.Database.Clients.Contracts
{
	using Entities.User;
	using Entities.Forum;
	using Entities.Squad;

	public interface IDiffenDbClient
	{
		// Post Related Requests
		Task<int> CountPostsAsync();
		Task<List<Post>> GetPostsAsync();
		Task<List<Post>> GetPagedPostsAsync(int pageNumber, int pageSize);
		Task<List<Post>> GetPostsOnFilterAsync(Models.Forum.Filter filter);
		Task<List<Post>> GetPostsOnUserIdAsync(string userId);
		Task<List<Post>> GetSavedPostsOnUserIdAsync(string userId);
		Task<Post> GetPostOnIdAsync(int postId);
		Task<List<UrlTip>> GetUrlTipsAsync();
		Task<UrlTip> GetUrlTipOnIdAsync(int tipId);
		Task<List<Vote>> GetVotesAsync();
		Task<List<Vote>> GetVotesOnPostIdAsync(int postId);
		Task<List<Vote>> GetVotesOnUserIdAsync(string userId);
		Task<bool> CreatePostAsync(Post post);
		Task<bool> UpdatePostAsync(Post post);
		Task<bool> DeletePostAsync(int postId);
		Task<bool> ScissorPostAsync(Scissored scissoredPost);
		Task<bool> SavePostForUserAsync(SavedPost savedPost);
		Task<bool> ConnectLineupToPostAsync(PostToLineup postToLineup);
		Task<bool> DeleteLineupConnectionToPostAsync(int postId);
		Task<bool> PostHasALineupConnectedToItAsync(int postId);
		Task<bool> PostHasAnUrlTipConnectedToItAsync(int postId);
		Task<bool> CreateUrlTipAsync(UrlTip urlTip);
		Task<bool> UpdateUrlTipAsync(UrlTip urlTip);
		Task<bool> DeleteUrlTipAsync(int postId);
		Task<bool> IncrementUrlTipClickCounterAsync(int postId);
		Task<bool> CreateVoteAsync(Vote vote);
		Task<bool> UserHasAlreadyVotedOnPostAsync(int postId, string userId);

		// User Related Requests
		Task<List<AppUser>> GetUsersExceptForLoggedInUserAsync();
		Task<AppUser> GetUserOnIdAsync(string userId);
		Task<AppUser> GetUserOnEmailAsync(string userEmail);
		Task<FavoritePlayer> GetFavoritePlayerOnUserIdAsync(string userId);
		Task<string> GetCurrentNickNameOnUserIdAsync(string userId);
		Task<Filter> GetBaseFilterForForumOnUserIdAsync(string userId);
		Task<List<Invite>> GetInvitesAsync();
		Task<Invite> GetInviteOnUserEmailAsync(string userEmail);
		Task<List<PersonalMessage>> GetPmsSentFromUserToUserAsync(string fromUserId, string toUserId);
		Task<List<AppUser>> GetUsersThatUserHasOngoingConversationWithAsync(string userId);
		Task<bool> CreatePersonalMessageAsync(PersonalMessage personalMessage);
		Task<bool> UpdateUserAsync(AppUser user);
		Task<bool> UpdateUserBioAsync(string userId, string newBio);
		Task<bool> UserHasAFavoritePlayerSelectedAsync(string userId);
		Task<bool> ConnectFavoritePlayerToUserAsync(FavoritePlayer favoritePlayer);
		Task<bool> DeleteFavoritePlayerConnectionToUserAsync(string userId);
		Task<bool> CreateNewNickNameForUserAsync(NickName nickName);
		Task<bool> NickNameIsAlreadyTakenByOtherUserAsync(string nickName);
		Task<bool> SetSelectedAvatarFileNameForUserAsync(string userId, string fileName);
		Task<bool> CreateBaseFilterForForumOnUserAsync(Filter filter);
		Task<bool> UpdateBaseFilterForForumOnUserAsync(Filter filter);
		Task<bool> AnActiveInviteExistsOnSelectedEmailAsync(string userEmail);
		Task<bool> CreateInviteAsync(Invite invite);
		Task<bool> UpdateInviteAsync(Invite invite);
		Task<bool> DeleteInviteAsync(int inviteId);

		// Squad Related Requests
		Task<Lineup> GetLineupOnIdAsync(int lineupId);
		Task<Lineup> GetLineupOnPostIdAsync(int postId);
		Task<List<Lineup>> GetLineupsCreatedByUserIdAsync(string userId);
		Task<List<Player>> GetPlayersAsync();
		Task<Player> GetPlayerOnIdAsync(int playerId);
		Task<List<Position>> GetPositionsAsync();
		Task<List<Formation>> GetFormationsAsync();
		Task<bool> CreateLineupAsync(Lineup lineup);
		Task<bool> CreatePlayerAsync(Player player);
		Task<bool> UpdatePlayerAsync(Player player);
		Task<bool> DeleteFavoritePlayerRelationToUserForPlayerAsync(int playerId);
		Task<bool> UpdateAvailablePositionsForPlayerAsync(int playerId, IEnumerable<int> positionIds);
	}
}