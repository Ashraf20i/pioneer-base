// Pioneer Base 3D - PioneerGame.UI.cs
// Mission-control style interface: HUD, dialogs, the module card, PIP's bubble, banners, letterbox and timeline.
// Look: dark glass panels with hairline borders, small monospaced labels (telemetry), one cyan accent,
// amber for warnings, green for good news. Fonts: Inter (text) and Roboto Mono (numbers and labels).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using PioneerBase;

public partial class PioneerGame
{
    // palette
    static readonly Color UiBg = new Color(0.035f, 0.055f, 0.09f, 0.86f);
    static readonly Color UiBg2 = new Color(0.05f, 0.08f, 0.13f, 0.96f);
    static readonly Color UiLine = new Color(0.55f, 0.75f, 1f, 0.22f);
    static readonly Color UiAccent = new Color(0.35f, 0.82f, 1f);
    static readonly Color UiAmber = new Color(1f, 0.72f, 0.28f);
    static readonly Color UiGood = new Color(0.24f, 0.86f, 0.59f);
    static readonly Color UiBad = new Color(1f, 0.38f, 0.36f);
    static readonly Color UiText = new Color(0.91f, 0.95f, 1f);
    static readonly Color UiMuted = new Color(0.58f, 0.66f, 0.77f);

    Font fontBold, fontMono;
    Sprite sprRound, sprOutline, sprFade, sprDot;

    Canvas canvas; CanvasGroup hudGroup;
    Text solText, siteText, crateText, alertText, infoText, rationTxt, actTxt;
    Button endBtn;
    class Gauge { public Text name, label; public Image fill; public bool low; }
    readonly Dictionary<string, Gauge> gauges = new Dictionary<string, Gauge>();
    GameObject modalRoot; Text modalTitle, modalBody, modalKicker, modalFooter; RectTransform modalButtons, modalOptions, modalBox;
    GameObject cardRoot; Text cardTitle, cardBody, cardBtnTxt; Button cardBtn; Mod cardMod;
    GameObject pipRoot; Text pipText; Image pipDot; string pipFull = ""; float pipShown, pipHide; readonly Queue<KeyValuePair<string, int>> pipQueue = new Queue<KeyValuePair<string, int>>();
    Image flashImg; Color flashCol; float flashT, flashDur;
    CanvasGroup bannerGroup; Text bannerTitle, bannerSub, bannerKicker; float bannerT; Color bannerCol;
    class BannerItem { public string title, sub; public Color col; }
    readonly Queue<BannerItem> bannerQueue = new Queue<BannerItem>();
    RectTransform barTop, barBot; float letter, letterTarget;
    Image[] tlCell, tlLow, tlTop, tlCard; float solPop;

