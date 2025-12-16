using System.Security.Cryptography.X509Certificates;
using LibreLancer.Net;

namespace LLServer;

public class ServerConfig
{
    public string ServerName = "";
    public string ServerDescription = "";
    public string FreelancerPath = "";
    public string LoginUrl;
    public string DatabasePath = "";
    public int Port = LNetConst.DEFAULT_PORT;
    public bool UseLrpkFile = false;
    public string LrpkFilePath = "";
    public string lastConfigPath = "";

    public void CopyFrom(ServerConfig other)
    {
        ServerName = other.ServerName;
        ServerDescription = other.ServerDescription;
        FreelancerPath= other.FreelancerPath;
        LoginUrl = other.LoginUrl;
        DatabasePath = other.DatabasePath;
        Port = other.Port;
        UseLrpkFile = other.UseLrpkFile;
        LrpkFilePath= other.LrpkFilePath;
        lastConfigPath = other.lastConfigPath;
    }
}
