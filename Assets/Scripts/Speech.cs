using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class Speech : MonoBehaviour
{
  [SerializeField] Engine engine_;
  [SerializeField] Transform head_;
  [SerializeField] TextMeshProUGUI text_;
  [SerializeField] RectTransform text_rect_;
  [SerializeField] RectTransform bg_rect_;
  [SerializeField] UnityEngine.UI.Image bg_;
  [SerializeField] RectTransform screen_rect_;
  [SerializeField] TextAsset file_;

  Camera cam_ = null;
  List<string> phrases_ = new List<string>();
  float opacity_ = 0;
  float elapsed_ = 0;
  bool show_ = false;
  FileSystemWatcher watcher_ = null;
  bool watcher_dirty_ = false;

  const float kTimeBetweenPhrases = 30.0f;
  const float kTimeToLeavePhraseOnScreen = 5.0f;
  const float kFadeSpeed = 1.5f;

  public static string folder_path
  {
    get { return Application.persistentDataPath; }
  }

  public static string file_name
  {
    get { return "phrases.txt"; }
  }

  public static string file_path
  {
    get { return Path.Combine(folder_path, file_name); }
  }

  public void RevealFile()
  {
    WriteDefaultFileIfNotExists();

    string native_path = file_path.Replace('/', Path.DirectorySeparatorChar);

    if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
    {
      UnityEngine.Debug.Log("Revealing Phrases File: " + native_path);
      Process.Start("explorer.exe", "/select,\"" + native_path + "\"");
    }
    else if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
    {
      UnityEngine.Debug.Log("Revealing Phrases File: " + native_path);
      Process.Start("open", "-R \"" + native_path + "\"");
    }
  }

  public void RestoreDefaultPhrases()
  {
    UnityEngine.Debug.Log("Deleting Phrases File: " + file_path);
    File.Delete(file_path);
  }

  void SetPhrases(string[] lines)
  {
    phrases_.Clear();

    foreach (string line in lines)
    {
      if (line.Length > 0)
        phrases_.Add(line);
    }
  }

  void WriteDefaultFileIfNotExists()
  {
    if (!File.Exists(file_path))
    {
      UnityEngine.Debug.Log("Writing Default Phrases File: " + file_path);
      File.WriteAllText(file_path, file_.text);
    }
  }

  public void LoadPhrases(float show_in_secs = 10)
  {
    // if phrases.txt file does not exist, create it from the default phrases, so users can edit it
    watcher_.EnableRaisingEvents = false;
    WriteDefaultFileIfNotExists();
    watcher_.EnableRaisingEvents = true;

    // if phrases.txt exists, load it, user may have edited it
    if (File.Exists(file_path))
    {
      UnityEngine.Debug.Log("Loading Phrases File: " + file_path);
      SetPhrases(File.ReadAllLines(file_path));
    }

    // if no phrases loaded, fall back to built-in default phrases resource
    if (phrases_.Count < 1)
    {
      UnityEngine.Debug.Log("Falling Back To Built-In Phrases: " + file_path);
      SetPhrases(file_.text.Split('\n'));
    }

    elapsed_ = kTimeBetweenPhrases - show_in_secs;
  }

  void Start()
  {
    cam_ = Camera.main;

    watcher_ = new FileSystemWatcher(folder_path);
    watcher_.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
    watcher_.Changed += OnFileChanged;
    watcher_.Created += OnFileChanged;
    watcher_.Deleted += OnFileChanged;
    watcher_.EnableRaisingEvents = true;

    LoadPhrases();
  }

  private void OnDestroy()
  {
    watcher_?.Dispose();
  }

  void OnFileChanged(object sender, FileSystemEventArgs e)
  {

    if (e.Name != file_name)
      return;

    UnityEngine.Debug.Log("Phrases File Changed: " + e.ChangeType);
    watcher_dirty_ = true;
  }

  void LateUpdate()
  {
    if (watcher_dirty_)
    {
      LoadPhrases(/*show_in_secs*/ 0);
      watcher_dirty_ = false;
    }

    // update show state
    if (engine_.settings.chatty)
    {
      elapsed_ += Time.deltaTime;

      if (show_)
      {
        if (elapsed_ >= kTimeToLeavePhraseOnScreen)
        {
          show_ = false;
          elapsed_ = 0;
        }
      }
      else if (elapsed_ >= kTimeBetweenPhrases)
      {
        text_.text = phrases_[UnityEngine.Random.Range(0, phrases_.Count)];
        show_ = true;
        elapsed_ = 0;
      }
    }
    else
      show_ = false;

    // fade in/out
    if (show_)
    {
      opacity_ = Mathf.Clamp01(opacity_ + Time.deltaTime * kFadeSpeed);
      text_.gameObject.SetActive(true);
      bg_.gameObject.SetActive(true);
    }
    else
    {
      opacity_ -= Mathf.Min(1.0f, Time.deltaTime * kFadeSpeed);

      if (opacity_ <= 0)
      {
        opacity_ = 0;
        text_.gameObject.SetActive(false);
        bg_.gameObject.SetActive(false);
      }
    }

    UpdateText();
  }

  void UpdateText()
  {
    if (!text_.gameObject.activeSelf)
      return;

    const float kMinSize = 200;
    const float kWorldOffset = 0.4f;
    const float kRise = 10.0f;
    const float kMargin = 10.0f;

    Vector3 pos = Camera.main.WorldToScreenPoint(head_.position + cam_.transform.right * kWorldOffset);
    text_.transform.position = pos;

    text_rect_.anchoredPosition = new Vector2(text_rect_.anchoredPosition.x, text_rect_.anchoredPosition.y - (1 - opacity_) * kRise);

    float w = screen_rect_.rect.width - text_rect_.anchoredPosition.x;
    float h = -text_rect_.anchoredPosition.y;
    text_rect_.sizeDelta = new Vector2(Mathf.Max(kMinSize, w), Mathf.Max(kMinSize, h));
    text_.alpha = opacity_;

    bg_rect_.anchoredPosition = text_rect_.anchoredPosition + new Vector2(-kMargin, -kMargin);
    bg_rect_.sizeDelta = text_.GetRenderedValues() + new Vector2(kMargin * 2, kMargin * 2);
    bg_.color = new Color(0, 0, 0, opacity_ * 0.5f);
  }
}
