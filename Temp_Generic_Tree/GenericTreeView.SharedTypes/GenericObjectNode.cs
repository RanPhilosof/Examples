using Google.Protobuf;
using Google.Protobuf.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace GenericTreeView.SharedTypes
{
    public static class ExtensionMethods
    {
        public static string CSharpName(this Type type)
        {
            var sb = new StringBuilder();
            var name = type.Name;
            if (!type.IsGenericType) return name;
            sb.Append(name.Substring(0, name.IndexOf('`')));
            sb.Append("<");
            sb.Append(string.Join(", ", type.GetGenericArguments()
                                            .Select(t => t.CSharpName())));
            sb.Append(">");
            return sb.ToString();
        }
    }

    public class GenericObjectNode
    {
        public bool CanBeRemoved { get; set; }

        public bool CanBeAdded { get; set; }

        public void UpdateValue(Type type)
        {
            if (type != null && type == typeof(string))
            {
                //_value = string.Empty;
                PropertyValue = string.Empty;
                ParseObjectTree(_name, _nameType, _value, _valueType, _propertyName, _parent);
            }
            else
            {
                PropertyValue = type != null ? Activator.CreateInstance(type) : null;                
                //_value = type != null ? Activator.CreateInstance(type) : null;
                //if (_parent != null && _parent._valueType. _value
                ParseObjectTree(_name, _nameType, _value, _valueType, _propertyName, _parent);
            }
        }

        public void AddItem()
        {
            if (CanBeAdded)
            {
                var addMethods = _valueType.GetMethods().Where(x => x.Name.ToLowerInvariant() == "add").ToList();
                if (addMethods.Count > 0)
                    if (_valueType.GetGenericArguments()[0] == typeof(string))
                        addMethods.First().Invoke(_value, new object[] { string.Empty });
                    else
                        addMethods.First().Invoke(_value, new object[] { Activator.CreateInstance(_valueType.GetGenericArguments()[0]) });

                ParseObjectTree(_name, _nameType, _value, _valueType, _propertyName, _parent);
            }
        }

        public void RemoveMe()
        {
            if (_parent != null && _parent._valueType != null && _parent._value != null && CanBeRemoved)
            {
                var removeAtMethod = _parent._valueType.GetMethod("RemoveAt");
                var index = int.Parse(_name.Replace("[", "").Replace("]", ""));
                removeAtMethod.Invoke(_parent._value, new object[] { index });

                _parent.ParseObjectTree(_parent._name, _parent._nameType, _parent._value, _parent._valueType, _parent._propertyName, _parent._parent);
            }
        }

        public bool IsReadonly { get; set; }

        public object GetAggregator() { return aggregator; }
        private object aggregator;

        private Func<GenericObjectNode, object> _aggregatorCreator;

        #region Private Properties

        private GenericObjectNode _parent;

        private string _name;
        private Type _nameType;
        private object _value;
        private Type _valueType;
        private string _valueToPresent;
        private PropertyInfo _propertyName;

        #endregion

        #region Constructor

        public GenericObjectNode(object value, Func<GenericObjectNode, object> aggregatorCreator) : this("root", null, value, null, null, aggregatorCreator) { }

        private GenericObjectNode(
            string name,
            Type nameType,
            object value,
            PropertyInfo propertyName,
            GenericObjectNode parent,
            Func<GenericObjectNode, object> aggregatorCreator
            )
        {
            _aggregatorCreator = aggregatorCreator;

            ParseObjectTree(
                name,
                nameType,
                value,
                value != null ? value.GetType() : nameType,
                propertyName,
                parent);

            aggregator = _aggregatorCreator?.Invoke(this);
        }

        #endregion

        #region Public Properties        

        public string PropertyName
        {
            get
            {
                return _name;
            }
        }

        public string PropertyValueText
        {
            get
            {
                if (_valueToPresent != null)
                    return _valueToPresent;

                if (PropertyValue == null)
                    return "null";

                var type = PropertyValue.GetType();

                if (type.IsPrimitive || PropertyValue is string)
                {
                    return PropertyValue.ToString();
                }

                //if (!type.IsValueType && PropertyValue is not string)
                //{
                return ExtensionMethods.CSharpName(type);
                //}

                //return 
            }
            set
            {
                PropertyValue = value;
            }
        }

        public object PropertyValue
        {
            get
            {
                if (_valueType.IsValueType && _parent != null && _propertyName != null)
                {
                    object obj;
                    
                    try
                    {
                        obj = _propertyName.GetValue(_parent._value, null);
                    }
                    catch
                    {
                        object defaultValue = Activator.CreateInstance(_propertyName.PropertyType);
                        _propertyName.SetValue(_parent._value, defaultValue);
                        obj = _propertyName.GetValue(_parent._value, null);
                    }

                    return obj;
                }

                return _value;
            }
            set
            {
                if (_parent != null && _parent._value is IList)
                {
                    var index = int.Parse(_name.Substring(1, _name.Length - 2));
                    var iList = (IList)_parent._value;

                    if (value is string stringValue)
                        iList[index] = GetValueByTypeWithCorrectType(stringValue, iList[index].GetType());
                    else
                        iList[index] = value;

                    _value = iList[index];
                }
                else
                {
                    if (_parent.PropertyValue.GetType().IsValueType)
                        return;

                    var prop = _parent.PropertyValue.GetType().GetProperty(_name);
                    if (value is string stringValue)
                    {
                        if (_nameType.Name.EndsWith("OneofCase"))
                        {
                            var correctProperty = char.ToLower(_name[0]) + _name.Substring(1) + "_";
                            var field = _parent.PropertyValue.GetType().GetField(correctProperty, BindingFlags.NonPublic | BindingFlags.Instance);
                            //var valCorrectType = GetValueByTypeWithCorrectType(stringValue, _value);
                            //var valCorrectType = GetValueByTypeWithCorrectType(stringValue, value);
                            var valCorrectType = GetValueByTypeWithCorrectType(stringValue, _valueType != null ? _valueType : _nameType);
                            field.SetValue(_parent.PropertyValue, valCorrectType);
                            _value = valCorrectType;
                        }
                        else
                        {
                            //var valCorrectType = GetValueByTypeWithCorrectType(stringValue, _value);
                            //var valCorrectType = GetValueByTypeWithCorrectType(stringValue, value);
                            var valCorrectType = GetValueByTypeWithCorrectType(stringValue, _valueType != null ? _valueType : _nameType);
                            prop.SetValue(_parent.PropertyValue, valCorrectType);
                            //prop.SetValueDirect(__makeref(_parent.PropertyValue), valCorrectType);
                            _value = valCorrectType;
                        }
                    }
                    else
                    {
                        prop.SetValue(_parent.PropertyValue, value);
                        _value = value;
                    }
                }
            }
        }
        //Convert.ChangeType(value, t)
        private object GetValueByTypeWithCorrectType(
            string stringValue,
            object obj)
        {
            switch (obj)
            {
                case Type t when t == typeof(double):
                    if (double.TryParse(stringValue, out double doubleValue))
                        return doubleValue;
                    break;

                case Type t when t == typeof(int):
                    if (int.TryParse(stringValue, out int intValue))
                        return intValue;
                    break;

                case Type t when t == typeof(bool):
                    if (bool.TryParse(stringValue, out bool boolValue))
                        return boolValue;
                    break;

                case Type t when t == typeof(float):
                    if (float.TryParse(stringValue, out float floatValue))
                        return floatValue;
                    break;

                case Type t when t == typeof(long):
                    if (long.TryParse(stringValue, out long longValue))
                        return longValue;
                    break;

                case Type t when t == typeof(decimal):
                    if (decimal.TryParse(stringValue, out decimal decimalValue))
                        return decimalValue;
                    break;

                case Type t when t == typeof(string):
                    return stringValue;

                case Type t when t.IsEnum:
                    try
                    {
                        return Enum.Parse(t, stringValue);
                    }
                    catch
                    {
                        // Handle invalid enum values, or let it bubble up
                    }
                    break;
            }

            return null;
        }

        //private object GetValueByTypeWithCorrectType(
        //    string stringValue,
        //    object obj)
        //{
        //    switch (obj)
        //    {
        //        case double d:
        //            return double.Parse(stringValue);
        //        case int i:
        //            return int.Parse(stringValue);
        //        case bool b:
        //            return bool.Parse(stringValue);
        //        case float f:
        //            return float.Parse(stringValue);
        //        case long l:
        //            return long.Parse(stringValue);
        //        case decimal dec:
        //            return decimal.Parse(stringValue);
        //        case string str:
        //            return stringValue;
        //        case Enum e:
        //            return Enum.Parse(e.GetType(), stringValue);
        //    }
        //
        //    return null;
        //}

        public Type Type
        {
            get
            {
                return _valueType;
            }
        }

        public Type NameType
        {
            get
            {
                return _nameType;
            }
        }

        public string NameTypeText
        {
            get
            {
                return ExtensionMethods.CSharpName(_nameType);
            }
        }

        public List<GenericObjectNode> PropertyChildren { get; set; }

        #endregion

        #region Private Methods

        private void ParseObjectTree(string name, Type nameType, object value, Type type, PropertyInfo propertyName, GenericObjectNode parent)
        {
            PropertyChildren = new List<GenericObjectNode>();

            _parent = parent;
            _valueType = type;
            _name = name;
            _nameType = nameType;
            _value = value;
            _propertyName = propertyName;

            IsReadonly = true;

            if (_value == null)
            {
                IsReadonly = true;
                return;
            }

            PropertyInfo[] props = type.GetProperties();

            if (type.IsPrimitive || value is string)
            {
                IsReadonly = false;
                if (_parent != null && _parent._valueType != null)
                {
                    var removeAtMethod = _parent._valueType.GetMethod("RemoveAt");
                    if (removeAtMethod != null)
                    {
                        CanBeRemoved = true;
                    }
                }
            }

            if (type.IsClass && value is IEnumerable enumValue)
            {
                var addMethod = _valueType.GetMethods().Where(x => x.Name.ToLowerInvariant() == "add").ToList();
                if (addMethod.Count > 0)
                    CanBeAdded = true;

                if (value is string)
                {
                    return;
                }
                else
                {
                    int i = 0;
                    foreach (object element in enumValue)
                    {
                        PropertyChildren.Add(new GenericObjectNode("[" + i + "]", element.GetType(), element, null, this, _aggregatorCreator));
                        i++;
                    }

                    _valueToPresent = $"Count = {((IList)value).Count}";
                }

                return;
            }
            else if (_valueType.FullName.StartsWith("System.Collections.Generic.List`1["))
            {
                int z = 0;
                foreach (var val in (IList)value)
                {
                    PropertyChildren.Add(new GenericObjectNode("[" + z + "]", val.GetType(), val, null, this, _aggregatorCreator));
                    z++;
                }

                _valueToPresent = $"Count = {((IList)value).Count}";

                return;
            }

            foreach (PropertyInfo p in props)
            {
                if (p.PropertyType.Assembly.FullName.ToLowerInvariant().StartsWith("Google.Protobuf".ToLowerInvariant())
                    &&
                    (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(MessageParser<>)
                    ||
                    p.PropertyType == typeof(MessageDescriptor)))
                {
                    continue;
                }

                if (p.PropertyType.IsPublic || p.PropertyType.IsNestedPublic)
                {
                    if (p.PropertyType.IsClass || p.PropertyType.IsAbstract || p.PropertyType.IsArray || (p.GetIndexParameters().Length > 0 && !(value is string)))
                    {
                        if (p.PropertyType.IsArray)
                        {
                            object v = p.GetValue(value, null);
                            IEnumerable arr = v as IEnumerable;

                            PropertyChildren.Add(new GenericObjectNode(
                                p.Name,
                                p.PropertyType,
                                arr,
                                p,
                                this,
                                _aggregatorCreator));

                        }
                        else if (p.PropertyType.FullName.StartsWith("System.Collections.Generic.List`1["))
                        {
                            var list = (IList)p.GetValue(value, null);
                            PropertyChildren.Add(
                                new GenericObjectNode(
                                    p.Name,
                                    p.PropertyType,
                                    list,
                                    p,
                                    this,
                                    _aggregatorCreator));

                        }
                        else
                        {
                            object v = p.GetValue(value, null);

                            //if (v != null)
                            PropertyChildren.Add(new GenericObjectNode(p.Name, p.PropertyType, v, p, this, _aggregatorCreator));
                        }
                    }
                    else if (p.PropertyType.IsValueType && !(value is string))
                    {
                        object v = p.GetValue(value, null);

                        if (v != null)
                            PropertyChildren.Add(new GenericObjectNode(p.Name, p.PropertyType, v, p, this, _aggregatorCreator));
                    }
                }
            }
        }
        #endregion

        public override string ToString()
        {
            return $"{_name} | {_value.ToString()} | {_nameType?.Name} | {_valueType.Name} | {_propertyName?.ToString()}";
        }

        public bool IsPotentialToBeInstantiable()
        {
            return _nameType == typeof(string) || (_nameType != null && _nameType.IsClass && !(_nameType.Namespace?.StartsWith("System") ?? false) && _nameType.GetConstructor(Type.EmptyTypes) != null
                && _parent != null && _propertyName != null && _parent.Type.GetProperty(_propertyName.Name)?.SetMethod != null);
        }

        private TypeFinder typeFinder = new TypeFinder();
        public List<Type> GetInstantiableTypes()
        {
            if (_nameType == typeof(string))
                return new List<Type>() { typeof(string) };

            return typeFinder.GetInstantiableTypes(_nameType);

            //var targetType = _nameType;
            //
            //var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            //
            //var types = assemblies
            //    .SelectMany(assembly => assembly.GetTypes())
            //    .Where(type =>
            //        type.IsClass &&
            //        !type.IsAbstract &&
            //        !type.IsInterface &&
            //        targetType.IsAssignableFrom(type) &&
            //        !(type.Namespace?.StartsWith("System") ?? false) &&
            //        type.GetConstructor(Type.EmptyTypes) != null
            //    )
            //    .ToList();
            //
            //var v = assemblies.SelectMany(assembly => assembly.GetTypes()).Where(x => x.Name.Contains("RepeatedField")).ToList();
            //var v1 = assemblies.SelectMany(assembly => assembly.GetTypes()).Where(type => targetType.IsAssignableFrom(type)).ToList();
            //
            //return types;
        }
    }

    public class TypeFinder
    {
        public List<Type> GetInstantiableTypes(Type targetType)
        {                  
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var types = assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    type.IsClass &&
                    !type.IsAbstract &&
                    HasDefaultConstructor(type) &&
                    MatchesTargetType(type, targetType)
                )
                .Select(type => CloseGenericTypeIfNeeded(type, targetType))
                .Where(type => type != null) // Exclude null (failed to close generic types)
                .ToList();

            return types;
        }

        // Helper method to check if a type has a default (parameterless) constructor
        private bool HasDefaultConstructor(Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        // Helper method to check if a type matches the target type
        private bool MatchesTargetType(Type type, Type targetType)
        {
            if (targetType.IsGenericType)
            {
                if (targetType.IsConstructedGenericType)
                {
                    // For closed generic types like List<string>
                    return type.IsGenericType &&
                           type.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition() ||
                           ImplementsOrInheritsFromGenericType(type, targetType);
                }

                // For open generic types
                return type.IsGenericType &&
                       type.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition();
            }

            // Non-generic fallback
            return targetType.IsAssignableFrom(type);
        }



        class TypesComparer : IEqualityComparer<Type>
        {
            public bool Equals(Type type1, Type type2)
            {
                return IsCompatibleWith(type1, type2);
            }

            public int GetHashCode(Type type)
            {
                return type.GetHashCode();
            }

            bool IsCompatibleWith(Type firstType, Type secondType)
            {
                return firstType.IsAssignableFrom(secondType) || CanBeConvertedTo(secondType, firstType);
            }

            bool CanBeConvertedTo(Type firstType, Type secondType)
            {
                var converter = System.ComponentModel.TypeDescriptor.GetConverter(secondType);
                return converter != null && converter.CanConvertTo(firstType);
            }
        }

        // Helper method to check if a type implements or inherits from a generic type
        private bool ImplementsOrInheritsFromGenericType(Type type, Type targetType)
        {
            if (targetType.IsInterface)
            {
                return type.GetInterfaces()
                           .Any(interfaceType =>
                               interfaceType.IsGenericType &&
                               interfaceType.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition() &&
                               interfaceType.GetGenericArguments().Length == targetType.GetGenericArguments().Length);
                               //interfaceType.GetGenericArguments().SequenceEqual(targetType.GetGenericArguments(), new TypesComparer()));
            }

            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType &&
                    baseType.GetGenericTypeDefinition() == targetType.GetGenericTypeDefinition() &&
                    baseType.GetGenericArguments().SequenceEqual(targetType.GetGenericArguments()))
                {
                    return true;
                }

                baseType = baseType.BaseType;
            }

            return false;
        }

        // Helper method to close generic types (e.g., convert List<T> to List<string>)
        private Type CloseGenericTypeIfNeeded(Type type, Type targetType)
        {
            // If the type is not generic or already closed, return as is
            if (!type.IsGenericType || !type.ContainsGenericParameters)
                return type;

            // If the target type is not generic, we cannot close the type
            if (!targetType.IsGenericType || !targetType.IsConstructedGenericType)
                return null;

            try
            {
                // Create closed generic type using the generic arguments of the target type
                return type.MakeGenericType(targetType.GetGenericArguments());
            }
            catch
            {
                // If type cannot be closed (e.g., due to mismatched arguments), return null
                return null;
            }
        }
    }
}
