﻿using System;
using System.Linq;
using System.Collections.Generic;

using AutoMapper;

namespace Diffen.Helpers.Mapper.Resolvers
{
	using Business;

	public class SquadResolver : 
		ITypeConverter<Database.Entities.Squad.Player, Models.Squad.Player>, 
		ITypeConverter<Database.Entities.Squad.Lineup, Models.Squad.Lineup>,
		ITypeConverter<Database.Entities.Squad.PlayerToLineup, Models.Squad.PlayerToLineup>,
		ITypeConverter<Models.Squad.CRUD.Lineup, Database.Entities.Squad.Lineup>, 
		ITypeConverter<Database.Entities.User.FavoritePlayer, Models.Squad.Player>, 
		ITypeConverter<Models.Squad.CRUD.Player, Database.Entities.Squad.Player>,
		ITypeConverter<Database.Entities.Squad.Game, Models.Squad.Game>,
		ITypeConverter<Database.Entities.Squad.PlayerEvent, Models.Squad.PlayerEvent>,
		ITypeConverter<Models.Squad.CRUD.Game, Database.Entities.Squad.Game>,
		ITypeConverter<Database.Entities.Squad.Title, Models.Squad.Title>
	{
		public Models.Squad.Player Convert(Database.Entities.Squad.Player source, Models.Squad.Player destination, ResolutionContext context)
		{
			return new Models.Squad.Player
			{
				Id = source.Id,
				FirstName = source.FirstName,
				LastName = source.LastName,
				KitNumber = source.KitNumber,
				IsOutOnLoan = source.IsOutOnLoan,
				IsHereOnLoan = source.IsHereOnLoan,
				IsCaptain = source.IsCaptain,
				IsSold = source.IsSold,
				AvailablePositions = source.AvailablePositions.Select(x => new Models.Squad.Position
				{
					Id = x.Position.Id,
					Name = x.Position.Name
				}),
				InNumberOfStartingElevens = source.InLineups.Count
			};
		}

		public Models.Squad.Lineup Convert(Database.Entities.Squad.Lineup source, Models.Squad.Lineup destination, ResolutionContext context)
		{
			return new Models.Squad.Lineup
			{
				Id = source.Id,
				Formation = new Models.Squad.Formation
				{
					Id = source.FormationId,
					Name = source.Formation.Name,
					ComponentName = FormationList.All().FirstOrDefault(f => f.Name == source.Formation.Name)?.ComponentName
				},
				Players = context.Mapper.Map<List<Models.Squad.PlayerToLineup>>(source.Players),
				Created = source.Created.ToString("yyyy-MM-dd")
			};
		}

		public Models.Squad.PlayerToLineup Convert(Database.Entities.Squad.PlayerToLineup source, Models.Squad.PlayerToLineup destination, ResolutionContext context)
		{
			return new Models.Squad.PlayerToLineup
			{
				Id = source.Id,
				Player = context.Mapper.Map<Models.Squad.Player>(source.Player),
				Position = new Models.Squad.Position
				{
					Id = source.Position.Id,
					Name = source.Position.Name
				}
			};
		}

		public Database.Entities.Squad.Lineup Convert(Models.Squad.CRUD.Lineup source, Database.Entities.Squad.Lineup destination, ResolutionContext context)
		{
			return new Database.Entities.Squad.Lineup
			{
				Id = source.Id,
				FormationId = source.FormationId,
				Players = source.Players.Select(player => new Database.Entities.Squad.PlayerToLineup
				{
					PlayerId = player.PlayerId,
					PositionId = player.PositionId
				}).ToList(),
				CreatedByUserId = source.CreatedByUserId,
				Created = DateTime.Now
			};
		}

		public Models.Squad.Player Convert(Database.Entities.User.FavoritePlayer source, Models.Squad.Player destination, ResolutionContext context)
		{
			return new Models.Squad.Player
			{
				Id = source.PlayerId,
				FirstName = source.Player.FirstName,
				LastName = source.Player.LastName,
				KitNumber = source.Player.KitNumber,
				IsOutOnLoan = source.Player.IsOutOnLoan,
				IsHereOnLoan = source.Player.IsHereOnLoan,
				IsCaptain = source.Player.IsCaptain,
				IsSold = source.Player.IsSold
			};
		}

		public Database.Entities.Squad.Player Convert(Models.Squad.CRUD.Player source, Database.Entities.Squad.Player destination, ResolutionContext context)
		{
			return new Database.Entities.Squad.Player
			{
				Id = source.Id,
				FirstName = source.FirstName,
				LastName = source.LastName,
				IsCaptain = source.IsCaptain,
				IsOutOnLoan = source.IsOutOnLoan,
				IsHereOnLoan = source.IsHereOnLoan,
				KitNumber = source.KitNumber,
				IsSold = source.IsSold
			};
		}

		public Models.Squad.Game Convert(Database.Entities.Squad.Game source, Models.Squad.Game destination, ResolutionContext context)
		{
			return new Models.Squad.Game
			{
				Id = source.Id,
				Type = source.Type,
				PlayedOn = source.OnDate.ToString("yyyy-MM-dd"),
				PlayerEvents = context.Mapper.Map<IEnumerable<Models.Squad.PlayerEvent>>(source.PlayerEvents)
			};
		}

		public Models.Squad.PlayerEvent Convert(Database.Entities.Squad.PlayerEvent source, Models.Squad.PlayerEvent destination, ResolutionContext context)
		{
			return new Models.Squad.PlayerEvent
			{
				Id = source.Id,
				Player = context.Mapper.Map<Models.Squad.Player>(source.Player),
				EventType = source.Type
			};
		}

		public Database.Entities.Squad.Game Convert(Models.Squad.CRUD.Game source, Database.Entities.Squad.Game destination, ResolutionContext context)
		{
			return new Database.Entities.Squad.Game
			{
				Id = source.Id,
				Type = source.Type,
				OnDate = source.PlayedDate
			};
		}

		public Models.Squad.Title Convert(Database.Entities.Squad.Title source, Models.Squad.Title destination,
			ResolutionContext context)
		{
			return new Models.Squad.Title
			{
				Id = source.Id,
				Type = source.Type,
				Year = source.Year,
				Description = source.Description
			};
		}
	}
}
