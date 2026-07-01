
namespace LibreLancer.Interface
{
    public interface IContactListData
    {
        int Count { get; }
        bool IsSelected(int index);
        void SelectIndex(int index);
        string GetLabel(int index);
        string GetDistanceString(int index);

        void SetFilter(string filter);

        RepAttitude GetAttitude(int index);
        ContactIcon GetIcon(int index);
        bool IsWaypoint(int index);
    }
}
