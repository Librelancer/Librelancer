using System;
namespace LibreLancer.GameData.Items
{
	public class PowerEquipment : Equipment
	{
		public IDrawable Model;
		public override IDrawable GetDrawable()
		{
			return Model;
		}
		public PowerEquipment()
		{
		}
	}
}
