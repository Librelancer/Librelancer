namespace LibreLancer.Data.Dll;

public class VersionInfoResource
{
    public byte[] Data { get; private set; }

    public VersionInfoResource(byte[] data)
    {
        Data = data;
    }
}