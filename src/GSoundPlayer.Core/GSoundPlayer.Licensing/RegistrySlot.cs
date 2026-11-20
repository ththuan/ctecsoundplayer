using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace GSoundPlayer.Licensing;

public sealed class RegistrySlot(string name) : IStoreSlot
{
	public const string KeyPath = "Software\\GSoundPlayer";

	public string? Read()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\GSoundPlayer");
			return registryKey?.GetValue(name) as string;
		}
		catch (Exception ex) when ((ex is SecurityException || ex is UnauthorizedAccessException || ex is IOException) ? true : false)
		{
			return null;
		}
	}

	public void Write(string value)
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\GSoundPlayer");
			registryKey.SetValue(name, value);
		}
		catch (Exception ex) when ((ex is SecurityException || ex is UnauthorizedAccessException || ex is IOException) ? true : false)
		{
		}
	}
}
