using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

	public static int ModM(this int value, int modulus)
	{
		return (value % modulus + modulus) % modulus;
	}
	#endregion

	#region Common check for Monobehaviors having Inspector public fields
	public static void CheckInspectorConnection<T>(this T controlScript) where T : MonoBehaviour
	{
		// Check only in editor if all references are all set
		if (Application.isPlaying && Application.isEditor)
		{
			bool Check = true;

			// Using reflection to yank out all public instance fields declared here
			foreach (var fi in controlScript.GetType().GetFields(
				System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.Instance |
				System.Reflection.BindingFlags.DeclaredOnly))
			{
				// And is a Unity object
				if (typeof(UnityEngine.Object).IsAssignableFrom(fi.FieldType))
				{
					if (fi.GetValue(controlScript).Equals(null))
					{
						Debug.LogError($"{fi.Name} is not set in {controlScript.gameObject.name}");
						Check = false;
					}
				}
			}

			// If anything is wrong, stop playing
			if (!Check)
			{
				UnityEditor.EditorApplication.isPlaying = false;
			}
		}
	}
	#endregion

	#region IEnumerable Extensions
	// cf. https://stackoverflow.com/questions/20678653/
	public static IEnumerable<T> Slice<T>(this IEnumerable<T> source, int from = 0, int to = 0)
	{
		int srcCount = source.Count();
		if (from < 0) from += srcCount;
		from = Math.Min(from, srcCount);
		if (to == 0)
		{
			return source.Skip(from);
		}
		else
		{
			if (to < 0) to += srcCount;
			to = Math.Min(to, srcCount);
			return source.Take(to).Skip(from);
		}
	}

	public static IEnumerable<T> Subsequence<T>(this IEnumerable<T> source, int from = 0, int count = -1)
	{
		int srcCount = source.Count();
		if (from < 0) from += srcCount;
		from = Math.Min(from, srcCount);
		if (count < 0)
			return source.Skip(from);
		else
			return source.Skip(from).Take(count);
	}
	#endregion

	#region Vector234 Extensions
	// Extend Unity Vector2/3/4 so one can only "change" one component of the vector
	// 
	// From https://answers.unity.com/questions/600421/how-to-change-xyz-values-in-a-vector3-properly-in.html

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
