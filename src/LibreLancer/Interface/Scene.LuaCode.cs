namespace LibreLancer.Interface
{
    public partial class Scene
    {
        private const string LUA_SANDBOX = @"
import ('LibreLancer', 'LibreLancer.Interface')

function shallowcopy(orig)
    local copy = {}
    for orig_key, orig_value in pairs(orig) do
        copy[orig_key] = orig_value
    end
    return copy
end

local function serializeTable(val, name, skipnewlines, depth)
    skipnewlines = skipnewlines or false
    depth = depth or 0
    local tmp = string.rep(' ', depth)

    if name then tmp = tmp .. name .. ' = ' end

    if type(val) == 'table' then
        tmp = tmp .. '{' .. (not skipnewlines and '\n' or '')

        for k, v in pairs(val) do
            tmp =  tmp .. serializeTable(v, k, skipnewlines, depth + 1) .. ',' .. (not skipnewlines and '\n' or '')
        end

        tmp = tmp .. string.rep(' ', depth) .. '}'
    elseif type(val) == 'number' then
        tmp = tmp .. tostring(val)
    elseif type(val) == 'string' then
        tmp = tmp .. string.format('%q', val)
    elseif type(val) == 'boolean' then
        tmp = tmp .. (val and 'true' or 'false')
    else
        tmp = tmp .. '\'[inserializeable datatype:' .. type(val) .. ']\''
    end
    return  tmp
end

function Serialize(table)
    return 'return ' .. serializeTable(table)
end

function Deserialize(code)
    local chunk = load(code,'data', 't', {})
    return chunk()
end

function DeserializeToEnv(name, code)
    local chunk = load(code,'data', 't', {})
    Env[name] = chunk()    
end

function NetEnum(enum)
    return function (a)
        return luanet.enum(enum, a)
    end
end

Env = { 
    next = next,
    pairs = pairs,
    pcall = pcall,
    error = error,
    tonumber = tonumber,
    tostring = tostring,
    type = type,
    unpack = unpack,
    math = shallowcopy(math),
    string = shallowcopy(string),
    table = shallowcopy(table),
    AnchorKind = NetEnum(AnchorKind),
    HorizontalAlignment = NetEnum(HorizontalAlignment),
    VerticalAlignment = NetEnum(VerticalAlignment),
    Events = { }
}

-- Patch .NET array access with ipairs to work properly
-- .NET arrays are still done as 0-based which is not lua,
-- but at least get some of it right
function netipairs(tbl)
	local function stateless_iter(tbl, i)
    	i = i + 1
		if i < tbl.Length then
    		local v = tbl[i]
    		if v then return i, v end
		end
	end
	return stateless_iter, tbl, -1
end

function Env.ipairs(tbl)
    if type(tbl) == 'userdata' then
        return netipairs(tbl)
    else
        return ipairs(tbl)
    end
end

Env.print = function (...)
    printResult = """"
    for i, v in ipairs{...} do
        printResult = printResult .. tostring(v) .. '\t'
    end
    LogString(printResult)
end

local loadedScripts = { }
local scriptReturns = { }
Env.require = function (script)
    if loadedScripts[script] == nil then
        loadedScripts[script] = 0
        local chunk, err = load(ReadAllText(script), script, 't', Env)
        if chunk == nil then
            error(err)
        else
            scriptReturns[script] = chunk()
        end
    end
    return scriptReturns[script]
end

function RunSandboxed (code, chunkname)
    local chunk, err = load(code, chunkname, 't', Env)
    if chunk == nil then
        error(err)
    else
        chunk()
    end
end

local eventCode = [[
    local function DoEvent(event, ...)
        if Events[event] ~= nil then
            Events[event](...)
        end
    end
    DoEvent(...)
]]

local eventChunk = load(eventCode, 'eventChunk', 't', Env)
function CallEvent (event, ...)
    eventChunk(event, ...)
end
";
    }
}