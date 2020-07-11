// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Interface
{
    public partial class LuaContext
    {
        private const string DEFAULT_LUA = @"
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

Events = {}
function Serialize(table)
    return 'return ' .. serializeTable(table)
end
function CallEvent(ev, ...)
    if Events[ev] ~= nil then
        Events[ev](...)
    end
end

local _f = Funcs
function GetElement(e)
    return _f:GetElement(e)
end
function NewObject(o)
    return _f:NewObject(o)
end
function OpenModal(xml, data, func)
    _f:OpenModal(xml,data,func)
end
function CloseModal(table)
    _f:CloseModal(table)
end
function Timer(time, func)
    _f:Timer(time,func)
end
function ApplyStyles()
    _f:ApplyStyles()
end
function GetScene()
    return _f:GetScene()
end
function PlaySound(sound)
    _f:PlaySound(sound)
end
function Color(color)
    return _f:Color(color)
end
function GetNavbarIconPath(ico)
    return _f:GetNavbarIconPath(ico)
end
function SwitchTo(scn)
    return _f:SwitchTo(scn)
end
function SceneID()
    return _f:SceneID()
end
function require(mod)
    return _f:Require(mod)
end

";
    }
}