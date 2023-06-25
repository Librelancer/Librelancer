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
}