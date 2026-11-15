// Pioneer Base 3D - PioneerGame.UI.cs
// HUD, menus, the module card, PIP's speech bubble, banners, letterbox bars and the 30-day timeline.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using PioneerBase;

public partial class PioneerGame
{
    static readonly Color PanelCol = new Color(0.03f, 0.06f, 0.11f, 0.78f);
    Canvas canvas; CanvasGroup hudGroup;
    Text solText, crateText, alertText, infoText, rationTxt, actTxt;
    Button endBtn;
    class Gauge { public Text label; public Image fill; public bool low; }
    readonly Dictionary<string, Gauge> gauges = new Dictionary<string, Gauge>();
    GameObject modalRoot; Text modalTitle, modalBody; RectTransform modalButtons;
    GameObject cardRoot; Text cardTitle, cardBody, cardBtnTxt; Button cardBtn; Mod cardMod;
    GameObject pipRoot; Text pipText; Image pipDot; string pipFull = ""; float pipShown, pipHide; readonly Queue<KeyValuePair<string, int>> pipQueue = new Queue<KeyValuePair<string, int>>();
    Image flashImg; Color flashCol; float flashT, flashDur;
    CanvasGroup bannerGroup; Text bannerTitle, bannerSub; float bannerT; Color bannerCol;
    class BannerItem { public string title, sub; public Color col; }
    readonly Queue<BannerItem> bannerQueue = new Queue<BannerItem>();
    RectTransform barTop, barBot; float letter, letterTarget;
    Image[] tlCell, tlLow, tlTop, tlCard; float solPop;

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
    Text TextUI(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size, TextAnchor anchor, Color col)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = new Vector2(8, 2); rt.offsetMax = new Vector2(-8, -2);
        var t = go.GetComponent<Text>(); t.font = font; t.fontSize = size; t.alignment = anchor; t.color = col;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
        return t;
    }
    Button ButtonUI(string name, Transform parent, string label, Action onClick, int fontSize = 20)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>(); img.color = new Color(0.14f, 0.38f, 0.66f);
        var b = go.GetComponent<Button>(); b.targetGraphic = img;
        var cs = b.colors; cs.disabledColor = new Color(0.3f, 0.3f, 0.35f, 0.7f); cs.highlightedColor = new Color(0.8f, 0.95f, 1f); b.colors = cs;
        b.onClick.AddListener(() => { Sfx("click"); onClick(); });
        var t = TextUI("Text", go.transform, Vector2.zero, Vector2.one, fontSize, TextAnchor.MiddleCenter, Color.white); t.text = label;
        return b;
    }
    static void Place(Component c, float x0, float y0, float x1, float y1)
    {
        var rt = (RectTransform)c.transform; rt.anchorMin = new Vector2(x0, y0); rt.anchorMax = new Vector2(x1, y1); rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // ------------------------------------------------------------------ build
    void BuildUI()
    {
        var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cgo.GetComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720); cs.matchWidthOrHeight = 0.5f;
        var root = cgo.transform;

        var hud = Panel("Hud", root, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0)); hud.GetComponent<Image>().raycastTarget = false;
        hudGroup = hud.gameObject.AddComponent<CanvasGroup>();

        var top = Panel("TopHud", hud, new Vector2(0, 1 - 92f / 720f), Vector2.one, PanelCol);
        Img("TopLine", top, new Vector2(0, 0), new Vector2(1, 0.025f), new Color(0.35f, 0.8f, 1f, 0.8f));
        solText = TextUI("Sol", top, new Vector2(0, 0.52f), new Vector2(0.25f, 1), 28, TextAnchor.MiddleLeft, Color.white); solText.fontStyle = FontStyle.Bold;
        crateText = TextUI("Crates", top, new Vector2(0.25f, 0.52f), new Vector2(0.37f, 1), 19, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.4f));
        alertText = TextUI("Alerts", top, new Vector2(0.37f, 0.52f), Vector2.one, 17, TextAnchor.MiddleLeft, new Color(1f, 0.74f, 0.4f));
        string[] gk = { "o2", "water", "food", "battery", "health", "dose" };
        for (int i = 0; i < gk.Length; i++)
        {
            float x0 = i / 6f + 0.005f, x1 = (i + 1) / 6f - 0.005f;
            var g = new Gauge();
            g.label = TextUI("GL" + gk[i], top, new Vector2(x0, 0.26f), new Vector2(x1, 0.54f), 14, TextAnchor.MiddleLeft, Color.white); g.label.horizontalOverflow = HorizontalWrapMode.Overflow;
            var bg = Panel("GB" + gk[i], top, new Vector2(x0 + 0.005f, 0.09f), new Vector2(x1 - 0.005f, 0.25f), new Color(1, 1, 1, 0.14f));
            bg.GetComponent<Image>().raycastTarget = false;
            g.fill = Img("GF" + gk[i], bg, Vector2.zero, Vector2.one, new Color(0.3f, 0.8f, 0.45f));
            gauges[gk[i]] = g;
        }

        var bot = Panel("BottomBar", hud, Vector2.zero, new Vector2(1, 60f / 720f), PanelCol);
        Img("BotLine", bot, new Vector2(0, 0.97f), new Vector2(1, 1f), new Color(0.35f, 0.8f, 1f, 0.8f));
        infoText = TextUI("Info", bot, Vector2.zero, new Vector2(0.44f, 1), 16, TextAnchor.MiddleLeft, Color.white);
        var rb = ButtonUI("Ration", bot, "", CycleRation, 16); Place(rb, 0.45f, 0.12f, 0.60f, 0.88f); rationTxt = rb.GetComponentInChildren<Text>(); rb.GetComponent<Image>().color = new Color(0.45f, 0.32f, 0.12f);
        var ab = ButtonUI("Activity", bot, "", CycleActivity, 16); Place(ab, 0.61f, 0.12f, 0.78f, 0.88f); actTxt = ab.GetComponentInChildren<Text>(); ab.GetComponent<Image>().color = new Color(0.3f, 0.22f, 0.5f);
        endBtn = ButtonUI("EndSol", bot, "END SOL", () => StartCoroutine(EndSol()), 22); Place(endBtn, 0.795f, 0.1f, 0.99f, 0.9f);
        endBtn.GetComponent<Image>().color = new Color(0.12f, 0.6f, 0.36f);

        var tl = Panel("Timeline", hud, new Vector2(0.02f, 64f / 720f), new Vector2(0.98f, 86f / 720f), new Color(0.02f, 0.04f, 0.08f, 0.6f));
        tl.GetComponent<Image>().raycastTarget = false;
        tlCell = new Image[Sim.MAX_DAY]; tlLow = new Image[Sim.MAX_DAY]; tlTop = new Image[Sim.MAX_DAY]; tlCard = new Image[Sim.MAX_DAY];
        for (int i = 0; i < Sim.MAX_DAY; i++)
        {
            float x0 = i / (float)Sim.MAX_DAY + 0.0025f, x1 = (i + 1) / (float)Sim.MAX_DAY - 0.0025f;
            tlCell[i] = Img("D" + (i + 1), tl, new Vector2(x0, 0.12f), new Vector2(x1, 0.88f), new Color(0.2f, 0.28f, 0.4f, 0.55f));
            tlLow[i] = Img("Low", tlCell[i].transform, new Vector2(0, 0), new Vector2(1, 0.3f), new Color(0, 0, 0, 0));
            tlTop[i] = Img("Top", tlCell[i].transform, new Vector2(0, 0.7f), new Vector2(1, 1f), new Color(0, 0, 0, 0));
            tlCard[i] = Img("Card", tlCell[i].transform, new Vector2(0.35f, 0.35f), new Vector2(0.65f, 0.65f), new Color(0, 0, 0, 0));
        }

        // module card (right) and PIP bubble (left), just above the timeline
        var card = Panel("Card", hud, new Vector2(0.52f, 94f / 720f), new Vector2(0.985f, 224f / 720f), new Color(0.03f, 0.07f, 0.13f, 0.9f));
        cardRoot = card.gameObject;
        Img("CardLine", card, new Vector2(0, 0.96f), new Vector2(1, 1f), new Color(0.35f, 0.8f, 1f, 0.9f));
        cardTitle = TextUI("T", card, new Vector2(0, 0.74f), new Vector2(0.62f, 0.96f), 19, TextAnchor.MiddleLeft, new Color(0.6f, 0.9f, 1f)); cardTitle.fontStyle = FontStyle.Bold;
        cardBody = TextUI("B", card, new Vector2(0, 0.02f), new Vector2(0.99f, 0.74f), 14, TextAnchor.UpperLeft, Color.white);
        cardBtn = ButtonUI("Act", card, "", () => { if (cardMod != null) Interact(cardMod.station); }, 15); Place(cardBtn, 0.62f, 0.76f, 0.985f, 0.95f);
        cardBtnTxt = cardBtn.GetComponentInChildren<Text>();
        cardRoot.SetActive(false);

        var pb = Panel("Pip", root, new Vector2(0.015f, 94f / 720f), new Vector2(0.50f, 190f / 720f), new Color(0.03f, 0.07f, 0.13f, 0.9f));
        pipRoot = pb.gameObject; pb.GetComponent<Image>().raycastTarget = false;
        pipDot = Img("Dot", pb, new Vector2(0.012f, 0.62f), new Vector2(0.045f, 0.9f), new Color(0.3f, 0.9f, 1f));
        var pn = TextUI("Name", pb, new Vector2(0.045f, 0.6f), new Vector2(0.3f, 0.95f), 15, TextAnchor.MiddleLeft, new Color(0.6f, 0.9f, 1f)); pn.text = "PIP"; pn.fontStyle = FontStyle.Bold;
        pipText = TextUI("Text", pb, new Vector2(0.0f, 0.0f), new Vector2(1f, 0.64f), 15, TextAnchor.UpperLeft, Color.white);
        pipRoot.SetActive(false);

        barTop = Panel("BarTop", root, new Vector2(0, 1), new Vector2(1, 1), Color.black); barTop.pivot = new Vector2(0.5f, 1); barTop.GetComponent<Image>().raycastTarget = false;
        barBot = Panel("BarBot", root, new Vector2(0, 0), new Vector2(1, 0), Color.black); barBot.pivot = new Vector2(0.5f, 0); barBot.GetComponent<Image>().raycastTarget = false;

        flashImg = Img("Flash", root, Vector2.zero, Vector2.one, new Color(1, 1, 1, 0));
        var ban = Panel("Banner", root, new Vector2(0.08f, 0.60f), new Vector2(0.92f, 0.78f), new Color(0, 0, 0, 0.5f));
        ban.GetComponent<Image>().raycastTarget = false;
        bannerGroup = ban.gameObject.AddComponent<CanvasGroup>(); bannerGroup.alpha = 0; bannerGroup.blocksRaycasts = false;
        bannerTitle = TextUI("T", ban, new Vector2(0, 0.42f), Vector2.one, 46, TextAnchor.MiddleCenter, Color.white); bannerTitle.fontStyle = FontStyle.Bold;
        bannerSub = TextUI("S", ban, new Vector2(0, 0), new Vector2(1, 0.46f), 20, TextAnchor.UpperCenter, new Color(0.88f, 0.94f, 1f));

        var overlay = Panel("Modal", root, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.35f));
        modalRoot = overlay.gameObject;
        var box = Panel("Box", overlay, new Vector2(0.16f, 0.12f), new Vector2(0.84f, 0.9f), new Color(0.04f, 0.08f, 0.15f, 0.94f));
        Img("BoxLine", box, new Vector2(0, 0.992f), new Vector2(1, 1f), new Color(0.35f, 0.8f, 1f, 0.9f));
        modalTitle = TextUI("Title", box, new Vector2(0, 0.88f), Vector2.one, 30, TextAnchor.MiddleCenter, new Color(0.6f, 0.88f, 1f)); modalTitle.fontStyle = FontStyle.Bold;
        modalBody = TextUI("Body", box, new Vector2(0.03f, 0.36f), new Vector2(0.97f, 0.88f), 19, TextAnchor.UpperLeft, Color.white);
        modalButtons = Panel("Buttons", box, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.36f), new Color(0, 0, 0, 0));
        var grid = modalButtons.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(262, 42); grid.spacing = new Vector2(10, 8); grid.childAlignment = TextAnchor.UpperCenter;
        modalRoot.SetActive(false);
    }

    // ------------------------------------------------------------------ modal
    class Btn { public string label; public Action act; public bool enabled = true; public bool close = true; }
    void ShowModal(string title, string body, List<Btn> btns, int bodySize = 19)
    {
        if (mode != Mode.Title && mode != Mode.Cine) mode = Mode.Modal;
        modalRoot.SetActive(true);
        modalTitle.text = title; modalBody.text = body; modalBody.fontSize = bodySize;
        foreach (Transform c in modalButtons) Destroy(c.gameObject);
        foreach (var b in btns)
        {
            var bb = b;
            var ui = ButtonUI("B", modalButtons, b.label, () => { if (bb.close) CloseModal(); if (bb.act != null) bb.act(); }, 17);
            ui.interactable = b.enabled;
        }
    }
    void CloseModal() { modalRoot.SetActive(false); if (mode == Mode.Modal) mode = Mode.Play; }

    // ------------------------------------------------------------------ PIP, banners, flash, letterbox
    void PipSay(string text, int mood = 0)
    {
        pipQueue.Enqueue(new KeyValuePair<string, int>(text, mood));
        if (pipQueue.Count > 3) pipQueue.Dequeue();
    }
    static readonly Color[] MoodCol = { new Color(0.3f, 0.9f, 1f), new Color(1f, 0.75f, 0.25f), new Color(1f, 0.3f, 0.25f), new Color(0.4f, 1f, 0.55f) };
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
            var b = bannerQueue.Dequeue(); bannerTitle.text = b.title; bannerSub.text = b.sub; bannerCol = b.col; bannerT = shots ? 1.4f : 2.5f; bannerDur = bannerT;
        }
        if (bannerT > 0)
        {
            bannerT -= dt; float k = bannerDur - bannerT;
            bannerGroup.alpha = Mathf.Clamp01(k / 0.2f) * Mathf.Clamp01(bannerT / 0.4f);
            bannerGroup.transform.localScale = Vector3.one * (k < 0.3f ? Mathf.Lerp(1.4f, 1f, k / 0.3f) : 1f);
            bannerTitle.color = Color.Lerp(Color.white, bannerCol, 0.6f + 0.4f * Mathf.Sin(Time.time * 6f));
        }
        else bannerGroup.alpha = 0;
        if (flashT > 0) { flashT -= dt; flashImg.color = new Color(flashCol.r, flashCol.g, flashCol.b, flashCol.a * Mathf.Clamp01(flashT / flashDur)); }
        else if (flashImg.color.a > 0) flashImg.color = new Color(1, 1, 1, 0);
        letter = Mathf.MoveTowards(letter, letterTarget, dt * 2.5f);
        barTop.sizeDelta = new Vector2(0, 74f * letter); barBot.sizeDelta = new Vector2(0, 74f * letter);
        float hudA = (mode == Mode.Cine || mode == Mode.Title) ? 0f : 1f;
        hudGroup.alpha = Mathf.MoveTowards(hudGroup.alpha, hudA, dt * 3f); hudGroup.blocksRaycasts = hudGroup.alpha > 0.5f; hudGroup.interactable = mode == Mode.Play;
        if (solPop > 0) { solPop -= dt; solText.transform.localScale = Vector3.one * (1f + Mathf.Clamp01(solPop) * 0.3f); }
        foreach (var g in gauges.Values)
            if (g.low) { var c = g.fill.color; c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 5f)); g.fill.color = c; }

        // module card for the nearest module
        Mod near = mode == Mode.Play ? NearMod(3.4f) : null;
        if (near != cardMod) { cardMod = near; cardRoot.SetActive(near != null); if (near != null) FillCard(); }
    }
    float bannerDur = 2.5f;

    void FillCard()
    {
        if (cardMod == null || st == null) return;
        string body, action; Describe(cardMod.station, out body, out action);
        cardTitle.text = cardMod.title; cardBody.text = body;
        cardBtn.gameObject.SetActive(action != null); if (action != null) cardBtnTxt.text = action + "  [E]";
    }

    // ------------------------------------------------------------------ gauges and timeline
    void SetGauge(string key, string name, double val, double max, string unit, int? runout, bool invert = false)
    {
        var g = gauges[key]; float f = max <= 0 ? 0 : Mathf.Clamp01((float)(val / max));
        g.label.text = name + " " + Fmt(val) + (max > 0 ? "/" + Fmt(max) : "") + (unit != "" ? " " + unit : "") + (runout.HasValue ? "  (" + runout.Value + "d left)" : "");
        var rt = (RectTransform)g.fill.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(f, 1);
        float good = invert ? 1 - f : f;
        g.fill.color = good > 0.5f ? new Color(0.3f, 0.8f, 0.45f) : (good > 0.25f ? new Color(0.95f, 0.75f, 0.25f) : new Color(0.9f, 0.3f, 0.3f));
        g.low = good <= 0.25f;
    }
    void UpdateTimeline()
    {
        if (st == null || tlCell == null) return;
        var sc = GameData.Scenarios[st.site];
        for (int i = 0; i < Sim.MAX_DAY; i++)
        {
            int day = i + 1;
            tlCell[i].color = day < st.day ? new Color(0.3f, 0.85f, 1f, 0.55f) : (day == st.day ? new Color(1, 1, 1, 0.95f) : new Color(0.2f, 0.28f, 0.4f, 0.55f));
            Color low = new Color(0, 0, 0, 0), top = low;
            if (st.site == "moon" && day >= sc.nightStart && day <= sc.nightEnd) low = new Color(0.25f, 0.35f, 1f, 0.95f);
            if (st.dust != null && day >= st.dust.start && day <= st.dust.end && (st.dust.start - st.day <= st.dust.alert)) low = new Color(1f, 0.62f, 0.25f, 0.95f);
            foreach (var s in st.storms) if (s.day == day && (s.day - st.day <= s.alert)) top = new Color(1f, 0.25f, 0.2f, 1f);
            tlLow[i].color = low; tlTop[i].color = top;
            tlCard[i].color = GameData.Cards.Any(c => c.afterDay == day) ? new Color(1, 1, 1, 0.85f) : new Color(0, 0, 0, 0);
        }
    }

    void RefreshHud()
    {
        solText.text = DayWord + " " + Mathf.Min(st.day, Sim.MAX_DAY) + " / " + Sim.MAX_DAY + "   " + (site == "moon" ? "MOON" : "MARS");
        crateText.text = "Crates: " + (st.crates - QueuedCost() - (plan.repair ? 1 : 0));
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
        var sb = new StringBuilder("Energy today: made " + Fmt(pv.supply) + ", need " + Fmt(pv.demand));
        sb.Append(pv.shut.Count > 0 ? "\nNOT ENOUGH POWER, off: " + string.Join(", ", pv.shut.Distinct()) : "   ok");
        infoText.text = sb.ToString(); infoText.color = pv.shut.Count > 0 ? new Color(1f, 0.6f, 0.45f) : Color.white;
        rationTxt.text = "Rations: " + plan.ration.ToUpper(); actTxt.text = "Activity: " + plan.activity.ToUpper();
        endBtn.GetComponentInChildren<Text>().text = "END " + DayWord;
        endBtn.interactable = mode == Mode.Play && !st.over;
        UpdateTimeline(); FillCard();
    }
}
