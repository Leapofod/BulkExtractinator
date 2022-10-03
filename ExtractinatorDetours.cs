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
	}

	private void BlockCreativeUI_Draw(On.Terraria.GameContent.Creative.CreativeUI.orig_Draw orig, Terraria.GameContent.Creative.CreativeUI self, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
	{
		if (ExtractinatorUI.IsUIOpen && !Main.CreativeMenu.Enabled)
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
			ExtractionHelper.EnqueueItem(Type, Stack);
			return 0;
		}
		return orig(source, X, Y, Width, Height, Type, Stack, noBroadcast, pfix, noGrabDelay, reverseLookup);
	}
}