    // ------------------------------------------------------------------ fonts and sprites
    void LoadFonts()
    {
        font = Resources.Load<Font>("Fonts/Inter-Regular");
        fontBold = Resources.Load<Font>("Fonts/Inter-Bold");
        fontMono = Resources.Load<Font>("Fonts/RobotoMono-Regular");
        Font builtin = null;
        try { builtin = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { }
        if (builtin == null) { try { builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { } }
        if (font == null) font = builtin; if (fontBold == null) fontBold = font; if (fontMono == null) fontMono = font;
    }
    void MakeUiSprites()
    {
        sprRound = RoundSprite(64, 14f, 0f);
        sprOutline = RoundSprite(64, 14f, 1.6f);
        sprDot = RoundSprite(32, 15.5f, 0f);
        var t = new Texture2D(64, 4, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < 64; x++) { float k = 1f - Mathf.Abs(x / 63f * 2f - 1f); for (int y = 0; y < 4; y++) t.SetPixel(x, y, new Color(1, 1, 1, Mathf.SmoothStep(0, 1, k))); }
        t.Apply(); sprFade = Sprite.Create(t, new Rect(0, 0, 64, 4), new Vector2(0.5f, 0.5f), 100);
    }
    // rounded rectangle (filled, or only the outline when border > 0), 9-sliced so corners keep their size
    Sprite RoundSprite(int n, float r, float border)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float px = x + 0.5f, py = y + 0.5f;
            float cx = Mathf.Clamp(px, r, n - r), cy = Mathf.Clamp(py, r, n - r);
            float d = Vector2.Distance(new Vector2(px, py), new Vector2(cx, cy)) - r;     // <0 inside
            float inside = Mathf.Clamp01(0.5f - d);
            float a = border > 0 ? inside * Mathf.Clamp01(d + border + 0.5f) : inside;
            t.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        t.Apply();
        float b = Mathf.Ceil(r) + 1;
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
    }

    // ------------------------------------------------------------------ helpers
    RectTransform Panel(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color col)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = col; return rt;
    }
    Image Img(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color col)
    {
        var im = Panel(name, parent, aMin, aMax, col).GetComponent<Image>(); im.raycastTarget = false; return im;
    }
    // glass card: rounded fill, hairline outline
    RectTransform Card(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color fill, Color? line = null)
    {
        var rt = Panel(name, parent, aMin, aMax, fill); var im = rt.GetComponent<Image>(); im.sprite = sprRound; im.type = Image.Type.Sliced;
        var o = Img("Line", rt, Vector2.zero, Vector2.one, line ?? UiLine); o.sprite = sprOutline; o.type = Image.Type.Sliced;
        return rt;
    }
    Text TextUI(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size, TextAnchor anchor, Color col, Font f = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = new Vector2(8, 2); rt.offsetMax = new Vector2(-8, -2);
        var t = go.GetComponent<Text>(); t.font = f ?? font; t.fontSize = size; t.alignment = anchor; t.color = col; t.lineSpacing = 1.08f;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
        return t;
    }
    static void Pad(Text t, float l, float r) { var rt = (RectTransform)t.transform; rt.offsetMin = new Vector2(l, rt.offsetMin.y); rt.offsetMax = new Vector2(-r, rt.offsetMax.y); }

