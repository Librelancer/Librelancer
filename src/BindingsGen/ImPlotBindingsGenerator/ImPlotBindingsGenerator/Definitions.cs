namespace ImPlotBindingsGenerator;

public class FuncDefinition
{
    public string args { get; set; } = "";
    public FuncArg[] argsT { get; set; } = [];
    public Dictionary<string, string> defaults { get; set; } = [];
    public string argsoriginal { get; set; } = "";
    public string call_args { get; set; } = "";
    public string call_args_old { get; set; } = "";
    public string cimguiname { get; set; } = "";
    public string funcname { get; set; } = "";
    public string @namespace { get; set; } = "";
    public string ret { get; set; } = "";
    public string retref { get; set; } = "";
    public string signature { get; set; } = "";
    public string stname { get; set; } = "";
    public string ov_cimguiname { get; set; } = "";
    public string location { get; set; } = "";
    public bool destructor { get; set; }
    public bool constructor { get; set; }

    public string CppFileArgs { get; set; }
    public string CppFileCall { get; set; }
}

public class FuncArg
{
    public string name { get; set; }
    public string type { get; set; }
}


public class EnumMember
{
    public int calc_value { get; set; }
    public string value { get; set; } = "";
    public string name { get; set; } = "";
}

public class StructMember
{
    public string name { get; set; } = "";
    public string type { get; set; } = "";
}

public class StructsAndEnums
{
    public Dictionary<string, StructMember[]> structs { get; set; } = [];
    public Dictionary<string, EnumMember[]> enums { get; set; } = [];
    public Dictionary<string, string> locations { get; set; } = [];
}
