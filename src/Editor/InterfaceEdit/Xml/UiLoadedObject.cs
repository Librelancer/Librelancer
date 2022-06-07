// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Text;

namespace LibreLancer.Interface.Reflection
{
    public class LuaPrinterContext
    {
        StringBuilder builder = new StringBuilder();
        private int tabLevel = 0;
        private int objNameIndex = 0;
        public string ElementProperty;
        private Queue<string> freeIdentifiers = new Queue<string>();
        public void WriteLine(string text)
        {
            for (int i = 0; i < tabLevel; i++)
                builder.Append("    ");
            builder.AppendLine(text);
        }
        public (string, bool) GetIdentifier()
        {
            if (freeIdentifiers.Count > 0)
                return (freeIdentifiers.Dequeue(), false);
            return ($"_o{objNameIndex++:X}", true);
        }
        public void FreeIdentifier(string identifier)
        {
            freeIdentifiers.Enqueue(identifier);
        }
        public void TabIn()
        {
            tabLevel++;
        }

        public void TabOut()
        {
            tabLevel--;
        }
        public string GetString()
        {
            return builder.ToString();
        }
    }
    public class UiLoadedObject
    {
        public Type Type { get; private set; }
        internal List<UiLoadedProperty> Setters = new List<UiLoadedProperty>();
        public void Fill(object obj,  List<XmlObjectMap> maps = null)
        {
            if (maps != null)
            {
                foreach (var m in maps)
                {
                    if (m.Object == this)
                    {
                        m.Object = obj;
                        break;
                    }
                }
            }
            foreach (var s in Setters)
            {
                s.Set(obj, maps);
            }
        }

        delegate object ObjectActivator();

        static ObjectActivator CreateCtor(Type type)
        {
            if (type == null)
            {
                throw new NullReferenceException("type");
            }
            ConstructorInfo emptyConstructor = type.GetConstructor(Type.EmptyTypes);
            var dynamicMethod = new DynamicMethod("CreateInstance", type, Type.EmptyTypes, true);
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Nop);
            ilGenerator.Emit(OpCodes.Newobj, emptyConstructor);
            ilGenerator.Emit(OpCodes.Ret);
            return (ObjectActivator)dynamicMethod.CreateDelegate(typeof(ObjectActivator));
        }
        static Dictionary<Type, ObjectActivator> activators = new Dictionary<Type,ObjectActivator>();

        static ObjectActivator GetActivator(Type t)
        {
            if (!activators.TryGetValue(t, out var o))
            {
                o = CreateCtor(t);
                activators.Add(t, o);
            }
            return o;
        }

        private readonly ObjectActivator createInstance;
        public UiLoadedObject(Type type)
        {
            Type = type;
            createInstance = GetActivator(type);
        }
        

        
        public object Create(List<XmlObjectMap> maps = null)
        {
            var t = createInstance();
            Fill(t, maps);
            return t;
        }

        public void PrintSetter(LuaPrinterContext printer, string identifier)
        {
            foreach (var s in Setters)
            {
                s.Print(printer, identifier);
            }
        }
        public string PrintClassInit(string className, string fieldName)
        {
            var printer = new LuaPrinterContext();
            //class
            printer.WriteLine($"class {className}_Designer {{");
            printer.TabIn();
            //constructor
            printer.WriteLine($"{className}_Designer()");
            printer.WriteLine("{");
            printer.TabIn();
            printer.WriteLine($"this.{fieldName} = {UiComplexProperty.TypeInitExpression(Type)}");
            printer.WriteLine("this.Elements = {}");
            printer.ElementProperty = "this.Elements";
            PrintSetter(printer, $"this.{fieldName}");
            printer.ElementProperty = null;
            printer.TabOut();
            printer.WriteLine("}");
            printer.TabOut();
            printer.WriteLine("}");
            return printer.GetString();
        }

