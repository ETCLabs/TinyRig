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

  class Universe
  {
    public byte priority = 0;
    public byte[] values = null;
    public float timestamp = 0;
  }

  Settings settings_ = null;
  bool sacn_started_ = false;
  bool sacn_packet_received_ = false;
  bool sacn_on_ = false;
  SacnReceiver sacn_ = null;
  readonly object incoming_universes_lock_ = new object();
  Dictionary<UInt16, Universe> incoming_universes_ = new Dictionary<UInt16, Universe>();
  Dictionary<UInt16, Universe> universes_ = new Dictionary<UInt16, Universe>();

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

    lock (incoming_universes_lock_)
    {
      incoming_universes_.Clear();
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

      Universe universe = new Universe();
      universe.priority = packet.FramingLayer.Priority;
      universe.values = packet.DMPLayer.PropertyValues.ToArray();

      lock (incoming_universes_lock_)
      { 
        incoming_universes_[packet.FramingLayer.Universe] = universe;
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

    lock (incoming_universes_lock_)
    {
      foreach (KeyValuePair<UInt16, Universe> incoming in incoming_universes_)
      {
        Universe existing = null;
        if (universes_.TryGetValue(incoming.Key, out existing) && existing.priority > incoming.Value.priority)
        {
          float elapsed = Time.realtimeSinceStartup - existing.timestamp;
          if (elapsed < 2.5f)
            continue;
        }

        incoming.Value.timestamp = Time.realtimeSinceStartup;
        universes_[incoming.Key] = incoming.Value;

        foreach (Fixture fixture in fixtures_)
          fixture.RecvDMX(settings_, incoming.Key, incoming.Value.values);
      }
      incoming_universes_.Clear();
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
