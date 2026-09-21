using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.Windows;

public class UI : MonoBehaviour
{
  enum Option
  {
    GeneralWidth,
    GeneralHeight,
    GeneralCorner,
    GeneralFloaty,
    GeneralZoomy,
    GeneralChatty,
    GeneralPhrases,
    GeneralRestoreDefaultPhrases,
    ProfileIntensity,
    ProfileIntensity16Bit,
    ProfileColorRGB,
    ProfileColor16Bit,
    ProfileColor1,
    ProfileColor2,
    ProfileColor3,
    ProfilePan,
    ProfilePan16Bit,
    ProfilePanMin,
    ProfilePanMax,
    ProfileTilt,
    ProfileTilt16Bit,
    ProfileTiltMin,
    ProfileTiltMax,
    ProfileZoom,
    ProfileZoomMin,
    ProfileZoomMax,
    Fixture
  }

  enum OptionType
  {
    Input,
    Checkbox,
    Dropdown,
    NormalButton,
    RedButton
  }

  [SerializeField] GameObject settings_;
  [SerializeField] RectTransform content_;
  [SerializeField] Button close_button_;
  [SerializeField] Button quit_button_;
  [SerializeField] TMP_Dropdown dropdown_;
  [SerializeField] Button normal_button_;
  [SerializeField] Button red_button_;
  [SerializeField] Speech speech_;

  const float row_height_ = 40f;
  const float spacing_ = 8f;
  const float label_width_ = 100f;
  const float dropdown_width_ = 200f;

  bool built_ = false;
  List<RowEntry> rows_ = new List<RowEntry>();

  public UnityEvent OnSettingsChanged;

  class RowEntry
  {
    Option option_;
    TextMeshProUGUI label_ = null;
    Toggle checkbox_ = null;
    TMP_InputField input_ = null;
    TMP_Dropdown dropdown_ = null;
    Button button_ = null;
    Selectable selectable_ = null;

    public RowEntry(Option row_option, TextMeshProUGUI row_label)
    {
      label_ = row_label;
    }

    public RowEntry(Option row_option, Toggle row_checkbox)
    {
      option_ = row_option;
      checkbox_ = row_checkbox;
      selectable_ = checkbox_;
    }

    public RowEntry(Option row_option, TMP_InputField row_input)
    {
      option_ = row_option;
      input_ = row_input;
      selectable_ = input_;
    }

    public RowEntry(Option row_option, TMP_Dropdown row_dropdown)
    {
      option_ = row_option;
      dropdown_ = row_dropdown;
      selectable_ = dropdown_;
    }

    public RowEntry(Option row_option, Button row_button)
    {
      option_ = row_option;
      button_ = row_button;
      selectable_ = button_;
    }

    public Option option
    {
      get { return option_; }
    }

    public string label
    {
      get { return label_ != null ? label_.text : string.Empty; }
      set
      {
        if (label_ != null)
          label_.text = value;
      }
    }

    public bool isChecked
    {
      get { return checkbox_ != null && checkbox_.isOn; }

      set
      {
        if (checkbox_ != null)
          checkbox_.isOn = value;
      }
    }

    public Toggle checkbox
    {
      get { return checkbox_; }
    }

    public TMP_Dropdown dropdown
    {
      get { return dropdown_; }
    }

    public Button button
    {
      get { return button_; }
    }

    public string value
    {
      get { return input_ != null ? input_.text : string.Empty; }
      set
      {
        if (input_ != null)
          input_.text = value;
      }
    }

    public int index
    {
      get { return dropdown_ != null ? dropdown_.value : 0; }
      set
      {
        if (dropdown_ == null)
          return;

        dropdown_.value = value;
        dropdown_.RefreshShownValue();
      }
    }

    public bool selected
    {
      get
      {
        return selectable_ != null && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == selectable_.gameObject;
      }

      set
      {
        if (!value || selectable_ == null)
          return;

        selectable_.Select();

        if (input_ != null)
          input_.ActivateInputField();
      }
    }
  }

  public bool visible
  {
    get { return settings_.activeSelf; }
    set { settings_.SetActive(value); }
  }

