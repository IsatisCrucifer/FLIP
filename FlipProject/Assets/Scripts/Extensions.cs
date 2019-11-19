﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
	#region Integer extensions
	public static bool IsInRange<T>(this T value, T lower, T upper) where T : IComparable
	{
		return value.CompareTo(lower) >= 0 && value.CompareTo(upper) <= 0;
	}

	public static int IncrementModM(this int value, int modulus)
	{
		return (value + 1) % modulus;
	}

	public static int DecrementModM(this int value, int modulus)
	{
		return (value + modulus - 1) % modulus;
	}
	#endregion

	// Extend Unity Vector2/3/4 so one can only "change" one component of the vector
	// 
	// From https://answers.unity.com/questions/600421/how-to-change-xyz-values-in-a-vector3-properly-in.html
	#region Vector234 Extensions
	// Vector2
	public static Vector2 SetX(this Vector2 aVec, float aXValue)
	{
		aVec.x = aXValue;
		return aVec;
	}
	public static Vector2 SetY(this Vector2 aVec, float aYValue)
	{
		aVec.y = aYValue;
		return aVec;
	}

	// Vector3
	public static Vector3 SetX(this Vector3 aVec, float aXValue)
	{
		aVec.x = aXValue;
		return aVec;
	}
	public static Vector3 SetY(this Vector3 aVec, float aYValue)
	{
		aVec.y = aYValue;
		return aVec;
	}
	public static Vector3 SetZ(this Vector3 aVec, float aZValue)
	{
		aVec.z = aZValue;
		return aVec;
	}

	// Vector4
	public static Vector4 SetX(this Vector4 aVec, float aXValue)
	{
		aVec.x = aXValue;
		return aVec;
	}
	public static Vector4 SetY(this Vector4 aVec, float aYValue)
	{
		aVec.y = aYValue;
		return aVec;
	}
	public static Vector4 SetZ(this Vector4 aVec, float aZValue)
	{
		aVec.z = aZValue;
		return aVec;
	}
	public static Vector4 SetW(this Vector4 aVec, float aWValue)
	{
		aVec.w = aWValue;
		return aVec;
	}
	#endregion
}