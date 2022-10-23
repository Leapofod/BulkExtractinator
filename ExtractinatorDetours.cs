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

		IL.Terraria.GameContent.ObjectInteractions.TileSmartInteractCandidateProvider.FillPotentialTargetTiles += IL_FillPotentialTargetTiles;
	}

	private void IL_FillPotentialTargetTiles(ILContext il)
	{
		var c = new ILCursor(il);
		while (c.TryGotoNext(i => i.MatchBrfalse(out _)))
		{
			if (c.Prev.MatchCall(out _) && c.Prev.Previous.MatchLdarg(1))
			{
				// matches if (TileLoader.HasSmartInteract(i, j, tile.type, settings))
				// loads local tile variable into stack
				c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_2);
				// evaluate
				c.EmitDelegate<Func<Tile, bool>>((tile) =>
				{
					return BulkExtractinator.ExtractinatorTiles.Contains(tile.TileType);
				});
				// bitwise or with return of TileLoader.HasSmartInteract
				c.Emit(Mono.Cecil.Cil.OpCodes.Or);
				return;
			}
		}
		throw new Exception("Failed to IL Edit FillPotentialTargetTiles");
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
