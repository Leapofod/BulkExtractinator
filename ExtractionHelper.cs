using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace BulkExtractinator;

internal static class ExtractionHelper
{
	internal static bool ShouldInterceptNewItem { get; private set; }

	private static readonly Dictionary<int, int> _toDropItems;
	
	private static readonly Dictionary<int, int> _aquiredToInventory;
	private static readonly Dictionary<int, int> _aquiredToVoidVault;

	private static Item _justExtractedItem;

	static ExtractionHelper()
	{
		_toDropItems = new Dictionary<int, int>();
		_aquiredToInventory = new Dictionary<int, int>();
		_aquiredToVoidVault = new Dictionary<int, int>();
	}

	internal static bool MassExtract(Player player, bool limitToAvailableSpace)
	{
		ExtractinatorPlayer modPlr;
		if (!player.TryGetModPlayer(out modPlr) || modPlr.ExtractinatorSlotItem.IsAir)
			return false;

		ref var extractionFuel = ref modPlr.ExtractinatorSlotItem;
		bool successfulExtract = false;

		_justExtractedItem = modPlr.RemainderExtractedItem.Clone();
		modPlr.RemainderExtractedItem.TurnToAir();

		if (!_justExtractedItem.IsAir)
		{
			successfulExtract = InsertToPlayerContainer(player, ref _justExtractedItem);

			if (!limitToAvailableSpace)
			{
				// add to drop queue.
				//successfulExtract = true;
			}
		}

		var p_extractinatorUse = player.GetType().GetMethod("ExtractinatorUse",
			System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		while (!extractionFuel.IsAir && _justExtractedItem.IsAir)
		{
			ShouldInterceptNewItem = true;
			p_extractinatorUse.Invoke(player, new object[] { extractionFuel.type });
			ShouldInterceptNewItem = false;

			if (_justExtractedItem.IsAir)
			{
				extractionFuel.stack--;
				continue;
			}

			var didExtract = InsertToPlayerContainer(player, ref _justExtractedItem);

			if (!limitToAvailableSpace)
			{
				//didExtract = true;
				// add to drop queue.
			}

			if (didExtract)
			{
				extractionFuel.stack--;
				successfulExtract = true;
			}
		}

		modPlr.RemainderExtractedItem = _justExtractedItem.Clone();
		AnnounceAquiredItems(player);
		return successfulExtract;
	}

	internal static void OnNewItemIntercept(int Type, int Stack)
	{
		var extractedItem = new Item(Type, Stack);
		_justExtractedItem = extractedItem;
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
		if (itemToInsert.IsAir)
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
		spaceStatus = player.ItemSpace(itemToInsert);
		if (spaceStatus.ItemIsGoingToVoidVault)
		{
			var origItem = itemToInsert.Clone();
			inserted = InsertToInventory(player.bank4.item, ref itemToInsert, 0, 50) || inserted;
			AddItemAquiredToVoidVault(origItem.type, origItem.stack - itemToInsert.stack);
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
				itemToInsert.stack--;
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
		var queryInv = inv[startIndex..(int)MathHelper.Min(inv.Length, startIndex + slotCount)];
		for (int i = startIndex; i < queryInv.Length + startIndex; i++)
		{
			if (inv[i].IsAir || !inv[i].IsACoin || inv[i].type == ItemID.PlatinumCoin)
				continue;

			if (inv[i].stack == inv[i].maxStack)
			{
				inv[i].SetDefaults(inv[i].type + 1);
				for (int j = startIndex; j < queryInv.Length + startIndex; j++)
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

	internal static void EnqueueItem(int Type, int Stack)
	{
		if (_toDropItems.ContainsKey(Type))
			_toDropItems[Type] += Stack;
		else
			_toDropItems.Add(Type, Stack);
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

	// redo the system
}