    // style 0 = primary (accent fill), 1 = ghost (dark glass, outline), 2 = answer option (letter badge, left aligned)
    Button ButtonUI(string name, Transform parent, string label, Action onClick, int fontSize = 18, int style = 1, string badge = null, Color? tint = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.sprite = sprRound; img.type = Image.Type.Sliced;
        Color acc = tint ?? UiAccent;
        img.color = style == 0 ? acc : new Color(0.09f, 0.13f, 0.2f, 0.95f);
        var b = go.GetComponent<Button>(); b.targetGraphic = img;
        var cs = b.colors; cs.normalColor = Color.white; cs.highlightedColor = style == 0 ? new Color(1.15f, 1.15f, 1.15f) : new Color(1.6f, 1.75f, 2f);
        cs.pressedColor = new Color(0.8f, 0.85f, 0.9f); cs.selectedColor = Color.white; cs.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.45f); cs.fadeDuration = 0.08f; b.colors = cs;
        if (style != 0) { var o = Img("Line", go.transform, Vector2.zero, Vector2.one, new Color(acc.r, acc.g, acc.b, 0.45f)); o.sprite = sprOutline; o.type = Image.Type.Sliced; }
        b.onClick.AddListener(() => { Sfx("click"); onClick(); });
        var t = TextUI("Text", go.transform, Vector2.zero, Vector2.one, fontSize, style == 2 ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter,
            style == 0 ? new Color(0.02f, 0.07f, 0.12f) : UiText, style == 0 ? fontBold : font);
        t.text = label;
        if (style == 2)
        {
            Pad(t, 62, 12);
            var bd = Panel("Badge", go.transform, new Vector2(0, 0), new Vector2(0, 1), new Color(acc.r, acc.g, acc.b, 0.16f));
            bd.offsetMin = new Vector2(8, 7); bd.offsetMax = new Vector2(46, -7); var bi = bd.GetComponent<Image>(); bi.sprite = sprRound; bi.type = Image.Type.Sliced; bi.raycastTarget = false;
            var bt = TextUI("L", bd, Vector2.zero, Vector2.one, 16, TextAnchor.MiddleCenter, acc, fontMono); bt.text = badge ?? ""; Pad(bt, 0, 0);
        }
        return b;
    }
    static void Place(Component c, float x0, float y0, float x1, float y1)
    {
        var rt = (RectTransform)c.transform; rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1); rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    void Brackets(Transform parent, Color col, float len = 18f, float th = 2f)
    {   // corner brackets, a recurring "instrument frame" motif
        foreach (int sx in new[] { 0, 1 }) foreach (int sy in new[] { 0, 1 })
        {
            var h = Img("BrH", parent, new Vector2(sx, sy), new Vector2(sx, sy), col); var hr = (RectTransform)h.transform;
            hr.pivot = new Vector2(sx, sy); hr.sizeDelta = new Vector2(len, th); hr.anchoredPosition = new Vector2(sx == 0 ? -4 : 4, sy == 0 ? -4 : 4);
            var v = Img("BrV", parent, new Vector2(sx, sy), new Vector2(sx, sy), col); var vr = (RectTransform)v.transform;
            vr.pivot = new Vector2(sx, sy); vr.sizeDelta = new Vector2(th, len); vr.anchoredPosition = new Vector2(sx == 0 ? -4 : 4, sy == 0 ? -4 : 4);
        }
    }

    // ------------------------------------------------------------------ build
    void BuildUI()
    {
        MakeUiSprites();
        var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cgo.GetComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720); cs.matchWidthOrHeight = 0.5f;
        var root = cgo.transform;

        var hud = Panel("Hud", root, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0)); hud.GetComponent<Image>().raycastTarget = false;
        hudGroup = hud.gameObject.AddComponent<CanvasGroup>();

        // top bar: mission header on the left, six telemetry gauges, alerts on the right
        var top = Panel("TopHud", hud, new Vector2(0, 1 - 108f / 720f), Vector2.one, UiBg); top.GetComponent<Image>().raycastTarget = false;
        Img("TopLine", top, new Vector2(0, 0), new Vector2(1, 0), new Color(UiAccent.r, UiAccent.g, UiAccent.b, 0.5f)).rectTransform.sizeDelta = new Vector2(0, 1.5f);
        siteText = TextUI("Site", top, new Vector2(0.012f, 0.72f), new Vector2(0.3f, 0.96f), 12, TextAnchor.MiddleLeft, UiAccent, fontMono);
        solText = TextUI("Sol", top, new Vector2(0.012f, 0.36f), new Vector2(0.3f, 0.74f), 26, TextAnchor.MiddleLeft, UiText, fontBold);
        crateText = TextUI("Crates", top, new Vector2(0.012f, 0.07f), new Vector2(0.3f, 0.36f), 13, TextAnchor.MiddleLeft, UiAmber, fontMono);
        alertText = TextUI("Alerts", top, new Vector2(0.5f, 0.7f), new Vector2(0.99f, 0.98f), 12, TextAnchor.UpperRight, UiAmber, font);
        string[] gk = { "o2", "water", "food", "battery", "health", "dose" };
        for (int i = 0; i < gk.Length; i++)
        {
            float w = 0.7f / 6f, gx0 = 0.29f + i * w, gx1 = gx0 + w - 0.008f;
            var g = new Gauge();
            g.name = TextUI("GN" + gk[i], top, new Vector2(gx0, 0.47f), new Vector2(gx1, 0.68f), 11, TextAnchor.MiddleLeft, UiMuted, fontMono);
            g.label = TextUI("GV" + gk[i], top, new Vector2(gx0, 0.2f), new Vector2(gx1, 0.47f), 14, TextAnchor.MiddleLeft, UiText, fontMono);
            g.label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var bg = Panel("GB" + gk[i], top, new Vector2(gx0, 0.09f), new Vector2(gx1, 0.15f), new Color(1, 1, 1, 0.09f));
            var bgi = bg.GetComponent<Image>(); bgi.sprite = sprRound; bgi.type = Image.Type.Sliced; bgi.raycastTarget = false;
            bg.offsetMin = new Vector2(8, 0); bg.offsetMax = new Vector2(-8, 0);
            g.fill = Img("GF" + gk[i], bg, Vector2.zero, Vector2.one, UiGood); g.fill.sprite = sprRound; g.fill.type = Image.Type.Sliced;
            gauges[gk[i]] = g;
        }

        // bottom bar: energy line on the left, plan buttons, END SOL
        var bot = Panel("BottomBar", hud, Vector2.zero, new Vector2(1, 62f / 720f), UiBg); bot.GetComponent<Image>().raycastTarget = false;
        Img("BotLine", bot, new Vector2(0, 1), new Vector2(1, 1), new Color(UiAccent.r, UiAccent.g, UiAccent.b, 0.5f)).rectTransform.sizeDelta = new Vector2(0, 1.5f);
        infoText = TextUI("Info", bot, new Vector2(0.006f, 0), new Vector2(0.45f, 1), 13, TextAnchor.MiddleLeft, UiText, fontMono);
        var rb = ButtonUI("Ration", bot, "", CycleRation, 14, 1, null, UiAmber); Place(rb, 0.455f, 0.16f, 0.6f, 0.84f); rationTxt = rb.transform.Find("Text").GetComponent<Text>(); rationTxt.font = fontMono;
        var ab = ButtonUI("Activity", bot, "", CycleActivity, 14, 1, null, new Color(0.7f, 0.6f, 1f)); Place(ab, 0.608f, 0.16f, 0.78f, 0.84f); actTxt = ab.transform.Find("Text").GetComponent<Text>(); actTxt.font = fontMono;
        endBtn = ButtonUI("EndSol", bot, "END SOL", () => { if (journey) StartCoroutine(Camp()); else StartCoroutine(EndSol()); }, 18, 0); Place(endBtn, 0.79f, 0.14f, 0.992f, 0.86f);

        var tl = Panel("Timeline", hud, new Vector2(0.012f, 68f / 720f), new Vector2(0.988f, 84f / 720f), new Color(0, 0, 0, 0));
        tl.GetComponent<Image>().raycastTarget = false;
        tlCell = new Image[Sim.MAX_DAY]; tlLow = new Image[Sim.MAX_DAY]; tlTop = new Image[Sim.MAX_DAY]; tlCard = new Image[Sim.MAX_DAY];
        for (int i = 0; i < Sim.MAX_DAY; i++)
        {
            float x0 = i / (float)Sim.MAX_DAY + 0.0018f, x1 = (i + 1) / (float)Sim.MAX_DAY - 0.0018f;
            tlCell[i] = Img("D" + (i + 1), tl, new Vector2(x0, 0.0f), new Vector2(x1, 1f), new Color(0.2f, 0.28f, 0.4f, 0.55f));
            tlCell[i].sprite = sprRound; tlCell[i].type = Image.Type.Sliced;
            tlLow[i] = Img("Low", tlCell[i].transform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.3f), new Color(0, 0, 0, 0));
            tlTop[i] = Img("Top", tlCell[i].transform, new Vector2(0.08f, 0.7f), new Vector2(0.92f, 0.92f), new Color(0, 0, 0, 0));
            tlCard[i] = Img("Card", tlCell[i].transform, new Vector2(0.36f, 0.36f), new Vector2(0.64f, 0.64f), new Color(0, 0, 0, 0)); tlCard[i].sprite = sprDot;
        }

        // module card (right) and PIP bubble (left)
        var card = Card("Card", hud, new Vector2(0.53f, 92f / 720f), new Vector2(0.988f, 236f / 720f), UiBg2);
        cardRoot = card.gameObject;
        var ck = TextUI("K", card, new Vector2(0, 0.8f), new Vector2(0.6f, 0.97f), 11, TextAnchor.MiddleLeft, UiAccent, fontMono); ck.text = "MODULE"; Pad(ck, 18, 8);
        cardTitle = TextUI("T", card, new Vector2(0, 0.6f), new Vector2(0.62f, 0.83f), 19, TextAnchor.MiddleLeft, UiText, fontBold); Pad(cardTitle, 18, 8);
        cardBody = TextUI("B", card, new Vector2(0, 0.03f), new Vector2(1f, 0.6f), 13, TextAnchor.UpperLeft, new Color(0.82f, 0.88f, 0.96f)); Pad(cardBody, 18, 18);
        cardBtn = ButtonUI("Act", card, "", () => { if (cardMod != null) Interact(cardMod.station); }, 14, 0); Place(cardBtn, 0.6f, 0.66f, 0.975f, 0.93f);
        cardBtnTxt = cardBtn.GetComponentInChildren<Text>();
        cardRoot.SetActive(false);

        var pb = Card("Pip", root, new Vector2(0.012f, 92f / 720f), new Vector2(0.5f, 196f / 720f), UiBg2);
        pipRoot = pb.gameObject; pb.GetComponent<Image>().raycastTarget = false;
        var av = Panel("Avatar", pb, new Vector2(0, 1), new Vector2(0, 1), new Color(0.12f, 0.17f, 0.25f)); av.pivot = new Vector2(0, 1); av.sizeDelta = new Vector2(34, 34); av.anchoredPosition = new Vector2(14, -12);
        var avi = av.GetComponent<Image>(); avi.sprite = sprDot; avi.raycastTarget = false;
        pipDot = Img("Eye", av, new Vector2(0.28f, 0.28f), new Vector2(0.72f, 0.72f), UiAccent); pipDot.sprite = sprDot;
        var pn = TextUI("Name", pb, new Vector2(0, 0.62f), new Vector2(0.7f, 0.97f), 12, TextAnchor.MiddleLeft, UiAccent, fontMono); pn.text = "PIP  ·  BASE ASSISTANT"; Pad(pn, 56, 8);
        pipText = TextUI("Text", pb, new Vector2(0, 0.04f), new Vector2(1f, 0.66f), 14, TextAnchor.UpperLeft, UiText); Pad(pipText, 56, 16);
        pipRoot.SetActive(false);

        barTop = Panel("BarTop", root, new Vector2(0, 1), new Vector2(1, 1), Color.black); barTop.pivot = new Vector2(0.5f, 1); barTop.GetComponent<Image>().raycastTarget = false;
        barBot = Panel("BarBot", root, new Vector2(0, 0), new Vector2(1, 0), Color.black); barBot.pivot = new Vector2(0.5f, 0); barBot.GetComponent<Image>().raycastTarget = false;

        flashImg = Img("Flash", root, Vector2.zero, Vector2.one, new Color(1, 1, 1, 0));
        var ban = Panel("Banner", root, new Vector2(0f, 0.6f), new Vector2(1f, 0.8f), new Color(0.02f, 0.04f, 0.08f, 0.75f));
        var bimg = ban.GetComponent<Image>(); bimg.sprite = sprFade; bimg.raycastTarget = false;
        bannerGroup = ban.gameObject.AddComponent<CanvasGroup>(); bannerGroup.alpha = 0; bannerGroup.blocksRaycasts = false;
        bannerKicker = TextUI("K", ban, new Vector2(0, 0.74f), new Vector2(1, 0.96f), 12, TextAnchor.MiddleCenter, UiAccent, fontMono);
        bannerTitle = TextUI("T", ban, new Vector2(0, 0.34f), new Vector2(1, 0.78f), 44, TextAnchor.MiddleCenter, Color.white, fontBold);
        bannerSub = TextUI("S", ban, new Vector2(0, 0.04f), new Vector2(1, 0.36f), 17, TextAnchor.MiddleCenter, new Color(0.85f, 0.91f, 1f));

        // dialog
        var overlay = Panel("Modal", root, Vector2.zero, Vector2.one, new Color(0.0f, 0.01f, 0.03f, 0.62f));
        modalRoot = overlay.gameObject;
        modalBox = Card("Box", overlay, new Vector2(0.17f, 0.11f), new Vector2(0.83f, 0.9f), UiBg2);
        Brackets(modalBox, new Color(UiAccent.r, UiAccent.g, UiAccent.b, 0.7f));
        var acc = Img("Accent", modalBox, new Vector2(0, 1), new Vector2(0, 1), UiAccent); var accr = acc.rectTransform; accr.pivot = new Vector2(0, 1); accr.sizeDelta = new Vector2(64, 3); accr.anchoredPosition = new Vector2(32, -1);
        modalKicker = TextUI("Kicker", modalBox, new Vector2(0, 0.905f), new Vector2(1, 0.975f), 12, TextAnchor.MiddleLeft, UiAmber, fontMono); Pad(modalKicker, 32, 32);
        modalTitle = TextUI("Title", modalBox, new Vector2(0, 0.8f), new Vector2(1, 0.915f), 27, TextAnchor.MiddleLeft, UiText, fontBold); Pad(modalTitle, 32, 32);
        var div = Img("Divider", modalBox, new Vector2(0, 0.795f), new Vector2(1, 0.795f), UiLine); div.rectTransform.sizeDelta = new Vector2(-64, 1);
        modalBody = TextUI("Body", modalBox, new Vector2(0, 0.38f), new Vector2(1, 0.77f), 18, TextAnchor.UpperLeft, new Color(0.86f, 0.91f, 0.98f)); Pad(modalBody, 32, 32);
        modalButtons = Panel("Buttons", modalBox, new Vector2(0.03f, 0.09f), new Vector2(0.97f, 0.36f), new Color(0, 0, 0, 0));
        var grid = modalButtons.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(250, 44); grid.spacing = new Vector2(12, 10); grid.childAlignment = TextAnchor.UpperCenter;
        modalOptions = Panel("Options", modalBox, new Vector2(0, 0.09f), new Vector2(1, 0.4f), new Color(0, 0, 0, 0));
        modalOptions.offsetMin = new Vector2(32, 0); modalOptions.offsetMax = new Vector2(-32, 0);
        var vl = modalOptions.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = 10; vl.childForceExpandHeight = false; vl.childControlHeight = false; vl.childControlWidth = true; vl.childForceExpandWidth = true; vl.childAlignment = TextAnchor.UpperLeft;
        modalFooter = TextUI("Footer", modalBox, new Vector2(0, 0.012f), new Vector2(1, 0.075f), 12, TextAnchor.MiddleLeft, UiGood, fontMono); Pad(modalFooter, 32, 32);
        modalRoot.SetActive(false);
    }

    // ------------------------------------------------------------------ dialog
    class Btn { public string label; public Action act; public bool enabled = true; public bool close = true; public bool primary; }
    void ShowModal(string title, string body, List<Btn> btns, int bodySize = 18, string kicker = null, bool options = false, string footer = null, Color? kickerCol = null)
    {
        if (mode != Mode.Title && mode != Mode.Cine) mode = Mode.Modal;
        modalRoot.SetActive(true);
        modalKicker.text = kicker ?? ""; modalKicker.color = kickerCol ?? UiAmber;
        modalTitle.text = title; modalBody.text = body; modalBody.fontSize = bodySize;
        modalFooter.text = footer ?? "";
        modalButtons.gameObject.SetActive(!options); modalOptions.gameObject.SetActive(options);
        const float row = 54f / 569f;
        int rows = options ? btns.Count : (btns.Count + 2) / 3;
        float btnTop = 0.085f + rows * row;
        var br = options ? modalOptions : modalButtons;
        br.anchorMin = new Vector2(br.anchorMin.x, 0.085f); br.anchorMax = new Vector2(br.anchorMax.x, btnTop);
        modalBody.rectTransform.anchorMin = new Vector2(0, btnTop + 0.02f);
        foreach (Transform c in modalButtons) Destroy(c.gameObject);
        foreach (Transform c in modalOptions) Destroy(c.gameObject);
        string letters = "ABCDEFGH"; int i = 0;
        foreach (var b in btns)
        {
            var bb = b; Action click = () => { if (bb.close) CloseModal(); if (bb.act != null) bb.act(); };
            Button ui;
            if (options)
            {
                ui = ButtonUI("Opt", modalOptions, b.label, click, 18, 2, letters[i].ToString());
                var le = ui.gameObject.AddComponent<LayoutElement>(); le.preferredHeight = 46; ((RectTransform)ui.transform).sizeDelta = new Vector2(0, 46);
            }
            else ui = ButtonUI("B", modalButtons, b.label, click, 16, b.primary || (btns.Count == 1) ? 0 : 1);
            ui.interactable = b.enabled; i++;
        }
    }
    void CloseModal() { modalRoot.SetActive(false); if (mode == Mode.Modal) mode = Mode.Play; }

    // ------------------------------------------------------------------ PIP, banners, flash, letterbox
    void PipSay(string text, int mood = 0)
    {
        pipQueue.Enqueue(new KeyValuePair<string, int>(text, mood));
        if (pipQueue.Count > 3) pipQueue.Dequeue();
    }
    static readonly Color[] MoodCol = { new Color(0.35f, 0.82f, 1f), new Color(1f, 0.72f, 0.28f), new Color(1f, 0.38f, 0.36f), new Color(0.24f, 0.86f, 0.59f) };
    void BannerQ(string title, string sub, Color col) { bannerQueue.Enqueue(new BannerItem { title = title, sub = sub, col = col }); }
    void Flash(Color col, float dur = 0.6f) { flashCol = col; flashT = dur; flashDur = dur; }
    void Letterbox(bool on) { letterTarget = on ? 1f : 0f; }

    void TickUI(float dt)
    {
        // PIP typewriter
        if (pipShown >= pipFull.Length && pipQueue.Count > 0 && (pipHide < 3.2f || !pipRoot.activeSelf))
        {
            var kv = pipQueue.Dequeue(); pipFull = kv.Key; pipShown = 0; pipHide = 4f + pipFull.Length * 0.045f; pipRoot.SetActive(true);
            pipEyeTarget = MoodCol[Mathf.Clamp(kv.Value, 0, 3)]; pipDot.color = pipEyeTarget; Sfx(kv.Value >= 1 && kv.Value <= 2 ? "warn" : "on", 0.35f);
        }
        if (pipRoot.activeSelf)
        {
            if (pipShown < pipFull.Length) { pipShown = Mathf.Min(pipFull.Length, pipShown + dt * 55f); pipText.text = pipFull.Substring(0, (int)pipShown); }
            else { pipHide -= dt; if (pipHide <= 0) { pipRoot.SetActive(false); pipEyeTarget = MoodCol[0]; } }
        }
        // banners, one after the other
        if (bannerT <= 0 && bannerQueue.Count > 0)
        {
            var b = bannerQueue.Dequeue(); bannerTitle.text = b.title; bannerSub.text = b.sub; bannerCol = b.col; bannerT = shots ? 1.4f : 2.1f; bannerDur = bannerT;
            bannerKicker.text = (journey ? "SOL " + (js != null ? js.sol : 1) : (st != null ? DayWord + " " + Mathf.Min(st.day, Sim.MAX_DAY) : "")) + "  ·  MISSION EVENT";
            bannerKicker.color = b.col;
        }
        if (bannerT > 0)
        {
            bannerT -= dt; float k = bannerDur - bannerT;
            bannerGroup.alpha = Mathf.Clamp01(k / 0.2f) * Mathf.Clamp01(bannerT / 0.4f);
            bannerGroup.transform.localScale = new Vector3(1f, k < 0.25f ? Mathf.Lerp(0.6f, 1f, k / 0.25f) : 1f, 1f);
            bannerTitle.color = Color.Lerp(Color.white, bannerCol, 0.55f + 0.25f * Mathf.Sin(Time.time * 5f));
        }
        else bannerGroup.alpha = 0;
        if (flashT > 0) { flashT -= dt; flashImg.color = new Color(flashCol.r, flashCol.g, flashCol.b, flashCol.a * Mathf.Clamp01(flashT / flashDur)); }
        else if (flashImg.color.a > 0) flashImg.color = new Color(1, 1, 1, 0);
        letter = Mathf.MoveTowards(letter, letterTarget, dt * 2.5f);
        barTop.sizeDelta = new Vector2(0, 64f * letter); barBot.sizeDelta = new Vector2(0, 64f * letter);
        float hudA = (mode == Mode.Cine || mode == Mode.Title) ? 0f : 1f;
        hudGroup.alpha = Mathf.MoveTowards(hudGroup.alpha, hudA, dt * 3f); hudGroup.blocksRaycasts = hudGroup.alpha > 0.5f; hudGroup.interactable = mode == Mode.Play;
        if (solPop > 0) { solPop -= dt; solText.transform.localScale = Vector3.one * (1f + Mathf.Clamp01(solPop) * 0.2f); }
        foreach (var g in gauges.Values)
            if (g.low) { var c = g.fill.color; c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 5f)); g.fill.color = c; }

        Mod near = mode == Mode.Play && !journey ? NearMod(3.4f) : null;
        if (near != cardMod) { cardMod = near; cardRoot.SetActive(near != null); if (near != null) FillCard(); }
    }
    float bannerDur = 2.5f;

    void FillCard()
    {
        if (cardMod == null || st == null) return;
        string body, action; Describe(cardMod.station, out body, out action);
        cardTitle.text = cardMod.title; cardBody.text = body;
        cardBtn.gameObject.SetActive(action != null); if (action != null) cardBtnTxt.text = action.ToUpper() + "   [E]";
    }

    // ------------------------------------------------------------------ gauges and timeline
    void SetGauge(string key, string name, double val, double max, string unit, int? runout, bool invert = false)
    {
        var g = gauges[key]; float f = max <= 0 ? 0 : Mathf.Clamp01((float)(val / max));
        g.name.text = name.ToUpper();
        g.label.text = Fmt(val) + (unit != "" ? " " + unit : "") + (runout.HasValue ? " (" + runout.Value + "d)" : "");
        var rt = (RectTransform)g.fill.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(Mathf.Max(f, 0.02f), 1);
        float good = invert ? 1 - f : f;
        g.fill.color = good > 0.5f ? UiGood : (good > 0.25f ? UiAmber : UiBad);
        g.low = good <= 0.25f;
    }
    void UpdateTimeline()
    {
        if (st == null || tlCell == null) return;
        var sc = GameData.Scenarios[st.site];
        for (int i = 0; i < Sim.MAX_DAY; i++)
        {
            int day = i + 1;
            tlCell[i].color = day < st.day ? new Color(UiAccent.r, UiAccent.g, UiAccent.b, 0.42f) : (day == st.day ? new Color(1, 1, 1, 0.95f) : new Color(1, 1, 1, 0.08f));
            Color low = new Color(0, 0, 0, 0), top = low;
            if (st.site == "moon" && day >= sc.nightStart && day <= sc.nightEnd) low = new Color(0.4f, 0.5f, 1f, 0.95f);
            if (st.dust != null && day >= st.dust.start && day <= st.dust.end && (st.dust.start - st.day <= st.dust.alert)) low = UiAmber;
            foreach (var s in st.storms) if (s.day == day && (s.day - st.day <= s.alert)) top = UiBad;
            tlLow[i].color = low; tlTop[i].color = top;
            tlCard[i].color = GameData.Cards.Any(c => c.afterDay == day) ? new Color(1, 1, 1, 0.8f) : new Color(0, 0, 0, 0);
        }
    }

    void RefreshHud()
    {
        siteText.text = (site == "moon" ? "LUNAR OUTPOST" : "MARTIAN OUTPOST") + (hardMode ? "  ·  HARD MODE" : "") + "  ·  CREW 4";
        solText.text = DayWord + " " + Mathf.Min(st.day, Sim.MAX_DAY) + " / " + Sim.MAX_DAY;
        crateText.text = "CRATES " + (st.crates - QueuedCost() - (plan.repair ? 1 : 0)) + "   ·   SCIENCE " + Fmt(st.science);
        var alerts = Sim.AlertsFor(st, st.day).Select(a => a.text).ToList();
        if (st.faults.Count > 0) alerts.Add("Broken: " + string.Join(", ", st.faults.Select(f => SysName(f.type))) + ".");
        alertText.text = string.Join("\n", alerts.Take(2));
        double cap = V("battery_per") * st.mod["battery"];
        SetGauge("o2", "Oxygen", st.o2, V("o2_cap"), "kg", pv.runoutO2);
        SetGauge("water", "Water", st.water, V("water_cap"), "L", pv.runoutWater);
        SetGauge("food", "Food", st.food, V("food_cap"), "kg", pv.runoutFood);
        SetGauge("battery", "Battery", st.battery, cap, "", pv.runoutBattery);
        SetGauge("health", "Health", st.health, 100, "", null);
        SetGauge("dose", "Dose", st.dose, V("dose_limit"), "mSv", null, true);
        var sb = new StringBuilder("POWER  made " + Fmt(pv.supply) + "  /  need " + Fmt(pv.demand));
        sb.Append(pv.shut.Count > 0 ? "\nSHORTAGE  off: " + string.Join(", ", pv.shut.Distinct()) : "   NOMINAL");
        infoText.text = sb.ToString(); infoText.color = pv.shut.Count > 0 ? UiBad : UiText;
        rationTxt.text = "RATIONS  " + plan.ration.ToUpper(); actTxt.text = "ACTIVITY  " + plan.activity.ToUpper();
        endBtn.GetComponentInChildren<Text>().text = "END " + DayWord;
        endBtn.interactable = mode == Mode.Play && !st.over;
        UpdateTimeline(); FillCard();
    }
}
