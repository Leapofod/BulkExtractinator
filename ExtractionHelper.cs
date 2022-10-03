using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;

namespace BulkExtractinator;

internal static class ExtractionHelper
{
	internal static bool ShouldInterceptNewItem { get; private set; }

	private static readonly Dictionary<int, int> _pendingItems;

	static ExtractionHelper()
	{
		_pendingItems = new Dictionary<int, int>();
	}

	internal static void EnqueueItem(int Type, int Stack)
	{
		if (_pendingItems.ContainsKey(Type))
			_pendingItems[Type] += Stack;
		else
			_pendingItems.Add(Type, Stack);
	}

	internal static void BulkExtractinate(int count, int extractType, Player player, Vector2 extractinatorPosition)
	{


		SpawnQueuedItems(player, extractinatorPosition);
	}


	private static void SpawnQueuedItems(Player player, Vector2 sourcePosition)
	{
		foreach (var key in _pendingItems.Keys)
		{
			var itemIdx = Item.NewItem(
				player.GetSource_TileInteraction((int)sourcePosition.X, (int)sourcePosition.Y), 
				player.Center, 1, 1, key, _pendingItems[key], false, -1);
			// syncing here maybe
		}
	}

}
