using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BulkExtractinator;

internal sealed class ExtractinatorPlayer : ModPlayer
{
	public bool HasExtractinatorOpen { get; private set; }
	public static bool ExtractinatorOpenLocally => Main.LocalPlayer.TryGetModPlayer<ExtractinatorPlayer>(out var mdPlr) && mdPlr.HasExtractinatorOpen;

	public Rectangle CurrentOpenExtractinator;

	public Item ExtractinatorSlotItem;
	public Item RemainderExtractedItem;

	public ExtractinatorPlayer()
	{
		ExtractinatorSlotItem = new Item();
		RemainderExtractedItem = new Item();
	}

	public override void PreUpdate()
	{
		if (HasExtractinatorOpen)
		{
			var playerCenterTile = Player.Center / 16;
			if (playerCenterTile.X < CurrentOpenExtractinator.X - Player.tileRangeX
				|| playerCenterTile.X > CurrentOpenExtractinator.X + CurrentOpenExtractinator.Width + 
				Player.tileRangeX
				|| playerCenterTile.Y < CurrentOpenExtractinator.Y - Player.tileRangeY
				|| playerCenterTile.Y > CurrentOpenExtractinator.Y + CurrentOpenExtractinator.Height +
				Player.tileRangeY
				|| !BulkExtractinator.ExtractinatorTiles.Contains(
					Main.tile[CurrentOpenExtractinator.X, CurrentOpenExtractinator.Y].TileType))
			{
				CloseExtractinator();
			}
		}
		
		if (BulkExtractinator.ExtractinatorTiles
			.Contains(Main.tile[Player.tileTargetX, Player.tileTargetY].TileType))
		{
			Player.noThrow = 2;
			Player.cursorItemIconEnabled = true;
			Player.cursorItemIconID = ItemID.Extractinator;
		}
	}

	public override void PostUpdate()
	{
		if (HasExtractinatorOpen && !(Main.playerInventory && Main.npcShop == 0 && Main.LocalPlayer.talkNPC == -1
			&& Main.LocalPlayer.chest == -1))
		{
			CloseExtractinator(true);
		}
	}

	public override void SaveData(TagCompound tag)
	{
		if (!ExtractinatorSlotItem.IsAir)
			tag["LastExtractinatorItem"] = ExtractinatorSlotItem;

		if (!RemainderExtractedItem.IsAir)
			tag["RemainderExtractedItem"] = RemainderExtractedItem;
	}

	public override void LoadData(TagCompound tag)
	{
		if (tag.TryGet<Item>("LastExtractinatorItem", out var lastExtractinatorItem) && !lastExtractinatorItem.IsAir)
			ExtractinatorSlotItem = lastExtractinatorItem;

		if (tag.TryGet<Item>("RemainderExtractedItem", out var remainderItem) && !remainderItem.IsAir)
			RemainderExtractedItem = remainderItem;

	}

	public override void OnEnterWorld(Player player)
	{
		DropExtractinatorItem();
	}

	public override bool HoverSlot(Item[] inventory, int context, int slot)
	{
		if(ExtractinatorOpenLocally && context == ItemSlot.Context.InventoryItem)
		{
			if ((ItemSlot.NotUsingGamepad && ItemSlot.Options.DisableLeftShiftTrashCan && !ItemSlot.ShiftForcedOn
				&& ItemSlot.ShiftInUse) || ItemSlot.ShiftInUse)
			{
				if (!inventory[slot].IsAir && !inventory[slot].favorited && 
					ItemID.Sets.ExtractinatorMode[inventory[slot].type] >= 0)
				{
					Main.cursorOverride = CursorOverrideID.InventoryToChest;
					return true;
				}
			}
		}
		return false;
	}

	public override bool ShiftClickSlot(Item[] inventory, int context, int slot)
	{
		if (ExtractinatorOpenLocally && context == ItemSlot.Context.InventoryItem
			&& Main.cursorOverride == CursorOverrideID.InventoryToChest)
		{
			Utils.Swap(ref inventory[slot], ref ExtractinatorSlotItem);
			SoundEngine.PlaySound(in SoundID.Grab);
			return true;
		}
		return false;
	}

	public void OpenExtractinator()
	{
		HasExtractinatorOpen = true;
		Main.CreativeMenu.CloseMenu();
		Main.SetNPCShopIndex(0);
		Player.SetTalkNPC(-1);
		Player.chest = -1;
		Main.playerInventory = true;
		SoundEngine.PlaySound(in SoundID.MenuOpen);
	}

	public void CloseExtractinator(bool silent = false)
	{
		HasExtractinatorOpen = false;
		DropExtractinatorItem();
		if (silent)
			SoundEngine.PlaySound(in SoundID.MenuClose);
	}

	private void DropExtractinatorItem()
	{
		if (ExtractinatorSlotItem.IsAir)
			return;

		ExtractinatorSlotItem.position = Player.Center;
		var itemDropID = Item.NewItem(Player.GetSource_DropAsItem(), Player.Center,
			ExtractinatorSlotItem.type, ExtractinatorSlotItem.stack, false, 
			ExtractinatorSlotItem.prefix, true);
		Main.item[itemDropID].netDefaults(ExtractinatorSlotItem.netID);
		Main.item[itemDropID] = ExtractinatorSlotItem.Clone();
		Main.item[itemDropID].newAndShiny = false;

		if (Main.netMode == NetmodeID.MultiplayerClient)
			NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemDropID);

		ExtractinatorSlotItem.TurnToAir();
	}
}
