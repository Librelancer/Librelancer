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

function class(base)
   local c = {}    -- a new class instance
   if type(base) == 'table' then
    -- our new class is a shallow copy of the base class!
      for i,v in pairs(base) do
         c[i] = v
      end
      c._base = base
   end
   -- the class will be the metatable for all its objects,
   -- and they will look up their methods in it.
   c.__index = c

   -- expose a constructor which can be called by <classname>(<args>)
   local mt = {}
   mt.__call = function(class_tbl, ...)
   local obj = {}
   setmetatable(obj,c)
   if class_tbl.init then
      class_tbl.init(obj,...)
   else 
      -- make sure that any stuff from the base class is initialized!
      if base and base.init then
      base.init(obj, ...)
      end
   end
   return obj
   end
   c.is_a = function(self, klass)
      local m = getmetatable(self)
      while m do 
         if m == klass then return true end
         m = m._base
      end
      return false
   end
   setmetatable(c, mt)
   return c
end


Events = {}
function Serialize(table)
    return serializeTable(table)
end
function CallEvent(ev, ...)
    if Events[ev] ~= nil then
        Events[ev](...)
    end
end

local _f = Funcs
function NewObject(o)
    return _f:NewObject(o)
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
function GetColor(color)
    return _f:GetColor(color)
end
function GetModel(model)
    return _f:GetModel(model)
end
function GetImage(image)
    return _f:GetImage(image)
end
function SetWidget(w)
    _f:SetWidget(w)
end
function GetNavbarIconPath(ico)
    return _f:GetNavbarIconPath(ico)
end
function Vector3(x,y,z)
    return _f:Vector3(x,y,z)
end
function StringFromID(id)
    return _f:StringFromID(id)
end
function require(mod)
    return _f:Require(mod)
end
function NumberToStringCS(num, fmt)
    return _f:NumberToStringCS(num, fmt)
end
function GetInfocard(id)
    return _f:GetInfocard(id)
end
";
    }
}