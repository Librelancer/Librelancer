namespace LibreLancer.Net
{
    public interface INetResponder
    {
        void Respond_int(int sequence, int i);
        void Respond_bool(int sequence, bool b);
    }
}