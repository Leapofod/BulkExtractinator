using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace BulkExtractinator;

internal class ExtractionHelper
{
	internal static bool ShouldInterceptNewItem { get; private set; }

	private static readonly Dictionary<int, int> _toDropItems;

	private static readonly Dictionary<int, int> _aquiredToInventory;
	private static readonly Dictionary<int, int> _aquiredToVoidVault;

	private static List<Item> _justExtractedItems;

	static ExtractionHelper()
	{
		_toDropItems = new Dictionary<int, int>();
		_aquiredToInventory = new Dictionary<int, int>();
		_aquiredToVoidVault = new Dictionary<int, int>();

		_justExtractedItems = new List<Item>();
	}

	internal static bool CanConvertWithChlorophyte(Item item) => 
		ItemTrader.ChlorophyteExtractinator.TryGetTradeOption(item, out _);

	internal static bool MassConvert(Player player, bool limitToAvailableSpace)
	{
		if (!player.TryGetModPlayer(out ExtractinatorPlayer modPlr) || modPlr.ExtractinatorSlotItem.IsAir)
			return false;

		if (!CanConvertWithChlorophyte(modPlr.ExtractinatorSlotItem))
			return false;

		ref var convertionFuel = ref modPlr.ExtractinatorSlotItem;
		bool successfulConvert = false;

		if (!ItemTrader.ChlorophyteExtractinator.TryGetTradeOption(convertionFuel, out var tradeOpt))
			return false;

		for (bool didConvert = true; didConvert && !convertionFuel.IsAir;)
		{
			didConvert = false;
			if (convertionFuel.stack >= tradeOpt.TakingItemStack)
			{
				var newItem = new Item(tradeOpt.GivingITemType, tradeOpt.GivingItemStack);
				didConvert |= InsertToPlayerContainer(player, ref newItem);

				if (!limitToAvailableSpace)
				{
					EnqueueItem(ref newItem);
					didConvert = true;
				}
			}

			if (didConvert)
			{
				convertionFuel.stack -= tradeOpt.TakingItemStack;
				successfulConvert = true;
			}
		}

		AnnounceAquiredItems(player);
		DropQueuedItems(player);
		return successfulConvert;
	}

