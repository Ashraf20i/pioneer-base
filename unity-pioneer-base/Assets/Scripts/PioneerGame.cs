// Pioneer Base 3D - PioneerGame.cs
// Game flow, input and menus. The whole scene is generated at runtime, so the scene file only
// needs a camera and this component. Game rules live in Assets/Scripts/Core and are not changed here.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PioneerBase;

public partial class PioneerGame : MonoBehaviour
{
    enum Mode { Title, Play, Modal, Cine }
    Mode mode = Mode.Title;

    GameState st; Plan plan; PreviewResult pv;
    string site = "mars"; int seed = 1;
    Camera cam; Font font; Transform world;
    bool shots, lapseActive, tipBuild, tipWarned;

    static double V(string id) { return GameData.V(id); }
    static string Fmt(double x) { return (Math.Round(x * 10) / 10).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture); }
    string DayWord { get { return site == "mars" ? "SOL" : "DAY"; } }
    string dayWord { get { return site == "mars" ? "sol" : "day"; } }

    // ------------------------------------------------------------------ setup
    void Start()
    {
        Application.targetFrameRate = 60;
        LoadFonts();
        try { shots = Environment.GetCommandLineArgs().Any(a => a == "-pbshots"); } catch (Exception) { shots = false; }
        cam = Camera.main;
        if (cam == null) { var cg = new GameObject("Main Camera"); cg.tag = "MainCamera"; cam = cg.AddComponent<Camera>(); }
        cam.orthographic = false; cam.fieldOfView = 55f; cam.nearClipPlane = 0.3f; cam.farClipPlane = 2600f; cam.rect = new Rect(0, 0, 1, 1);
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        if (FindObjectOfType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowDistance = 95f; QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowCascades = 1; QualitySettings.pixelLightCount = 4; QualitySettings.antiAliasing = 2;
        world = new GameObject("World").transform;
        if (font == null) KeepPhysicsTypes();
        InitMaterials(); InitFx(); InitAudio(); BuildSky(); BuildUI();
        if (FindObjectOfType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        st = Sim.NewGame(site, 7); plan = Sim.DefaultPlan(st);
        BuildSite(site); Refresh();
        ShowTitle();
        if (shots) StartCoroutine(ShotRun());
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) { if (paused) ResumeGame(); else if (mode == Mode.Play) ShowPauseMenu(); }
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (mode == Mode.Play && !journey) HandleInput(dt);
        if (journey) TickJourney(dt);
        TickActors(dt); TickWorld(dt); TickCamera(dt); TickSky(dt); TickFx(dt); TickUI(dt);
    }

    // Never runs. It only tells the WebGL build to keep the collider types that CreatePrimitive needs.
    void KeepPhysicsTypes()
    {
        gameObject.AddComponent<SphereCollider>(); gameObject.AddComponent<CapsuleCollider>(); gameObject.AddComponent<BoxCollider>(); gameObject.AddComponent<MeshCollider>();
    }

    // ------------------------------------------------------------------ input
    Vector3 mDown, mLast; bool mTrack, mDrag; Vector3? walkTarget; Mod walkMod; float walkTimer;
    static bool K(KeyCode k) { return Input.GetKey(k); }

    void HandleInput(float dt)
    {
        float f = 0, r = 0;
        if (K(KeyCode.W) || K(KeyCode.Z) || K(KeyCode.UpArrow)) f += 1; if (K(KeyCode.S) || K(KeyCode.DownArrow)) f -= 1;
        if (K(KeyCode.D) || K(KeyCode.RightArrow)) r += 1; if (K(KeyCode.A) || K(KeyCode.Q) || K(KeyCode.LeftArrow)) r -= 1;
        if (f != 0 || r != 0)
        {
            walkTarget = null; walkMod = null; var q = Quaternion.Euler(0, camYaw, 0);
            StepPlayer(q * Vector3.forward * f + q * Vector3.right * r, dt);
        }
        else if (walkTarget.HasValue)
        {
            Vector3 to = walkTarget.Value - player.pos; to.y = 0; float d = to.magnitude; walkTimer -= dt;
            bool arrived = d < 0.5f || walkTimer < 0;
            if (walkMod != null && Flat(player.pos, walkMod.pos) < walkMod.radius + 2.4f) arrived = true;
            if (arrived) { var m = walkMod; walkTarget = null; walkMod = null; if (m != null) Interact(m.station); }
            else StepPlayer(to / d, dt);
        }
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            var m = NearMod(3.4f); if (m != null) Interact(m.station);
        }
        if (Input.GetMouseButtonDown(0)) { mTrack = !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()); mDown = mLast = Input.mousePosition; mDrag = false; }
        if (Input.GetMouseButton(0) && mTrack)
        {
            Vector3 d = Input.mousePosition - mLast; mLast = Input.mousePosition;
            if (!mDrag && (Input.mousePosition - mDown).magnitude > 12f) mDrag = true;
            if (mDrag) { camYaw += d.x * 0.3f; camPitch = Mathf.Clamp(camPitch - d.y * 0.2f, 4f, 72f); }
        }
        if (Input.GetMouseButtonUp(0) && mTrack) { mTrack = false; if (!mDrag) Click(Input.mousePosition); }
        if (Input.GetMouseButton(1)) { camYaw += Input.GetAxis("Mouse X") * 4f; camPitch = Mathf.Clamp(camPitch - Input.GetAxis("Mouse Y") * 3f, 4f, 72f); }
        camDist = Mathf.Clamp(camDist - Input.mouseScrollDelta.y * 1.3f, 5f, 36f);
    }
    static float Flat(Vector3 a, Vector3 b) { float dx = a.x - b.x, dz = a.z - b.z; return Mathf.Sqrt(dx * dx + dz * dz); }

    void Click(Vector3 screen)
    {
        RaycastHit hit;
        if (!Physics.Raycast(cam.ScreenPointToRay(screen), out hit, 600f)) return;
        Mod m;
        if (modByCol.TryGetValue(hit.collider, out m)) { WalkTo(m); return; }
        Vector3 p = hit.point; float rr = Mathf.Sqrt(p.x * p.x + p.z * p.z); if (rr > 66f) { p.x *= 66f / rr; p.z *= 66f / rr; }
        walkTarget = p; walkMod = null; walkTimer = 12f; Burst(Ground(p) + Vector3.up * 0.2f, C(0.4f, 0.9f, 1f), 6, 1.5f, 0.3f, 0.5f);
    }
    void WalkTo(Mod m) { walkTarget = m.ipos; walkMod = m; walkTimer = 14f; }

    Mod NearMod(float range)
    {
        Mod best = null; float bd = 1e9f;
        foreach (var m in mods)
        {
            if (!m.root.gameObject.activeInHierarchy) continue;
            float d = Mathf.Min(Flat(player.pos, m.ipos), Flat(player.pos, m.pos) - m.radius * 0.6f + 1.2f); if (d < range && d < bd) { bd = d; best = m; }
        }
        return best;
    }

    // ------------------------------------------------------------------ flow
    void ShowTitle()
    {
        mode = Mode.Title;
        ShowModal("Pioneer Base",
            "Four astronauts. Thirty days. One outpost.\n\nWalk around the base (WASD or ZQSD, or click the ground). Click a module to use it. " +
            "Drag to look around, scroll to zoom. Spend your 14 crates at the Supply Depot, plan each day, then press END SOL.\n\n" +
            "Hard mode starts with a crash 40 km from the outpost: drive the rover there and solve chemistry and physics challenges on the way.",
            new List<Btn> {
                new Btn { label = "MARS  ·  dust storms", act = () => StartGame("mars"), primary = true },
                new Btn { label = "MOON  ·  15-day night", act = () => StartGame("moon"), primary = true },
                new Btn { label = "HARD  ·  Crash Landing", act = () => StartHard() },
                new Btn { label = "DATA SOURCES", close = false, act = () => ShowSources() } }, 17,
            "JUNIOR ASTRONAUT MISSION TRAINER  ·  SELECT A SITE", false, GameData.SourcesNote.ToUpper(), UiAccent);
        modalFooter.color = UiMuted;
    }

    void StartGame(string s, GameState preset = null)
    {
        journey = false; hardMode = preset != null; SetJourneyHud(false); debugStorm = -1f;
        site = s; seed = (int)(DateTime.Now.Ticks % 100000) + 1;
        st = preset ?? Sim.NewGame(site, seed); plan = Sim.DefaultPlan(st); pv = null;
        ClearFx(); bannerQueue.Clear(); pipQueue.Clear(); pipFull = ""; pipShown = 0; pipRoot.SetActive(false); bannerT = 0;
        dayT = 0.34f; moonDayF = 1f; camYaw = 0f; camPitch = 20f; camDist = 13f; tipBuild = false; tipWarned = false; cardMod = null; cardRoot.SetActive(false);
        BuildSite(site); Refresh();
        modalRoot.SetActive(false);
        StartCoroutine(Intro());
    }

    IEnumerator Intro()
    {
        mode = Mode.Cine; Letterbox(true); cineActive = true; cineSpeed = 1.1f;
        camPos = new Vector3(60f, 75f, -150f); camLook = new Vector3(0, 0, 0);
        cinePos = new Vector3(10f, 9f, -30f); cineLook = new Vector3(0, 3f, 0);
        BannerQ(DayWord + " 1", hardMode ? "Hard mode. Thirty sols with what the journey left you." : (site == "mars" ? "Martian outpost. Thirty sols until the relief crew arrives." : "Lunar outpost. Thirty days until the relief crew arrives."), C(0.7f, 0.92f, 1f));
        yield return new WaitForSeconds(shots ? 1.2f : 3.6f);
        cineActive = false; Letterbox(false); mode = Mode.Play; Refresh();
        if (hardMode) { PipSay("We made it to the outpost. Stocks are low and you have " + st.crates + " crates. Spend them at the Supply Depot, then plan each sol.", 3); yield break; }
        PipSay("I am PIP, your base assistant. Your crew needs air, water, food and power for 30 " + dayWord + "s. Start at the Supply Depot: you have 14 crates.", 0);
        if (site == "moon") PipSay("We are on the Moon. The sky is black even by day, because there is no air. Night comes on day 11 and lasts 15 days.", 1);
        else PipSay("We are on Mars. Sunlight here is only 43% of what the Moon gets, and dust settles on the arrays every sol.", 1);
    }

    void Refresh()
    {
        if (st == null) return;
        pv = Sim.Preview(st, plan);
        RefreshHud(); RefreshWorld();
        if (mode == Mode.Play && pv.shut.Count > 0 && !tipWarned) { tipWarned = true; PipSay("Warning: not enough power for today's plan. First to switch off: " + string.Join(", ", pv.shut.Distinct()) + ".", 1); }
        if (pv.shut.Count == 0) tipWarned = false;
    }

    IEnumerator EndSol()
    {
        if (mode != Mode.Play || st == null || st.over) yield break;
        mode = Mode.Cine; Letterbox(true); cineActive = true; cineSpeed = 1.6f; lapseActive = true; walkTarget = null; walkMod = null;
        float ang = camYaw * Mathf.Deg2Rad;
        cinePos = new Vector3(-Mathf.Sin(ang) * 42f + 8f, 17f, -Mathf.Cos(ang) * 42f); cineLook = new Vector3(0, 3f, 2f);
        Sfx("on");
        float T = shots ? 1.6f : 4.4f, t = 0; DayRecord rec = null; float t0 = dayT, m0 = moonDayF; int dayRun = st.day;
        while (t < T)
        {
            t += Time.deltaTime; float k = Mathf.Clamp01(t / T), e = k * k * (3f - 2f * k);
            dayT = t0 + e; moonDayF = m0 + e;
            if (rec == null && k >= 0.6f) { rec = Sim.RunDay(st, plan); AfterRun(rec); }
            yield return null;
        }
        if (rec == null) { rec = Sim.RunDay(st, plan); AfterRun(rec); }
        dayT = t0; moonDayF = st.over ? m0 + 1f : st.day; lapseActive = false;
        OnDayResult(rec); solPop = 0.7f;
        // at most two banners a day (the first and the most serious), so a bad day does not drag on
        if (bannerQueue.Count > 2) { var all = bannerQueue.ToArray(); bannerQueue.Clear(); bannerQueue.Enqueue(all[0]); bannerQueue.Enqueue(all[all.Length - 1]); }
        yield return new WaitForSeconds(shots ? 0.5f : 1.3f);
        while (bannerQueue.Count > 0 || bannerT > 0.5f) yield return null;
        if (st.over) { yield return Ending(); cineActive = false; Letterbox(false); ShowDebrief(); yield break; }
        cineActive = false; Letterbox(false); mode = Mode.Play; Refresh();
        MorningBrief();
        var card = GameData.Cards.FirstOrDefault(c => c.afterDay == rec.day);
        if (card != null) ShowCard(card);
    }

    void AfterRun(DayRecord rec)
    {
        plan.build.Clear(); plan.repair = false; plan.emergency = false;
        foreach (var a in rec.activated)
        {   // newly finished modules start switched on
            if (a == "greenhouse") plan.greenhouse = st.mod["greenhouse"];
            if (a == "drill") plan.drill = st.mod["drill"];
        }
        plan.greenhouse = Math.Min(plan.greenhouse, st.mod["greenhouse"]);
        plan.drill = Math.Min(plan.drill, st.mod["drill"]);
        if (plan.activity == "shelter" && !Sim.EnvFor(st, st.day).solarStorm) plan.activity = "science";
        if (!st.over) Refresh();   // when the mission is over, the base keeps the state it ended in
    }

    static readonly string[] MarsFacts = {
        "One astronaut breathes 0.84 kg of oxygen a day. Four need 3.36 kg. (Real value)",
        "The space station recovers about 98% of its water. This outpost recycles 90%.",
        "Radiation on the Mars surface is about 0.67 mSv a day. The Curiosity rover measured it.",
        "Fun fact: sunsets on Mars look blue. Watch the Sun when you end the sol.",
        "NASA's MOXIE test made oxygen from Martian air. Our oxygen plant uses the same idea.",
        "A radio message to Earth takes 3 to 22 minutes from Mars. In an emergency, you decide alone." };
    static readonly string[] MoonFacts = {
        "One astronaut breathes 0.84 kg of oxygen a day. Four need 3.36 kg. (Real value)",
        "Radiation on the Moon is about 1.37 mSv a day. There is no air to block it.",
        "Splitting water gives oxygen: 4.5 litres of water for 4 kg of oxygen.",
        "A night on the Moon lasts about 14.8 Earth days. The arrays make no power the whole time.",
        "The space station recovers about 98% of its water. This outpost recycles 90%.",
        "A radio message to Earth takes about 1.3 seconds from the Moon. Mission Control can help quickly." };

    void MorningBrief()
    {
        var alerts = Sim.AlertsFor(st, st.day);
        if (alerts.Count > 0) { PipSay(alerts[0].text, alerts[0].daysLeft == 0 ? 2 : 1); if (alerts[0].type == "solar" && alerts[0].daysLeft == 0) PipSay("Choose the Shelter activity today. It cuts the storm dose to one tenth.", 2); }
        else if (pv.runoutO2.HasValue && pv.runoutO2.Value <= 3) PipSay("Oxygen runs out in " + pv.runoutO2.Value + " " + dayWord + "s at this rate.", 2);
        else if (pv.runoutWater.HasValue && pv.runoutWater.Value <= 3) PipSay("Water runs out in " + pv.runoutWater.Value + " " + dayWord + "s at this rate.", 2);
        else if (pv.runoutFood.HasValue && pv.runoutFood.Value <= 4) PipSay("Food runs out in " + pv.runoutFood.Value + " " + dayWord + "s at this rate.", 1);
        else if (st.faults.Count > 0) PipSay("The " + SysName(st.faults[0].type) + " is broken. Repair it at the Supply Depot for 1 crate, or use three Maintenance days.", 1);
        else { var f = site == "mars" ? MarsFacts : MoonFacts; PipSay(f[(st.day + seed) % f.Length], 0); }
    }

    IEnumerator Ending()
    {
        cineActive = true; Letterbox(true); cineSpeed = 1.4f;
        bool ok = st.ending.type == "success";
        float T = shots ? 1.6f : 5.2f, t = 0;
        if (ok)
        {
            Vector3 land = Ground(new Vector3(15f, 0, -36f)); reliefShip.SetActive(true);
            cinePos = new Vector3(-6f, 5f, -52f); cineLook = land + Vector3.up * 14f;
            BannerQ("MISSION COMPLETE", "The relief crew has arrived.  " + st.ending.stars + " of 3 stars", C(0.55f, 1f, 0.72f)); Sfx("win"); rocketOn = true;
            while (t < T)
            {
                t += Time.deltaTime; float k = Mathf.Clamp01(t / T), e = 1f - (1f - k) * (1f - k);
                var p = land + Vector3.up * Mathf.Lerp(95f, 0f, e); reliefShip.transform.position = p; cineLook = p + Vector3.up * 4f;
                if (k < 0.97f) for (int i = 0; i < 3; i++) Emit(psGlow, p + Vector3.up * 0.8f + UnityEngine.Random.insideUnitSphere * 0.5f, Vector3.down * UnityEngine.Random.Range(8f, 16f), C(1f, 0.7f, 0.3f), UnityEngine.Random.Range(1f, 2.2f), 0.5f);
                if (p.y - land.y < 12f) DustPuff(land + UnityEngine.Random.insideUnitSphere * 3f, 2);
                if (UnityEngine.Random.value < Time.deltaTime * 1.5f) Fireworks(Vector3.zero);
                yield return null;
            }
            rocketOn = false; Fireworks(Vector3.zero); Flash(C(0.6f, 1f, 0.8f, 0.35f), 1f);
        }
        else
        {
            Vector3 p0 = landerMod.pos; cinePos = p0 + new Vector3(18f, 4f, -14f); cineLook = p0 + Vector3.up * 5f;
            BannerQ(st.ending.reason == "dose" ? "EMERGENCY EVACUATION" : "MEDICAL EVACUATION", DayWord + " " + st.ending.day + ". The crew is going home safely.", C(1f, 0.4f, 0.34f));
            Sfx("alarm"); Flash(C(1f, 0.05f, 0.05f, 0.45f), 1.2f); rocketOn = true; Shake(0.4f);
            foreach (var c in crew) c.root.gameObject.SetActive(false);
            player.root.gameObject.SetActive(false); pip.gameObject.SetActive(false);
            while (t < T)
            {
                t += Time.deltaTime; float k = Mathf.Clamp01(t / T);
                var p = p0 + Vector3.up * (k * k * 120f); landerMod.root.position = p; cineLook = p + Vector3.up * 4f;
                for (int i = 0; i < 3; i++) Emit(psGlow, p + Vector3.up * 0.8f + UnityEngine.Random.insideUnitSphere * 0.5f, Vector3.down * UnityEngine.Random.Range(10f, 18f), C(1f, 0.7f, 0.3f), UnityEngine.Random.Range(1f, 2.4f), 0.6f);
                if (k < 0.3f) DustPuff(p0 + UnityEngine.Random.insideUnitSphere * 3f, 3);
                Smoke(p, C(0.8f, 0.8f, 0.8f, 0.35f), 2.5f, 2.5f);
                yield return null;
            }
            rocketOn = false;
        }
        yield return new WaitForSeconds(shots ? 0.3f : 1.2f);
    }

    // ------------------------------------------------------------------ pause menu
    bool paused;
    void ShowPauseMenu()
    {
        if (mode != Mode.Play && !paused) return;
        paused = true; Time.timeScale = 0f; Sfx("click");
        string where = journey ? "Hard mode, sol " + js.sol + ", " + Fmt(Journey.KmLeft(js)) + " km from the outpost."
            : (site == "moon" ? "Moon" : "Mars") + (hardMode ? " (hard mode)" : "") + ", " + dayWord + " " + st.day + " of " + Sim.MAX_DAY + ".";
        mode = Mode.Modal;
        ShowModal("Mission paused", where + "\n\nRestarting begins the same mission again from " + dayWord + " 1. Your progress in this mission will be lost.",
            new List<Btn> {
                new Btn { label = "RESUME", act = () => ResumeGame(), primary = true },
                new Btn { label = "RESTART MISSION", act = () => { Unpause(); if (journey || hardMode) StartHard(); else StartGame(site); } },
                new Btn { label = "MAIN MENU", act = () => { Unpause(); journey = false; hardMode = false; SetJourneyHud(false); ShowTitle(); } },
                new Btn { label = "DATA SOURCES", close = false, act = () => ShowSources() } }, 17,
            "PAUSE  ·  ESC TO RESUME", false, null, UiAccent);
    }
    void Unpause() { paused = false; Time.timeScale = 1f; }
    void ResumeGame() { Unpause(); modalRoot.SetActive(false); mode = Mode.Play; }

    // Every number in the game, with its tag and its NASA source
    static readonly Dictionary<string, string> SrcName = new Dictionary<string, string> {
        { "sun_earth", "NASA TSIS-1" }, { "mars_au", "NASA Mars Fact Sheet" }, { "lunar_night", "NASA Moon Facts" },
        { "dose_mars", "NASA Curiosity RAD (Hassler et al. 2014)" }, { "o2_person", "NASA life-support values (ECLSS)" },
        { "food_person", "NASA life-support values (ECLSS)" }, { "iss_water", "NASA ECLSS water milestone, 2023" },
        { "career_limit", "NASA-STD-3001" }, { "mars_sun_ratio", "from the NASA Mars Fact Sheet" }, { "o2p_water_moon", "chemistry: 2 H2O -> 2 H2 + O2" },
        { "crew_water", "4 astronauts x 3 L" }, { "comms_moon", "from the NASA Moon Fact Sheet" }, { "comms_mars", "from the NASA Mars Fact Sheet" },
        { "dose_moon", "about 2 x the NASA Curiosity value" } };
    void ShowSources()
    {
        var sb = new StringBuilder();
        foreach (var tag in new[] { "Real", "Derived" })
        {
            sb.AppendLine(tag == "Real" ? "REAL  ·  measured or published by NASA" : "DERIVED  ·  calculated from real values");
            foreach (var r in GameData.Rows.Where(x => x.tag == tag))
                sb.AppendLine("  " + r.label + ": " + Fmt(r.value) + " " + r.unit + (SrcName.ContainsKey(r.id) ? "   (" + SrcName[r.id] + ")" : ""));
            sb.AppendLine();
        }
        sb.AppendLine("SIMPLIFIED  ·  " + GameData.Rows.Count(x => x.tag == "Simplified") + " game values, made simple on purpose (crates, module sizes, the Moon dose " + Fmt(V("dose_moon")) + " mSv/day). Real values are never changed to make the game easier.");
        sb.AppendLine("Solar storm days come from two real storms in NASA DONKI: 11 May 2024 and 3 October 2024. Full list with links: SOURCES.md in the repository.");
        ShowModal("Where do these numbers come from?", sb.ToString(), new List<Btn> { new Btn { label = "BACK", act = () => { if (paused) ShowPauseMenu(); else ShowTitle(); }, primary = true } }, 13,
            "DATA SOURCES  ·  ALL NASA", false, null, UiAccent);
    }

    void ShowCard(Card c)
    {
        ShowModal("Myth or reality?", c.text, new List<Btn> {
            new Btn { label = "Myth", close = false, act = () => AnswerCard(c, true) },
            new Btn { label = "Reality", close = false, act = () => AnswerCard(c, false) } }, 24, "SCIENCE CHECK  ·  MYTH OR REALITY", true);
    }
    void AnswerCard(Card c, bool saidMyth)
    {
        bool right = saidMyth == c.myth;
        if (right) { Flash(C(0.3f, 1f, 0.5f, 0.3f), 0.6f); Sfx("good"); Fireworks(player.pos); } else { Flash(C(1f, 0.5f, 0.2f, 0.25f), 0.5f); Sfx("off"); }
        ShowModal(c.myth ? "It is a myth." : "It is real.", c.why, new List<Btn> { new Btn { label = "CONTINUE" } }, 19,
            right ? "CORRECT" : "NOT QUITE", false, "SOURCE  ·  " + c.source, right ? UiGood : UiAmber);
    }

    void ShowDebrief()
    {
        var d = Debrief.Analyze(st);
        var sb = new StringBuilder();
        sb.AppendLine(d.headline).AppendLine();
        sb.AppendLine("WHAT HAPPENED");
        foreach (var s in d.chain) sb.AppendLine("  " + (site == "mars" ? "Sol " : "Day ") + s.day + ": " + s.text);
        sb.AppendLine().AppendLine("THE TRADE-OFF  " + d.tradeoff);
        sb.AppendLine().AppendLine("NEXT TIME  " + d.tip);
        mode = Mode.Modal;
        ShowModal(d.ok ? "Mission complete" : (st.ending.reason == "dose" ? "Emergency evacuation" : "Medical evacuation"), sb.ToString(), new List<Btn> {
            new Btn { label = "PLAY AGAIN  ·  " + site.ToUpper(), act = () => StartGame(site), primary = true },
            new Btn { label = "OTHER SITE", act = () => StartGame(site == "moon" ? "mars" : "moon") },
            new Btn { label = "HARD MODE", act = () => StartHard() } }, 15,
            "MISSION DEBRIEF  ·  " + DayWord + " " + st.ending.day + (d.ok ? "  ·  " + d.stars + " OF 3 STARS" : ""), false, null, d.ok ? UiGood : UiBad);
    }

    // ------------------------------------------------------------------ modules
    void Describe(string id, out string body, out string action)
    {
        action = null; body = "";
        double o2p = site == "moon" ? V("o2p_power_moon") : V("o2p_power_mars");
        switch (id)
        {
            case "oxygen":
                body = (site == "mars" ? "Takes oxygen from the CO2 in the air, like NASA's MOXIE test." : "Splits water into oxygen: " + Fmt(V("o2p_water_moon")) + " L of water a day.")
                    + " Makes " + Fmt(V("o2p_out")) + " kg a day and uses " + Fmt(o2p) + " energy.\nReal value: four astronauts breathe " + Fmt(4 * V("o2_person")) + " kg a day.\nState: " + oxyMod.state;
                action = plan.oxygen ? "Switch OFF" : "Switch ON"; break;
            case "lab":
                body = "Earns " + Fmt(V("lab_science")) + " science points on a Science day. Uses " + Fmt(V("lab_power")) + " energy.\nIt is the first system to switch off when power is short.\nScience so far: " + Fmt(st.science) + " (3 stars need " + Fmt(V("star3_science")) + ").\nState: " + labMod.state;
                action = plan.lab ? "Switch OFF" : "Switch ON"; break;
            case "greenhouse":
                body = "Each greenhouse uses " + Fmt(V("gh_power")) + " energy and " + Fmt(V("gh_water")) + " L of water a day, and gives " + Fmt(V("gh_yield")) + " kg of food every " + Fmt(V("gh_cycle")) + " days.\nReal life is slower: lettuce on the space station takes about a month.\nRunning: " + plan.greenhouse + " of " + st.mod["greenhouse"];
                action = "Change how many run"; break;
            case "drill":
                body = "Melts buried ice into water: " + Fmt(V("drill_out")) + " L a day for " + Fmt(V("drill_power")) + " energy.\nThe crew uses " + Fmt(V("crew_water")) + " L a day before recycling.\nRunning: " + plan.drill + " of " + st.mod["drill"];
                action = "Change how many run"; break;
            case "hab":
                body = "Air pressure, heat and water recycling (" + Mathf.RoundToInt((float)V("hab_recycle") * 100) + "% here, about " + Fmt(V("iss_water")) + "% on the space station). Uses " + Fmt(V("hab_power")) + " energy.\nShield layers: " + st.mod["shield"] + ". Each layer lets through 75% of the radiation.\nRations: " + plan.ration + ".  Activity: " + plan.activity + ".";
                action = "Plan the " + dayWord; break;
            case "depot":
                body = "Crates left: " + (st.crates - QueuedCost() - (plan.repair ? 1 : 0)) + " of " + Fmt(V("crates_start")) + ". No more crates arrive, so every choice is a trade-off.\nNew modules start working the next " + dayWord + ".";
                action = "Build or repair"; break;
            case "lander":
                body = "The way home. One-time emergency: turn rocket fuel into " + Fmt(V("emerg_water")) + " L of water, with a " + Mathf.RoundToInt((float)V("emerg_risk") * 100) + "% risk of breaking a module.\nState: " + landerMod.state;
                if (!st.emergencyUsed) action = plan.emergency ? "Cancel emergency" : "Fuel to water"; break;
            case "solar":
                body = st.mod["solar"] + " arrays. Each makes " + Fmt(site == "moon" ? V("solar_moon") : V("solar_mars")) + " energy in full sunlight" + (site == "mars" ? " (Mars gets 43% of the Moon's sunlight).\nDust cuts the output by 3% a sol. Now at " + Mathf.RoundToInt(DustFactor() * 100) + "%. A Maintenance day cleans the arrays." : ", and nothing during the lunar night.") + "\nToday: " + Fmt(pv.rec.solar) + " energy.";
                if (site == "mars") action = plan.activity == "maintenance" ? "Cleaning planned" : "Clean today"; break;
            case "battery":
                body = "Stores spare energy for later. Charge: " + Fmt(st.battery) + " of " + Fmt(V("battery_per") * st.mod["battery"]) + ".\nWhen the battery is empty, systems switch off: lab first, habitat last."; break;
            case "reactor":
                body = "A small fission reactor. Makes " + Fmt(V("reactor_out")) + " energy every day: at night, in dust, in the dark.\nIt stands far from the habitat on purpose."; break;
        }
    }

    void Interact(string id)
    {
        if (st == null || st.over || mode != Mode.Play) return;
        switch (id)
        {
            case "oxygen": plan.oxygen = !plan.oxygen; Toggle(oxyMod, plan.oxygen ? "ON" : "OFF", plan.oxygen); break;
            case "lab": plan.lab = !plan.lab; Toggle(labMod, plan.lab ? "ON" : "OFF", plan.lab); break;
            case "greenhouse":
                if (st.mod["greenhouse"] == 0) break;
                plan.greenhouse = (plan.greenhouse + 1) % (st.mod["greenhouse"] + 1); Toggle(SysMod("greenhouse"), plan.greenhouse + " RUNNING", plan.greenhouse > 0); break;
            case "drill":
                if (st.mod["drill"] == 0) break;
                plan.drill = (plan.drill + 1) % (st.mod["drill"] + 1); Toggle(SysMod("drill"), plan.drill + " RUNNING", plan.drill > 0); break;
            case "hab": ShowPlan(); break;
            case "depot": ShowBuild(); break;
            case "lander":
                if (st.emergencyUsed) { PipSay("The fuel reserve is already used.", 1); break; }
                plan.emergency = !plan.emergency; Toggle(landerMod, plan.emergency ? "EMERGENCY ARMED" : "CANCELLED", plan.emergency); break;
            case "solar":
                if (site == "mars") { plan.activity = plan.activity == "maintenance" ? "science" : "maintenance"; Toggle(solarMod, plan.activity == "maintenance" ? "CLEANING TODAY" : "NO CLEANING", plan.activity == "maintenance"); }
                else PipSay("These arrays make " + Fmt(pv.rec.solar) + " energy today. During the lunar night they make nothing.", 0);
                break;
            case "battery": PipSay("Battery charge: " + Fmt(st.battery) + " of " + Fmt(V("battery_per") * st.mod["battery"]) + ". Batteries only store energy, they do not make it.", 0); break;
            case "reactor": PipSay("The reactor makes " + Fmt(V("reactor_out")) + " energy day and night. It never needs sunlight.", 0); break;
        }
    }
    void Toggle(Mod m, string text, bool good)
    {
        Color c = good ? C(0.45f, 1f, 0.8f) : C(1f, 0.55f, 0.4f);
        Burst(ModTop(m), c, 14, 4f); FloatText(ModTop(m), text, c); Sfx(good ? "on" : "off"); Refresh();
    }
    void CycleRation() { if (mode != Mode.Play) return; plan.ration = plan.ration == "full" ? "lean" : (plan.ration == "lean" ? "half" : "full"); Refresh(); }
    void CycleActivity() { if (mode != Mode.Play) return; plan.activity = plan.activity == "science" ? "maintenance" : (plan.activity == "maintenance" ? "shelter" : "science"); Refresh(); }

    void ShowPlan()
    {
        Func<string, string, Btn> ra = (r, label) => new Btn { label = (plan.ration == r ? "> " : "") + label, close = false, act = () => { plan.ration = r; Refresh(); ShowPlan(); } };
        Func<string, string, Btn> ac = (a, label) => new Btn { label = (plan.activity == a ? "> " : "") + label, close = false, act = () => { plan.activity = a; Refresh(); ShowPlan(); } };
        ShowModal("Plan the " + dayWord,
            "Rations. Four astronauts eat " + Fmt(4 * V("food_person")) + " kg a day on Full rations. Food left: " + Mathf.RoundToInt((float)st.food) + " kg.\n" +
            "   Full: health goes up if every need is met.   Lean (75%): health -1 a day.   Half (50%): health -3 a day.\n\n" +
            "Activity.\n   Science: the lab earns " + Fmt(V("lab_science")) + " points (needs power).\n   Maintenance: repairs a broken module in three days" + (site == "mars" ? " and cleans the dust off the arrays" : "") + ".\n" +
            "   Shelter: cuts a solar storm dose to one tenth.\n\nToday: " + (pv.shut.Count > 0 ? "NOT ENOUGH POWER (off: " + string.Join(", ", pv.shut.Distinct()) + ")" : "power is fine") + ". Dose today: " + Fmt(pv.doseToday) + " mSv.",
            new List<Btn> { ra("full", "Full rations"), ra("lean", "Lean rations"), ra("half", "Half rations"),
                ac("science", "Science"), ac("maintenance", "Maintenance"), ac("shelter", "Shelter"),
                new Btn { label = "END " + DayWord, act = () => StartCoroutine(EndSol()), primary = true }, new Btn { label = "Back to the base" } }, 16,
            "HABITAT  ·  DAILY PLAN  ·  " + DayWord + " " + st.day);
    }

    int QueuedCount(string type) { return plan.build.Count(b => b == type); }
    int QueuedCost() { return plan.build.Sum(b => Sim.GetTypeInfo(b).cost); }
    bool CanQueue(string type)
    {
        var i = Sim.GetTypeInfo(type);
        return st.crates - QueuedCost() - (plan.repair ? 1 : 0) >= i.cost && Sim.Owned(st, type) + QueuedCount(type) < i.max;
    }
    void ShowBuild()
    {
        var list = new List<Btn>();
        foreach (var t in Sim.TYPES)
        {
            var type = t; var i = Sim.GetTypeInfo(type);
            list.Add(new Btn { label = i.name + " (" + i.cost + ")  x" + (Sim.Owned(st, type) + QueuedCount(type)), enabled = CanQueue(type), close = false,
                act = () => { plan.build.Add(type); Sfx("thud"); Refresh(); ShowBuild(); } });
        }
        list.Add(new Btn { label = "Undo last", enabled = plan.build.Count > 0, close = false, act = () => { plan.build.RemoveAt(plan.build.Count - 1); Refresh(); ShowBuild(); } });
        bool canRepair = st.faults.Count > 0 && st.crates - QueuedCost() >= 1;
        list.Add(new Btn { label = plan.repair ? "Repair: ON (1 crate)" : "Repair a module (1 crate)", enabled = st.faults.Count > 0 && (plan.repair || canRepair), close = false,
            act = () => { plan.repair = !plan.repair; Refresh(); ShowBuild(); } });
        list.Add(new Btn { label = "DONE", primary = true });
        string broken = st.faults.Count > 0 ? "\nBroken: " + string.Join(", ", st.faults.Select(f => SysName(f.type))) + ". Repair costs 1 crate, or three Maintenance days." : "";
        ShowModal("Supply depot", "Crates left: " + (st.crates - QueuedCost() - (plan.repair ? 1 : 0)) + " of " + st.crates + ". No more crates arrive.\n" +
            "New modules start working the next " + dayWord + ". The number is built + planned.\n\n" +
            "Solar array: power in sunlight.   Battery: stores 10 energy.   Fission reactor: 8 energy, day and night.\n" +
            "Greenhouse: food every 10 days.   Ice drill: 6 L of water a day.   Shield layer: blocks a quarter of the radiation." + broken, list, 16,
            "LOGISTICS  ·  BUILD AND REPAIR");
        if (!tipBuild) { tipBuild = true; PipSay("Think about what is coming: " + (site == "moon" ? "a 15-day night with no solar power, and solar storms." : "a dust storm, dusty arrays, and a solar storm.") + " You cannot buy everything.", 1); }
    }
}
