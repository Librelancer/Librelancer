
namespace LibreLancer.Interface
{
    public interface IContactListData
    {
        int Count { get; }
        bool IsSelected(int index);
        void SelectIndex(int index);
        string Get(int index);
        
        RepAttitude GetAttitude(int index);
    }
}