  void Build(int fixtureCount)
  {
    if (built_)
      return;

    content_.anchorMin = new Vector2(0, 1);
    content_.anchorMax = new Vector2(1, 1);
    content_.pivot = new Vector2(0.5f, 1);

    VerticalLayoutGroup vlg = content_.GetComponent<VerticalLayoutGroup>();
    if (vlg == null)
      vlg = content_.gameObject.AddComponent<VerticalLayoutGroup>();
    vlg.spacing = spacing_;
    vlg.padding = new RectOffset(10, 10, 10, 10);
    vlg.childControlWidth = true;
    vlg.childControlHeight = true;
    vlg.childForceExpandWidth = true;
    vlg.childForceExpandHeight = false;

    ContentSizeFitter fitter = content_.GetComponent<ContentSizeFitter>();
    if (fitter == null)
      fitter = content_.gameObject.AddComponent<ContentSizeFitter>();
    fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

    AddHeader("General");

    AddRow(Option.GeneralWidth, "Width");
    AddRow(Option.GeneralHeight, "Height");

    RowEntry row = AddRow(Option.GeneralCorner, "Corner", OptionType.Dropdown);
    if (row.dropdown != null)
    {
      row.dropdown.ClearOptions();
      row.dropdown.AddOptions(new List<string> { "Bottom Right", "Bottom Left", "Top Right", "Top Left" });
      row.dropdown.RefreshShownValue();
    }

    AddRow(Option.GeneralFloaty, "Floaty", OptionType.Checkbox);
    AddRow(Option.GeneralZoomy, "Zoomy", OptionType.Checkbox);
    AddRow(Option.GeneralChatty, "Chatty", OptionType.Checkbox);

    row = AddRow(Option.GeneralPhrases, "Phrases", OptionType.NormalButton);
    if (row.button != null)
    {
      TMP_Text label = row.button.GetComponentInChildren<TMP_Text>();
      if (label != null)
        label.text = "Customize...";
      row.button.onClick.AddListener(OnPhrasesButtonClicked);
    }

    row = AddRow(Option.GeneralRestoreDefaultPhrases, string.Empty, OptionType.RedButton);
    if (row.button != null)
    {
      TMP_Text label = row.button.GetComponentInChildren<TMP_Text>();
      if (label != null)
        label.text = "Restore Defaults";
      row.button.onClick.AddListener(OnRestoreDefaultPhrasesButtonClicked);
    }

    AddHeader("Fixture Profile");

    AddRow(Option.ProfileIntensity, "Intensity");
    AddRow(Option.ProfileIntensity16Bit, "16-bit", OptionType.Checkbox);

    AddGap();

    AddRow(Option.ProfileColorRGB, "RGB/CMY", OptionType.Checkbox);
    AddRow(Option.ProfileColor16Bit, "16-bit", OptionType.Checkbox);
    AddRow(Option.ProfileColor1, string.Empty);
    AddRow(Option.ProfileColor2, string.Empty);
    AddRow(Option.ProfileColor3, string.Empty);

    AddGap();

    AddRow(Option.ProfilePan, "Pan");
    AddRow(Option.ProfilePan16Bit, "16-bit", OptionType.Checkbox);
    AddRow(Option.ProfilePanMin, "Pan Min");
    AddRow(Option.ProfilePanMax, "Pan Max");

    AddGap();

    AddRow(Option.ProfileTilt, "Tilt");
    AddRow(Option.ProfileTilt16Bit, "16-bit", OptionType.Checkbox);
    AddRow(Option.ProfileTiltMin, "Tilt Min");
    AddRow(Option.ProfileTiltMax, "Tilt Max");

    AddGap();

    AddRow(Option.ProfileZoom, "Zoom");
    AddRow(Option.ProfileZoomMin, "Zoom Min");
    AddRow(Option.ProfileZoomMax, "Zoom Max");

    if (fixtureCount > 0)
    {
      AddHeader("sACN Addresses");

      for (int i = 0; i < fixtureCount; i++)
        AddRow(Option.Fixture, "Fixture " + (i + 1));
    }

    TextMeshProUGUI header = AddHeader(Application.productName + " v" + Application.version);
    if (header != null)
    {
      header.fontSize = 18;
      header.color = Color.gray;
    }

    row = GetRow(Option.ProfileColorRGB);
    if (row != null && row.checkbox != null)
      row.checkbox.onValueChanged.AddListener(OnColorTypeChanged);

    close_button_.onClick.AddListener(OnCloseButtonClicked);
    quit_button_.onClick.AddListener(OnQuitButtonClicked);

    built_ = true;
  }

  RowEntry GetRow(Option option)
  {
    foreach (RowEntry row in rows_)
    {
      if (row.option == option)
        return row;
    }

    return null;
  }

