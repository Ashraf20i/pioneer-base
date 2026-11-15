// Pioneer Base 3D - PioneerGame.Fx.cs
// Particles, weather, floating text, generated sound, and the reaction to each day's events.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PioneerBase;

public partial class PioneerGame
{
    ParticleSystem psGlow, psSpark, psSmoke;
    class FloatT { public Transform t; public TextMesh tm; public float life, max; public Color col; }
    readonly List<FloatT> floats = new List<FloatT>();
    float dustAcc, radAcc, geigerT, moteAcc;

    // ------------------------------------------------------------------ particles
    void InitFx()
    {
        psGlow = MakePS("FxGlow", shAdd, glowTex, 0f, false);
        psSpark = MakePS("FxSpark", shAdd, glowTex, 0.9f, false);
        psSmoke = MakePS("FxSmoke", shBlend, softTex, -0.02f, true);
    }
    ParticleSystem MakePS(string name, Shader sh, Texture tex, float gravity, bool grow)
    {
        var go = new GameObject(name); go.transform.SetParent(world, false);
        var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 5000; main.gravityModifier = gravity; main.startSpeed = 0f;
        var em = ps.emission; em.enabled = false; var shp = ps.shape; shp.enabled = false;
        var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);
        if (grow) { var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.9f)); }
        var r = go.GetComponent<ParticleSystemRenderer>(); var m = new Material(sh); m.mainTexture = tex; r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        ps.Play(); return ps;
    }
    void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, Color c, float size, float life)
    {
        if (ps == null) return;
        var ep = new ParticleSystem.EmitParams(); ep.position = pos; ep.velocity = vel; ep.startColor = c; ep.startSize = size; ep.startLifetime = life;
        ps.Emit(ep, 1);
    }
    void Burst(Vector3 pos, Color col, int count, float speed, float size = 0.4f, float life = 0.9f)
    {
        for (int i = 0; i < count; i++) Emit(psGlow, pos, UnityEngine.Random.onUnitSphere * speed * UnityEngine.Random.Range(0.3f, 1f), col, size * UnityEngine.Random.Range(0.5f, 1.3f), life * UnityEngine.Random.Range(0.6f, 1f));
    }
    void Sparks(Vector3 pos)
    {
        for (int i = 0; i < 14; i++) { var v = UnityEngine.Random.onUnitSphere * 4f; v.y = Mathf.Abs(v.y) + 1.5f; Emit(psSpark, pos, v, C(1f, 0.8f, 0.3f), UnityEngine.Random.Range(0.1f, 0.22f), UnityEngine.Random.Range(0.4f, 0.9f)); }
    }
    void Smoke(Vector3 pos, Color col, float size, float life)
    {
        Emit(psSmoke, pos + UnityEngine.Random.insideUnitSphere * 0.3f, new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(0.8f, 1.6f), UnityEngine.Random.Range(-0.4f, 0.4f)), col, size, life);
    }
    Color DustCol(float a) { return site == "mars" ? C(0.8f, 0.52f, 0.34f, a) : C(0.7f, 0.7f, 0.72f, a); }
    void DustPuff(Vector3 pos, int count)
    {
        for (int i = 0; i < count; i++)
            Emit(psSmoke, pos + new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), 0.1f, UnityEngine.Random.Range(-0.3f, 0.3f)),
                new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), UnityEngine.Random.Range(0.2f, 0.7f), UnityEngine.Random.Range(-0.6f, 0.6f)), DustCol(0.3f), UnityEngine.Random.Range(0.5f, 1.1f), UnityEngine.Random.Range(0.7f, 1.4f));
    }
    void Fireworks(Vector3 center)
    {
        Color[] cols = { C(1f, 0.4f, 0.5f), C(0.4f, 0.9f, 1f), C(1f, 0.9f, 0.3f), C(0.5f, 1f, 0.5f) };
        for (int i = 0; i < 4; i++) Burst(center + new Vector3(UnityEngine.Random.Range(-14f, 14f), UnityEngine.Random.Range(10f, 20f), UnityEngine.Random.Range(-10f, 10f)), cols[i], 46, 9f, 0.7f, 1.6f);
    }
    void FloatText(Vector3 pos, string text, Color col, float size = 0.11f)
    {
        var tm = Label3D(world, pos, text, size, col);
        floats.Add(new FloatT { t = tm.transform, tm = tm, life = 2.4f, max = 2.4f, col = col });
    }

    void TickFx(float dt)
    {
        for (int i = floats.Count - 1; i >= 0; i--)
        {
            var f = floats[i]; f.life -= dt;
            if (f.life <= 0 || f.t == null) { if (f.t != null) { billboards.Remove(f.t); Destroy(f.t.gameObject); } floats.RemoveAt(i); continue; }
            float k = 1f - f.life / f.max;
            f.t.position += Vector3.up * dt * 1.3f * (1f - k * 0.6f);
            f.t.localScale = f.t.localScale * (k < 0.12f ? Mathf.Lerp(0.4f, 1.2f, k / 0.12f) : 1f);
            f.tm.color = new Color(f.col.r, f.col.g, f.col.b, Mathf.Clamp01(f.life / (f.max * 0.4f)));
        }
        if (cam == null) return;
        Vector3 cp = cam.transform.position;
        // dust storm: sheets of dust blown across the base
        if (stormK > 0.02f)
        {
            dustAcc += dt * 70f * stormK;
            while (dustAcc > 1f)
            {
                dustAcc -= 1f;
                Vector3 p = cp + new Vector3(UnityEngine.Random.Range(-45f, 20f), 0, UnityEngine.Random.Range(-40f, 40f)); p.y = H(p.x, p.z) + UnityEngine.Random.Range(0.3f, 9f);
                Emit(psSmoke, p, new Vector3(UnityEngine.Random.Range(10f, 17f), UnityEngine.Random.Range(-0.3f, 0.6f), UnityEngine.Random.Range(-2f, 3f)), DustCol(0.28f), UnityEngine.Random.Range(3f, 8f), UnityEngine.Random.Range(1.8f, 3f));
            }
        }
        else if (site == "mars" && mode != Mode.Title)
        {
            moteAcc += dt * 3f;
            while (moteAcc > 1f) { moteAcc -= 1f; Vector3 p = cp + UnityEngine.Random.insideUnitSphere * 22f; p.y = Mathf.Max(p.y, H(p.x, p.z) + 0.3f); Emit(psSmoke, p, new Vector3(1.2f, 0.1f, 0.3f), DustCol(0.07f), UnityEngine.Random.Range(1.5f, 3f), 3f); }
        }
        // solar storm: glints of radiation and Geiger ticks
        if (solarK > 0.02f)
        {
            radAcc += dt * 45f * solarK;
            while (radAcc > 1f)
            {
                radAcc -= 1f; Vector3 p = player.pos + new Vector3(UnityEngine.Random.Range(-22f, 22f), UnityEngine.Random.Range(0.3f, 12f), UnityEngine.Random.Range(-22f, 22f));
                Emit(psGlow, p, -sunDir * UnityEngine.Random.Range(14f, 26f), UnityEngine.Random.value < 0.5f ? C(1f, 0.3f, 0.25f) : C(0.9f, 0.4f, 1f), UnityEngine.Random.Range(0.12f, 0.3f), 0.5f);
            }
            geigerT -= dt; if (geigerT <= 0) { geigerT = UnityEngine.Random.Range(0.03f, 0.22f); Sfx("tick", 0.5f); }
        }
        if (windSrc != null) windSrc.volume = Mathf.MoveTowards(windSrc.volume, (stormK * 0.55f + (site == "mars" && mode != Mode.Title ? 0.05f : 0f)) * masterVol, dt * 0.5f);
        if (rocketSrc != null) rocketSrc.volume = Mathf.MoveTowards(rocketSrc.volume, rocketOn ? 0.6f * masterVol : 0f, dt * 1.5f);
    }

    void ClearFx()
    {
        foreach (var f in floats) if (f.t != null) Destroy(f.t.gameObject);
        floats.Clear(); stormK = 0; solarK = 0; shakeAmt = 0; rocketOn = false;
        if (psGlow != null) { psGlow.Clear(); psSpark.Clear(); psSmoke.Clear(); }
    }

    // ------------------------------------------------------------------ generated sound
    AudioSource sfxSrc, windSrc, rocketSrc; readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    float masterVol = 1f; bool rocketOn;

    void InitAudio()
    {
        try
        {
            sfxSrc = gameObject.AddComponent<AudioSource>(); sfxSrc.playOnAwake = false; sfxSrc.spatialBlend = 0f;
            const float TAU = Mathf.PI * 2f;
            Clip("click", 0.05f, t => Mathf.Sin(TAU * 900f * t) * Mathf.Exp(-t * 60f));
            Clip("on", 0.14f, t => Mathf.Sin(TAU * (500f + 2800f * t) * t) * Mathf.Exp(-t * 14f));
            Clip("off", 0.16f, t => Mathf.Sin(TAU * (700f - 1300f * t) * t) * Mathf.Exp(-t * 12f));
            Clip("warn", 0.45f, t => (t % 0.22f < 0.12f ? Mathf.Sin(TAU * 620f * t) : 0f) * 0.8f);
            Clip("alarm", 1.0f, t => Mathf.Sign(Mathf.Sin(TAU * (t % 0.5f < 0.25f ? 520f : 390f) * t)) * 0.35f * (1f - t * 0.4f));
            Clip("down", 0.8f, t => Mathf.Sin(TAU * (420f - 240f * t) * t) * (1f - t / 0.8f) * 0.8f);
            Clip("good", 0.5f, t => Mathf.Sin(TAU * (t < 0.15f ? 660f : (t < 0.3f ? 880f : 1100f)) * t) * Mathf.Exp(-(t % 0.15f) * 9f) * 0.8f);
            Clip("win", 1.4f, t => { float[] n = { 523f, 659f, 784f, 1047f, 1319f }; int i = Mathf.Min(4, (int)(t / 0.22f)); return (Mathf.Sin(TAU * n[i] * t) + 0.4f * Mathf.Sin(TAU * n[i] * 2f * t)) * Mathf.Exp(-(t - i * 0.22f) * (i == 4 ? 3f : 7f)) * 0.6f; });
            var rnd = new System.Random(3);
            Clip("tick", 0.012f, t => (float)(rnd.NextDouble() * 2 - 1) * (1f - t / 0.012f));
            float last = 0f; Clip("thud", 0.35f, t => { last = (last + 0.08f * (float)(rnd.NextDouble() * 2 - 1)) / 1.02f; return last * 7f * Mathf.Exp(-t * 9f) + Mathf.Sin(TAU * 90f * t) * Mathf.Exp(-t * 14f) * 0.7f; });
            float lw = 0f; var wind = Clip("wind", 4f, t => { lw = (lw + 0.03f * (float)(rnd.NextDouble() * 2 - 1)) / 1.004f; return lw * 5f * (0.6f + 0.4f * Mathf.Sin(TAU * 0.5f * t)); });
            float lr = 0f; var rocket = Clip("rocket", 2f, t => { lr = (lr + 0.12f * (float)(rnd.NextDouble() * 2 - 1)) / 1.03f; return Mathf.Clamp(lr * 6f, -1f, 1f); });
            windSrc = Loop(wind); rocketSrc = Loop(rocket);
        }
        catch (Exception e) { Debug.LogWarning("Audio init failed: " + e.Message); }
    }
    AudioClip Clip(string name, float dur, Func<float, float> f)
    {
        int n = Mathf.Max(8, (int)(dur * 44100)); var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / 44100f), -1f, 1f);
        int fade = Mathf.Min(400, n / 4); for (int i = 0; i < fade; i++) { data[i] *= i / (float)fade; data[n - 1 - i] *= i / (float)fade; }
        var c = AudioClip.Create(name, n, 1, 44100, false); c.SetData(data, 0); clips[name] = c; return c;
    }
    AudioSource Loop(AudioClip c)
    {
        var s = gameObject.AddComponent<AudioSource>(); s.clip = c; s.loop = true; s.volume = 0f; s.spatialBlend = 0f; s.playOnAwake = false; s.Play(); return s;
    }
    void Sfx(string name, float vol = 1f)
    {
        AudioClip c; if (sfxSrc != null && clips.TryGetValue(name, out c)) sfxSrc.PlayOneShot(c, vol * 0.6f * masterVol);
    }

    // ------------------------------------------------------------------ what just happened today
    Vector3 ModTop(Mod m) { return m.pos + Vector3.up * (m.height + 0.6f); }
    Mod SysMod(string sys)
    {
        if (sys == "oxygen") return oxyMod; if (sys == "lab") return labMod; if (sys == "habitat") return habMod;
        var s = slots.FirstOrDefault(x => x.type == sys && x.state == 3 && x.mod != null); return s != null ? s.mod : habMod;
    }

    void OnDayResult(DayRecord rec)
    {
        foreach (var e in rec.events)
        {
            if (e.StartsWith("The lunar night")) { BannerQ("LUNAR NIGHT", "15 days without sunlight. The arrays make no power.", C(0.55f, 0.65f, 1f)); Sfx("down"); PipSay("Night has fallen. It lasts about two Earth weeks. Only stored power and the reactor work now.", 1); }
            else if (e.StartsWith("Sunrise")) { BannerQ("SUNRISE", "The lunar night is over. Solar power is back.", C(1f, 0.87f, 0.45f)); Flash(C(1f, 0.9f, 0.5f, 0.4f), 1.2f); Sfx("good"); }
            else if (e.StartsWith("A dust storm begins")) { BannerQ("DUST STORM", "Solar arrays drop to " + Mathf.RoundToInt((float)V("dust_storm_mult") * 100) + "% for 6 sols", C(1f, 0.68f, 0.35f)); Sfx("warn"); Shake(0.25f); PipSay("Dust storm! On Mars the wind is too thin to knock things over. The danger is the dust blocking the Sun.", 1); }
            else if (e.StartsWith("The dust storm is over")) { BannerQ("SKY CLEARING", "The dust storm is over", C(0.7f, 0.95f, 1f)); Sfx("good"); }
            else if (e.StartsWith("A solar storm"))
            {
                if (rec.sheltered) { BannerQ("STORM ENDURED", "The crew sheltered. Storm dose cut to one tenth.", C(0.45f, 0.88f, 1f)); Sfx("good"); }
                else { BannerQ("SOLAR STORM", "Radiation surge. Sheltering would have cut it to one tenth.", C(1f, 0.38f, 0.32f)); Flash(C(1f, 0.1f, 0.05f, 0.5f), 1f); Shake(0.5f); Sfx("alarm"); }
                FloatText(player.pos + Vector3.up * 2.8f, "+" + Fmt(rec.doseToday) + " mSv", C(1f, 0.55f, 0.3f), 0.13f);
            }
        }
        foreach (var s in rec.shut.Distinct()) { var m = SysMod(s); Sparks(ModTop(m)); FloatText(ModTop(m), "POWER OUT", C(1f, 0.45f, 0.3f)); }
        if (rec.shut.Count > 0)
        {
            Sfx("down"); Shake(0.2f);
            if (rec.shut.Contains("habitat")) BannerQ("HABITAT CORE DOWN", "No heat and no water recycling tonight", C(1f, 0.42f, 0.32f));
            else BannerQ("POWER SHORTAGE", "Switched off: " + string.Join(", ", rec.shut.Distinct()), C(1f, 0.6f, 0.3f));
        }
        if (rec.faultNew != null) { var m = SysMod(rec.faultNew); Sparks(ModTop(m)); Burst(ModTop(m), C(1f, 0.3f, 0.2f), 24, 6f); BannerQ("MODULE BROKEN", "The " + SysName(rec.faultNew) + " needs a repair (Supply Depot, 1 crate)", C(1f, 0.58f, 0.28f)); Sfx("alarm"); Shake(0.3f); }
        if (rec.emergencyDamage != null) { var m = SysMod(rec.emergencyDamage); Sparks(ModTop(m)); FloatText(ModTop(m), "BROKE IN THE EMERGENCY", C(1f, 0.5f, 0.3f)); }
        if (rec.repaired != null) { var m = SysMod(rec.repaired); Burst(ModTop(m), C(0.4f, 1f, 0.6f), 30, 6f); FloatText(ModTop(m), "REPAIRED", C(0.45f, 1f, 0.6f), 0.13f); Sfx("good"); }
        if (rec.harvest > 0) { var m = SysMod("greenhouse"); Burst(ModTop(m), C(0.4f, 1f, 0.5f), 36, 6f, 0.5f, 1.2f); FloatText(ModTop(m), "+" + Fmt(rec.harvest) + " kg FOOD", C(0.55f, 1f, 0.6f), 0.13f); BannerQ("HARVEST", "The greenhouse crop is ready", C(0.55f, 1f, 0.6f)); Sfx("good"); }
        if (rec.science > 0) { Burst(ModTop(labMod), C(0.75f, 0.6f, 1f), 12, 4f); FloatText(ModTop(labMod), "+" + Fmt(rec.science) + " SCIENCE", C(0.82f, 0.72f, 1f)); }
        foreach (var a in rec.activated) { Sfx("thud", 0.8f); }
        if (rec.activated.Count > 0) FloatText(habMod.pos + Vector3.up * 7.2f, "ONLINE: " + string.Join(", ", rec.activated.Select(x => Sim.GetTypeInfo(x).name)), C(0.6f, 0.95f, 1f), 0.09f);
        if (rec.emergency) { Burst(ModTop(landerMod), C(0.4f, 0.8f, 1f), 30, 6f); FloatText(ModTop(landerMod), "+" + Fmt(V("emerg_water")) + " L WATER", C(0.5f, 0.85f, 1f), 0.13f); }
        if (rec.cleaned) { Burst(solarMod.pos + Vector3.up * 3f, C(1f, 1f, 0.85f), 30, 8f); FloatText(solarMod.pos + Vector3.up * 5f, "PANELS CLEANED", C(1f, 0.96f, 0.75f)); }
        if (rec.o2Short) { BannerQ("OXYGEN DEPLETED", "The crew is losing health fast", C(1f, 0.32f, 0.32f)); Flash(C(1f, 0.1f, 0.1f, 0.45f), 1f); Sfx("alarm"); }
        else if (rec.waterEmpty) { BannerQ("NO WATER", "The tank is empty", C(0.45f, 0.72f, 1f)); Sfx("warn"); }
        else if (rec.foodEmpty) { BannerQ("NO FOOD", "The store is empty", C(1f, 0.72f, 0.32f)); Sfx("warn"); }
    }
    static string SysName(string s) { return s == "oxygen" ? "oxygen plant" : s == "lab" ? "science lab" : s == "drill" ? "ice drill" : s == "greenhouse" ? "greenhouse" : s; }
}
