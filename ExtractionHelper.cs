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
			if (!modPlr.RemainderExtractedItem.IsAir)
			{
				var remainderItem = modPlr.RemainderExtractedItem;
				if (TryInsertIntoInventory(remainderItem.type, remainderItem.stack, player)
					|| TryInsertIntoVoidVault(remainderItem.type, remainderItem.stack, player))
				{
					AddToAquiredTally(modPlr.RemainderExtractedItem);
					modPlr.RemainderExtractedItem.TurnToAir();
					extractionFuel.stack--;
					successfulExtract = true;
				}
				else if (!limitToAvailableSpace)
				{

				}
				else // can't insert into inventory and dropping isn't enabled so early return
					return false;
			}

			var p_extractinatorUse = player.GetType().GetMethod("ExtractinatorUse",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			while (!extractionFuel.IsAir)
			{
				ShouldInterceptNewItem = true;
				p_extractinatorUse.Invoke(player, new object[] { extractionFuel.type });
				ShouldInterceptNewItem = false;

				if (_justExtractedItem.IsAir)
				{
					extractionFuel.stack--;
					continue;
				}

				if (TryInsertIntoInventory(_justExtractedItem.type, _justExtractedItem.stack, player) 
					|| TryInsertIntoVoidVault(_justExtractedItem.type, _justExtractedItem.stack, player))
				{
					AddToAquiredTally(_justExtractedItem);
					successfulExtract = true;
				}
				else if (!limitToAvailableSpace)
				{
					
				}
				else
				{
					modPlr.RemainderExtractedItem = _justExtractedItem;
					break;
				}

				extractionFuel.stack--;
				_justExtractedItem.TurnToAir();
			}
			return successfulExtract;
		}
		return false;
	}

	internal static void OnNewItemIntercept(int Type, int Stack)
	{
		var extractedItem = new Item(Type, Stack);
		_justExtractedItem = extractedItem;
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