  Toggle BuildCheckbox(Transform parent)
  {
    GameObject toggle_go = new GameObject("Checkbox", typeof(RectTransform));
    toggle_go.transform.SetParent(parent, false);

    Toggle toggle = toggle_go.AddComponent<Toggle>();
    ColorBlock colors = toggle.colors;
    colors.normalColor = Color.black;
    colors.selectedColor = new Color(0.68f, 0.43f, 0);
    colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
    colors.fadeDuration = 0.25f;
    toggle.colors = colors;

    LayoutElement toggle_layout = toggle_go.AddComponent<LayoutElement>();
    toggle_layout.preferredWidth = row_height_;
    toggle_layout.preferredHeight = row_height_;
    toggle_layout.flexibleWidth = 0;

    // background
    GameObject bg_go = new GameObject("Background", typeof(RectTransform));
    bg_go.transform.SetParent(toggle_go.transform, false);
    Image bg_image = bg_go.AddComponent<Image>();
    bg_image.color = Color.white;
    RectTransform bg_rect = bg_go.GetComponent<RectTransform>();
    bg_rect.anchorMin = Vector2.zero;
    bg_rect.anchorMax = Vector2.one;
    bg_rect.offsetMin = Vector2.zero;
    bg_rect.offsetMax = Vector2.zero;

    // checkmark
    GameObject check_go = new GameObject("Checkmark", typeof(RectTransform));
    check_go.transform.SetParent(bg_go.transform, false);
    Image checkImage = check_go.AddComponent<Image>();
    checkImage.color = new Color(0.3f, 0.1f, 1.0f);
    RectTransform check_rect = check_go.GetComponent<RectTransform>();
    check_rect.anchorMin = new Vector2(0.2f, 0.2f);
    check_rect.anchorMax = new Vector2(0.8f, 0.8f);
    check_rect.offsetMin = Vector2.zero;
    check_rect.offsetMax = Vector2.zero;

    toggle.targetGraphic = bg_image;
    toggle.graphic = checkImage;

    return toggle;
  }

  TMP_InputField BuildInputField(Transform parent)
  {
    GameObject field_go = new GameObject("InputField", typeof(RectTransform));
    field_go.transform.SetParent(parent, false);

    Image bg = field_go.AddComponent<Image>();
    bg.color = Color.white;

    LayoutElement field_layout = field_go.AddComponent<LayoutElement>();
    field_layout.preferredWidth = label_width_;
    field_layout.flexibleWidth = 0;

    TMP_InputField input = field_go.AddComponent<TMP_InputField>();
    ColorBlock colors = input.colors;
    colors.normalColor = Color.black;
    colors.selectedColor = new Color(0.68f, 0.43f, 0);
    colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
    colors.fadeDuration = 0.25f;
    input.colors = colors;

    GameObject text_area = new GameObject("Text Area", typeof(RectTransform));
    text_area.transform.SetParent(field_go.transform, false);
    RectTransform text_area_rect = text_area.GetComponent<RectTransform>();
    text_area_rect.anchorMin = Vector2.zero;
    text_area_rect.anchorMax = Vector2.one;
    text_area_rect.offsetMin = new Vector2(8, 4);
    text_area_rect.offsetMax = new Vector2(-8, -4);
    text_area.AddComponent<RectMask2D>();

    GameObject text_go = new GameObject("Text", typeof(RectTransform));
    text_go.transform.SetParent(text_area.transform, false);
    TextMeshProUGUI text = text_go.AddComponent<TextMeshProUGUI>();
    text.fontSize = 18f;
    text.color = Color.white;
    text.alignment = TextAlignmentOptions.MidlineLeft;
    RectTransform text_rect = text_go.GetComponent<RectTransform>();
    text_rect.anchorMin = Vector2.zero;
    text_rect.anchorMax = Vector2.one;
    text_rect.offsetMin = Vector2.zero;
    text_rect.offsetMax = Vector2.zero;

    input.textViewport = text_area_rect;
    input.textComponent = text;
    input.selectionColor = new Color(0.3f, 0.1f, 1.0f);

    // hack to force input to generate its caret object
    input.gameObject.SetActive(false);
    input.gameObject.SetActive(true);
    input.pointSize = 21;

    return input;
  }

