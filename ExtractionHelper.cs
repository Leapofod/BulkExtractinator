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
				successfulExtract = InsertToInventory(player.inventory, ref _justExtractedItem, 0, _justExtractedItem.IsACoin ? 54 : 50)
					//| InsertToInventory(player.bank4.item, ref _justExtractedItem, 0, 50)
					;

				if (!limitToAvailableSpace)
				{
					// add to drop queue.
					successfulExtract = true;
				}
			}			
			//if (!modPlr.RemainderExtractedItem.IsAir)
			//{
			//	ref var remainderItem = ref modPlr.RemainderExtractedItem;	
				//var inserted = InsertToOneSlot(player.inventory, ref remainderItem, endIndex: remainderItem.IsACoin ? 54 : 50);

				//if (inserted)
				//{
				//	extractionFuel.stack--;
				//	successfulExtract = true;
				//	for (int i = 0; i < 54; i++)
				//		player.DoCoins(i);
				//}
				//else
				//	return false;

				//if (!remainderItem.IsAir)
				//	return successfulExtract;
				//if (TryInsertIntoInventory(remainderItem.type, remainderItem.stack, player)
				//	|| TryInsertIntoVoidVault(remainderItem.type, remainderItem.stack, player))
				//{
				//	AddToAquiredTally(modPlr.RemainderExtractedItem);
				//	modPlr.RemainderExtractedItem.TurnToAir();
				//	extractionFuel.stack--;
				//	successfulExtract = true;
				//}
				//else if (!limitToAvailableSpace)
				//{

				//}
				//else // can't insert into inventory and dropping isn't enabled so early return
				//	return false;
			//}

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

				var didExtract = 
					InsertToInventory(player.inventory, ref _justExtractedItem, 0, _justExtractedItem.IsACoin ? 54 : 50);

				// if has void bag:
				// result = insert to void bag

				if (!limitToAvailableSpace)
				{
					didExtract = true;
					// add to drop queue.
				}

				//if (TryInsertIntoInventory(_justExtractedItem.type, _justExtractedItem.stack, player) 
				//	|| TryInsertIntoVoidVault(_justExtractedItem.type, _justExtractedItem.stack, player))
				//{
				//	AddToAquiredTally(_justExtractedItem);
				//	successfulExtract = true;
				//}
				//else if (!limitToAvailableSpace)
				//{

				//}
				//else
				//{
				//	//modPlr.RemainderExtractedItem = _justExtractedItem;
				//	break;
				//}
				if (didExtract)
				{
					extractionFuel.stack--;
					successfulExtract = true;
				}
				//_justExtractedItem.TurnToAir();
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

	private static void SortCoinsInInv(Item[] inv, int slot, int startIndex, int slotCount)
	{
		if (inv[slot].IsAir || !inv[slot].IsACoin || inv[slot].stack < inv[slot].maxStack || inv[slot].type == ItemID.PlatinumCoin)
			return;

		inv[slot].SetDefaults(inv[slot].type + 1);
		var queryInv = inv[startIndex..(int)MathHelper.Min(inv.Length, startIndex + slotCount)];
		for (int i = startIndex; i < queryInv.Length + startIndex; i++)
		{
			if (inv[i].type == inv[slot].type && i != slot && inv[i].stack < inv[i].maxStack)
			{
				inv[i].stack++;
				inv[slot].TurnToAir();
				if (!inv[slot].IsAir && inv[slot].IsACoin && inv[slot].stack == inv[slot].maxStack && inv[slot].type != ItemID.PlatinumCoin)
				{
					slot = i;
					inv[slot].SetDefaults(inv[slot].type + 1);
					i = startIndex;
				}
			}
		}
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

	private static bool InsertToOneSlot(Item[] inv, ref Item itemToInsert, bool reverseFillEmpty = true, int startIndex = 0, int endIndex = 0)
	{
		if (itemToInsert.IsAir)
			return false;
		int slot;
		{
			int emptySlot = -1;
			int existingSlot = -1;
			for (int i = startIndex; i < MathHelper.Min(inv.Length, endIndex); i++)
			{
				if (!inv[i].IsAir && inv[i].type == itemToInsert.type && inv[i].stack < inv[i].maxStack)
				{
					existingSlot = i;
					break;
				}
				else if (inv[i].IsAir)
				{
					if (reverseFillEmpty || emptySlot == -1)
						emptySlot = i;
				}
			}
			if (existingSlot != -1)
				slot = existingSlot;
			else 
				slot = emptySlot;
		}
		if (slot == -1)
			return false;

		if (inv[slot].IsAir)
		{
			inv[slot].SetDefaults(itemToInsert.type);
			itemToInsert.stack--;
		}

		int slotCapacity = inv[slot].maxStack - inv[slot].stack;
		int amountToMove = (int)MathHelper.Min(slotCapacity, itemToInsert.stack);

		inv[slot].stack += amountToMove;
		itemToInsert.stack -= amountToMove;

		if (itemToInsert.stack <= 0)
			itemToInsert.TurnToAir();

		return true;
	}

	private static bool TryInsertCoin(int Type, int stack, Player player)
	{
		if (!ItemID.Sets.CommonCoin[Type])
			return false;
		int slot = FindExistingOrEmptyStack(player.inventory[0..54], Type, out var existing);
		if (slot != -1)
		{
			var inv = player.inventory;
			if ((!existing || inv[slot].maxStack - inv[slot].stack >= stack) && stack <= new Item(Type).maxStack)
			{
				if (!existing)
				{
					inv[slot].SetDefaults(Type);
					inv[slot].newAndShiny = true;
					stack--;
				}
				inv[slot].stack += stack;
				return true;
			}
			var origInv = new Item[inv.Length];
			for (int i = 0; i < inv.Length; i++)
				origInv[i] = inv[i].Clone();

			while (stack > 0)
			{
				if (!existing)
				{
					inv[slot].SetDefaults(Type);
					inv[slot].newAndShiny = true;
					stack--;
				}
				int remainingStack = inv[slot].maxStack - inv[slot].stack;
				int amountToMove = (int)MathHelper.Min(remainingStack, stack);
				inv[slot].stack += amountToMove;
				stack -= amountToMove;
				player.DoCoins(slot);
				if (stack > 0)
					slot = FindExistingOrEmptyStack(inv[0..54], Type, out existing);
				if (slot == -1)
					break;
			}
			if (stack > 0)
			{
				for (int i = 0; i < inv.Length; i++)
					inv[i] = origInv[i].Clone();
			}
			else
				return true;
		}
		return false;
	}

	private static bool TryInsertIntoInventory(int Type, int stack, Player player)
	{
		if (ItemID.Sets.CommonCoin[Type])
			return TryInsertCoin(Type, stack, player);

		var inv = player.inventory;
		int slot = FindExistingOrEmptyStack(inv[0..50], Type, out var existing);
		if (slot != -1)
		{
			if ((!existing || inv[slot].maxStack - inv[slot].stack >= stack) && stack <= new Item(Type).maxStack)
			{
				if (!existing)
				{
					inv[slot].SetDefaults(Type);
					inv[slot].newAndShiny = true;
					stack--;
				}

				inv[slot].stack += stack;
				return true;
			}


		}
		return false;
	}

	private static bool TryInsertIntoVoidVault(int Type, int Stack, Player player)
	{
		bool hasOpenVoidPouch = false;
		for (int i = 0; i < 50; i++)
		{
			if (player.inventory[i].type == ItemID.VoidVault)
			{
				hasOpenVoidPouch = true;
				break;
			}
		}
		if (!hasOpenVoidPouch)
			return false;


		return false;
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
