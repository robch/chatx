using System;
using System.Collections.Generic;
using System.Linq;

public class ConfigValue
{
    public ConfigValue()
    {
        _value = null;
        Source = ConfigSource.NotFound;
        IsSecret = false;
    }

    public ConfigValue(object? value)
    {
        _value = value;
        Source = ConfigSource.Default;
        IsSecret = false;
    }
    
    public ConfigValue(object? value, ConfigSource source)
    {
        _value = value;
        Source = source;
        IsSecret = false;
    }
    
    public ConfigValue(object? value, ConfigSource source, bool isSecret)
    {
        _value = value;
        Source = source;
        IsSecret = isSecret;
    }

    public object? Value => _value;
    
    /// <summary>
    /// Gets the source of this configuration value.
    /// </summary>
    public ConfigSource Source { get; }

    /// <summary>
    /// Gets the configuration file associated with this value, if any.
    /// </summary>
    public ConfigFile? File { get; internal set; }

    /// <summary>
    /// Gets whether this value should be treated as a secret.
    /// </summary>
    public bool IsSecret { get; }

    public string? AsString()
    {
        return _value?.ToString();
    }
    
    /// <summary>
    /// Returns an obfuscated version of the value if it's a secret.
    /// </summary>
    /// <returns>The obfuscated value if it's a secret, otherwise the normal string representation.</returns>
    public string? AsObfuscated()
    {
        if (!IsSecret || _value == null)
        {
            return AsString();
        }
        
        var valueStr = _value.ToString() ?? string.Empty;
        if (valueStr.Length <= 4)
        {
            return "****";
        }
        
        // Show first 2 and last 2 characters, obfuscate the rest
        return valueStr.Substring(0, 2) + new string('*', Math.Min(60, valueStr.Length - 4)) + valueStr.Substring(valueStr.Length - 2);
    }

    public int AsInt(int defaultValue = 0)
    {
        if (_value is int intValue)
        {
            return intValue;
        }

        if (_value is string stringValue && int.TryParse(stringValue, out var parsedInt))
        {
            return parsedInt;
        }

        return defaultValue;
    }

    public bool AsBool(bool defaultValue = false)
    {
        if (_value is bool boolValue)
        {
            return boolValue;
        }

        if (_value is string stringValue)
        {
            if (bool.TryParse(stringValue, out var parsedBool))
            {
                return parsedBool;
            }

            return stringValue.ToLower() == "true" || stringValue == "1";
        }

        if (_value is int intValue)
        {
            return intValue != 0;
        }

        return defaultValue;
    }

    public List<string> AsList()
    {
        // Handle null value
        if (_value == null)
        {
            ConsoleHelpers.WriteDebugLine($"AsList: null value");
            return new List<string>();
        }

        // Handle List<object>
        if (_value is List<object> objList)
        {
            var list = objList.Select(o => o?.ToString() ?? string.Empty).ToList();
            ConsoleHelpers.WriteDebugLine($"AsList: List<object> [{string.Join(", ", list)}]");
            return list;
        }

        // Handle List<string>
        if (_value is List<string> stringList)
        {
            ConsoleHelpers.WriteDebugLine($"AsList: List<string> [{string.Join(", ", stringList)}]");
            return stringList;
        }

        // Handle arrays (object[] and string[])
        if (_value is object[] objArray)
        {
            var list = objArray.Select(o => o?.ToString() ?? string.Empty).ToList();
            ConsoleHelpers.WriteDebugLine($"AsList: object[] [{string.Join(", ", list)}]");
            return list;
        }

        if (_value is string[] strArray)
        {
            ConsoleHelpers.WriteDebugLine($"AsList: string[] [{string.Join(", ", strArray)}]");
            return strArray.ToList();
        }

        // Handle single string
        if (_value is string singleString)
        {
            // Check if it's an empty array representation from YAML
            if (singleString == "[]")
            {
                ConsoleHelpers.WriteDebugLine($"AsList: empty array string representation");
                return new List<string>();
            }
            
            ConsoleHelpers.WriteDebugLine($"AsList: single string {singleString}");
            return new List<string> { singleString };
        }

        // Handle IEnumerable<object> (catches more generic collections)
        if (_value is System.Collections.IEnumerable enumerable && 
            !(_value is string)) // Exclude strings which are IEnumerable<char>
        {
            var list = new List<string>();
            foreach (var item in enumerable)
            {
                list.Add(item?.ToString() ?? string.Empty);
            }
            ConsoleHelpers.WriteDebugLine($"AsList: IEnumerable [{string.Join(", ", list)}]");
            return list;
        }

        // Default - empty list
        ConsoleHelpers.WriteDebugLine($"AsList: unknown type ({_value?.GetType().Name ?? "null"}) - returning empty list");
        return new List<string>();
    }

    public bool IsNotFound()
    {
        return Source == ConfigSource.NotFound;
    }

    public bool IsNotFoundNullOrEmpty()
    {
        if (Source == ConfigSource.NotFound)
        {
            return true;
        }

        if (_value == null)
        {
            return true;
        }

        if (_value is string stringValue)
        {
            return string.IsNullOrEmpty(stringValue);
        }

        if (_value is List<object> objList)
        {
            return objList.Count == 0;
        }

        if (_value is List<string> stringList)
        {
            return stringList.Count == 0;
        }

        return false;
    }

    public bool AddToList(string value)
    {
        var list = AsList();
        if (!list.Contains(value))
        {
            list.Add(value);
            _value = list;
            return true;
        }

        return false;
    }

    public bool RemoveFromList(string value)
    {
        var list = AsList();
        var removed = list.Remove(value);
        if (removed)
        {
            _value = list;
        }

        return removed;
    }

    public void Clear()
    {
        _value = null;
    }

    public void Set(object? value)
    {
        _value = value;
    }

    public override string ToString()
    {
        if (_value == null)
        {
            return string.Empty;
        }

        if (_value is List<object> objList)
        {
            return string.Join(", ", objList.Select(o => o?.ToString() ?? string.Empty));
        }

        if (_value is List<string> stringList)
        {
            return string.Join(", ", stringList);
        }

        return _value.ToString() ?? string.Empty;
    }

    private object? _value;
}