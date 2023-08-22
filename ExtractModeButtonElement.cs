using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace BulkExtractinator;

internal sealed class ExtractModeButtonElement : UIElement
{
	public override void OnInitialize()
	{
		OnLeftMouseDown += HandlePress;
		
		Width.Set(TextureAssets.HbLock[0].Value.Width / 2, 0);
		Height.Set(TextureAssets.HbLock[0].Value.Height, 0);
	}

	private void HandlePress(UIMouseEvent evt, UIElement listeningElement)
	{
		if (evt.Target != this)
			return;

		if (Main.LocalPlayer.TryGetModPlayer<ExtractinatorPlayer>(out var modPlayer))
		{
			modPlayer.LimitExtractToInventory = !modPlayer.LimitExtractToInventory;
			SoundEngine.PlaySound(in SoundID.MenuTick);
		}
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		ExtractinatorPlayer mdPlr;
		if (!ExtractinatorPlayer.ExtractinatorOpenLocally || !Main.LocalPlayer.TryGetModPlayer(out mdPlr))
			return;

		var tex = TextureAssets.HbLock[mdPlr.LimitExtractToInventory ? 0 : 1].Value;
		var msg = Language.GetTextValue($"Mods.BulkExtractinator.{(mdPlr.LimitExtractToInventory ? "LimitsToInventory" : "IgnoresInventory")}");

		spriteBatch.Draw(tex, GetViewCullingArea().Center(), tex.Frame(2), 
			Color.White, 0f, tex.Frame(2).Size() / 2f, 0.8f, SpriteEffects.None, 0f);

		if (IsMouseHovering)
		{
			Main.LocalPlayer.mouseInterface = true;
			Main.instance.MouseText(msg);
			Main.mouseText = true;

			spriteBatch.Draw(tex, GetViewCullingArea().Center(), tex.Frame(2, 1, 1), 
				Main.OurFavoriteColor, 0f, tex.Frame(2, 1, 1).Size() / 2f, 0.8f, SpriteEffects.None, 0f);
		}
	}
}
