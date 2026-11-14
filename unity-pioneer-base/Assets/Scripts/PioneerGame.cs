// Pioneer Base - PioneerGame.cs
// Top-down base view. Builds the whole scene at runtime (rooms, astronaut, stations, UI) so the
// scene file only needs a camera and this component. Game rules live in Assets/Scripts/Core.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PioneerBase;

public partial class PioneerGame : MonoBehaviour
{
    enum Mode { Title, Play, Modal }
    Mode mode = Mode.Title;

    GameState st; Plan plan; PreviewResult pv;
    string site = "moon"; int seed = 1;
    Camera cam; Font font; Sprite square, circle;
    Transform world;

    class Room
    {
        public string id, name, system; public Vector2 center; public bool top; public SpriteRenderer dark, alarm; public TextMesh label;
        public Color accent; public Transform doorL, doorR; public Vector2 doorPos; public float doorOpen, targetDark, curDark; public bool broken, noPower;
    }
    class Station { public string id, roomId; public Vector2 pos; }
    readonly Dictionary<string, Room> rooms = new Dictionary<string, Room>();
    readonly List<Station> stations = new List<Station>();
    SpriteRenderer hallDark;
    Transform astro, rig, legL, legR;
    Sprite slice, ring;
    float walkPhase, facing = 1; bool moving;
    readonly List<Transform> pulses = new List<Transform>();
    readonly List<Vector2> path = new List<Vector2>();
    string pendingStation, armedStation;

    // UI
    Text dayText, alertText, infoText, toastText, crateText;
    float toastUntil;
    class Gauge { public Text label; public Image fill; public bool low; }
    readonly Dictionary<string, Gauge> gauges = new Dictionary<string, Gauge>();
    GameObject modalRoot; Text modalTitle, modalBody; RectTransform modalButtons;
    Button runButton;

    static readonly Color PanelCol = new Color(0.06f, 0.09f, 0.16f, 0.95f);

    // ------------------------------------------------------------------ setup
    void Start()
    {
        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        MakeSprites();
        cam = Camera.main;
        cam.orthographic = true; cam.orthographicSize = 5.4f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.09f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.rect = new Rect(0, 60f / 720f, 1, 1 - 160f / 720f);
        world = new GameObject("World").transform;
        BuildWorld();
        BuildUI();
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        ShowTitle();
    }

