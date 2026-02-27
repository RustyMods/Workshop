using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Workshop;

public static class PlanExtensions
{
    public static string ToCustomString(this Quaternion q, char separator = ',') =>
        $"{q.x}{separator}{q.y}{separator}{q.z}{separator}{q.w}";
    public static string ToCustomString(this Vector3 v, char separator = ',') => $"{v.x}{separator}{v.y}{separator}{v.z}";
    
    public static T GetEnum<T>(this string[] parts, int index, T defaultValue) where T : struct, Enum
    {
        if (parts.Length - 1 < index) return defaultValue;
        return Enum.TryParse(parts[index].Trim(), true, out T result) ? result : defaultValue;
    }
    
    public static Quaternion GetQuaternion(this string[] parts, int startIndex)
    {
        return new Quaternion(parts.GetFloat(startIndex), parts.GetFloat(startIndex + 1), parts.GetFloat(startIndex + 2), parts.GetFloat(startIndex + 3));
    }
    
    public static Vector3 GetVector(this string[] parts, int startIndex)
    {
        return new Vector3(parts.GetFloat(startIndex), parts.GetFloat(startIndex + 1), parts.GetFloat(startIndex + 2));
    }

    public static int GetInt(this string[] parts, int index, int defaultValue = 0)
    {
        if (parts.Length - 1 < index) return defaultValue;
        return int.TryParse(parts[index].Trim(), out int x) ? x : 0;
    }
    
    public static float GetFloat(this string[] parts, int index, float defaultValue = 0f)
    {
        if (parts.Length - 1 < index) return defaultValue;
        return float.TryParse(parts[index].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : defaultValue;
    }

    public static bool GetBool(this string[] parts, int index, bool defaultValue = false)
    {
        if (parts.Length - 1 < index) return defaultValue;
        return bool.TryParse(parts[index].Trim(), out bool result) ? result : defaultValue;
    }
    
    public static string GetString(this string[] parts, int index, string defaultValue = "")
    {
        if (parts.Length - 1 < index) return defaultValue;
        return parts[index].Trim();
    }

    public static string GetStringFrom(this string[] parts, int index, string defaultValue = "")
    {
        if (parts.Length - 1 < index) return defaultValue;
        return string.Join(" ", parts.Skip(index));
    }
    
    private static readonly MethodInfo LoadImage = AccessTools.Method(typeof(ImageConversion), nameof(ImageConversion.LoadImage), new [] { typeof(Texture2D), typeof(byte[]) });
    public static bool LoadImage4x(this Texture2D tex, byte[] data)
    {
        return (bool)LoadImage.Invoke(null, new object[] { tex , data});
    }

    public static string StripCitations(this string s)
    {
        return s.Replace("\"", string.Empty);
    }
}