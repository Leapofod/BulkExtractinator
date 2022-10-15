using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace BulkExtractinator;

internal static class ExtractionHelper
{
	internal static bool ShouldInterceptNewItem { get; private set; }

	private static readonly Dictionary<int, int> _toDropItems;
	private static readonly Dictionary<int, int> _aquiredItems;
	
	private static Item _justExtractedItem;

	static ExtractionHelper()
	{
		_toDropItems = new Dictionary<int, int>();
		_aquiredItems = new Dictionary<int, int>();
	}

	internal static bool MassExtract(Player player, bool limitToAvailableSpace)
	{
		if (player.TryGetModPlayer<ExtractinatorPlayer>(out var modPlr) && !modPlr.ExtractinatorSlotItem.IsAir)
		{
			ref var extractionFuel = ref modPlr.ExtractinatorSlotItem;
			bool successfulExtract = false;

			_justExtractedItem = modPlr.RemainderExtractedItem.Clone();
			modPlr.RemainderExtractedItem.TurnToAir();

			if (!_justExtractedItem.IsAir)
			{
				var itemClone = _justExtractedItem.Clone();
				itemClone.position = player.Center;
				successfulExtract = InsertToInventory(player.inventory, ref _justExtractedItem, 0, _justExtractedItem.IsACoin ? 54 : 50);
					//| InsertToInventory(player.bank4.item, ref _justExtractedItem, 0, 50)
					
				PopupText.NewText(PopupTextContext.RegularItemPickup, itemClone, itemClone.stack - _justExtractedItem.stack, false, true);

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

				var extractedClone = _justExtractedItem.Clone();
				extractedClone.position = player.Center;
				var didExtract = 
					InsertToInventory(player.inventory, ref _justExtractedItem, 0, _justExtractedItem.IsACoin ? 54 : 50);

				PopupText.NewText(PopupTextContext.RegularItemPickup, extractedClone, extractedClone.stack - _justExtractedItem.stack, false, true);

				// if has void bag:
				// result = insert to void bag

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
			return successfulExtract;
		}
		return false;
	}

	internal static void OnNewItemIntercept(int Type, int Stack)
	{
		var extractedItem = new Item(Type, Stack);
		_justExtractedItem = extractedItem;
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

	private static void AddToAquiredTally(Item item) => AddToAquiredTally(item.type, item.stack);
	
	private static void AddToAquiredTally(int Type, int Stack) {
		if (!_aquiredItems.TryAdd(Type, Stack))
			_aquiredItems[Type] += Stack;
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
