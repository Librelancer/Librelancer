// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace LibreLancer.Interface.Reflection
{
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

        private PropertyInfo property;
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
    }
    class UiComplexProperty : UiLoadedProperty
    {
        public UiLoadedObject Value;
        public UiComplexProperty(PropertyInfo p, UiLoadedObject v) : base(p)
        {
            Value = v;
        }
        protected override void SetInternal(Action<object, object> setter, object obj, List<XmlObjectMap> maps)  => setter(obj, Value.Create(maps));
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
    }
}