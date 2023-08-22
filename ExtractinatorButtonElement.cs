using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace BulkExtractinator;

internal sealed class ExtractinatorButtonElement : UIElement
{
	private static bool ShowButton => ExtractinatorPlayer.ExtractinatorOpenLocally 
		&& !Main.LocalPlayer.GetModPlayer<ExtractinatorPlayer>().ExtractinatorSlotItem.IsAir;

	public override void OnInitialize()
	{
		OnLeftMouseDown += HandlePress;

		Width.Set(TextureAssets.Reforge[0].Value.Width + 16f, 0f);
		Height.Set(TextureAssets.Reforge[0].Value.Height + 16f, 0f);
	}

	private void HandlePress(UIMouseEvent evt, UIElement listeningElement)
	{
		if (evt.Target != this)
			return;

		bool limitToInventory = ExtractinatorPlayer.GetLocalModPlayer(out var mPlr) && mPlr.LimitExtractToInventory;

		var res = ExtractionHelper.MassConvert(Main.LocalPlayer, limitToInventory) ||
			ExtractionHelper.MassExtract(Main.LocalPlayer, limitToInventory);

		if (res)
			SoundEngine.PlaySound(in SoundID.Grab);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (!ShowButton)
			return;

		var tex = TextureAssets.Reforge[IsMouseHovering ? 1 : 0].Value;
		spriteBatch.Draw(tex, GetViewCullingArea().Center(), null, 
			Color.White, 0f, tex.Size() / 2f, 1f, SpriteEffects.None, 0f);

		if (IsMouseHovering)
			Main.LocalPlayer.mouseInterface = true;
	}
}
