using UnityEngine;

public class Flare : MonoBehaviour
{
  [SerializeField] Light light_ = null;

  const float MinOpacity = 0.0f;
  const float MaxOpacity = 0.6f;
  const float MinScale = 1.0f;
  const float MaxScale = 20.0f;
  const float MinRotation = 0.0f;
  const float MaxRotation = 30.0f;

  private Transform cam_;
  private Material mat_ = null;

  void Start()
  {
    cam_ = Camera.main.transform;
    mat_ = GetComponent<Renderer>().material;
  }

  void LateUpdate()
  {
    transform.parent.LookAt(cam_); // billboard

    float angle = 180.0f - Vector3.Angle(cam_.transform.forward, light_.transform.forward);
    float viewAnglePercent = Mathf.Pow(Mathf.InverseLerp(90.0f, 0, angle), 2.0f);
    float power = viewAnglePercent * light_.intensity;
    float scale = Mathf.Lerp(MinScale, MaxScale, power);
    float rotation = Mathf.Lerp(MinRotation, MaxRotation, viewAnglePercent);
    Color color = light_.color;
    color.a = Mathf.Lerp(MinOpacity, MaxOpacity, power);
    mat_.color = color;
    transform.localScale = Vector3.one * scale;
    transform.localEulerAngles = new Vector3(0, 0, rotation);
  }
}