    // ------------------------------------------------------------------ UI helpers
    RectTransform Panel(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color col)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = col; return rt;
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
        var img = go.GetComponent<Image>(); img.color = new Color(0.16f, 0.36f, 0.62f);
        var b = go.GetComponent<Button>(); b.targetGraphic = img;
        var cs = b.colors; cs.disabledColor = new Color(0.3f, 0.3f, 0.35f, 0.7f); b.colors = cs;
        b.onClick.AddListener(() => onClick());
        var t = TextUI("Text", go.transform, Vector2.zero, Vector2.one, fontSize, TextAnchor.MiddleCenter, Color.white); t.text = label;
        return b;
    }

    void BuildUI()
    {
        var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = cgo.GetComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720); cs.matchWidthOrHeight = 0.5f;
        var root = cgo.transform;

        // top HUD (100 px of 720)
        var top = Panel("TopHud", root, new Vector2(0, 1 - 100f / 720f), Vector2.one, PanelCol);
        dayText = TextUI("Day", top, new Vector2(0, 0.55f), new Vector2(0.28f, 1), 26, TextAnchor.MiddleLeft, Color.white);
        crateText = TextUI("Crates", top, new Vector2(0.28f, 0.55f), new Vector2(0.42f, 1), 20, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.4f));
        alertText = TextUI("Alerts", top, new Vector2(0.42f, 0.55f), Vector2.one, 18, TextAnchor.MiddleLeft, new Color(1f, 0.7f, 0.35f));
        string[] gk = { "o2", "water", "food", "battery", "health", "dose" };
        for (int i = 0; i < gk.Length; i++)
        {
            float x0 = i / 6f + 0.005f, x1 = (i + 1) / 6f - 0.005f;
            var g = new Gauge();
            g.label = TextUI("GL" + gk[i], top, new Vector2(x0, 0.28f), new Vector2(x1, 0.55f), 16, TextAnchor.MiddleLeft, Color.white);
            var bg = Panel("GB" + gk[i], top, new Vector2(x0 + 0.005f, 0.08f), new Vector2(x1 - 0.005f, 0.27f), new Color(1, 1, 1, 0.15f));
            var f = Panel("GF" + gk[i], bg, Vector2.zero, Vector2.one, new Color(0.3f, 0.8f, 0.45f));
            f.pivot = new Vector2(0, 0.5f);
            g.fill = f.GetComponent<Image>(); gauges[gk[i]] = g;
        }

        // bottom bar (60 px)
        var bot = Panel("BottomBar", root, Vector2.zero, new Vector2(1, 60f / 720f), PanelCol);
        infoText = TextUI("Info", bot, Vector2.zero, new Vector2(0.78f, 1), 18, TextAnchor.MiddleLeft, Color.white);
        runButton = ButtonUI("RunDay", bot, "RUN DAY", RunDay, 24);
        var rrt = (RectTransform)runButton.transform; rrt.anchorMin = new Vector2(0.80f, 0.1f); rrt.anchorMax = new Vector2(0.99f, 0.9f);
        rrt.offsetMin = rrt.offsetMax = Vector2.zero;
        runButton.GetComponent<Image>().color = new Color(0.15f, 0.6f, 0.35f);

        toastText = TextUI("Toast", root, new Vector2(0.2f, 0.74f), new Vector2(0.8f, 0.8f), 22, TextAnchor.MiddleCenter, new Color(1, 1, 0.7f));
        var shadow = toastText.gameObject.AddComponent<Shadow>(); shadow.effectColor = Color.black;

        // modal
        var overlay = Panel("Modal", root, Vector2.zero, Vector2.one, new Color(0, 0, 0, 0.55f));
        modalRoot = overlay.gameObject;
        var box = Panel("Box", overlay, new Vector2(0.14f, 0.1f), new Vector2(0.86f, 0.92f), new Color(0.08f, 0.11f, 0.2f));
        modalTitle = TextUI("Title", box, new Vector2(0, 0.88f), Vector2.one, 30, TextAnchor.MiddleCenter, new Color(0.6f, 0.85f, 1f));
        modalBody = TextUI("Body", box, new Vector2(0.03f, 0.34f), new Vector2(0.97f, 0.88f), 20, TextAnchor.UpperLeft, Color.white);
        modalButtons = Panel("Buttons", box, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.34f), new Color(0, 0, 0, 0));
        var grid = modalButtons.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(250, 44); grid.spacing = new Vector2(10, 8); grid.childAlignment = TextAnchor.UpperCenter;
        modalRoot.SetActive(false);
    }

    class Btn { public string label; public Action act; public bool enabled = true; public bool close = true; }

    void ShowModal(string title, string body, List<Btn> btns, int bodySize = 20)
    {
        mode = Mode.Modal;
        modalRoot.SetActive(true);
        modalTitle.text = title; modalBody.text = body; modalBody.fontSize = bodySize;
        foreach (Transform c in modalButtons) Destroy(c.gameObject);
        foreach (var b in btns)
        {
            var bb = b;
            var ui = ButtonUI("B", modalButtons, b.label, () => { if (bb.close) CloseModal(); bb.act?.Invoke(); }, 18);
            ui.interactable = b.enabled;
        }
    }
    void CloseModal() { modalRoot.SetActive(false); if (mode == Mode.Modal) mode = Mode.Play; }

    // ------------------------------------------------------------------ flow
    void ShowTitle()
    {
        mode = Mode.Title;
        ShowModal("PIONEER BASE",
            "Keep four astronauts alive on a 30-day mission.\n\nWalk to the stations (click them, or use WASD and E) to choose the day's plan, then press RUN DAY.\n" +
            "Power is limited. When the base runs short, the lights go out room by room.\n\nChoose a site:\n\n" + GameData.SourcesNote,
            new List<Btn> {
                new Btn { label = "Moon (15-day night)", act = () => StartGame("moon") },
                new Btn { label = "Mars (dust storms)", act = () => StartGame("mars") } }, 19);
        mode = Mode.Title;
    }

    void StartGame(string s)
    {
        site = s; seed = (int)(DateTime.Now.Ticks % 100000) + 1;
        st = Sim.NewGame(site, seed);
        plan = Sim.DefaultPlan(st);
        astro.localPosition = Vector2.zero; path.Clear(); pendingStation = null; armedStation = null;
        ClearFx(); SetPlanet(site);
        mode = Mode.Play;
        dayPop = 0.6f;
        Toast("Day 1. Plan your day, then press RUN DAY.");
        Refresh();
    }

    void Toast(string s) { toastText.text = s; toastUntil = Time.time + 5f; }

    void RunDay()
    {
        if (mode != Mode.Play || st == null || st.over) return;
        var rec = Sim.RunDay(st, plan);
        var sb = new List<string>();
        sb.AddRange(rec.events);
        if (rec.faultNew != null) sb.Add("A " + rec.faultNew + " module broke! Repair it in the Build Bay.");
        if (rec.emergencyDamage != null) sb.Add("The emergency broke the " + rec.emergencyDamage + ".");
        if (rec.repaired != null) sb.Add("The " + rec.repaired + " was repaired.");
        if (rec.harvest > 0) sb.Add("Harvest: +" + rec.harvest + " kg of food.");
        if (rec.science > 0) sb.Add("Science +" + rec.science + ".");
        if (rec.shut.Count > 0) sb.Add("Not enough power: " + string.Join(", ", rec.shut) + " switched off.");
        if (rec.o2Short) sb.Add("Oxygen ran out!");
        Toast(string.Join("  ", sb));
        OnDayResult(rec);

        plan.build.Clear(); plan.repair = false; plan.emergency = false;
        foreach (var a in rec.activated)
        {   // newly finished modules start switched on
            if (a == "greenhouse") plan.greenhouse = st.mod["greenhouse"];
            if (a == "drill") plan.drill = st.mod["drill"];
        }
        plan.greenhouse = Math.Min(plan.greenhouse, st.mod["greenhouse"]);
        plan.drill = Math.Min(plan.drill, st.mod["drill"]);

        Refresh();
        // let the event animations play before the next screen appears
        if (st.over) { mode = Mode.Modal; Invoke("ShowDebrief", 3.2f); return; }
        pendingCard = GameData.Cards.FirstOrDefault(c => c.afterDay == rec.day);
        if (pendingCard != null) { mode = Mode.Modal; Invoke("ShowPendingCard", 1.9f); }
    }
    Card pendingCard;
    void ShowPendingCard() { if (pendingCard != null) ShowCard(pendingCard); pendingCard = null; }

    void ShowCard(Card c)
    {
        ShowModal("Myth or Reality?", c.text, new List<Btn> {
            new Btn { label = "MYTH", close = false, act = () => AnswerCard(c, true) },
            new Btn { label = "REALITY", close = false, act = () => AnswerCard(c, false) } }, 26);
    }
    void AnswerCard(Card c, bool saidMyth)
    {
        if (saidMyth == c.myth) { Flash(new Color(0.3f, 1f, 0.5f, 0.35f), 0.6f); Confetti(40); } else { Flash(new Color(1f, 0.5f, 0.2f, 0.3f), 0.5f); }
        ShowModal(saidMyth == c.myth ? "Correct!" : "Not quite", c.text + "\n\n" + c.why + "\n\nSource: " + c.source,
            new List<Btn> { new Btn { label = "Continue" } }, 20);
    }

    void ShowDebrief()
    {
        var d = Debrief.Analyze(st);
        var sb = new StringBuilder();
        if (d.ok) sb.AppendLine(new string('*', d.stars) + "   " + d.stars + " of 3 stars");
        sb.AppendLine(d.headline).AppendLine();
        sb.AppendLine("What happened:");
        foreach (var s in d.chain) sb.AppendLine("  Day " + s.day + ": " + s.text);
        sb.AppendLine().AppendLine("The trade-off: " + d.tradeoff);
        sb.AppendLine().AppendLine("Next time: " + d.tip);
        ShowModal(d.ok ? "MISSION COMPLETE" : "DEBRIEF", sb.ToString(), new List<Btn> {
            new Btn { label = "Play again (" + site + ")", act = () => StartGame(site) },
            new Btn { label = "Other site", act = () => StartGame(site == "moon" ? "mars" : "moon") } }, 16);
    }

    // ------------------------------------------------------------------ stations
    void Interact(string id)
    {
        switch (id)
        {
            case "oxygen": plan.oxygen = !plan.oxygen; Feedback("oxygen", plan.oxygen ? "ON" : "OFF", plan.oxygen ? Accent("oxygen") : new Color(1f, 0.5f, 0.4f)); Refresh(); break;
            case "lab": plan.lab = !plan.lab; Feedback("lab", plan.lab ? "ON" : "OFF", plan.lab ? Accent("lab") : new Color(1f, 0.5f, 0.4f)); Refresh(); break;
            case "greenhouse":
                if (st.mod["greenhouse"] == 0) { Toast("No greenhouse yet. Build one in the Build Bay (it works the next day)."); break; }
                plan.greenhouse = (plan.greenhouse + 1) % (st.mod["greenhouse"] + 1); Feedback("greenhouse", plan.greenhouse + " ON", Accent("greenhouse")); Refresh(); break;
            case "drill":
                if (st.mod["drill"] == 0) { Toast("No ice drill yet. Build one in the Build Bay (it works the next day)."); break; }
                plan.drill = (plan.drill + 1) % (st.mod["drill"] + 1); Feedback("drill", plan.drill + " ON", Accent("drill")); Refresh(); break;
            case "kitchen": ShowRation(); break;
            case "airlock": ShowActivity(); break;
            case "build": ShowBuild(); break;
            case "command": RunDay(); break;
        }
    }

    void ShowRation()
    {
        Func<string, string, Btn> mk = (r, label) => new Btn { label = label + (plan.ration == r ? "  (chosen)" : ""), act = () => { plan.ration = r; Refresh(); } };
        ShowModal("KITCHEN: Rations", "Four astronauts eat 7.2 kg of food a day on Full rations.\n\nFull: crew health goes up if all needs are met.\nLean (75% food): health drops 1 a day.\nHalf (50% food): health drops 3 a day.\n\nFood left: " + Mathf.RoundToInt((float)st.food) + " kg.",
            new List<Btn> { mk("full", "Full rations"), mk("lean", "Lean rations"), mk("half", "Half rations") });
    }

    void ShowActivity()
    {
        Func<string, string, Btn> mk = (a, label) => new Btn { label = label + (plan.activity == a ? "  (chosen)" : ""), act = () => { plan.activity = a; Refresh(); } };
        var list = new List<Btn> { mk("science", "Science"), mk("maintenance", site == "mars" ? "Maintenance (cleans dust)" : "Maintenance") , mk("shelter", "Shelter (storm)") };
        var em = new Btn { label = st.emergencyUsed ? "Emergency used" : (plan.emergency ? "Emergency: ON" : "Emergency: fuel to water"), enabled = !st.emergencyUsed,
            act = () => { plan.emergency = !plan.emergency; Refresh(); } };
        list.Add(em);
        ShowModal("AIRLOCK: Today's activity",
            "Science: the lab earns 4 points (needs power).\nMaintenance: repairs faults" + (site == "mars" ? " and cleans the dust off the solar arrays" : "") + ".\nShelter: cuts a solar storm dose to one tenth.\n\nEmergency (once): turn rocket fuel into 40 L of water. 20% chance it breaks a module.", list);
    }

    int QueuedCount(string type) { return plan.build.Count(b => b == type); }
    int QueuedCost() { return plan.build.Sum(b => Sim.GetTypeInfo(b).cost); }
    bool CanQueue(string type)
    {
        var i = Sim.GetTypeInfo(type);
        return st.crates - QueuedCost() >= i.cost && Sim.Owned(st, type) + QueuedCount(type) < i.max;
    }
    void ShowBuild()
    {
        var list = new List<Btn>();
        foreach (var t in Sim.TYPES)
        {
            var type = t; var i = Sim.GetTypeInfo(type);
            list.Add(new Btn { label = i.name + " (" + i.cost + ")  x" + (Sim.Owned(st, type) + QueuedCount(type)), enabled = CanQueue(type), close = false,
                act = () => { plan.build.Add(type); Refresh(); ShowBuild(); } });
        }
        list.Add(new Btn { label = "Undo last", enabled = plan.build.Count > 0, close = false, act = () => { plan.build.RemoveAt(plan.build.Count - 1); Refresh(); ShowBuild(); } });
        bool canRepair = st.faults.Count > 0 && st.crates - QueuedCost() >= 1;
        list.Add(new Btn { label = plan.repair ? "Repair: ON (1 crate)" : "Repair a module (1 crate)", enabled = st.faults.Count > 0 && (plan.repair || canRepair), close = false,
            act = () => { plan.repair = !plan.repair; Refresh(); ShowBuild(); } });
        list.Add(new Btn { label = "Done" });
        string broken = st.faults.Count > 0 ? "\nBroken: " + string.Join(", ", st.faults.Select(f => f.type)) + ". Repair costs 1 crate, or three Maintenance days." : "";
        ShowModal("BUILD BAY", "Crates left: " + (st.crates - QueuedCost() - (plan.repair ? 1 : 0)) + " of " + st.crates + ". No more crates arrive.\nNew modules start working tomorrow. Number = built + queued." + broken, list);
    }

    // ------------------------------------------------------------------ refresh / lights
    static readonly Dictionary<string, string> SysName = new Dictionary<string, string> { { "oxygen", "oxygen plant" }, { "lab", "science lab" }, { "greenhouse", "greenhouse" }, { "drill", "ice drill" } };

    void Refresh()
    {
        if (st == null) return;
        pv = Sim.Preview(st, plan);
        dayText.text = "Day " + Mathf.Min(st.day, Sim.MAX_DAY) + " / " + Sim.MAX_DAY + "   " + (site == "moon" ? "MOON" : "MARS");
        crateText.text = "Crates: " + (st.crates - QueuedCost() - (plan.repair ? 1 : 0));
        var alerts = Sim.AlertsFor(st, st.day).Select(a => a.text).ToList();
        if (st.faults.Count > 0) alerts.Add("Broken: " + string.Join(", ", st.faults.Select(f => SysName[f.type])) + ".");
        alertText.text = string.Join("\n", alerts);

        double cap = V("battery_per") * st.mod["battery"];
        SetGauge("o2", "Oxygen", st.o2, 40, "kg", pv.runoutO2);
        SetGauge("water", "Water", st.water, 120, "L", pv.runoutWater);
        SetGauge("food", "Food", st.food, 250, "kg", pv.runoutFood);
        SetGauge("battery", "Battery", st.battery, cap, "", pv.runoutBattery);
        SetGauge("health", "Health", st.health, 100, "", null);
        SetGauge("dose", "Dose", st.dose, 40, "mSv", null, true);

        var sb = new StringBuilder("Energy today: made " + Fmt(pv.supply) + ", need " + Fmt(pv.demand));
        if (pv.shut.Count > 0) sb.Append("  -  NOT ENOUGH POWER, off: " + string.Join(", ", pv.shut));
        else sb.Append("  -  ok");
        sb.Append("   |   Ration: " + plan.ration + "   Activity: " + plan.activity);
        infoText.text = sb.ToString();

        // room lights: dark when the player switched a system off, darker when the base cannot power it
        foreach (var r in rooms.Values)
        {
            string state = ""; float a = 0f;
            bool broken = r.system != null && st.faults.Any(f => f.type == r.system);
            switch (r.id)
            {
                case "oxygen": state = plan.oxygen ? "ON" : "OFF"; if (!plan.oxygen) a = 0.45f; break;
                case "lab": state = plan.lab ? "ON" : "OFF"; if (!plan.lab) a = 0.45f; break;
                case "greenhouse": state = st.mod["greenhouse"] == 0 ? "not built" : plan.greenhouse + " / " + st.mod["greenhouse"] + " on"; if (st.mod["greenhouse"] == 0 || plan.greenhouse == 0) a = 0.45f; break;
                case "drill": state = st.mod["drill"] == 0 ? "not built" : plan.drill + " / " + st.mod["drill"] + " on"; if (st.mod["drill"] == 0 || plan.drill == 0) a = 0.45f; break;
                case "kitchen": state = plan.ration + " rations"; break;
                case "airlock": state = plan.activity; break;
                case "build": state = plan.build.Count > 0 ? plan.build.Count + " queued" : "ready"; break;
                case "command": state = "run the day"; break;
            }
            if (r.system != null && (pv.shut.Contains(r.system) || pv.idle.Contains(r.system))) { a = 0.85f; state = "NO POWER"; }
            if (broken) { a = 0.85f; state = "BROKEN"; }
            r.targetDark = a; r.broken = broken; r.noPower = state == "NO POWER";
            r.label.text = r.name + "\n" + state;
        }
        UpdateTimeline();
        hallDark.color = new Color(0, 0, 0, pv.habOn ? 0f : 0.8f);
        runButton.interactable = mode == Mode.Play && !st.over;
    }

    static double V(string id) { return GameData.V(id); }
    static string Fmt(double x) { return (Math.Round(x * 10) / 10).ToString("0.#"); }
    void SetGauge(string key, string name, double val, double max, string unit, int? runout, bool invert = false)
    {
        var g = gauges[key]; float f = max <= 0 ? 0 : Mathf.Clamp01((float)(val / max));
        g.label.text = name + " " + Fmt(val) + (max > 0 ? "/" + Fmt(max) : "") + (unit != "" ? " " + unit : "") + (runout.HasValue ? "  (0 in " + runout.Value + "d)" : "");
        var rt = (RectTransform)g.fill.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(f, 1);
        float good = invert ? 1 - f : f;
        g.fill.color = good > 0.5f ? new Color(0.3f, 0.8f, 0.45f) : (good > 0.25f ? new Color(0.95f, 0.75f, 0.25f) : new Color(0.9f, 0.3f, 0.3f));
        g.low = good <= 0.25f;
    }

    // ------------------------------------------------------------------ update
    void Update()
    {
        if (toastText != null && Time.time > toastUntil) toastText.text = "";
        AnimateAstronaut();
        AnimateWorld();
        UpdateFx();
        // always show the whole base, even on a narrow (phone) screen
        if (cam != null) cam.orthographicSize = Mathf.Max(5.4f, 9.9f / Mathf.Max(0.2f, cam.aspect));
        if (runButton != null) runButton.interactable = mode == Mode.Play && st != null && !st.over;
        if (mode != Mode.Play || st == null) return;

        // keyboard movement
        var dir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (dir.sqrMagnitude > 0.01f)
        {
            path.Clear(); pendingStation = null;
            Move((Vector2)astro.localPosition + dir.normalized * 6f * Time.deltaTime);
        }
        else if (path.Count > 0)
        {
            var next = path[0]; var pos = (Vector2)astro.localPosition;
            var np = Vector2.MoveTowards(pos, next, 7f * Time.deltaTime);
            Move(np);
            if ((np - next).sqrMagnitude < 0.0001f)
            {
                path.RemoveAt(0);
                if (path.Count == 0 && pendingStation != null) { var id = pendingStation; pendingStation = null; if (id != "command") armedStation = id; Interact(id); return; }
            }
        }
        // walking into a room acts at once (once per visit). The Command console never fires by itself.
        var here = (Vector2)astro.localPosition;
        var entered = Nearest(here, 0.9f);
        if (entered != null)
        {
            if (armedStation != entered.id && entered.id != "command")
            {
                armedStation = entered.id;
                if (pendingStation == entered.id) pendingStation = null;   // already acting: do not act again on arrival
                Interact(entered.id); return;
            }
        }
        else if (Nearest(here, 1.9f) == null) armedStation = null;
        UpdatePrompt();
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            var near = Nearest(here, 1.6f);
            if (near != null) Interact(near.id);
        }
        if (Input.GetMouseButtonDown(0) && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            var wp = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
            var s = Nearest(wp, 1.4f);
            if (s != null) GoTo(s.pos, s.id); else GoTo(wp, null);
        }
    }

    Station Nearest(Vector2 p, float maxDist)
    {
        Station best = null; float bd = maxDist;
        foreach (var s in stations) { float d = Vector2.Distance(p, s.pos); if (d < bd) { bd = d; best = s; } }
        return best;
    }

    void Move(Vector2 p)
    {
        p.x = Mathf.Clamp(p.x, -9.2f, 9.2f); p.y = Mathf.Clamp(p.y, -5f, 5f);
        var old = (Vector2)astro.localPosition;
        if (Mathf.Abs(p.x - old.x) > 0.001f) facing = p.x > old.x ? 1f : -1f;
        if ((p - old).sqrMagnitude > 1e-6f) moving = true;
        astro.localPosition = p;
    }

    void GoTo(Vector2 target, string stationId)
    {
        target.x = Mathf.Clamp(target.x, -9.2f, 9.2f); target.y = Mathf.Clamp(target.y, -5f, 5f);
        var pos = (Vector2)astro.localPosition;
        path.Clear(); pendingStation = stationId;
        bool inHall = Mathf.Abs(pos.y) < 1.1f, targetInHall = Mathf.Abs(target.y) < 1.1f;
        if (!(inHall && targetInHall))
        {
            if (!inHall) path.Add(new Vector2(pos.x, 0));
            path.Add(new Vector2(target.x, 0));
        }
        path.Add(target);
    }
}
