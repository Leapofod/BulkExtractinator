using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace BulkExtractinator;

internal sealed class ExtractinatorDetours : ModSystem
{
	public override void Load()
	{
		On_Item.NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool += (orig, source, X, Y, Width, Height, Type, Stack, noBroadcast, pfix, noGrabDelay, reverseLookup) =>
		{
			if (ExtractionHelper.ShouldInterceptNewItem)
			{
				ExtractionHelper.OnNewItemIntercept(Type, Stack);
				return 0;
			}
			return orig(source, X, Y, Width, Height, Type, Stack, noBroadcast, pfix, noGrabDelay, reverseLookup);
		};

		On_NetMessage.SendData += (orig, msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7) =>
		{ if (!ExtractionHelper.ShouldInterceptNewItem) orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7); };

		Terraria.GameContent.Creative.On_CreativeUI.ToggleMenu += (orig, self) =>
		{ if (!ExtractinatorPlayer.ExtractinatorOpenLocally) orig(self); };

		
		
		Terraria.GameContent.ObjectInteractions.IL_TileSmartInteractCandidateProvider.FillPotentialTargetTiles += (il) =>
		{
			var c = new ILCursor(il);
			while (c.TryGotoNext(i => i.MatchBrfalse(out _)))
			{
				if (c.Prev.MatchCall(out _) && c.Prev.Previous.MatchLdarg(1))
				{
					// matches if (TileLoader.HasSmartInteract(i, j, tile.type, settings))
					// loads local tile variable into stack
					c.Emit(OpCodes.Ldloc_2);
					// evaluate
					c.EmitDelegate<Func<Tile, bool>>((tile) =>
					{
						return BulkExtractinator.ExtractinatorTiles.Contains(tile.TileType);
					});
					// bitwise or with return of TileLoader.HasSmartInteract
					c.Emit(OpCodes.Or);
					return;
				}
			}
			throw new Exception("Failed to IL Edit FillPotentialTargetTiles");
		};

		Terraria.GameContent.ObjectInteractions.IL_TileSmartInteractCandidateProvider.ProvideCandidate += il =>
		{
			var c = new ILCursor(il);

			int widthIndex = -1, heightIndex = -1, frameWidthIndex = -1, frameHeightIndex = -1, extraYIndex = -1;
			int tileIndex = -1;
			Mono.Cecil.MethodReference getTypeFunc = null;

			/*
			 * Jumps to
			 *	TileLoader.ModifySmartInteractCoords(tile.type, ref num7, ref num8, ref num9, ref num10, ref num11);
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
				x.Previous.Previous.Previous.Previous.Previous.Previous.Previous.MatchCall(out getTypeFunc)&&
				x.Previous.Previous.Previous.Previous.Previous.Previous.Previous.Previous.MatchLdloca(out tileIndex)))
			{
				Mod.Logger.Error("TileSmartInteractCandidateProvider::ProvideCandidate");
				return;
			}

			var skipLabel = c.DefineLabel();
			c.Emit(OpCodes.Ldloca_S, (byte)tileIndex);
			c.Emit(OpCodes.Call, getTypeFunc);
			c.Emit(OpCodes.Ldind_U2);

			c.EmitDelegate<Func<int, bool>>(type =>
			{
				ProvideCandidateHelper.CurrentTileType = type;
				return BulkExtractinator.ExtractinatorTiles?.Contains(type) ?? false;
			});
			c.Emit(OpCodes.Brfalse, skipLabel);

			c.EmitDelegate(() =>
			{
				ProvideCandidateHelper.CurrentObjectData = Terraria.ObjectData.TileObjectData.GetTileData(ProvideCandidateHelper.CurrentTileType, 0);
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
			 *	if (BulkExtractinator.ExtractinatorTiles.Contains(type))
			 *	{
			 *		var data = Terraria.ObjectData.TileObjectData.GetTileData(type, 0);
			 *		if (data != null)
			 *		{	
			 *			width = data.Width;
			 *			height = data.Height;
			 *			frameWidth = data.CoordinateWidth + data.CoordinatePadding;
			 *			frameHeight = data.CoordinateHeights[0] + data.CoordinatePadding;
			 *			extraY = data.CoordinateFullHeight % frameHeight;
			 *		}
			 *	}
			 */
		};
	}	
}
