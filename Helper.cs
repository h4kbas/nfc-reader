using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static class Helper
{
	public static IEnumerable<string> SplitByLength(this string str, int maxLength)
	{
		int index = 0;
		while (true)
		{
			if (index + maxLength >= str.Length)
			{
				yield return str.Substring(index);
				yield break;
			}
			yield return str.Substring(index, maxLength);
			index += maxLength;
		}
	}
	public static Byte[] Decode(this Byte[] bytes)
	{
		if (bytes.Length == 0) return bytes;
		var i = bytes.Length - 1;
		while (bytes[i] == 0 || bytes[i] == 144)
		{
			i--;
		}
		Byte[] copy = new Byte[i + 1];
		Array.Copy(bytes, copy, i + 1);
		return copy;
	}
	
	public static string Base64Encode(string plainText)
	{
		var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
		return System.Convert.ToBase64String(plainTextBytes);
	}
	
	public static string Base64Decode(string base64EncodedData)
	{
		var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
		return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
	}
}

