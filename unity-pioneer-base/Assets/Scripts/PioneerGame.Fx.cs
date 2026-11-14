// Pioneer Base - PioneerGame.Fx.cs
// Animations for every notable event: particles, floating text, banners, screen flashes and shake,
// persistent weather (night, dust storm, solar storm), and the 30-day mission timeline.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using PioneerBase;

public partial class PioneerGame
{
    class Particle { public Transform t; public SpriteRenderer sr; public Vector2 vel; public float life, max, s0, s1, grav; public Color c0, c1; }
    class FloatT { public Transform t; public TextMesh tm; public float life, max; public Color col; }
    readonly List<Particle> parts = new List<Particle>();
    readonly List<FloatT> floats = new List<FloatT>();

    Image flashImg; Color flashCol; float flashT, flashDur;
    CanvasGroup bannerGroup; Text bannerTitle, bannerSub; float bannerT; Color bannerCol;
    float shakeAmt, nextRad;
    SpriteRenderer weather;
    Image[] tlCell, tlLow, tlTop, tlCard;
    float dayPop, dustTimer;
    Image Img(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color col) { return Panel(name, parent, aMin, aMax, col).GetComponent<Image>(); }

    // ------------------------------------------------------------------ setup
    void BuildFxRoot()
    {
        weather = Quad("Weather", world, Vector2.zero, new Vector2(60, 40), new Color(0, 0, 0, 0), 15);

        var cgo = new GameObject("FxCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = -5;
        var cs = cgo.GetComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1280, 720); cs.matchWidthOrHeight = 0.5f;
        var root = cgo.transform;

        flashImg = Img("Flash", root, Vector2.zero, Vector2.one, new Color(1, 1, 1, 0)); flashImg.raycastTarget = false;

        var ban = Panel("Banner", root, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.72f), new Color(0, 0, 0, 0.55f));
        ban.GetComponent<Image>().raycastTarget = false;
        bannerGroup = ban.gameObject.AddComponent<CanvasGroup>(); bannerGroup.alpha = 0; bannerGroup.blocksRaycasts = false;
        bannerTitle = TextUI("T", ban, new Vector2(0, 0.45f), Vector2.one, 44, TextAnchor.MiddleCenter, Color.white);
        bannerTitle.fontStyle = FontStyle.Bold;
        bannerSub = TextUI("S", ban, new Vector2(0, 0), new Vector2(1, 0.5f), 20, TextAnchor.UpperCenter, new Color(0.85f, 0.92f, 1f));

