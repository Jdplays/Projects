﻿using System.Collections;
using MoonSharp.Interpreter;
using UnityEngine;

[MoonSharpUserData]
public static class ModUtils
{
    private static string defaultLogChannel = "Lua";

    public static float Clamp01(float value) 
    {
        return Mathf.Clamp01(value); 
    }

    public static int FloorToInt(float value)
    {
        return Mathf.FloorToInt(value);
    }

    public static float Round(float value, int digits)
    {
        return (float)System.Math.Round((double)value, digits);
    }

    public static void Log(object obj) 
    {
        Debug.Log(obj);
    }

    public static void LogWarning(object obj) 
    {
        Debug.LogWarning(obj);
    }

    public static void LogError(object obj) 
    {
        Debug.LogError(obj);
    }

    public static void ULogChannel(string channel, string message)
    {
        Debug.ULogChannel(channel, message);
    }

    public static void ULogWarningChannel(string channel, string message)
    {
        Debug.ULogWarningChannel(channel, message);
    }

    public static void ULogErrorChannel(string channel, string message)
    {
        Debug.ULogErrorChannel(channel, message);
    }

    public static void ULog(string message)
    {
        Debug.ULogChannel(defaultLogChannel, message);
    }

    public static void ULogWarning(string message)
    {
        Debug.ULogWarningChannel(defaultLogChannel, message);
    }

    public static void ULogError(string message)
    {
        Debug.ULogErrorChannel(defaultLogChannel, message);
    }

    public static float Clamp(float value, float min, float max)
    {
        return value.Clamp(min, max);
    }

    public static int Min(int a, int b)
    {
        return Mathf.Min(a, b);
    }

    public static int Max(int a, int b)
    {
        return Mathf.Max(a, b);
    }
}