  TMP_Dropdown BuildDropdown(Transform parent)
  {
    TMP_Dropdown dropdown = Instantiate(dropdown_, parent);

    LayoutElement dropdown_layout = dropdown.gameObject.AddComponent<LayoutElement>();
    dropdown_layout.preferredWidth = dropdown_width_;
    dropdown_layout.flexibleWidth = 0;

    return dropdown;
  }

  Button BuildButton(Button prefab, Transform parent)
  {
    Button button = Instantiate(prefab, parent);

    LayoutElement button_layout = button.gameObject.AddComponent<LayoutElement>();
    button_layout.preferredWidth = dropdown_width_;
    button_layout.flexibleWidth = 0;

    return button;
  }

  void AddGap()
  {
    // row container
    GameObject row = new GameObject($"Gap_{rows_.Count}", typeof(RectTransform));
    row.transform.SetParent(content_, false);

    LayoutElement row_size = row.AddComponent<LayoutElement>();
    row_size.minHeight = row_size.preferredHeight = row_height_ / 2;
  }

  TextMeshProUGUI AddHeader(string labelText)
  {
    // row container
    GameObject row = new GameObject($"Header_{rows_.Count}", typeof(RectTransform));
    row.transform.SetParent(content_, false);

    HorizontalLayoutGroup row_layout = row.AddComponent<HorizontalLayoutGroup>();
    row_layout.spacing = spacing_;
    row_layout.childControlWidth = true;
    row_layout.childControlHeight = true;
    row_layout.childForceExpandWidth = true;
    row_layout.childForceExpandHeight = true;

    LayoutElement row_size = row.AddComponent<LayoutElement>();
    row_size.minHeight = row_height_ * 2;
    row_size.preferredHeight = row_height_ * 2;

    // label
    GameObject label_go = new GameObject("Label", typeof(RectTransform));
    label_go.transform.SetParent(row.transform, false);

    TextMeshProUGUI label = label_go.AddComponent<TextMeshProUGUI>();
    label.text = labelText;
    label.fontSize = 24f;
    label.alignment = TextAlignmentOptions.MidlineLeft;
    label.color = Color.white;
    return label;
  }

  RowEntry AddRow(Option option, string labelText, OptionType type = OptionType.Input)
  {
    // row container
    GameObject row = new GameObject($"Row_{rows_.Count}", typeof(RectTransform));
    row.transform.SetParent(content_, false);

    HorizontalLayoutGroup row_layout = row.AddComponent<HorizontalLayoutGroup>();
    row_layout.spacing = spacing_;
    row_layout.childControlWidth = true;
    row_layout.childControlHeight = true;
    row_layout.childForceExpandWidth = false;
    row_layout.childForceExpandHeight = true;

    LayoutElement row_size = row.AddComponent<LayoutElement>();
    row_size.minHeight = row_height_;
    row_size.preferredHeight = row_height_;

    // label
    GameObject label_go = new GameObject("Label", typeof(RectTransform));
    label_go.transform.SetParent(row.transform, false);

    TextMeshProUGUI label = label_go.AddComponent<TextMeshProUGUI>();
    label.text = labelText;
    label.fontSize = 18f;
    label.alignment = TextAlignmentOptions.MidlineRight;
    label.color = new Color(0.8f, 0.8f, 0.8f);

    LayoutElement label_size = label_go.AddComponent<LayoutElement>();
    label_size.preferredWidth = label_width_;
    label_size.flexibleWidth = 0;

    RowEntry entry = null;
    switch (type)
    {
      case OptionType.Checkbox: entry = new RowEntry(option, BuildCheckbox(row.transform)); break;
      case OptionType.Dropdown: entry = new RowEntry(option, BuildDropdown(row.transform)); break;
      case OptionType.NormalButton: entry = new RowEntry(option, BuildButton(normal_button_, row.transform)); break;
      case OptionType.RedButton: entry = new RowEntry(option, BuildButton(red_button_, row.transform)); break;
      default: entry = new RowEntry(option, BuildInputField(row.transform)); break;
    }

    rows_.Add(entry);
    return entry;
  }

  void UpdateColorLabels()
  {
    RowEntry row = GetRow(Option.ProfileColorRGB);
    bool rgb = row != null && row.isChecked;

    row = GetRow(Option.ProfileColor1);
    if (row != null)
      row.label = rgb ? "Red" : "Cyan";

    row = GetRow(Option.ProfileColor2);
    if (row != null)
      row.label = rgb ? "Green" : "Magenta";

    row = GetRow(Option.ProfileColor3);
    if (row != null)
      row.label = rgb ? "Blue" : "Yellow";
  }