	// TOOD: You can recycle fishing trash into bait, implement later
	internal static bool MassExtract(Player player, bool limitToAvailableSpace)
	{
		if (!player.TryGetModPlayer(out ExtractinatorPlayer modPlr) || modPlr.ExtractinatorSlotItem.IsAir)
			return false;

		int extractType = ItemID.Sets.ExtractinatorMode[modPlr.ExtractinatorSlotItem.type];
		if (extractType < 0)
			return false;

		ref var extractionFuel = ref modPlr.ExtractinatorSlotItem;
		bool successfulExtract = false;

		if (modPlr.ExtractinatorBacklog.ContainsKey(extractType))
		{
			modPlr.ExtractinatorBacklog.Remove(extractType, out _justExtractedItems);
			for (int i = 0; i < _justExtractedItems.Count; i++)
			{
				var backlogItem = _justExtractedItems[i];
				successfulExtract |= InsertToPlayerContainer(player, ref backlogItem);
				if (!limitToAvailableSpace)
				{
					EnqueueItem(ref backlogItem);
					successfulExtract = true;
				}
				_justExtractedItems[i] = backlogItem;
			}
			_justExtractedItems.RemoveAll(i => i.IsAir);
		}

		// 	private void ExtractinatorUse(int extractType, int extractinatorBlockType)
		var p_extractinatorUse = player.GetType().GetMethod("ExtractinatorUse",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

		var extractinatorTileID = modPlr.OpenExtractinatorType == ExtractinatorType.Normal ?
			TileID.Extractinator : TileID.ChlorophyteExtractinator;

		while (!extractionFuel.IsAir && _justExtractedItems.Count == 0)
		{
			ShouldInterceptNewItem = true;
			p_extractinatorUse.Invoke(player, new object[] { extractType, extractinatorTileID });
			ShouldInterceptNewItem = false;

			if (_justExtractedItems.TrueForAll(i => i.IsAir))
			{
				// Debug message, behaves incorrectly if another mod has 'air' as a possible drop
				// Main.NewText("Whoa, look, the function runs! and not the correct one too!");
				extractionFuel.stack--;
				continue;
			}

			bool didExtract = false;
			for (int i = 0; i < _justExtractedItems.Count; i++)
			{
				var newItem = _justExtractedItems[i];
				didExtract |= InsertToPlayerContainer(player, ref newItem);

				if (!limitToAvailableSpace)
				{
					EnqueueItem(ref newItem);
					didExtract = true;
				}
				_justExtractedItems[i] = newItem;
			}
			_justExtractedItems.RemoveAll(i => i.IsAir);

			if (didExtract)
			{
				extractionFuel.stack--;
				successfulExtract = true;
			}
		}

		modPlr.ExtractinatorBacklog[extractType] = _justExtractedItems;
		AnnounceAquiredItems(player);
		DropQueuedItems(player);
		return successfulExtract;
	}

	internal static void OnNewItemIntercept(int Type, int Stack)
	{
		var extractedItem = new Item(Type, Stack);
		_justExtractedItems.Add(extractedItem);
	}


	private static void AnnounceAquiredItems(Player player)
	{
		foreach (var invItem in _aquiredToInventory)
		{
			var item = new Item(invItem.Key, invItem.Value);
			item.position = player.Center;
			PopupText.NewText(PopupTextContext.RegularItemPickup, item, item.stack, !item.IsACoin, true);
		}
		foreach (var voidItem in _aquiredToVoidVault)
		{
			var item = new Item(voidItem.Key, voidItem.Value);
			item.position = player.Center;
			PopupText.NewText(PopupTextContext.ItemPickupToVoidContainer, item, item.stack, !item.IsACoin, true);
		}

		_aquiredToInventory.Clear();
		_aquiredToVoidVault.Clear();
	}

	private static bool InsertToPlayerContainer(Player player, ref Item itemToInsert)
	{
		if (itemToInsert.IsAir || !player.CanAcceptItemIntoInventory(itemToInsert))
			return false;
		var spaceStatus = player.ItemSpace(itemToInsert);
		bool inserted = false;
		if (spaceStatus.CanTakeItemToPersonalInventory)
		{
			var origItem = itemToInsert.Clone();
			inserted = InsertToInventory(player.inventory, ref itemToInsert, 0, itemToInsert.IsACoin ? 54 : 50);
			if (itemToInsert.ammo > 0 && !itemToInsert.notAmmo)
				inserted = InsertToInventory(player.inventory, ref itemToInsert, 54, 4) || inserted;
			AddItemAquiredToInventory(origItem.type, origItem.stack - itemToInsert.stack);
		}
		if (!itemToInsert.IsAir)
		{
			spaceStatus = player.ItemSpace(itemToInsert);
			if (spaceStatus.ItemIsGoingToVoidVault)
			{
				var origItem = itemToInsert.Clone();
				inserted = InsertToInventory(player.bank4.item, ref itemToInsert, 0, 50) || inserted;
				AddItemAquiredToVoidVault(origItem.type, origItem.stack - itemToInsert.stack);
			}
		}

		return inserted;
	}

	private static bool InsertToInventory(Item[] inv, ref Item itemToInsert, int startIndex = 0, int slotCount = 1)
	{
		if (itemToInsert.IsAir)
			return false;
		bool handleCoin = itemToInsert.IsACoin;
		var queryInv = inv[startIndex..(int)MathHelper.Min(inv.Length, startIndex + slotCount)];

		bool successfulInsert = false;
		int slot = FindExistingOrEmptyStack(queryInv, itemToInsert.type, out _) + startIndex;

		while (slot >= startIndex)
		{
			successfulInsert = true;
			if (inv[slot].IsAir)
			{
				inv[slot].SetDefaults(itemToInsert.type);
				inv[slot].newAndShiny = true;
				inv[slot].stack = 0;
			}
			int slotCapLeft = inv[slot].maxStack - inv[slot].stack;
			int amountToMove = (int)MathHelper.Min(slotCapLeft, itemToInsert.stack);

			inv[slot].stack += amountToMove;
			itemToInsert.stack -= amountToMove;

			if (handleCoin)
				SortCoinsInInv(inv, startIndex, slotCount);

			if (!itemToInsert.IsAir)
				slot = FindExistingOrEmptyStack(queryInv, itemToInsert.type, out _) + startIndex;
			else
				break;
		}
		if (itemToInsert.IsAir)
			itemToInsert.TurnToAir();
		return successfulInsert;
	}

	private static void SortCoinsInInv(Item[] inv, int startIndex, int slotCount)
	{
		//var queryInv = inv[startIndex..(int)MathHelper.Min(inv.Length, startIndex + slotCount)];
		var queryEnd = (int)MathHelper.Min(inv.Length, startIndex + slotCount);
		for (int i = startIndex; i < queryEnd; i++)
		{
			if (inv[i].IsAir || !inv[i].IsACoin || inv[i].type == ItemID.PlatinumCoin)
				continue;

			if (inv[i].stack == inv[i].maxStack)
			{
				inv[i].SetDefaults(inv[i].type + 1);
				for (int j = startIndex; j < queryEnd; j++)
				{
					if (j != i && inv[j].type == inv[i].type && !inv[j].IsAir && inv[j].stack < inv[j].maxStack)
					{
						inv[j].stack++;
						inv[i].TurnToAir();
					}
				}
			}
		}
	}

	private static void AddItemAquiredToInventory(int type, int stack)
	{
		if (type == 0 || stack == 0)
			return;
		if (_aquiredToInventory.ContainsKey(type))
			_aquiredToInventory[type] += stack;
		else
			_aquiredToInventory.Add(type, stack);
	}

	private static void AddItemAquiredToVoidVault(int type, int stack)
	{
		if (type == 0 || stack == 0)
			return;
		if (_aquiredToVoidVault.ContainsKey(type))
			_aquiredToVoidVault[type] += stack;
		else
			_aquiredToVoidVault.Add(type, stack);
	}

	internal static void DropQueuedItems(Player player)
	{
		var dropPosition = player.Center;
		var openExtractinatorPos = dropPosition.ToPoint16();
		if (player.TryGetModPlayer<ExtractinatorPlayer>(out var mPlr))
			openExtractinatorPos = mPlr.CurrentOpenExtractinator.Center.ToVector2().ToPoint16();
		foreach (var itemType in _toDropItems.Keys)
		{
			var item = new Item(itemType, _toDropItems[itemType]);
			while (item.stack > 0)
			{
				int stackToDrop = (int)MathHelper.Min(item.maxStack, item.stack);
				item.stack -= stackToDrop;
				var newItem = Item.NewItem(player.GetSource_TileInteraction(openExtractinatorPos.X, openExtractinatorPos.Y),
					dropPosition, Vector2.Zero, itemType, stackToDrop, false, -1, true);
				if (Main.netMode == NetmodeID.MultiplayerClient)
					NetMessage.SendData(MessageID.SyncItem, -1, -1, null, newItem, 1f);
			}
		}
		_toDropItems.Clear();
	}

	internal static void EnqueueItem(ref Item item)
	{
		if (item.IsAir)
			return;
		if (_toDropItems.ContainsKey(item.type))
			_toDropItems[item.type] += item.stack;
		else
			_toDropItems.Add(item.type, item.stack);

		item.TurnToAir();
	}

	private static int FindExistingOrEmptyStack(Item[] inventory, int type, out bool existingStack)
	{
		int lastEmptySlot = -1;
		int firstExistingStack = -1;
		existingStack = false;
		for (int i = 0; i < inventory.Length; i++)
		{
			if (!inventory[i].IsAir && inventory[i].type == type && inventory[i].stack < inventory[i].maxStack)
			{
				firstExistingStack = i;
				existingStack = true;
				break;
			}
			else if (inventory[i].IsAir)
				lastEmptySlot = i;
		}

		if (existingStack)
			return firstExistingStack;
		else
			return lastEmptySlot;
	}
}
