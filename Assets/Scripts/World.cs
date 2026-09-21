using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class World : MonoBehaviour
{
  [SerializeField] Engine engine_;
  [SerializeField] GameObject settings_;
  [SerializeField] Camera ray_cam_;
  [SerializeField] TransparentWindow window_;

  Camera cam_ = null;
  Vector3 cam_origin_ = Vector3.zero;
  Quaternion rotation_ = Quaternion.identity;
  Vector3 scale_ = Vector3.one;
  float time_ = 0;
  int fixture_count_ = 0;

  void Start()
  {
    cam_ = Camera.main;
    cam_origin_ = cam_.transform.localPosition;

    rotation_ = transform.localRotation;
    scale_ = transform.localScale;

    List<Fixture> fixtures = engine_.fixtures;
    foreach (Fixture fixture in fixtures)
    {
      Vector3 pos = fixture.transform.localPosition;
      fixture.transform.localPosition = pos + new Vector3(0, 2, 0);
      fixture.gameObject.SetActive(false);
      ++fixture_count_;
      StartCoroutine(TransformInTime(fixture.transform, pos, fixture.transform.localRotation, fixture.transform.localScale, 1.5f + fixture_count_ * 0.1f, 1.5f));
    }
  }

  IEnumerator TransformInTime(Transform t, Vector3 endPos, Quaternion endRot, Vector3 endScale, float delay, float duration)
  {
    float elapsed = 0;
    Vector3 startPos = t.localPosition;
    Quaternion startRot = t.localRotation;
    Vector3 startScale = t.localScale;

    if (delay > 0)
      yield return new WaitForSeconds(delay);

    t.gameObject.SetActive(true);

    for (; ; )
    {
      elapsed += Time.deltaTime;
      if (elapsed >= duration)
        break;

      float percent = 1 - Mathf.Pow(1 - elapsed / duration, 4.0f);
      t.localPosition = Vector3.Lerp(startPos, endPos, percent);
      t.localRotation = Quaternion.Lerp(startRot, endRot, percent);
      t.localScale = Vector3.Lerp(startScale, endScale, percent);
      yield return null;
    }

    t.localPosition = endPos;
    t.localRotation = endRot;
    t.localScale = endScale;

    if (--fixture_count_ == 0)
      engine_.scan_on = true;
  }

  void Update()
  {
    Vector2 mouse_pos = new Vector2(-1, -1);
    if (window_.Tick(ref mouse_pos))
      engine_.ui_.visible = true;

    if (!settings_.activeSelf)
    {
      // zoom in
      Vector3 pos_target = cam_origin_;
      Vector3 look_target = Vector3.zero;
      float fov_target = 60;

      if (engine_.settings.zoomy && window_.allow_mouse)
      {
        Rect screen = new Rect(0, 0, Screen.width, Screen.height);
        if (screen.Contains(mouse_pos))
        {
          Ray ray = ray_cam_.ScreenPointToRay(mouse_pos);
          Plane ground = new Plane(Vector3.up, Vector3.zero);
          float enter = 0;
          if (ground.Raycast(ray, out enter))
          {
            look_target = ray.GetPoint(enter);

            // keep hit point within 10 m of origin
            Vector3 dir = look_target - Vector3.zero;
            if (dir.magnitude > 5)
              look_target = dir.normalized * 5;

            fov_target = 20;
            pos_target = cam_origin_ + cam_.transform.forward * 6;
          }
        }
      }

      const float MovementSpeed = 2;
      cam_.transform.localPosition = Vector3.Lerp(cam_.transform.localPosition, pos_target, Time.deltaTime);
      Quaternion target_rotation = Quaternion.LookRotation(look_target - cam_.transform.position);
      cam_.transform.localRotation = Quaternion.Slerp(cam_.transform.localRotation, target_rotation, 1f - Mathf.Exp(-MovementSpeed * Time.deltaTime));
      cam_.fieldOfView = Mathf.Lerp(cam_.fieldOfView, fov_target, Time.deltaTime);
    }

    time_ += Time.deltaTime;

    Quaternion rotation = rotation_;

    // intro sequence
    const float IntroTime = 5.0f;
    if (time_ < IntroTime)
    {
      float percent = Mathf.Pow(time_ / IntroTime - 1, 4.0f);
      rotation = rotation_ * Quaternion.Euler(0, 60 * percent, 0);
      transform.localScale = Vector3.Lerp(scale_, new Vector3(0.1f, 0.1f, 0.1f), percent);
    }
    else
      transform.localScale = scale_;

    // floating
    if (engine_.settings.floaty)
    {
      const float RotationSpeed = 0.33f;
      const float RotationDegrees = 7.5f;
      const float HoverSpeed = 0.5f;
      const float HoverDistance = 0.3f;

      transform.localRotation = rotation * Quaternion.Euler(0, Mathf.Sin(time_ * RotationSpeed) * RotationDegrees, 0);
      transform.localPosition = new Vector3(0, Mathf.Sin(time_ * HoverSpeed) * HoverDistance, 0);
    }
    else
    {
      transform.localRotation = rotation_;
      transform.localPosition = Vector3.zero;
    }
  }
}