  void OnColorTypeChanged(bool is_checked)
  {
    UpdateColorLabels();
  }

  public void Load(Settings settings)
  {
    Build(settings.addrs.Count);

    RowEntry row = GetRow(Option.GeneralWidth);
    if (row != null)
      row.value = settings.width.ToString();

    row = GetRow(Option.GeneralHeight);
    if (row != null)
      row.value = settings.height.ToString();

    row = GetRow(Option.GeneralCorner);
    if (row != null)
      row.index = settings.corner;

    row = GetRow(Option.GeneralFloaty);
    if (row != null)
      row.isChecked = settings.floaty;

    row = GetRow(Option.GeneralZoomy);
    if (row != null)
      row.isChecked = settings.zoomy;

    row = GetRow(Option.GeneralChatty);
    if (row != null)
      row.isChecked = settings.chatty;

    row = GetRow(Option.ProfileIntensity);
    if (row != null)
      row.value = settings.intensity.ToString();

    row = GetRow(Option.ProfileIntensity16Bit);
    if (row != null)
      row.isChecked = settings.intensity16Bit;

    row = GetRow(Option.ProfileColorRGB);
    if (row != null)
      row.isChecked = settings.rgb;

    row = GetRow(Option.ProfileColor16Bit);
    if (row != null)
      row.isChecked = settings.color16Bit;

    row = GetRow(Option.ProfileColor1);
    if (row != null)
      row.value = settings.color1.ToString();

    row = GetRow(Option.ProfileColor2);
    if (row != null)
      row.value = settings.color2.ToString();

    row = GetRow(Option.ProfileColor3);
    if (row != null)
      row.value = settings.color3.ToString();

    row = GetRow(Option.ProfilePan);
    if (row != null)
      row.value = settings.pan.ToString();

    row = GetRow(Option.ProfilePan16Bit);
    if (row != null)
      row.isChecked = settings.pan16Bit;

    row = GetRow(Option.ProfilePanMin);
    if (row != null)
      row.value = settings.panMin.ToString();

    row = GetRow(Option.ProfilePanMax);
    if (row != null)
      row.value = settings.panMax.ToString();

    row = GetRow(Option.ProfileTilt);
    if (row != null)
      row.value = settings.tilt.ToString();

    row = GetRow(Option.ProfileTilt16Bit);
    if (row != null)
      row.isChecked = settings.tilt16Bit;

    row = GetRow(Option.ProfileTiltMin);
    if (row != null)
      row.value = settings.tiltMin.ToString();

    row = GetRow(Option.ProfileTiltMax);
    if (row != null)
      row.value = settings.tiltMax.ToString();

    row = GetRow(Option.ProfileZoom);
    if (row != null)
      row.value = settings.zoom.ToString();

    row = GetRow(Option.ProfileZoomMin);
    if (row != null)
      row.value = settings.zoomMin.ToString();

    row = GetRow(Option.ProfileZoomMax);
    if (row != null)
      row.value = settings.zoomMax.ToString();

    // load fixtures from settings
    int rowIndex = 0;
    for (int addrIndex = 0; addrIndex < settings.addrs.Count; ++addrIndex)
    {
      for (; rowIndex < rows_.Count; ++rowIndex)
      {
        if (rows_[rowIndex].option != Option.Fixture)
          continue;

        rows_[rowIndex++].value = settings.addrs[addrIndex].ToString();
        break;
      }

      if (rowIndex >= rows_.Count)
        break;
    }

    // clear out remaining fixtures not found in settings
    for (; rowIndex < rows_.Count; ++rowIndex)
    {
      if (rows_[rowIndex].option == Option.Fixture)
        rows_[rowIndex].value = string.Empty;
    }

    UpdateColorLabels();
  }

