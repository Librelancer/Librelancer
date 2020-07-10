namespace LibreLancer.Entities.Character
{
    public class CargoItem
    {
        // The nickname of the item. 
        public string ItemName { get; set; }

        // The amount of the item present in the cargo hold
        public ulong ItemCount { get; set; }

        // Can the item be dropped
        public bool IsMissionItem { get; set; }
    }
}
