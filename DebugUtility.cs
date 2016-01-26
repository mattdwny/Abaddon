﻿#define DEBUG

using UnityEngine;
using System;
using System.Diagnostics;

public class DebugUtility
{
	[Conditional("DEBUG")]
	public static void Assert(bool condition, string message)
	{
		if (!condition)
		{
			UnityEngine.Debug.Log(message);
			throw new Exception();
		}
	}

	[Conditional("DEBUG")]
	public static void Print(string message)
	{
		UnityEngine.Debug.Log(message);
	}
}