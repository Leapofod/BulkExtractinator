using MonoMod.Cil;
using System;
using Terraria;
using Terraria.ModLoader;

namespace BulkExtractinator;

internal sealed class ExtractinatorDetours : ModSystem
{
	public override void Load()
	{
		On.Terraria.Item.NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool += BlockItem_NewItem;
		On.Terraria.NetMessage.SendData += BlockNetMessage_SendData;

		On.Terraria.GameContent.Creative.CreativeUI.Draw += BlockCreativeUI_Draw;
		On.Terraria.GameContent.Creative.CreativeUI.ToggleMenu += BlockCreativeUI_ToggleMenu;

		//On.Terraria.GameContent.ObjectInteractions.TileSmartInteractCandidateProvider.FillPotentialTargetTiles += CallOrigOnFillTileCandidates;
		//ExtractinatorHooks.On_HasSmartInteract += TileLoader_HasSmartInteract;

		IL.Terraria.GameContent.ObjectInteractions.TileSmartInteractCandidateProvider.FillPotentialTargetTiles += IL_FillPotentialTargetTiles;
		On.Terraria.GameContent.Drawing.TileDrawing.GetTileOutlineInfo += CustomGetTileOutlineInfo;
	}

	private void IL_FillPotentialTargetTiles(ILContext il)
	{
		var c = new ILCursor(il);
		while (c.TryGotoNext(i => i.MatchBrfalse(out _)))
		{
			if (c.Prev.MatchCall(out _) && c.Prev.Previous.MatchLdarg(1))
			{
				//c.Remove();
				//c.Index++;
				c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_2);
				c.EmitDelegate<Func<Tile, bool>>((tile) =>
				{
					return BulkExtractinator.ExtractinatorTiles.Contains(tile.TileType);
				});
				c.Emit(Mono.Cecil.Cil.OpCodes.Or);
				//c.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(BulkExtractinator).GetProperty(nameof(BulkExtractinator.ExtractinatorTiles)).GetGetMethod());
				//c.Emit(Mono.Cecil.Cil.OpCodes.Ldloca_S, 2);
				//c.Emit(Mono.Cecil.Cil.OpCodes.Call, typeof(Tile));
				return;
			}
		}
		throw new Exception(c.Index.ToString());
	}

	// this is so bodged, fix by finding out how to get Asset<Texture2D> values from a Texture2D value
	private void CustomGetTileOutlineInfo(On.Terraria.GameContent.Drawing.TileDrawing.orig_GetTileOutlineInfo orig, Terraria.GameContent.Drawing.TileDrawing self, int x, int y, ushort typeCache, ref Microsoft.Xna.Framework.Color tileLight, ref Microsoft.Xna.Framework.Graphics.Texture2D highlightTexture, ref Microsoft.Xna.Framework.Color highlightColor)
	{
		orig(self, x, y, typeCache, ref tileLight, ref highlightTexture, ref highlightColor);
		if (BulkExtractinator.ExtractinatorTiles.Contains(typeCache))
		{
			if (Main.InSmartCursorHighlightArea(x, y, out bool actuallySelected))
			{
				int num = (tileLight.R + tileLight.G | tileLight.B) / 3;
				if (num > 10)
				{
					highlightTexture = TileOutlineHelper.HighlightTextures[typeCache];
					highlightColor = Terraria.ID.Colors.GetSelectionGlowColor(actuallySelected, num);
				}
			}
		}
	}

	// Without this the custom hook isn't called, and I don't know why.
	private void CallOrigOnFillTileCandidates(On.Terraria.GameContent.ObjectInteractions.TileSmartInteractCandidateProvider.orig_FillPotentialTargetTiles orig, Terraria.GameContent.ObjectInteractions.TileSmartInteractCandidateProvider self, Terraria.GameContent.ObjectInteractions.SmartInteractScanSettings settings)
	{
		orig(self, settings);
	}

	private static bool TileLoader_HasSmartInteract(ExtractinatorHooks.Orig_HasSmartInteract orig, int i, int j, int type, Terraria.GameContent.ObjectInteractions.SmartInteractScanSettings settings)
	{
		if (BulkExtractinator.ExtractinatorTiles.Contains(type))
			return true;
		return orig(i, j, type, settings);
	}

	private void BlockCreativeUI_ToggleMenu(On.Terraria.GameContent.Creative.CreativeUI.orig_ToggleMenu orig, Terraria.GameContent.Creative.CreativeUI self)
	{
		if (ExtractinatorPlayer.ExtractinatorOpenLocally)
			return;
		orig(self);
	}

	private void BlockCreativeUI_Draw(On.Terraria.GameContent.Creative.CreativeUI.orig_Draw orig, Terraria.GameContent.Creative.CreativeUI self, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
	{
		if (ExtractinatorPlayer.ExtractinatorOpenLocally)
			return;
		orig(self, spriteBatch);
	}

	private void BlockNetMessage_SendData(On.Terraria.NetMessage.orig_SendData orig, int msgType, int remoteClient, int ignoreClient, Terraria.Localization.NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
	{
		if (ExtractionHelper.ShouldInterceptNewItem)
			return;
		orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
	}

	private int BlockItem_NewItem(On.Terraria.Item.orig_NewItem_IEntitySource_int_int_int_int_int_int_bool_int_bool_bool orig, Terraria.DataStructures.IEntitySource source, int X, int Y, int Width, int Height, int Type, int Stack, bool noBroadcast, int pfix, bool noGrabDelay, bool reverseLookup)
	{
		if (ExtractionHelper.ShouldInterceptNewItem)
		{
			ExtractionHelper.OnNewItemIntercept(Type, Stack);
			return 0;
		}
		return orig(source, X, Y, Width, Height, Type, Stack, noBroadcast, pfix, noGrabDelay, reverseLookup);
	}
}
