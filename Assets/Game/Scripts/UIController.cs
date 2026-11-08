using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public TMP_FontAsset typeFont, signFont;
    Canvas canvas;
    Image fade;
    TMP_Text center, toast, hint;
    GameObject menu, pause, credits;
    TMP_Text creditsText;
    public static readonly Color Paper = new Color(0.92f, 0.88f, 0.8f);
    public static readonly Color Accent = new Color(0.86f, 0.62f, 0.2f);

    void Awake()
    {
        if (!FindFirstObjectByType<EventSystem>())
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        var cgo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cgo.transform.SetParent(transform, false);
        canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
        var sc = cgo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;

        toast = Txt(cgo.transform, "Toast", signFont, 54, TextAlignmentOptions.Top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -90), new Vector2(0, 120));
        toast.characterSpacing = 18; toast.alpha = 0;
        hint = Txt(cgo.transform, "Hint", typeFont, 30, TextAlignmentOptions.Bottom, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 70), new Vector2(0, 100));
        hint.alpha = 0;

        fade = new GameObject("Fade", typeof(Image)).GetComponent<Image>();
        fade.transform.SetParent(cgo.transform, false); Stretch(fade.rectTransform); fade.color = Color.black; fade.raycastTarget = false;
        center = Txt(cgo.transform, "Center", typeFont, 46, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        center.rectTransform.offsetMin = new Vector2(200, 100); center.rectTransform.offsetMax = new Vector2(-200, -100);
        center.lineSpacing = 20; center.text = "";

        BuildMenu(cgo.transform);
        BuildPause(cgo.transform);
        BuildCredits(cgo.transform);
        pause.SetActive(false); credits.SetActive(false);
    }

    // ---------- builders
    static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
    TMP_Text Txt(Transform p, string name, TMP_FontAsset f, float size, TextAlignmentOptions al, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sizeDelta)
    {
        var t = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.transform.SetParent(p, false);
        t.font = f; t.fontSize = size; t.alignment = al; t.color = Paper; t.raycastTarget = false;
        t.rectTransform.anchorMin = amin; t.rectTransform.anchorMax = amax; t.rectTransform.anchoredPosition = pos; t.rectTransform.sizeDelta = sizeDelta;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }
    GameObject Panel(Transform p, string name, float alpha)
    {
        var g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(p, false); Stretch((RectTransform)g.transform);
        g.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, alpha);
        return g;
    }
    Button Btn(Transform p, string label, Vector2 pos, UnityAction a)
    {
        var g = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        g.transform.SetParent(p, false);
        var rt = (RectTransform)g.transform; rt.sizeDelta = new Vector2(420, 74); rt.anchoredPosition = pos;
        var img = g.GetComponent<Image>(); img.color = new Color(1, 1, 1, 0.04f);
        var b = g.GetComponent<Button>();
        var cb = b.colors; cb.normalColor = new Color(1, 1, 1, 1); cb.highlightedColor = new Color(0.86f, 0.62f, 0.2f, 4f); cb.pressedColor = Accent; cb.selectedColor = Color.white; cb.colorMultiplier = 3; b.colors = cb;
        b.onClick.AddListener(a);
        var t = Txt(g.transform, "Label", signFont, 34, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        t.text = label; t.characterSpacing = 14;
        var line = new GameObject("Line", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        line.transform.SetParent(g.transform, false); line.color = new Color(0.86f, 0.62f, 0.2f, 0.6f);
        line.rectTransform.anchorMin = new Vector2(0.25f, 0); line.rectTransform.anchorMax = new Vector2(0.75f, 0); line.rectTransform.sizeDelta = new Vector2(0, 2); line.raycastTarget = false;
        return b;
    }
    Slider Sld(Transform p, string label, Vector2 pos, float min, float max, float val, UnityAction<float> a)
    {
        var root = new GameObject(label, typeof(RectTransform)); root.transform.SetParent(p, false);
        ((RectTransform)root.transform).anchoredPosition = pos; ((RectTransform)root.transform).sizeDelta = new Vector2(620, 60);
        var lt = Txt(root.transform, "L", typeFont, 26, TextAlignmentOptions.Left, new Vector2(0, 0), new Vector2(0.42f, 1), Vector2.zero, Vector2.zero); lt.text = label;
        var s = new GameObject("Slider", typeof(RectTransform), typeof(Slider)); s.transform.SetParent(root.transform, false);
        var srt = (RectTransform)s.transform; srt.anchorMin = new Vector2(0.45f, 0.35f); srt.anchorMax = new Vector2(1, 0.65f); srt.offsetMin = srt.offsetMax = Vector2.zero;
        var bg = new GameObject("BG", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); bg.transform.SetParent(s.transform, false); Stretch(bg.rectTransform); bg.color = new Color(1, 1, 1, 0.15f);
        var fa = new GameObject("FillArea", typeof(RectTransform)); fa.transform.SetParent(s.transform, false); Stretch((RectTransform)fa.transform);
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); fill.transform.SetParent(fa.transform, false); fill.color = Accent; Stretch(fill.rectTransform);
        var ha = new GameObject("HandleArea", typeof(RectTransform)); ha.transform.SetParent(s.transform, false); Stretch((RectTransform)ha.transform);
        var h = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); h.transform.SetParent(ha.transform, false); h.color = Paper; h.rectTransform.sizeDelta = new Vector2(16, 34);
        var sl = s.GetComponent<Slider>(); sl.fillRect = fill.rectTransform; sl.handleRect = h.rectTransform; sl.targetGraphic = h;
        sl.minValue = min; sl.maxValue = max; sl.value = val; sl.onValueChanged.AddListener(a);
        return sl;
    }

    void BuildMenu(Transform root)
    {
        menu = Panel(root, "Menu", 0.55f);
        var vignette = menu.GetComponent<Image>();
        var title = Txt(menu.transform, "Title", typeFont, 112, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -260), new Vector2(1600, 200));
        title.text = "NOTHING HAS SHIFTED"; title.characterSpacing = 6; title.color = Paper;
        var sub = Txt(menu.transform, "Sub", signFont, 30, TextAlignmentOptions.Center, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -370), new Vector2(1400, 60));
        sub.text = "C-BLOCK GENTS HOSTEL  ·  2:47 AM  ·  MONSOON"; sub.characterSpacing = 20; sub.color = Accent;
        Btn(menu.transform, "ENTER THE CORRIDOR", new Vector2(0, -20), () => GameManager.I.StartGame());
        Sld(menu.transform, "Mouse sensitivity", new Vector2(0, -130), 0.3f, 6f, PlayerController.Sensitivity, v => { PlayerController.Sensitivity = v; PlayerPrefs.SetFloat("sens", v); });
        Sld(menu.transform, "Volume", new Vector2(0, -200), 0f, 1f, AudioListener.volume, v => { AudioListener.volume = v; PlayerPrefs.SetFloat("vol", v); });
        if (Application.platform != RuntimePlatform.WebGLPlayer) Btn(menu.transform, "QUIT", new Vector2(0, -300), () => Application.Quit());
        var ctl = Txt(menu.transform, "Controls", typeFont, 24, TextAlignmentOptions.Bottom, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 60), new Vector2(0, 80));
        ctl.text = "WASD move   ·   Mouse look   ·   Shift run   ·   Esc pause\n<size=80%><color=#9a8f7a>Headphones recommended. A GitHub Game Off 2026 entry — theme: SHIFT</color></size>";
    }

    void BuildPause(Transform root)
    {
        pause = Panel(root, "Pause", 0.8f);
        var t = Txt(pause.transform, "T", typeFont, 80, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1200, 140));
        t.text = "PAUSED";
        Btn(pause.transform, "RESUME", new Vector2(0, 80), () => GameManager.I.SetPaused(false));
        Sld(pause.transform, "Mouse sensitivity", new Vector2(0, -20), 0.3f, 6f, PlayerController.Sensitivity, v => { PlayerController.Sensitivity = v; PlayerPrefs.SetFloat("sens", v); });
        Sld(pause.transform, "Volume", new Vector2(0, -90), 0f, 1f, AudioListener.volume, v => { AudioListener.volume = v; PlayerPrefs.SetFloat("vol", v); });
        Btn(pause.transform, "MAIN MENU", new Vector2(0, -200), () => GameManager.I.Restart());
        var r = Txt(pause.transform, "Rules", typeFont, 26, TextAlignmentOptions.Center, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 80), new Vector2(-400, 140));
        r.text = "Something shifted?  Turn back — take the stairs behind you.\nNothing shifted?  Keep walking — take the stairs at the far end.";
        r.color = new Color(0.75f, 0.7f, 0.6f);
    }

    void BuildCredits(Transform root)
    {
        credits = Panel(root, "Credits", 1f);
        creditsText = Txt(credits.transform, "T", typeFont, 36, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        creditsText.rectTransform.offsetMin = new Vector2(200, 260); creditsText.rectTransform.offsetMax = new Vector2(-200, -120);
        Btn(credits.transform, "MAIN MENU", new Vector2(0, -400), () => GameManager.I.Restart());
    }

    // ---------- API
    public void ShowMenu(bool v) { menu.SetActive(v); }
    public void ShowPause(bool v) { pause.SetActive(v); }
    public void ShowCredits(string text) { credits.SetActive(true); creditsText.text = text; }
    public void Center(string s) { center.text = s; center.alpha = 1; }

    public IEnumerator FadeTo(float a, float t)
    {
        float s = fade.color.a, e = 0;
        while (e < t) { e += Time.unscaledDeltaTime; fade.color = new Color(0, 0, 0, Mathf.Lerp(s, a, e / t)); yield return null; }
        fade.color = new Color(0, 0, 0, a);
    }

    public IEnumerator Story(string[] lines)
    {
        foreach (var l in lines)
        {
            center.text = l; center.alpha = 0;
            float hold = 1.8f + l.Length * 0.045f, e = 0;
            while (e < 0.9f) { e += Time.unscaledDeltaTime; center.alpha = e / 0.9f; yield return null; }
            e = 0;
            yield return null;
            while (e < hold && !Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space)) { e += Time.unscaledDeltaTime; yield return null; }
            e = 0;
            while (e < 0.6f) { e += Time.unscaledDeltaTime; center.alpha = 1 - e / 0.6f; yield return null; }
        }
        center.text = ""; center.alpha = 1;
    }

    Coroutine toastCo, hintCo;
    public void Toast(string s) { if (toastCo != null) StopCoroutine(toastCo); toastCo = StartCoroutine(FadeText(toast, s, 4f)); }
    public void Hint(string s, float hold = 5f) { if (hintCo != null) StopCoroutine(hintCo); hintCo = StartCoroutine(FadeText(hint, s, hold)); }
    IEnumerator FadeText(TMP_Text t, string s, float hold)
    {
        t.text = s; float e = 0;
        while (e < 1) { e += Time.unscaledDeltaTime; t.alpha = e; yield return null; }
        yield return new WaitForSecondsRealtime(hold);
        e = 0;
        while (e < 1.5f) { e += Time.unscaledDeltaTime; t.alpha = 1 - e / 1.5f; yield return null; }
        t.alpha = 0;
    }
}
