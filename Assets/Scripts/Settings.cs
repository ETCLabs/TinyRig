using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class Settings
{
  public int version = 1;
  public int width = 600;
  public int height = 600;
  public int corner = 0;
  public bool floaty = true;
  public bool zoomy = true;
  public bool chatty = true;
  public int intensity = 53;
  public bool intensity16Bit = true;
  public bool rgb = true;
  public bool color16Bit = false;
  public int color1 = 6;
  public int color2 = 7;
  public int color3 = 8;
  public int pan = 1;
  public bool pan16Bit = true;
  public int panMin = -270;
  public int panMax = 270;
  public int tilt = 3;
  public bool tilt16Bit = true;
  public int tiltMin = -129;
  public int tiltMax = 129;
  public int zoom = 46;
  public int zoomMin = 6;
  public int zoomMax = 64;
  public List<int> addrs = new List<int>();

  public static string path
  {
    get { return Path.Combine(Application.persistentDataPath, "settings.json"); }
  }

  public static Settings Load(int fixtureCount)
  {
    Settings settings = null;

    try
    {
      settings = JsonUtility.FromJson<Settings>(File.ReadAllText(path));
    }
    catch (Exception)
    {
    }

    if (settings == null)
      settings = new Settings();

    // initial defaults
    if (settings.addrs.Count < 1)
    {
      const int kFootprint = 60;

      int addr = 1;
      for (int i = 0; i < fixtureCount; ++i)
      {
        if (addr > 1)
        {
          // do not span universes
          int start_universe = (addr - 1) / 512 + 1;
          int end_universe = (addr + kFootprint - 1) / 512 + 1;
          if (start_universe != end_universe)
            addr = (end_universe - 1) * 512 + 1;
        }

        settings.addrs.Add(addr);
        addr += kFootprint;
      }
    }

    // resize
    while (settings.addrs.Count < fixtureCount)
      settings.addrs.Add(0);

    int excess = settings.addrs.Count - fixtureCount;
    if (excess > 0)
      settings.addrs.RemoveRange(fixtureCount, excess);

    settings.EnforceLimits();

    return settings;
  }

  void EnforceLimits()
  {
    const int MinSize = 100;
    width = Math.Max(MinSize, width);
    height = Math.Max(MinSize, height);
  }

  public void Save()
  {
    EnforceLimits();
    File.WriteAllText(path, JsonUtility.ToJson(this, true));
  }

  static public int addr(int a, bool is16Bit)
  {
    return is16Bit ? a : (a + 1);
  }

  public int maxAddr
  {
    get
    {
      int m = Math.Max(addr(intensity, intensity16Bit), addr(color1, color16Bit));
      m = Math.Max(m, addr(color2, color16Bit));
      m = Math.Max(m, addr(color3, color16Bit));
      m = Math.Max(m, addr(pan, pan16Bit));
      m = Math.Max(m, addr(tilt, tilt16Bit));
      return Math.Max(m, addr(zoom, false));
    }
  }

  static void AddUniverse(int addr, bool is16Bit, HashSet<ushort> universes)
  {
    int universe = (addr - 1) / 512 + 1;
    if (universe < 1 || universe > ushort.MaxValue)
      return;

    universes.Add((ushort)universe);

    if (is16Bit)
      AddUniverse(addr + 1, false, universes);
  }

  public HashSet<ushort> universes
  {
    get
    {
      HashSet<ushort> rtn = new HashSet<ushort>();
      foreach (int addr in addrs)
      {
        AddUniverse(addr + intensity, intensity16Bit, rtn);
        AddUniverse(addr + color1, color16Bit, rtn);
        AddUniverse(addr + color2, color16Bit, rtn);
        AddUniverse(addr + color3, color16Bit, rtn);
        AddUniverse(addr + pan, pan16Bit, rtn);
        AddUniverse(addr + tilt, tilt16Bit, rtn);
        AddUniverse(addr + zoom, false, rtn);
      }
      return rtn;
    }
  }

  public bool FromDMX(int index, int universe, int addr, int minValue, int maxValue, bool is16Bit, byte[] values, ref float value)
  {
    if (index < 0 || index >= addrs.Count || addr < 1 || addrs[index] < 1)
      return false;

    int offset = (addr - 1) + (addrs[index] - 1);
    int param_universe = offset / 512 + 1;
    if (universe != param_universe)
      return false;

    offset -= (universe - 1) * 512;
    if (offset < 0)
      return false;

    if (is16Bit)
    {
      if ((offset + 1) >= values.Length)
        return false;

      UInt16 hi = values[offset];
      UInt16 lo = values[offset + 1];
      value = Mathf.Lerp(minValue, maxValue, ((hi << 8) | lo) / 65535.0f);
      return true;
    }

    if (offset >= values.Length)
      return false;

    value = Mathf.Lerp(minValue, maxValue, values[offset] / 255.0f);
    return true;
  }

  public bool ColorFromDMX(int index, int universe, int addr1, int addr2, int addr3, bool isRGB, bool is16Bit, byte[] values, ref Color color)
  {
    if (!FromDMX(index, universe, addr1, 0, 1, is16Bit, values, ref color.r))
      return false;

    if (!FromDMX(index, universe, addr2, 0, 1, is16Bit, values, ref color.g))
      return false;

    if (!FromDMX(index, universe, addr3, 0, 1, is16Bit, values, ref color.b))
      return false;

    if (!isRGB)
    {
      color.r = 1.0f - color.r;
      color.g = 1.0f - color.g;
      color.b = 1.0f - color.b;
    }

    return true;
  }
}
