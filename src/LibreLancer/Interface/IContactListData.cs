
namespace LibreLancer.Interface
{
    public interface IContactListData
    {
        int Count { get; }
        bool IsSelected(int index);
        void SelectIndex(int index);
        string Get(int index);

        void SetFilter(string filter);

        RepAttitude GetAttitude(int index);
        bool IsWaypoint(int index);
    }
}
