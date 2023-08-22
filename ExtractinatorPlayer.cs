using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BulkExtractinator;

internal sealed class ExtractinatorPlayer : ModPlayer
{
	public static bool GetLocalModPlayer(out ExtractinatorPlayer result) =>
		Main.LocalPlayer.TryGetModPlayer(out result);

	public bool HasExtractinatorOpen { get; private set; }
	public static bool ExtractinatorOpenLocally => GetLocalModPlayer(out var mdPlr) && mdPlr.HasExtractinatorOpen;

	/// <summary>
	/// Coordinates and size of currently opened extractinator.
	/// </summary>
	public Rectangle CurrentOpenExtractinator;
	public ExtractinatorType OpenExtractinatorType;

	// Might change to allow multiple item stacks in the extractinator at a time
	public Item ExtractinatorSlotItem;
	// List of item stacks based on item ID
	public readonly Dictionary<int, List<Item>> ExtractinatorBacklog;


	public bool LimitExtractToInventory;

	public ExtractinatorPlayer()
	{
		ExtractinatorSlotItem = new Item();
		ExtractinatorBacklog = new Dictionary<int, List<Item>>();

		LimitExtractToInventory = false;
	}

	public override void PreUpdate()
	{
		// Autoclose extractinator when out of range or
		// if the stored location doesn't actually have an extractinator
		if (HasExtractinatorOpen)
		{
			var playerCenterTile = Player.Center.ToTileCoordinates16();
			var currExtractorX = CurrentOpenExtractinator.X;
			var currExtractorY = CurrentOpenExtractinator.Y;
			var currOpenTileType = Main.tile[currExtractorX, currExtractorY].TileType;
			if (playerCenterTile.X < currExtractorX - Player.tileRangeX
				|| playerCenterTile.X > currExtractorX + CurrentOpenExtractinator.Width + Player.tileRangeX
				|| playerCenterTile.Y < currExtractorY - Player.tileRangeY
				|| playerCenterTile.Y > currExtractorY + CurrentOpenExtractinator.Height + Player.tileRangeY
				|| (!BulkExtractinator.ExtractinatorTiles.Contains(currOpenTileType)
				&& !BulkExtractinator.ChloroExtractinatorTiles.Contains(currOpenTileType)))
			{
				CloseExtractinator();
			}
		}

		// If cursor is hovering an extractinator, show extractinator icon
		if (BulkExtractinator.ExtractinatorTiles
			.Contains(Main.tile[Player.tileTargetX, Player.tileTargetY].TileType))
		{
			Player.noThrow = 2;
			Player.cursorItemIconEnabled = true;
			Player.cursorItemIconID = ItemID.Extractinator;
		}
		else if (BulkExtractinator.ChloroExtractinatorTiles
			.Contains(Main.tile[Player.tileTargetX, Player.tileTargetY].TileType))
		{
			Player.noThrow = 2;
			Player.cursorItemIconEnabled = true;
			Player.cursorItemIconID = ItemID.ChlorophyteExtractinator;
		}
	}

	public override void PostUpdate()
	{
		// Open extractinator when:
		// player closes inventory / talks to an NPC /
		// has an NPC shop open / has a chest open
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

		if (LimitExtractToInventory)
			tag["LimitExtractToInventory"] = true;
	}

	public override void LoadData(TagCompound tag)
	{
		if (tag.TryGet<Item>("LastExtractinatorItem", out var lastExtractinatorItem) && !lastExtractinatorItem.IsAir)
			ExtractinatorSlotItem = lastExtractinatorItem;

		if (tag.TryGet<bool>("LimitExtractToInventory", out var limitToInventory))
			LimitExtractToInventory = limitToInventory;
	}

	public override void OnEnterWorld() { DropExtractinatorItem(); }

	public override bool HoverSlot(Item[] inventory, int context, int slot)
	{
		if (ExtractinatorOpenLocally &&
			(context == ItemSlot.Context.InventoryItem
			|| context == ItemSlot.Context.HotbarItem))
		{
			if ((ItemSlot.NotUsingGamepad && ItemSlot.Options.DisableLeftShiftTrashCan && !ItemSlot.ShiftForcedOn
				&& ItemSlot.ShiftInUse) || ItemSlot.ShiftInUse)
			{
				// Shift click item into extractinator (cursor icon)
				if (!inventory[slot].IsAir && !inventory[slot].favorited
					&& (ItemID.Sets.ExtractinatorMode[inventory[slot].type] >= 0
						|| (OpenExtractinatorType == ExtractinatorType.Chlorophyte
						&& ExtractionHelper.CanConvertWithChlorophyte(inventory[slot]))))
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
		if (ExtractinatorOpenLocally
			&& (context == ItemSlot.Context.InventoryItem || context == ItemSlot.Context.HotbarItem)
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
		if (!HasExtractinatorOpen)
			SoundEngine.PlaySound(in SoundID.MenuOpen);
		else
			SoundEngine.PlaySound(in SoundID.MenuTick);

		HasExtractinatorOpen = true;
		Main.CreativeMenu.CloseMenu();
		Main.SetNPCShopIndex(0);
		Player.SetTalkNPC(-1);
		Player.chest = -1;
		Main.playerInventory = true;
	}

	public void CloseExtractinator(bool silent = false)
	{
		HasExtractinatorOpen = false;
		DropExtractinatorItem();
		if (!silent)
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

		// Sync item drop on servers
		if (Main.netMode == NetmodeID.MultiplayerClient)
			NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemDropID);

		ExtractinatorSlotItem.TurnToAir();
	}
}