        // 30-day mission timeline just above the bottom bar
        var tl = Panel("Timeline", root, new Vector2(0.02f, 64f / 720f), new Vector2(0.98f, 88f / 720f), new Color(0.02f, 0.04f, 0.08f, 0.75f));
        tl.GetComponent<Image>().raycastTarget = false;
        tlCell = new Image[Sim.MAX_DAY]; tlLow = new Image[Sim.MAX_DAY]; tlTop = new Image[Sim.MAX_DAY]; tlCard = new Image[Sim.MAX_DAY];
        for (int i = 0; i < Sim.MAX_DAY; i++)
        {
            float x0 = i / (float)Sim.MAX_DAY + 0.0025f, x1 = (i + 1) / (float)Sim.MAX_DAY - 0.0025f;
            tlCell[i] = Img("D" + (i + 1), tl, new Vector2(x0, 0.12f), new Vector2(x1, 0.88f), new Color(0.2f, 0.28f, 0.4f, 0.55f));
            tlLow[i] = Img("Low", tlCell[i].transform, new Vector2(0, 0), new Vector2(1, 0.3f), new Color(0, 0, 0, 0));
            tlTop[i] = Img("Top", tlCell[i].transform, new Vector2(0, 0.7f), new Vector2(1, 1f), new Color(0, 0, 0, 0));
            tlCard[i] = Img("Card", tlCell[i].transform, new Vector2(0.35f, 0.35f), new Vector2(0.65f, 0.65f), new Color(0, 0, 0, 0));
            foreach (var im in new[] { tlCell[i], tlLow[i], tlTop[i], tlCard[i] }) im.raycastTarget = false;
        }
    }

    // ------------------------------------------------------------------ "what can I do here" prompt
    TextMesh promptText; SpriteRenderer promptPill;
    static string Hint(string id)
    {
        switch (id)
        {
            case "oxygen": return "OXYGEN PLANT: switch on / off";
            case "lab": return "SCIENCE LAB: switch on / off";
            case "greenhouse": return "GREENHOUSE: change how many run";
            case "drill": return "ICE DRILL: change how many run";
            case "kitchen": return "KITCHEN: choose the rations";
            case "airlock": return "AIRLOCK: choose today's activity";
            case "build": return "BUILD BAY: build and repair";
            default: return "COMMAND: end the day";
        }
    }
    void UpdatePrompt()
    {
        if (promptText == null)
        {
            promptPill = Slab("PromptPill", world, Vector2.zero, new Vector2(4.6f, 0.62f), new Color(0.02f, 0.05f, 0.1f, 0.9f), 58);
            promptText = Label(world, Vector2.zero, "", 0.042f, new Color(0.7f, 0.95f, 1f), 59);
        }
        var s = Nearest((Vector2)astro.localPosition, 1.9f);
        bool show = s != null && mode == Mode.Play;
        promptText.gameObject.SetActive(show); promptPill.gameObject.SetActive(show);
        if (!show) return;
        var p = s.pos + new Vector2(0, s.pos.y > 0 ? -0.95f : 0.95f);
        promptText.transform.localPosition = p; promptPill.transform.localPosition = p;
        promptText.text = Hint(s.id) + "   [E]";
    }

    // ------------------------------------------------------------------ primitives
    void Spawn(Vector2 pos, Vector2 vel, float life, float s0, float s1, Color c0, Color c1, float grav = 0f, Sprite spr = null, int order = 40)
    {
        var sr = Quad("P", world, pos, new Vector2(s0, s0), c0, order, spr ?? glow);
        parts.Add(new Particle { t = sr.transform, sr = sr, vel = vel, life = life, max = life, s0 = s0, s1 = s1, c0 = c0, c1 = c1, grav = grav });
    }
    void Puff(Vector2 pos)
    {
        Spawn(pos, new Vector2(Random.Range(-0.3f, 0.3f), 0.35f), 0.5f, 0.18f, 0.4f, new Color(0.8f, 0.9f, 1f, 0.45f), new Color(0.8f, 0.9f, 1f, 0f), 0f, circle, 17);
    }
    void Burst(Vector2 pos, Color col, int count, float speed, float life = 0.8f, float grav = 0f)
    {
        for (int i = 0; i < count; i++)
        {
            float a = Random.value * Mathf.PI * 2f, sp = speed * Random.Range(0.4f, 1f);
            Spawn(pos, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp, life * Random.Range(0.6f, 1f), Random.Range(0.18f, 0.4f), 0.05f, col, new Color(col.r, col.g, col.b, 0f), grav);
        }
    }
    void Sparks(Vector2 pos) { Burst(pos, new Color(1f, 0.85f, 0.35f, 1f), 14, 3.4f, 0.7f, 6f); Burst(pos, new Color(1f, 1f, 1f, 1f), 6, 2f, 0.4f, 3f); }
    void Ripple(Vector2 pos, Color col, float size = 3f)
    {
        Spawn(pos, Vector2.zero, 0.9f, 0.3f, size, new Color(col.r, col.g, col.b, 0.9f), new Color(col.r, col.g, col.b, 0f), 0f, ring, 38);
    }
    void FloatText(Vector2 pos, string text, Color col, float size = 0.06f)
    {
        var tm = Label(world, pos, text, size, col, 60);
        floats.Add(new FloatT { t = tm.transform, tm = tm, life = 1.8f, max = 1.8f, col = col });
    }
    void Confetti(int count)
    {
        Color[] cols = { new Color(1f, 0.4f, 0.5f), new Color(0.4f, 0.9f, 1f), new Color(1f, 0.9f, 0.3f), new Color(0.5f, 1f, 0.5f), Color.white };
        for (int i = 0; i < count; i++)
        {
            var c = cols[i % cols.Length];
            Spawn(new Vector2(Random.Range(-9f, 9f), 5.6f), new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(-3f, -6f)), Random.Range(2.5f, 4f),
                Random.Range(0.12f, 0.24f), 0.1f, c, new Color(c.r, c.g, c.b, 0f), 0.3f, square, 50);
        }
    }
    void Flash(Color col, float dur = 0.6f) { flashCol = col; flashT = dur; flashDur = dur; }
    void Banner(string title, string sub, Color col) { bannerTitle.text = title; bannerSub.text = sub; bannerCol = col; bannerTitle.color = col; bannerT = 3.2f; }
    void Shake(float a) { shakeAmt = Mathf.Max(shakeAmt, a); }
    Vector2 RoomPos(string id) { Room r; return rooms.TryGetValue(id, out r) ? r.center : Vector2.zero; }
    void Feedback(string roomId, string text, Color col)
    {
        Room r; var p = rooms.TryGetValue(roomId, out r) ? r.center : Vector2.zero;
        Ripple(p, col, 2.6f); FloatText(p + new Vector2(0, 0.2f), text, col);
    }

    // ------------------------------------------------------------------ per-frame
    void UpdateFx()
    {
        float dt = Time.deltaTime;
        for (int i = parts.Count - 1; i >= 0; i--)
        {
            var p = parts[i]; p.life -= dt;
            if (p.life <= 0) { Destroy(p.t.gameObject); parts.RemoveAt(i); continue; }
            float k = 1f - p.life / p.max;
            p.vel.y -= p.grav * dt;
            p.t.localPosition += (Vector3)(p.vel * dt);
            float s = Mathf.Lerp(p.s0, p.s1, k); p.t.localScale = new Vector3(s, s, 1);
            p.sr.color = Color.Lerp(p.c0, p.c1, k);
        }
        for (int i = floats.Count - 1; i >= 0; i--)
        {
            var f = floats[i]; f.life -= dt;
            if (f.life <= 0) { Destroy(f.t.gameObject); floats.RemoveAt(i); continue; }
            float k = 1f - f.life / f.max;
            f.t.localPosition += new Vector3(0, 0.9f * dt * (1f - k * 0.6f), 0);
            f.t.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.5f, 1.25f, k / 0.15f) : 1f);
            f.tm.color = new Color(f.col.r, f.col.g, f.col.b, Mathf.Clamp01(f.life / (f.max * 0.45f)));
        }
        // screen flash
        if (flashT > 0) { flashT -= dt; flashImg.color = new Color(flashCol.r, flashCol.g, flashCol.b, flashCol.a * Mathf.Clamp01(flashT / flashDur)); }
        else if (flashImg.color.a > 0) flashImg.color = new Color(1, 1, 1, 0);
        // banner: slides in, holds, fades
        if (bannerT > 0)
        {
            bannerT -= dt; float k = 3.2f - bannerT;
            bannerGroup.alpha = Mathf.Clamp01(k / 0.25f) * Mathf.Clamp01(bannerT / 0.6f);
            float sc = k < 0.35f ? Mathf.Lerp(1.5f, 1f, k / 0.35f) : 1f;
            bannerGroup.transform.localScale = Vector3.one * sc;
            bannerTitle.color = Color.Lerp(Color.white, bannerCol, Mathf.PingPong(Time.time * 3f, 1f) * 0.5f + 0.5f);
        }
        else bannerGroup.alpha = 0;
        if (dayPop > 0 && dayText != null) { dayPop -= dt; dayText.transform.localScale = Vector3.one * (1f + Mathf.Clamp01(dayPop) * 0.35f); }

        // camera: tiny parallax sway + shake
        Vector3 sh = shakeAmt > 0.001f ? (Vector3)(Random.insideUnitCircle * shakeAmt) : Vector3.zero;
        shakeAmt = Mathf.MoveTowards(shakeAmt, 0, dt * 1.6f);
        var ap = (Vector2)astro.localPosition;
        cam.transform.position = new Vector3(ap.x * 0.03f, ap.y * 0.02f, -10) + sh;

        WeatherTick(dt);
        // gauges that are nearly empty throb red
        foreach (var g in gauges.Values)
            if (g.low) { var c = g.fill.color; c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 5f)); g.fill.color = c; }
        // rooms: lights fade, flicker when the power is failing, alarm blinks when broken
        foreach (var r in rooms.Values)
        {
            float target = r.targetDark;
            if (r.noPower)
            {
                float n = Mathf.PerlinNoise(Time.time * 7f, r.center.x);
                target = n > 0.72f ? target * 0.55f : target;
            }
            r.curDark = Mathf.MoveTowards(r.curDark, target, dt * 2.2f);
            r.dark.color = new Color(0, 0, 0, r.curDark);
            float al = r.broken ? 0.22f * (0.5f + 0.5f * Mathf.Sin(Time.time * 6f)) : 0f;
            r.alarm.color = new Color(1f, 0.1f, 0.15f, al);
            if (r.broken && Random.value < dt * 1.4f) Sparks(r.center + new Vector2(Random.Range(-1.2f, 1.2f), Random.Range(-0.8f, 0.8f)));
        }
    }

    void WeatherTick(float dt)
    {
        Color target = new Color(0, 0, 0, 0);
        if (st != null && !st.over)
        {
            var env = Sim.EnvFor(st, st.day);
            if (env.night) target = new Color(0.04f, 0.1f, 0.4f, 0.34f);
            if (env.dustStorm)
            {
                target = new Color(0.85f, 0.5f, 0.2f, 0.26f);
                dustTimer += dt * 40f;
                while (dustTimer > 1f)
                {
                    dustTimer -= 1f;
                    Spawn(new Vector2(-10.8f, Random.Range(-5.4f, 5.4f)), new Vector2(Random.Range(9f, 14f), Random.Range(-1f, 1f)), 2.6f,
                        Random.Range(0.2f, 0.7f), 0.1f, new Color(0.9f, 0.6f, 0.3f, 0.5f), new Color(0.9f, 0.6f, 0.3f, 0f), 0f, glow, 42);
                }
            }
            if (env.solarStorm)
            {
                target = new Color(1f, 0.12f, 0.08f, 0.1f + 0.1f * Mathf.Abs(Mathf.Sin(Time.time * 4f)));
                if (Time.time > nextRad)
                {
                    nextRad = Time.time + 0.9f;
                    Ripple(new Vector2(Random.Range(-8f, 8f), Random.Range(-4f, 4f)), new Color(1f, 0.4f, 0.2f), 5f);
                }
            }
            bool sheltering = plan != null && plan.activity == "shelter";
            float sa = sheltering ? 0.16f + 0.08f * Mathf.Sin(Time.time * 4f) : 0f;
            var sc = shieldBubble.color; sc.a = Mathf.MoveTowards(sc.a, sa, dt); shieldBubble.color = sc;
        }
        weather.color = Color.Lerp(weather.color, target, dt * 2.5f);
    }

    // ------------------------------------------------------------------ mission timeline
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

    // ------------------------------------------------------------------ reactions to what just happened
    void OnDayResult(DayRecord rec)
    {
        var hall = Vector2.zero;
        foreach (var e in rec.events)
        {
            if (e.StartsWith("The lunar night")) { Banner("LUNAR NIGHT", "15 days without solar power", new Color(0.5f, 0.6f, 1f)); Flash(new Color(0.05f, 0.1f, 0.4f, 0.6f), 1.2f); Shake(0.05f); }
            else if (e.StartsWith("Sunrise")) { Banner("SUNRISE", "The night is over. Solar power is back.", new Color(1f, 0.85f, 0.4f)); Flash(new Color(1f, 0.9f, 0.5f, 0.55f), 1.2f); Burst(hall, new Color(1f, 0.9f, 0.5f), 30, 5f, 1.2f); }
            else if (e.StartsWith("A dust storm begins")) { Banner("DUST STORM", "Solar power drops to 15%", new Color(1f, 0.65f, 0.3f)); Flash(new Color(0.9f, 0.55f, 0.2f, 0.45f), 0.9f); Shake(0.08f); }
            else if (e.StartsWith("The dust storm is over")) { Banner("SKY CLEARING", "The dust storm is over", new Color(0.7f, 0.95f, 1f)); Flash(new Color(0.8f, 0.95f, 1f, 0.35f), 0.8f); }
            else if (e.StartsWith("A solar storm"))
            {
                if (rec.sheltered) { Banner("STORM ENDURED", "The crew sheltered. Dose cut to one tenth.", new Color(0.4f, 0.85f, 1f)); Flash(new Color(0.3f, 0.7f, 1f, 0.35f), 0.8f); }
                else { Banner("SOLAR STORM", "Radiation surge! Sheltering would have cut it.", new Color(1f, 0.35f, 0.3f)); Flash(new Color(1f, 0.1f, 0.05f, 0.6f), 1f); Shake(0.22f); }
                FloatText((Vector2)astro.localPosition + new Vector2(0, 1.3f), "+" + Fmt(rec.doseToday) + " mSv", new Color(1f, 0.55f, 0.3f), 0.07f);
            }
        }
        foreach (var s in rec.shut)
        {
            string rid = s == "habitat" ? "" : s;
            var pos = rid == "" ? hall : RoomPos(rid);
            Sparks(pos); FloatText(pos + new Vector2(0, 0.3f), "POWER OUT", new Color(1f, 0.45f, 0.3f));
        }
        if (rec.shut.Count > 0) { Shake(0.12f); Flash(new Color(1f, 0.3f, 0.1f, 0.25f), 0.5f); if (rec.shut.Contains("habitat")) Banner("HABITAT CORE DOWN", "No heat, no water recycling", new Color(1f, 0.4f, 0.3f)); }
        if (rec.shut.Count > 0 && st.battery <= 0.001) FloatText(hall + new Vector2(0, 1.4f), "BATTERY EMPTY", new Color(1f, 0.3f, 0.3f), 0.08f);
        if (rec.faultNew != null) { Sparks(RoomPos(rec.faultNew)); Burst(RoomPos(rec.faultNew), new Color(1f, 0.3f, 0.2f), 20, 4f); Banner("MODULE BROKEN", "The " + rec.faultNew + " needs a repair", new Color(1f, 0.55f, 0.25f)); Flash(new Color(1f, 0.2f, 0.1f, 0.35f), 0.6f); Shake(0.15f); }
        if (rec.emergencyDamage != null) { Sparks(RoomPos(rec.emergencyDamage)); FloatText(RoomPos(rec.emergencyDamage), "BROKE IN EMERGENCY", new Color(1f, 0.5f, 0.3f)); }
        if (rec.repaired != null) { Burst(RoomPos(rec.repaired), new Color(0.4f, 1f, 0.6f), 26, 4f); Ripple(RoomPos(rec.repaired), new Color(0.4f, 1f, 0.6f), 4f); FloatText(RoomPos(rec.repaired), "REPAIRED", new Color(0.4f, 1f, 0.6f), 0.07f); }
        if (rec.harvest > 0) { var p = RoomPos("greenhouse"); Burst(p, new Color(0.4f, 1f, 0.5f), 30, 4f, 1.1f); FloatText(p + new Vector2(0, 0.4f), "+" + Fmt(rec.harvest) + " kg FOOD", new Color(0.55f, 1f, 0.6f), 0.07f); Banner("HARVEST", "The greenhouse crop is ready", new Color(0.5f, 1f, 0.6f)); }
        if (rec.science > 0) { var p = RoomPos("lab"); Burst(p, new Color(0.7f, 0.55f, 1f), 10, 2.5f); FloatText(p + new Vector2(0, 0.4f), "+" + Fmt(rec.science) + " SCIENCE", new Color(0.8f, 0.7f, 1f)); }
        foreach (var a in rec.activated)
        {
            var p = (a == "greenhouse" || a == "drill") ? RoomPos(a) : hall;
            Ripple(p, new Color(0.4f, 0.9f, 1f), 4f); Burst(p, new Color(0.5f, 0.95f, 1f), 18, 3.5f);
            FloatText(p + new Vector2(0, 0.5f), a.ToUpper() + " ONLINE", new Color(0.55f, 0.95f, 1f), 0.065f);
        }
        if (rec.builtToday.Count > 0) { var p = RoomPos("build"); Burst(p, new Color(1f, 0.88f, 0.3f), 14, 3f); FloatText(p + new Vector2(0, 0.4f), "BUILDING " + rec.builtToday.Count, new Color(1f, 0.9f, 0.4f)); }
        if (rec.emergency) { var p = RoomPos("airlock"); Ripple(p, new Color(0.4f, 0.8f, 1f), 5f); Burst(p, new Color(0.4f, 0.8f, 1f), 24, 4f); FloatText(p + new Vector2(0, 0.4f), "+40 L WATER", new Color(0.5f, 0.85f, 1f), 0.07f); }
        if (rec.cleaned) { Burst(hall, new Color(1f, 1f, 0.8f), 24, 6f); FloatText(hall + new Vector2(0, 0.6f), "PANELS CLEANED", new Color(1f, 0.95f, 0.7f)); }
        if (rec.o2Short) { Banner("OXYGEN DEPLETED", "The crew is losing health fast", new Color(1f, 0.3f, 0.3f)); Flash(new Color(1f, 0.1f, 0.1f, 0.5f), 1f); Shake(0.2f); }
        else if (rec.waterEmpty) { Banner("NO WATER", "The tank is empty", new Color(0.4f, 0.7f, 1f)); Flash(new Color(0.2f, 0.4f, 1f, 0.35f), 0.8f); }
        else if (rec.foodEmpty) { Banner("NO FOOD", "The store is empty", new Color(1f, 0.7f, 0.3f)); Flash(new Color(1f, 0.6f, 0.1f, 0.3f), 0.8f); }
        if (st.over)
        {
            if (st.ending.type == "success") { Banner("MISSION COMPLETE", st.ending.stars + " of 3 stars", new Color(0.5f, 1f, 0.7f)); Confetti(160); Flash(new Color(0.6f, 1f, 0.8f, 0.5f), 1.2f); }
            else { Banner(st.ending.reason == "dose" ? "EMERGENCY EVACUATION" : "MEDICAL EVACUATION", "Day " + st.ending.day, new Color(1f, 0.35f, 0.3f)); Flash(new Color(1f, 0.05f, 0.05f, 0.6f), 1.5f); Shake(0.3f); }
        }
        dayPop = 0.6f;
    }

    void ClearFx()
    {
        foreach (var p in parts) if (p.t) Destroy(p.t.gameObject);
        foreach (var f in floats) if (f.t) Destroy(f.t.gameObject);
        parts.Clear(); floats.Clear(); bannerT = 0; flashT = 0; shakeAmt = 0;
        foreach (var r in rooms.Values) { r.curDark = 0; r.targetDark = 0; r.broken = false; r.noPower = false; }
    }
}