        public string PrintStylesheetInit()
        {
            var printer = new LuaPrinterContext();
            printer.WriteLine($"function CreateStylesheet()");
            printer.WriteLine("{");
            printer.TabIn();
            printer.WriteLine("local stylesheet = ClrTypes.LibreLancer_Interface_Stylesheet.__new()");
            PrintSetter(printer, "stylesheet");
            printer.WriteLine("return stylesheet");
            printer.TabOut();
            printer.WriteLine("}");
            return printer.GetString();
        }
    }

    abstract class UiLoadedProperty
    {

        static Action<object, object> BuildSetter(Type targetType, PropertyInfo propertyInfo)
        {
            var exInstParam = Expression.Parameter(typeof(object), "t");
            var exInstance = Expression.Convert(exInstParam, targetType); 
            var exMemberAccess = Expression.MakeMemberAccess(exInstance, propertyInfo);
            var exValue = Expression.Parameter(typeof(object), "p");
            var exConvertedValue = Expression.Convert(exValue, propertyInfo.PropertyType);
            var exBody = Expression.Assign(exMemberAccess, exConvertedValue);

            var lambda = Expression.Lambda<Action<object, object>>(exBody, exInstParam, exValue);
            var action = lambda.Compile();
            return action;
        }

        static Dictionary<(Type t, PropertyInfo p), Action<object,object>> setters = new Dictionary<(Type t, PropertyInfo p), Action<object, object>>();
        private static object _setterLock = new object();
        static Action<object, object> GetSetter(Type t, PropertyInfo p)
        {
            lock (_setterLock)
            {
                var key = (t, p);
                if (!setters.TryGetValue(key, out var act))
                {
                    act = BuildSetter(t, p);
                    setters.Add(key, act);
                }
                return act;
            }
        }

        protected PropertyInfo property;
        private Action<object, object> setter;

        protected UiLoadedProperty(PropertyInfo property)
        {
            this.property = property;
            setter = GetSetter(property.DeclaringType, property);
        }
        
        public void Set(object obj, List<XmlObjectMap> maps)
        {
            SetInternal(setter, obj, maps);
        }
        protected abstract void SetInternal(Action<object, object> setter, object obj,  List<XmlObjectMap> maps);
        public abstract void Print(LuaPrinterContext printer, string parent);

        public static string TypeInitExpression(Type t)
        {
            var fn =
                     GetFriendlyName(t)
                         .Replace(".", "_")
                         .Replace(",", "__")
                         .Replace("<", "___")
                         .Replace(">", "___");
            return $"ClrTypes.{fn}.__new()";
        }
        
        static string GetFriendlyName(Type type)
        {
            string friendlyName = type.FullName;
            if (type.IsGenericType)
            {
                int iBacktick = friendlyName.IndexOf('`');
                if (iBacktick > 0)
                {
                    friendlyName = friendlyName.Remove(iBacktick);
                }
                friendlyName += "<";
                Type[] typeParameters = type.GetGenericArguments();
                for (int i = 0; i < typeParameters.Length; ++i)
                {
                    string typeParamName = GetFriendlyName(typeParameters[i]);
                    friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
                }
                friendlyName += ">";
            }

            return friendlyName?.Replace("+", ".");
        }

        static string LiteralFloat(float f)
        {
            return f.ToString("0.###############");
        }
        protected static object ObjToString(object o)
        {
            string valuestr = "";
            if (o is string) valuestr = ToLiteral(o.ToString());
            else if (o is float f) valuestr = LiteralFloat(f);
            else if (o is bool b) valuestr = b ? "true" : "false";
            else if (o is InterfaceModel mdl) valuestr = $"GetModel({ToLiteral(mdl.Name)})";
            else if (o is InterfaceColor clr) valuestr = $"GetColor({ToLiteral(clr.ToString())})";
            else if (o is InterfaceImage img) valuestr = $"GetImage({ToLiteral(img.Name)})";
            else if (o is Vector3 vec) valuestr = $"Vector3({LiteralFloat(vec.X)}, {LiteralFloat(vec.Y)}, {LiteralFloat(vec.Z)})";
            else if (o.GetType().IsEnum) valuestr = $"{o.GetType().Name}.{o}";
            else valuestr = o.ToString();
            return valuestr;
        }
        public static string ToLiteral(string input) {
            StringBuilder literal = new StringBuilder(input.Length + 2);
            literal.Append("\"");
            foreach (var c in input) {
                switch (c) {
                    case '\'': literal.Append(@"\'"); break;
                    case '\"': literal.Append("\\\""); break;
                    case '\\': literal.Append(@"\\"); break;
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        // ASCII printable character
                        if (c >= 0x20 && c <= 0x7e) {
                            literal.Append(c);
                            // As UTF16 escaped character
                        } else {
                            literal.Append(@"\u");
                            literal.Append(((int)c).ToString("x4"));
                        }
                        break;
                }
            }
            literal.Append("\"");
            return literal.ToString();
        }
    }
    class UiSimpleProperty : UiLoadedProperty
    {
        public object Value;
        public UiSimpleProperty(PropertyInfo p, object v) : base(p)
        {
            Value = v;
        }

        protected override void SetInternal(Action<object, object> setter, object obj, List<XmlObjectMap> maps) =>
            setter(obj, Value);

        public override void Print(LuaPrinterContext printer, string parent)
        {
            printer.WriteLine($"{parent}.{property.Name} = {ObjToString(Value)}");
        }
       
    }
    class UiComplexProperty : UiLoadedProperty
    {
        public UiLoadedObject Value;
        public UiComplexProperty(PropertyInfo p, UiLoadedObject v) : base(p)
        {
            Value = v;
        }
        protected override void SetInternal(Action<object, object> setter, object obj, List<XmlObjectMap> maps)  => setter(obj, Value.Create(maps));
        public override void Print(LuaPrinterContext printer, string parent)
        {
            var (ident, define) = printer.GetIdentifier();
            printer.WriteLine($"{(define ? "local " : "")}{ident} = {TypeInitExpression(Value.Type)}");
            Value.PrintSetter(printer, ident);
            printer.WriteLine($"{parent}.{property.Name} = {ident}");
            printer.FreeIdentifier(ident);
        }
    }
    class UiPrimitiveList : UiLoadedProperty
    {
        public object[] Values;
        private Type type;

        public UiPrimitiveList(PropertyInfo p, object[] v) : base(p)
        {
            type = p.PropertyType;
            Values = v;
        }

        protected override void SetInternal(Action<object, object> setter, object obj, List<XmlObjectMap> maps) 
        {
            var list = Activator.CreateInstance(type) as IList;
            foreach (var val in Values)
                list.Add(val);
            setter(obj, list);
        }

        public override void Print(LuaPrinterContext printer, string parent)
        {
            var (ident, define) = printer.GetIdentifier();
            printer.WriteLine($"{(define ? "local " : "")}{ident} = {TypeInitExpression(type)}");
            foreach(var obj in Values)
                printer.WriteLine($"{ident}.Add({ObjToString(obj)}");
            printer.WriteLine($"{parent}.{property.Name} = {ident}");
            printer.FreeIdentifier(ident);
        }
    }

    class UiPrimitiveDictionary : UiLoadedProperty
    {
        public object[] Keys;
        public object[] Values;
        private Type type;

        public UiPrimitiveDictionary(PropertyInfo p, object[] k, object[] v) : base(p)
        {
            type = p.PropertyType;
            Keys = k;
            Values = v;
        }

        protected override void SetInternal(Action<object, object> setter, object obj, List<XmlObjectMap> maps) 
        {
            var dict = Activator.CreateInstance(type) as IDictionary;
            for (int i = 0; i < Keys.Length; i++) {
                dict.Add(Keys[i], Values[i]);
            }

            setter(obj, dict);
        }

        public override void Print(LuaPrinterContext printer, string parent)
        {
        }
    }
    
    class UiComplexDictionary : UiLoadedProperty
    {
        public object[] Keys;
        public UiLoadedObject[] Values;
        private Type type;

        public UiComplexDictionary(PropertyInfo p, object[] k, UiLoadedObject[] v) : base(p)
        {
            type = p.PropertyType;
            Keys = k;
            Values = v;
        }

        protected override void SetInternal(Action<object, object> setter, object obj, List<XmlObjectMap> maps) 
        {
            var dict = Activator.CreateInstance(type) as IDictionary;
            for (int i = 0; i < Keys.Length; i++) {
                dict.Add(Keys[i], Values[i].Create(maps));
            }
            setter(obj, dict);
        }

        public override void Print(LuaPrinterContext printer, string parent)
        {
        }
    }

    class UiComplexList : UiLoadedProperty
    {
        public UiLoadedObject[] Objects;
        private Type type;
        public UiComplexList(PropertyInfo p, UiLoadedObject[] v) : base(p)
        {
            type = p.PropertyType;
            Objects = v;
        }

        protected override void SetInternal(Action<object, object> setter, object obj, List<XmlObjectMap> maps) 
        {
            var list = Activator.CreateInstance(type) as IList;
            foreach (var val in Objects)
                list.Add(val.Create(maps));
            setter(obj, list);
        }

        public override void Print(LuaPrinterContext printer, string parent)
        {
            var (ident, define) = printer.GetIdentifier();
            printer.WriteLine($"{(define ? "local " : "")}{ident} = {TypeInitExpression(type)}");
            foreach (var obj in Objects)
            {
                var (objIdent, objDefine) = printer.GetIdentifier();
                printer.WriteLine($"{(objDefine ? "local " : "")}{objIdent} = {TypeInitExpression(obj.Type)}");
                obj.PrintSetter(printer, objIdent);
                printer.WriteLine($"{ident}.Add({objIdent})");
                printer.FreeIdentifier(objIdent);
                if (printer.ElementProperty != null)
                {
                    var src = obj.Create();
                    if (src is UiWidget uw && !string.IsNullOrWhiteSpace(uw.ID))
                    {
                        printer.WriteLine($"{printer.ElementProperty}[{ToLiteral(uw.ID)}] = {objIdent}");
                    }
                }
            }
            printer.WriteLine($"{parent}.{property.Name} = {ident}");
            printer.FreeIdentifier(ident);
        }
    }
}