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
	private bool ShowButton => ExtractinatorPlayer.ExtractinatorOpenLocally 
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

		bool limitToInventory = Main.LocalPlayer.TryGetModPlayer<ExtractinatorPlayer>(out var mPlr) && mPlr.LimitExtractToInventory;

		var res = ExtractionHelper.MassExtract(Main.LocalPlayer, limitToInventory);
		if (res)
			SoundEngine.PlaySound(in SoundID.Grab);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		if (!ShowButton)
			return;

		var tex = TextureAssets.Reforge[0].Value;
		if (IsMouseHovering)
			tex = TextureAssets.Reforge[1].Value;

		spriteBatch.Draw(tex, GetViewCullingArea().Center(), null, Color.White, 0f, tex.Size() / 2f, 1f, SpriteEffects.None, 0f);

		if (IsMouseHovering)
			Main.LocalPlayer.mouseInterface = true;
	}
}
