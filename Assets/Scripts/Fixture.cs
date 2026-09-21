using System;
using UnityEngine;

public class Fixture : MonoBehaviour
{
  [SerializeField] Transform pan_ = null;
  [SerializeField] Transform tilt_ = null;
  [SerializeField] Renderer lens_ = null;
  [SerializeField] Light light_ = null;
  [SerializeField] GameObject beam_ = null;
  [SerializeField] int index_ = 0;

  Material lens_mat_ = null;
  Material beam_mat_ = null;

  void Awake()
  {
    lens_mat_ = lens_.material;
    beam_mat_ = beam_.GetComponent<Renderer>().material;
    SetDefaults();
  }

  void SetDefaults()
  {
    intensity = 0.0f;
    color = Color.white;
    pan = 0.0f;
    tilt = -90.0f;
    zoom = 30.0f;
  }

  public void RecvDMX(Settings settings, UInt16 universe, byte[] values)
  {
    float v = 0;
    if (settings.FromDMX(index_, universe, settings.intensity, 0, 1, settings.intensity16Bit, values, ref v))
      intensity = v;

    Color c = Color.black;
    if (settings.ColorFromDMX(index_, universe, settings.color1, settings.color2, settings.color3, settings.rgb, settings.color16Bit, values, ref c))
      color = c;

    if (settings.FromDMX(index_, universe, settings.pan, settings.panMin, settings.panMax, settings.pan16Bit, values, ref v))
      pan = v;

    if (settings.FromDMX(index_, universe, settings.tilt, settings.tiltMin, settings.tiltMax, settings.tilt16Bit, values, ref v))
      tilt = v - 90;

    if (settings.FromDMX(index_, universe, settings.zoom, settings.zoomMin, settings.zoomMax, false, values, ref v))
      zoom = v;
  }

  public float intensity
  {
    get { return light_.intensity; }

    set
    {
      light_.intensity = value;
      beam_mat_.SetFloat("_Opacity", Mathf.Lerp(0, 0.3f, value));
    }
  }

  public Color color
  {
    get { return light_.color; }

    set
    {
      light_.color = value;
      beam_mat_.SetColor("_Color", value);
      lens_mat_.SetColor("_BaseColor", value * intensity);
    }
  }

  public float pan
  {
    get { return pan_.localEulerAngles.z; }
    set { pan_.localEulerAngles = new Vector3(0, 0, value); }
  }

  public float tilt
  {
    get { return tilt_.localEulerAngles.x; }
    set { tilt_.localEulerAngles = new Vector3(value, 0, 0); }
  }

  public float zoom
  {
    get { return light_.spotAngle; }

    set
    {
      const float Scale = 8.25f / 30;

      light_.spotAngle = value;
      light_.innerSpotAngle = value;
      beam_.transform.localScale = new Vector3(value * Scale, 15.0f, value * Scale);
    }
  }
}