  void Save()
  {
    Settings settings = new Settings();

    RowEntry row = GetRow(Option.GeneralWidth);
    if (row != null)
      int.TryParse(row.value, out settings.width);

    row = GetRow(Option.GeneralHeight);
    if (row != null)
      int.TryParse(row.value, out settings.height);

    row = GetRow(Option.GeneralCorner);
    if (row != null)
      settings.corner = row.index;

    row = GetRow(Option.GeneralFloaty);
    if (row != null)
      settings.floaty = row.isChecked;

    row = GetRow(Option.GeneralZoomy);
    if (row != null)
      settings.zoomy = row.isChecked;

    row = GetRow(Option.GeneralChatty);
    if (row != null)
      settings.chatty = row.isChecked;

    row = GetRow(Option.ProfileIntensity);
    if (row != null)
      int.TryParse(row.value, out settings.intensity);

    row = GetRow(Option.ProfileIntensity16Bit);
    if (row != null)
      settings.intensity16Bit = row.isChecked;

    row = GetRow(Option.ProfileColorRGB);
    if (row != null)
      settings.rgb = row.isChecked;

    row = GetRow(Option.ProfileColor16Bit);
    if (row != null)
      settings.color16Bit = row.isChecked;

    row = GetRow(Option.ProfileColor1);
    if (row != null)
      int.TryParse(row.value, out settings.color1);

    row = GetRow(Option.ProfileColor2);
    if (row != null)
      int.TryParse(row.value, out settings.color2);

    row = GetRow(Option.ProfileColor3);
    if (row != null)
      int.TryParse(row.value, out settings.color3);

    row = GetRow(Option.ProfilePan);
    if (row != null)
      int.TryParse(row.value, out settings.pan);

    row = GetRow(Option.ProfilePan16Bit);
    if (row != null)
      settings.pan16Bit = row.isChecked;

    row = GetRow(Option.ProfilePanMin);
    if (row != null)
      int.TryParse(row.value, out settings.panMin);

    row = GetRow(Option.ProfilePanMax);
    if (row != null)
      int.TryParse(row.value, out settings.panMax);

    row = GetRow(Option.ProfileTilt);
    if (row != null)
      int.TryParse(row.value, out settings.tilt);

    row = GetRow(Option.ProfileTilt16Bit);
    if (row != null)
      settings.tilt16Bit = row.isChecked;

    row = GetRow(Option.ProfileTiltMin);
    if (row != null)
      int.TryParse(row.value, out settings.tiltMin);

    row = GetRow(Option.ProfileTiltMax);
    if (row != null)
      int.TryParse(row.value, out settings.tiltMax);

    row = GetRow(Option.ProfileZoom);
    if (row != null)
      int.TryParse(row.value, out settings.zoom);

    row = GetRow(Option.ProfileZoomMin);
    if (row != null)
      int.TryParse(row.value, out settings.zoomMin);

    row = GetRow(Option.ProfileZoomMax);
    if (row != null)
      int.TryParse(row.value, out settings.zoomMax);

    foreach (RowEntry r in rows_)
    {
      if (r.option != Option.Fixture)
        continue;

      int addr = 0;
      int.TryParse(r.value, out addr);
      settings.addrs.Add(addr);
    }

    settings.Save();
  }

  void OnPhrasesButtonClicked()
  {
    speech_.RevealFile();
  }

  void OnRestoreDefaultPhrasesButtonClicked()
  {
    speech_.RestoreDefaultPhrases();
  }

  void OnCloseButtonClicked()
  {
    Save();
    visible = false;
    OnSettingsChanged.Invoke();
  }

  void OnQuitButtonClicked()
  {
    Save();

#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
  }

  void Update()
  {
    if (rows_.Count < 1)
      return;

    bool tabPressed = UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame;
    bool spacePressed = UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
    if (!tabPressed && !spacePressed)
      return;

    // find selected
    int selectedIndex = -1;
    if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null)
    {
      for (int index = 0; index < rows_.Count; ++index)
      {
        if (rows_[index].selected)
        {
          selectedIndex = index;
          break;
        }
      }
    }

    if (spacePressed)
    {
      if (selectedIndex >= 0)
      {
        Toggle checkbox = rows_[selectedIndex].checkbox;
        if (checkbox != null)
          checkbox.isOn = !checkbox.isOn;
      }

      return;
    }

    bool shiftDown = UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed || UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed;

    if (shiftDown)
    {
      if (selectedIndex < 0)
        selectedIndex = 0;

      for (int index = selectedIndex - 1; ;)
      {
        if (index < 0)
          index = rows_.Count - 1;

        if (index == selectedIndex)
          break;

        rows_[index].selected = true;
        break;
      }
    }
    else
    {
      if (selectedIndex < 0)
        selectedIndex = rows_.Count - 1;

      for (int index = selectedIndex + 1; ;)
      {
        if (index >= rows_.Count)
          index = 0;

        if (index == selectedIndex)
          break;

        rows_[index].selected = true;
        break;
      }
    }
  }
}
