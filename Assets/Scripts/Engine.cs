using Kadmium_sACN.SacnReceiver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.InputSystem;

public class Engine : MonoBehaviour
{
  [SerializeField] TransparentWindow window_ = null;

  [SerializeField] List<Fixture> fixtures_ = new List<Fixture>();
  public UI ui_ = null;

  Settings settings_ = null;
  bool sacn_started_ = false;
  bool sacn_packet_received_ = false;
  bool sacn_on_ = false;
  SacnReceiver sacn_ = null;
  readonly object universes_lock_ = new object();
  Dictionary<UInt16, byte[]> universes_ = new Dictionary<UInt16, byte[]>();

  public List<Fixture> fixtures
  {
    get { return fixtures_; }
  }

  public Settings settings
  {
    get { return settings_; }
  }

  void Awake()
  {
    settings_ = Settings.Load(fixtures_.Count);
    ui_.Load(settings_);
    ui_.OnSettingsChanged.AddListener(OnSettingsChanged);
    window_.Init(settings_.width, settings_.height, settings_.corner);
    StartsACN();
  }

  void StartsACN()
  {
    sacn_started_ = true;
    sacn_ = null;
    sacn_packet_received_ = false;

    lock (universes_lock_)
    {
      universes_.Clear();
    }

    HashSet<ushort> universes = settings_.universes;
    if (universes.Count < 1)
      return;

    Debug.Log("starting sACN...");

    sacn_ = new SacnReceiver();

    sacn_.OnDataPacketReceived += (sender, packet) =>
    {
      if (!sacn_packet_received_)
      {
        Debug.Log("sACN initial packet received from " + packet.FramingLayer.SourceName);
        sacn_packet_received_ = true;
      }

      if (packet.DMPLayer.StartCode != 0)
        return;

      lock (universes_lock_)
      {
        universes_[packet.FramingLayer.Universe] = packet.DMPLayer.PropertyValues.ToArray();
      }
    };

    sacn_.Listen(IPAddress.Any);

    foreach (ushort u in universes)
    {
      Debug.Log("sACN listening on universe " + u + "...");
      sacn_.JoinMulticastGroup(u);
    }
  }

  public bool scan_on
  {
    get { return sacn_on_; }
    set { sacn_on_ = value; }
  }

  void Update()
  {
    if (!sacn_on_)
      return;

    lock (universes_lock_)
    {
      foreach (KeyValuePair<UInt16, byte[]> entry in universes_)
      {
        foreach (Fixture fixture in fixtures_)
          fixture.RecvDMX(settings_, entry.Key, entry.Value);
      }
      universes_.Clear();
    }
  }

  void OnSettingsChanged()
  {
    int old_width = settings_.width;
    int old_height = settings_.height;
    int old_corner = settings_.corner;
    HashSet<ushort> old_universes = settings_.universes;

    settings_ = Settings.Load(fixtures_.Count);

    window_.bottom = true;

    if (settings_.width != old_width || settings_.height != old_height || settings_.corner != old_corner)
      window_.Resize(settings_.width, settings_.height, settings_.corner);

    if (sacn_started_ && !settings_.universes.SetEquals(old_universes))
      StartsACN();
  }
}
