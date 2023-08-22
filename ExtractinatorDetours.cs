using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ModLoader;

namespace BulkExtractinator;

internal sealed class ExtractinatorDetours : ModSystem
{
	public override void Load()
	{
		// To deal with vanilla ExtractinatorUse function and allow intercepting items
		On_Player.DropItemFromExtractinator += (orig, self, type, stack) =>
		{
			if (ExtractionHelper.ShouldInterceptNewItem)
				ExtractionHelper.OnNewItemIntercept(type, stack);
			else
				orig(self, type, stack);
		};

		On_NetMessage.SendData += (orig, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7) =>
		{ if (!ExtractionHelper.ShouldInterceptNewItem) orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7); };


		// Disable Journey mode menus while using extractinator
		On_CreativeUI.ToggleMenu += (orig, self) =>
		{ if (!ExtractinatorPlayer.ExtractinatorOpenLocally) orig(self); };

		On_CreativeUI.DrawToggleButton += (orig, self, sb, loc) =>
		{ if (!ExtractinatorPlayer.ExtractinatorOpenLocally) orig(self, sb, loc); };



		// IL edits for enabling smart cursor picking for extractinators
		IL_TileSmartInteractCandidateProvider.FillPotentialTargetTiles += (il) =>
		{
			var c = new ILCursor(il);
			while (c.TryGotoNext(i => i.MatchBrfalse(out _)))
			{
				if (c.Prev.MatchCall(out _) && c.Prev.Previous.MatchLdarg(1))
				{
					// matches `if (TileLoader.HasSmartInteract(i, j, tile.type, settings))`
					// loads local tile variable into stack
					c.Emit(OpCodes.Ldloc_2);
					// evaluate additional conditions
					c.EmitDelegate<Func<Tile, bool>>((tile) =>
					{
						return BulkExtractinator.ExtractinatorTiles.Contains(tile.TileType)
							|| BulkExtractinator.ChloroExtractinatorTiles.Contains(tile.TileType);
					});
					// bitwise or with return of TileLoader.HasSmartInteract
					c.Emit(OpCodes.Or);
					return;
				}
			}
			throw new Exception("Failed to IL Edit FillPotentialTargetTiles");
		};

		IL_TileSmartInteractCandidateProvider.ProvideCandidate += il =>
		{
			var c = new ILCursor(il);

			int widthIndex = -1, heightIndex = -1, frameWidthIndex = -1, frameHeightIndex = -1, extraYIndex = -1;
			int tileIndex = -1;
			Mono.Cecil.MethodReference getTypeFunc = null;

			/*
			 * Jumps to
			 *	TileLoader.ModifySmartInteractCoords(tile.type, ref num7, ref num8, ref num9, ref num10, ref num11);
			 *		num7 = width, num8 = height, num9 = frameWidth, num10 = frameHeight, num11 = extraY;
			 * and gets all argument indeces
			 */
			if (!c.TryGotoNext(MoveType.After,
				x => x.MatchCall(out _) &&
				x.Previous.MatchLdloca(out extraYIndex) &&
				x.Previous.Previous.MatchLdloca(out frameHeightIndex) &&
				x.Previous.Previous.Previous.MatchLdloca(out frameWidthIndex) &&
				x.Previous.Previous.Previous.Previous.MatchLdloca(out heightIndex) &&
				x.Previous.Previous.Previous.Previous.Previous.MatchLdloca(out widthIndex) &&
				x.Previous.Previous.Previous.Previous.Previous.Previous.MatchLdindU2() &&
				x.Previous.Previous.Previous.Previous.Previous.Previous.Previous.MatchCall(out getTypeFunc) &&
				x.Previous.Previous.Previous.Previous.Previous.Previous.Previous.Previous.MatchLdloca(out tileIndex)))
			{
				Mod.Logger.Error("TileSmartInteractCandidateProvider::ProvideCandidate");
				return;
			}

			var skipLabel = c.DefineLabel();
			c.Emit(OpCodes.Ldloca_S, (byte)tileIndex);
			c.Emit(OpCodes.Call, getTypeFunc); // Get tile type
			c.Emit(OpCodes.Ldind_U2); // convert uint16 to int32

			c.EmitDelegate<Func<int, bool>>(type =>
			{
				ProvideCandidateHelper.CurrentTileType = type;
				return (BulkExtractinator.ExtractinatorTiles?.Contains(type) ?? false) ||
					(BulkExtractinator.ChloroExtractinatorTiles?.Contains(type) ?? false);
			});
			c.Emit(OpCodes.Brfalse, skipLabel);

			c.EmitDelegate(() =>
			{
				ProvideCandidateHelper.CurrentObjectData =
					Terraria.ObjectData.TileObjectData.GetTileData
						(ProvideCandidateHelper.CurrentTileType, 0);
				return ProvideCandidateHelper.CurrentObjectData is null;
			});
			c.Emit(OpCodes.Brtrue, skipLabel);

			c.EmitDelegate(() => ProvideCandidateHelper.CurrentObjectWidth);
			c.EmitDelegate(() => ProvideCandidateHelper.CurrentObjectHeight);
			c.EmitDelegate(() => ProvideCandidateHelper.CurrentObjectFrameWidth);
			c.EmitDelegate(() => ProvideCandidateHelper.CurrentObjectFrameHeight);
			c.EmitDelegate(() => ProvideCandidateHelper.CurrentObjectExtraY);
			c.Emit(OpCodes.Stloc_S, (byte)extraYIndex);
			c.Emit(OpCodes.Stloc_S, (byte)frameHeightIndex);
			c.Emit(OpCodes.Stloc_S, (byte)frameWidthIndex);
			c.Emit(OpCodes.Stloc_S, (byte)heightIndex);
			c.Emit(OpCodes.Stloc_S, (byte)widthIndex);

			c.MarkLabel(skipLabel);

			/* Basically Adds
			 *	[...]
			 *	TileLoader.ModifySmartInteractCoords(tile.type, ref num7, ref num8, ref num9, ref num10, ref num11);
			 
			 +	if (BulkExtractinator.ExtractinatorTiles.Contains(type) || 
			 +		BulkExtractinator.ChloroExtractinatorTiles.Contains(type))
			 +	{
			 +		var data = Terraria.ObjectData.TileObjectData.GetTileData(type, 0);
			 +		if (data != null)
			 +		{	
			 +			width = data.Width;
			 +			height = data.Height;
			 +			frameWidth = data.CoordinateWidth + data.CoordinatePadding;
			 +			frameHeight = data.CoordinateHeights[0] + data.CoordinatePadding;
			 +			extraY = data.CoordinateFullHeight % frameHeight;
			 +		}
			 +	}
			 
			 *	if (num7 == 0 || num8 == 0)
			 *	continue;
			 *	[...]
			 */
		};
	}
}
