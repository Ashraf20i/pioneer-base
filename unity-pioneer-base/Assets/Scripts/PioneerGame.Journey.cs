// Pioneer Base 3D - PioneerGame.Journey.cs
// Hard mode, "Crash Landing": the ship crashes 40 km from the outpost. The player drives the rover
// through a canyon, camps to recharge, opens supply pods by solving technical challenges
// (real chemistry and physics), then starts the 30-sol mission with what is left.
// The rules live in Core/Journey.cs; this file is the 3D world, the driving and the screens.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using PioneerBase;

public partial class PioneerGame
{
    bool journey, hardMode, campBusy, tipLow, tipEmpty, tipSand, autoDrive;
    JourneyState js;
    Transform rover, roverPanel; readonly List<Transform> wheels = new List<Transform>(); Light roverLamp;
    Vector3 roverPos; float roverYaw, roverSpeed, drivenToday, hudT, wreckSmokeT;
    Vector3? driveTarget;
    class Pod { public Vector3 pos; public string type; public bool taken; public GameObject go; public Material beam, lamp; public TextMesh label; }
    readonly List<Pod> pods = new List<Pod>();
    readonly List<Vector3> sandZones = new List<Vector3>();   // x, z, radius
    readonly List<int> qOrder = new List<int>(); int qIndex;
    Transform wreck; Material baseBeam;
    const float JourneyLen = 640f, MetersPerKm = JourneyLen / 40f;
    static readonly Vector3 OutpostPos = new Vector3(0, 0, JourneyLen);

    // ------------------------------------------------------------------ terrain of the canyon
    float HJ(float x, float z)
    {
        float n = Fbm(x * 0.02f + 3f, z * 0.02f + 8f);
        float flatA = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(14f, 42f, Mathf.Sqrt(x * x + z * z)));
        float dz = z - JourneyLen; float flatB = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(20f, 48f, Mathf.Sqrt(x * x + dz * dz)));
        float wall = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(64f, 104f, Mathf.Abs(x)));
        float ends = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-38f, -75f, z)) + Mathf.SmoothStep(0, 1, Mathf.InverseLerp(688f, 725f, z));
        return ((n - 0.45f) * 8f + Mathf.Sin(z * 0.07f + x * 0.03f) * 0.5f) * flatA * flatB
            + Mathf.Min(1f, wall + ends) * (13f + 24f * Fbm(x * 0.03f + 1f, z * 0.03f + 5f));
    }
    bool InSand(Vector3 p)
    {
        for (int i = 0; i < sandZones.Count; i++) { var s = sandZones[i]; float dx = p.x - s.x, dz = p.z - s.y; if (dx * dx + dz * dz < s.z * s.z) return true; }
        return false;
    }

    void BuildJourneySite()
    {
        journey = true; site = "mars";
        if (siteRoot != null) Destroy(siteRoot.gameObject);
        mods.Clear(); modByCol.Clear(); slots.Clear(); billboards.Clear(); shieldRings.Clear(); crates.Clear(); crew.Clear(); prints.Clear();
        pods.Clear(); sandZones.Clear(); wheels.Clear(); objMarker = null;
        siteRoot = new GameObject("JourneySite").transform; siteRoot.SetParent(world, false);
        seedX = 2f; seedZ = 6f;

        // canyon mesh
        const int NX = 46, NZ = 164; const float x0 = -115f, z0 = -90f, cell = 5f;
        var verts = new Vector3[(NX + 1) * (NZ + 1)]; var uv = new Vector2[verts.Length]; var tris = new int[NX * NZ * 6];
        for (int j = 0; j <= NZ; j++) for (int i = 0; i <= NX; i++)
        {
            float x = x0 + i * cell, z = z0 + j * cell; verts[j * (NX + 1) + i] = new Vector3(x, HJ(x, z), z); uv[j * (NX + 1) + i] = new Vector2(x / 16f, z / 16f);
        }
        int t = 0;
        for (int j = 0; j < NZ; j++) for (int i = 0; i < NX; i++)
        {
            int a = j * (NX + 1) + i, b = a + 1, c = a + NX + 1, d = c + 1;
            tris[t++] = a; tris[t++] = c; tris[t++] = b; tris[t++] = b; tris[t++] = c; tris[t++] = d;
        }
        var mesh = new Mesh(); mesh.vertices = verts; mesh.uv = uv; mesh.triangles = tris; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var go = new GameObject("Canyon"); go.transform.SetParent(siteRoot, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh; var mr = go.AddComponent<MeshRenderer>();
        var mat = new Material(shLit); mat.mainTexture = GroundTex(); mr.sharedMaterial = mat; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var mc = go.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; terrainCol = mc;

        var rnd = new System.Random(31);
        Color rc = C(0.42f, 0.22f, 0.14f);
        for (int i = 0; i < 150; i++)
        {
            var p = new Vector3((float)rnd.NextDouble() * 170f - 85f, 0, (float)rnd.NextDouble() * 720f - 40f);
            if (p.magnitude < 16f || Vector3.Distance(p, OutpostPos) < 26f) continue;
            float s = 0.3f + (float)rnd.NextDouble() * (Mathf.Abs(p.x) > 45 ? 3.2f : 0.9f);
            P(rnd.Next(2) == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube, siteRoot, Ground(p) + Vector3.up * s * 0.2f,
                new Vector3(s * (0.7f + (float)rnd.NextDouble()), s * (0.5f + (float)rnd.NextDouble() * 0.6f), s * (0.7f + (float)rnd.NextDouble())),
                Lit(Color.Lerp(rc, rc * 1.5f, (float)rnd.NextDouble())), new Vector3(rnd.Next(40), rnd.Next(360), rnd.Next(40)));
        }

        // soft sand: straight ahead is shorter, but the sand doubles the power use
        Vector3[] sands = { new Vector3(4, 120, 26), new Vector3(-10, 235, 30), new Vector3(14, 345, 28), new Vector3(-6, 455, 30), new Vector3(8, 548, 24) };
        var sandMat = Glass(C(1f, 0.86f, 0.6f, 0.34f));
        foreach (var s in sands)
        {
            sandZones.Add(s);
            for (int k = 0; k < 7; k++)
            {
                float a = k * 0.9f, rr = k == 0 ? 0 : s.z * 0.55f; var p = new Vector3(s.x + Mathf.Cos(a) * rr, 0, s.y + Mathf.Sin(a) * rr);
                P(PrimitiveType.Cylinder, siteRoot, Ground(p) + Vector3.up * 0.12f, new Vector3(s.z * (k == 0 ? 1.1f : 0.8f), 0.03f, s.z * (k == 0 ? 1.1f : 0.8f)), sandMat, null, false);
            }
            Label3D(siteRoot, Ground(new Vector3(s.x, 0, s.y)) + Vector3.up * 3f, "SOFT SAND\npower use x2", 0.07f, C(1f, 0.9f, 0.6f));
        }

        // supply pods thrown out during the crash
        string[] types = { "battery", "oxygen", "crates", "water", "crates", "oxygen", "food" };
        float[] px = { -44, 46, -50, 44, -42, 50, -40 }; float[] pz = { 78, 165, 255, 330, 420, 500, 580 };
        for (int i = 0; i < types.Length; i++) AddPod(types[i], Ground(new Vector3(px[i], 0, pz[i])));

        BuildWreck(); BuildOutpost();
        rover = BuildRoverModel();
        BuildActors();
        player.root.gameObject.SetActive(false); foreach (var c in crew) c.root.gameObject.SetActive(false);
    }

    static Color PodColor(string type)
    {
        return type == "oxygen" ? C(0.3f, 0.9f, 1f) : type == "water" ? C(0.35f, 0.55f, 1f) : type == "battery" ? C(0.4f, 1f, 0.5f) : type == "crates" ? C(1f, 0.8f, 0.3f) : C(1f, 0.55f, 0.35f);
    }
    void AddPod(string type, Vector3 pos)
    {
        var pod = new Pod { type = type, pos = pos }; var col = PodColor(type);
        pod.go = new GameObject("Pod_" + type); pod.go.transform.SetParent(siteRoot, false); pod.go.transform.position = pos;
        var r = pod.go.transform;
        P(PrimitiveType.Cylinder, r, new Vector3(0, 0.9f, 0), new Vector3(1.8f, 0.9f, 1.8f), Lit(0.9f, 0.9f, 0.93f), new Vector3(12, 0, 8));
        P(PrimitiveType.Sphere, r, new Vector3(0.2f, 1.85f, 0.1f), new Vector3(1.8f, 1.3f, 1.8f), Lit(0.85f, 0.85f, 0.9f));
        P(PrimitiveType.Cylinder, r, new Vector3(0.05f, 1.0f, 0), new Vector3(1.86f, 0.14f, 1.86f), Lit(col), new Vector3(12, 0, 8));
        P(PrimitiveType.Sphere, r, new Vector3(-2.6f, 0.25f, 1.2f), new Vector3(4.2f, 0.5f, 3.2f), Lit(0.95f, 0.45f, 0.15f), new Vector3(0, 30, 0));
        pod.lamp = Unlit(col); P(PrimitiveType.Sphere, r, new Vector3(0.3f, 2.7f, 0.1f), Vector3.one * 0.4f, pod.lamp, null, false);
        pod.beam = Glass(new Color(col.r, col.g, col.b, 0.3f));
        P(PrimitiveType.Cylinder, r, new Vector3(0, 30f, 0), new Vector3(0.7f, 30f, 0.7f), pod.beam, null, false);
        pod.label = Label3D(siteRoot, pos + Vector3.up * 5f, "SUPPLY POD\n" + type.ToUpper(), 0.08f, col);
        pods.Add(pod);
    }
    void BuildWreck()
    {
        wreck = new GameObject("Wreck").transform; wreck.SetParent(siteRoot, false);
        BuildShip(wreck);
        var dark = Lit(0.15f, 0.15f, 0.17f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward * (5f + (i * 37 % 9));
            P(i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Cylinder, siteRoot, Ground(d + new Vector3(-9, 0, 3)) + Vector3.up * 0.2f,
                new Vector3(0.6f + i % 3 * 0.5f, 0.3f, 1.2f), i % 3 == 0 ? Lit(0.9f, 0.9f, 0.93f) : dark, new Vector3(i * 23, i * 51, i * 17));
        }
    }
    void BuildOutpost()
    {
        var r = Empty("Outpost", siteRoot, Ground(OutpostPos));
        var white = Lit(0.9f, 0.91f, 0.94f);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 1.6f, 6f), new Vector3(9, 1.6f, 9), white);
        P(PrimitiveType.Sphere, r, new Vector3(0, 3.2f, 6f), new Vector3(9, 5f, 9), white);
        P(PrimitiveType.Cylinder, r, new Vector3(0, 0.9f, 6f), new Vector3(9.15f, 0.22f, 9.15f), Lit(0.95f, 0.5f, 0.15f));
        P(PrimitiveType.Cylinder, r, new Vector3(10f, 1.7f, 4f), new Vector3(2.8f, 2.6f, 2.8f), white, new Vector3(0, 0, 90));
        for (int i = 0; i < 4; i++) P(PrimitiveType.Cube, r, new Vector3(-12f + (i % 2) * 5f, 2.1f, 2f + (i / 2) * 5f), new Vector3(4.4f, 0.08f, 2.6f), Lit(0.08f, 0.14f, 0.42f), new Vector3(35, 0, 0));
        P(PrimitiveType.Cylinder, r, new Vector3(0, 9f, 6f), new Vector3(0.16f, 3f, 0.16f), Lit(0.5f, 0.52f, 0.6f));
        baseBeam = Glass(C(0.4f, 1f, 0.6f, 0.4f));
        P(PrimitiveType.Cylinder, r, new Vector3(0, 60f, 6f), new Vector3(1.2f, 60f, 1.2f), baseBeam, null, false);
        P(PrimitiveType.Sphere, r, new Vector3(0, 12.2f, 6f), Vector3.one * 0.7f, Unlit(C(0.4f, 1f, 0.6f)), null, false);
        Label3D(siteRoot, Ground(OutpostPos) + Vector3.up * 14f, "OUTPOST", 0.12f, C(0.5f, 1f, 0.7f));
    }
    Transform BuildRoverModel()
    {
        var r = Empty("PlayerRover", siteRoot, Vector3.zero);
        var body = Empty("Body", r, Vector3.zero);
        var white = Lit(0.9f, 0.9f, 0.94f); var dark = Lit(0.13f, 0.13f, 0.16f); var orange = Lit(0.95f, 0.5f, 0.15f);
        P(PrimitiveType.Cube, body, new Vector3(0, 1.05f, 0), new Vector3(2.1f, 0.75f, 3.8f), white);
        P(PrimitiveType.Cube, body, new Vector3(0, 1.05f, 0), new Vector3(2.14f, 0.16f, 3.84f), orange);
        P(PrimitiveType.Cube, body, new Vector3(0, 1.75f, 0.75f), new Vector3(1.8f, 0.8f, 1.7f), Glass(C(0.3f, 0.6f, 0.9f, 0.55f)));
        P(PrimitiveType.Cube, body, new Vector3(0, 2.18f, 0.75f), new Vector3(1.9f, 0.08f, 1.8f), white);
        foreach (float x in new[] { -1.2f, 1.2f }) foreach (float z in new[] { -1.4f, 0f, 1.4f })
        {
            var w = P(PrimitiveType.Cylinder, r, new Vector3(x, 0.55f, z), new Vector3(1.1f, 0.2f, 1.1f), dark, new Vector3(0, 0, 90)); wheels.Add(w.transform);
        }
        P(PrimitiveType.Cylinder, body, new Vector3(0.7f, 2.6f, -1.5f), new Vector3(0.05f, 1f, 0.05f), dark);
        P(PrimitiveType.Sphere, body, new Vector3(0.7f, 3.6f, -1.5f), Vector3.one * 0.14f, Unlit(C(1f, 0.3f, 0.3f)), null, false);
        roverPanel = Empty("Panel", body, new Vector3(0, 1.55f, -1.05f));
        P(PrimitiveType.Cube, roverPanel, Vector3.zero, new Vector3(2.0f, 0.06f, 1.5f), Lit(0.08f, 0.14f, 0.42f));
        foreach (float x in new[] { -0.7f, 0.7f }) P(PrimitiveType.Sphere, body, new Vector3(x, 1.1f, 1.92f), Vector3.one * 0.24f, Unlit(C(1f, 0.97f, 0.8f)), null, false);
        var lg = new GameObject("Headlights"); lg.transform.SetParent(body, false); lg.transform.localPosition = new Vector3(0, 1.3f, 2f); lg.transform.localEulerAngles = new Vector3(12, 0, 0);
        roverLamp = lg.AddComponent<Light>(); roverLamp.type = LightType.Spot; roverLamp.range = 45f; roverLamp.spotAngle = 70f; roverLamp.color = C(1f, 0.96f, 0.85f); roverLamp.shadows = LightShadows.None;
        return r;
    }

    // ------------------------------------------------------------------ start
    void StartHard()
    {
        StopCoroutine("Intro");
        seed = (int)(DateTime.Now.Ticks % 100000) + 1;
        js = new JourneyState(); st = null; plan = null; pv = null; hardMode = true;
        ClearFx(); bannerQueue.Clear(); pipQueue.Clear(); pipFull = ""; pipShown = 0; pipRoot.SetActive(false); bannerT = 0; debugStorm = -1f;
        campBusy = false; tipLow = tipEmpty = tipSand = false; driveTarget = null; drivenToday = 0; cardMod = null; cardRoot.SetActive(false);
        qOrder.Clear(); var rnd = new System.Random(seed); for (int i = 0; i < Journey.Questions.Count; i++) qOrder.Add(i);
        for (int i = qOrder.Count - 1; i > 0; i--) { int k = rnd.Next(i + 1); int tmp = qOrder[i]; qOrder[i] = qOrder[k]; qOrder[k] = tmp; }
        qIndex = 0;
        BuildJourneySite();
        roverPos = Ground(new Vector3(9f, 0, 4f)); roverYaw = 0; roverSpeed = 0; dayT = 0.30f; camYaw = 0; camPitch = 18f; camDist = 15f;
        modalRoot.SetActive(false); SetJourneyHud(true);
        StartCoroutine(CrashIntro());
    }
    void SetJourneyHud(bool on)
    {
        rationTxt.transform.parent.gameObject.SetActive(!on); actTxt.transform.parent.gameObject.SetActive(!on);
        tlCell[0].transform.parent.gameObject.SetActive(!on);
        endBtn.GetComponentInChildren<UnityEngine.UI.Text>().text = on ? "MAKE CAMP" : "END " + DayWord;
    }

    IEnumerator CrashIntro()
    {
        mode = Mode.Cine; Letterbox(true); cineActive = true; cineSpeed = 3f; rover.gameObject.SetActive(false); pip.gameObject.SetActive(false);
        Vector3 hit = Ground(Vector3.zero), from = hit + new Vector3(-70f, 120f, -110f);
        camPos = hit + new Vector3(34f, 9f, -30f); camLook = from; cinePos = camPos; rocketOn = true;
        float T = shots ? 1.2f : 3.0f, t = 0;
        while (t < T)
        {
            t += Time.deltaTime; float k = Mathf.Clamp01(t / T), e = k * k;
            Vector3 p = Vector3.Lerp(from, hit + Vector3.up * 1.5f, e); wreck.position = p; wreck.rotation = Quaternion.Euler(40f + 60f * k, 30f, 25f * k);
            cineLook = p;
            for (int i = 0; i < 4; i++) Emit(psGlow, p + UnityEngine.Random.insideUnitSphere, UnityEngine.Random.insideUnitSphere * 3f, C(1f, 0.6f, 0.2f), UnityEngine.Random.Range(1.5f, 3f), 0.6f);
            Smoke(p, C(0.2f, 0.2f, 0.2f, 0.6f), 3f, 2.5f);
            yield return null;
        }
        rocketOn = false; wreck.position = hit + Vector3.up * 0.4f; wreck.rotation = Quaternion.Euler(64f, 30f, 18f);
        Flash(C(1f, 0.85f, 0.6f, 0.9f), 1.2f); Shake(1.6f); Sfx("thud", 1.6f); Sfx("alarm");
        DustPuff(hit, 70); Burst(hit + Vector3.up * 2f, C(1f, 0.6f, 0.2f), 60, 14f, 1.2f, 1.4f);
        BannerQ("CRASH LANDING", "40 km from the outpost. Oxygen for less than 5 sols.", C(1f, 0.5f, 0.35f));
        cinePos = hit + new Vector3(22f, 7f, -22f); cineLook = hit + Vector3.up * 2f; cineSpeed = 1.5f;
        yield return new WaitForSeconds(shots ? 0.8f : 3.0f);
        rover.gameObject.SetActive(true); pip.gameObject.SetActive(true); pipPos = roverPos + Vector3.up * 3f;
        cineActive = false; Letterbox(false); mode = Mode.Play; SnapJourneyCamera();
        PipSay("Everyone is safe, but the ship is gone. The outpost is 40 km north: follow the green beacon. Drive with WASD or ZQSD, or click the ground.", 2);
        PipSay("The rover has no water recycler and little air. Supply pods fell along the canyon: their light beams show where. Each one is a detour.", 1);
        yield return new WaitForSeconds(shots ? 0.3f : 1.5f);
        AskQuestion("Before you leave, PIP checks the rover with you. Get it right and the battery is fully tuned.",
            right => { if (!right) js.battery = Math.Min(js.battery, 80); return right ? "Battery tuned: 100%." : "The battery is only at 80%."; });
    }

    // ------------------------------------------------------------------ technical challenges
    void AskQuestion(string context, Func<bool, string> reward)
    {
        if (qIndex >= qOrder.Count) qIndex = 0;
        var q = Journey.Questions[qOrder[qIndex++]];
        var list = new List<Btn>();
        for (int i = 0; i < q.options.Length; i++) { int k = i; list.Add(new Btn { label = q.options[i], close = false, act = () => AnswerQuestion(q, k, reward) }); }
        ShowModal("TECH CHALLENGE  -  " + q.topic, context + "\n\n" + q.text, list, 21);
        Sfx("on");
        if (shots) StartCoroutine(AutoAnswer(q, reward));
    }
    IEnumerator AutoAnswer(TechQuestion q, Func<bool, string> reward)
    {
        yield return Frames(shotHold); AnswerQuestion(q, q.answer, reward); yield return Frames(shotHold); DismissModal();
    }
    int shotHold = 12;
    void AnswerQuestion(TechQuestion q, int choice, Func<bool, string> reward)
    {
        bool right = choice == q.answer; js.asked++; if (right) js.right++;
        string gain = reward != null ? reward(right) : "";
        if (right) { Flash(C(0.3f, 1f, 0.5f, 0.3f), 0.6f); Sfx("good"); Burst(roverPos + Vector3.up * 3f, C(0.4f, 1f, 0.6f), 30, 7f); }
        else { Flash(C(1f, 0.5f, 0.2f, 0.25f), 0.5f); Sfx("off"); }
        ShowModal(right ? "Correct!" : "Not quite", q.text + "\n\nAnswer: " + q.options[q.answer] + "\n" + q.why + "\n\n" + gain, new List<Btn> { new Btn { label = "Continue" } }, 19);
        RefreshJourneyHud();
    }
    void OpenPod(Pod pod)
    {
        pod.taken = true; roverSpeed = 0; driveTarget = null;
        var c = pod.beam.color; c.a = 0f; pod.beam.color = c; pod.lamp.color = C(0.2f, 0.2f, 0.2f); pod.label.text = "";
        Burst(pod.pos + Vector3.up * 2f, PodColor(pod.type), 30, 6f);
        AskQuestion("Supply pod reached: " + pod.type.ToUpper() + ". The hatch is jammed. Solve this to open it fully (a wrong answer opens it half way).",
            right => "Pod opened: " + Journey.ApplyPod(js, pod.type, right) + (right ? "." : " (half)."));
    }

    // ------------------------------------------------------------------ driving
    void SnapJourneyCamera()
    {
        Vector3 look = roverPos + Vector3.up * 2f; camLook = look; camPos = look + Quaternion.Euler(camPitch, camYaw, 0) * new Vector3(0, 0, -camDist);
    }
    Vector3 RoverForward { get { return Quaternion.Euler(0, roverYaw, 0) * Vector3.forward; } }

    void TickJourney(float dt)
    {
        if (!journey || rover == null) return;
        bool play = mode == Mode.Play;
        float f = 0, steer = 0;
        if (play)
        {
            if (K(KeyCode.W) || K(KeyCode.Z) || K(KeyCode.UpArrow)) f += 1; if (K(KeyCode.S) || K(KeyCode.DownArrow)) f -= 1;
            if (K(KeyCode.D) || K(KeyCode.RightArrow)) steer += 1; if (K(KeyCode.A) || K(KeyCode.Q) || K(KeyCode.LeftArrow)) steer -= 1;
            if (f != 0 || steer != 0) driveTarget = null;
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.C)) StartCoroutine(Camp());
            if (autoDrive) AutoDrive();
            if (driveTarget.HasValue)
            {
                Vector3 to = driveTarget.Value - roverPos; to.y = 0;
                if (to.magnitude < 3.5f) driveTarget = null;
                else { float want = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg; float da = Mathf.DeltaAngle(roverYaw, want); steer = Mathf.Clamp(da / 25f, -1f, 1f); f = Mathf.Abs(da) > 100f ? 0.4f : 1f; }
            }
            // mouse: click to drive there, drag to look, wheel to zoom
            if (Input.GetMouseButtonDown(0)) { mTrack = !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()); mDown = mLast = Input.mousePosition; mDrag = false; }
            if (Input.GetMouseButton(0) && mTrack)
            {
                Vector3 d = Input.mousePosition - mLast; mLast = Input.mousePosition;
                if (!mDrag && (Input.mousePosition - mDown).magnitude > 12f) mDrag = true;
                if (mDrag) { camYaw += d.x * 0.3f; camPitch = Mathf.Clamp(camPitch - d.y * 0.2f, 4f, 72f); }
            }
            if (Input.GetMouseButtonUp(0) && mTrack)
            {
                mTrack = false; RaycastHit hit;
                if (!mDrag && Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, 900f)) { driveTarget = hit.point; Burst(hit.point + Vector3.up * 0.3f, C(0.4f, 0.9f, 1f), 6, 1.5f, 0.3f, 0.5f); }
            }
            camDist = Mathf.Clamp(camDist - Input.mouseScrollDelta.y * 1.3f, 7f, 40f);
        }
        bool sand = InSand(roverPos); bool empty = js.battery <= 0.01;
        float max = sand ? 5.2f : 9.5f; float target = empty ? 0 : (f > 0 ? max * f : f * 4f);
        roverSpeed = Mathf.MoveTowards(roverSpeed, target, dt * (target == 0 ? 14f : 9f));
        if (Mathf.Abs(roverSpeed) > 0.2f) roverYaw += steer * 62f * dt * Mathf.Sign(roverSpeed);
        float dist = Mathf.Abs(roverSpeed) * dt;
        if (dist > 0)
        {
            Vector3 np = roverPos + RoverForward * roverSpeed * dt; np.x = Mathf.Clamp(np.x, -82f, 82f); np.z = Mathf.Clamp(np.z, -44f, 690f);
            roverPos = Ground(np); drivenToday += dist;
            Journey.Drive(js, dist / MetersPerKm, sand);
            js.traveledKm = Math.Max(js.traveledKm, Mathf.Clamp(roverPos.z, 0, JourneyLen) / MetersPerKm);
            foreach (var w in wheels) w.Rotate(Vector3.up * roverSpeed * dt * 110f, Space.Self);
            puffT -= dt; if (puffT <= 0) { puffT = 0.07f; DustPuff(roverPos - RoverForward * 1.8f, sand ? 3 : 1); }
            if (!campBusy) dayT = 0.30f + Mathf.Min(0.36f, drivenToday / (float)(Journey.KmPerCharge * MetersPerKm) * 0.30f);
        }
        // sit on the ground and lean with the slope
        Vector3 fw = RoverForward, rt = Quaternion.Euler(0, roverYaw, 0) * Vector3.right;
        float hf = HJ(roverPos.x + fw.x * 1.6f, roverPos.z + fw.z * 1.6f), hb = HJ(roverPos.x - fw.x * 1.6f, roverPos.z - fw.z * 1.6f);
        float hr = HJ(roverPos.x + rt.x * 1.1f, roverPos.z + rt.z * 1.1f), hl = HJ(roverPos.x - rt.x * 1.1f, roverPos.z - rt.z * 1.1f);
        float pitch = -Mathf.Atan2(hf - hb, 3.2f) * Mathf.Rad2Deg, roll = Mathf.Atan2(hr - hl, 2.2f) * Mathf.Rad2Deg;
        rover.position = roverPos; rover.rotation = Quaternion.Slerp(rover.rotation, Quaternion.Euler(pitch, roverYaw, -roll), dt * 8f);
        roverPanel.localRotation = Quaternion.Slerp(roverPanel.localRotation, Quaternion.Euler(campBusy ? -38f : 0f, 0, 0), dt * 3f);
        if (roverLamp != null) roverLamp.intensity = 2.6f * (1f - dayA) + (stormK > 0.4f ? 1.6f : 0f);
        if (!mTrack && Mathf.Abs(roverSpeed) > 1f && play) camYaw = Mathf.LerpAngle(camYaw, roverYaw, dt * 1.6f);

        // wreck smoke, pod beacons
        wreckSmokeT -= dt; if (wreckSmokeT <= 0 && wreck != null) { wreckSmokeT = 0.15f; Smoke(wreck.position + Vector3.up * 2f, C(0.12f, 0.12f, 0.12f, 0.5f), 2.5f, 4f); }
        foreach (var pod in pods)
        {
            if (pod.taken) continue;
            pod.lamp.color = PodColor(pod.type) * (0.5f + 0.5f * Mathf.Sin(Time.time * 5f + pod.pos.z));
            if (play && Flat(roverPos, pod.pos) < 7.5f) { OpenPod(pod); break; }
        }
        debugStorm = js.sol == Journey.StormSol && !js.arrived ? 1f : -1f;

        if (play)
        {
            if (sand && !tipSand && Mathf.Abs(roverSpeed) > 1f) { tipSand = true; PipSay("Soft sand! The wheels sink, so the motor uses twice the power. Going around costs distance, going through costs battery.", 1); }
            if (js.battery < 18 && !tipLow) { tipLow = true; PipSay("Battery under 18%. When it is empty, press MAKE CAMP: the solar panel recharges it while the crew sleeps.", 1); }
            if (empty && !tipEmpty) { tipEmpty = true; PipSay("Battery empty. Press MAKE CAMP. A night costs one sol of oxygen, water and food.", 2); Sfx("down"); }
            if (Flat(roverPos, Ground(OutpostPos)) < 17f) StartCoroutine(Arrive());
        }
        hudT -= dt; if (hudT <= 0) { hudT = 0.15f; RefreshJourneyHud(); }
    }

    void AutoDrive()
    {   // scripted driver for the automatic screenshots: fetch near pods, avoid sand, camp when empty
        if (js.battery <= 0.5 && !campBusy) { StartCoroutine(Camp()); return; }
        Vector3 goal = Ground(OutpostPos);
        if (js.pods < 4) foreach (var p in pods) if (!p.taken && p.pos.z > roverPos.z - 5f && p.pos.z < roverPos.z + 70f) { goal = p.pos; break; }
        Vector3 aim = goal;
        foreach (var s in sandZones) { if (Mathf.Abs(roverPos.z - s.y) < s.z + 22f && Mathf.Abs(goal.x) < 20f) aim = new Vector3(s.x + (roverPos.x > s.x ? s.z + 10f : -s.z - 10f), 0, Mathf.Max(roverPos.z + 12f, s.y)); }
        driveTarget = aim;
    }

    void RefreshJourneyHud()
    {
        if (!journey || js == null) return;
        solText.text = "SOL " + js.sol + "   HARD MODE";
        crateText.text = "Crates: " + js.crates;
        Vector3 to = Ground(OutpostPos) - roverPos; float rel = Mathf.DeltaAngle(camYaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
        string dir = Mathf.Abs(rel) < 25f ? "straight ahead" : (Mathf.Abs(rel) > 140f ? "behind you" : (rel > 0 ? "to the right" : "to the left"));
        alertText.text = "Outpost: " + Fmt(Journey.KmLeft(js)) + " km, " + dir + (js.sol == Journey.StormSol ? "\nDust storm today: tonight's recharge is only " + Fmt(Journey.StormRecharge) + "%." : "");
        SetGauge("o2", "Oxygen", js.o2, 20, "kg", null); gauges["o2"].label.text += "  (" + Fmt(js.o2 / Journey.O2PerSol) + " sols)";
        SetGauge("water", "Water", js.water, 60, "L", null); gauges["water"].label.text += "  (" + Fmt(js.water / Journey.CrewWaterNoRecycler) + " sols)";
        SetGauge("food", "Food", js.food, 45, "kg", null); gauges["food"].label.text += "  (" + Fmt(js.food / Journey.FoodPerSol) + " sols)";
        SetGauge("battery", "Rover", js.battery, 100, "%", null);
        SetGauge("health", "Health", js.health, 100, "", null);
        SetGauge("dose", "Driven", js.traveledKm, Journey.TotalKm, "km", null); gauges["dose"].low = false; gauges["dose"].fill.color = new Color(0.35f, 0.8f, 1f);
        bool sand = InSand(roverPos);
        infoText.text = "Battery range: " + Fmt(js.battery / 100.0 * Journey.KmPerCharge / (sand ? Journey.SandFactor : 1)) + " km" + (sand ? "  (SOFT SAND: power use x2)" : "") +
            "\nChallenges solved: " + js.right + " of " + js.asked + ".  Pods opened: " + js.pods + ".";
        infoText.color = sand ? new Color(1f, 0.8f, 0.5f) : Color.white;
        endBtn.interactable = mode == Mode.Play && !campBusy;
    }

    // ------------------------------------------------------------------ camp, arrival, failure
    IEnumerator Camp()
    {
        if (!journey || mode != Mode.Play || campBusy || js.arrived || js.failed) yield break;
        campBusy = true; mode = Mode.Cine; Letterbox(true); cineActive = true; cineSpeed = 1.6f; roverSpeed = 0; driveTarget = null;
        cinePos = roverPos + Quaternion.Euler(0, roverYaw, 0) * new Vector3(11f, 6f, -13f); cineLook = roverPos + Vector3.up * 1.5f;
        Sfx("on");
        bool storm = js.sol == Journey.StormSol;
        float T = shots ? 1.3f : 3.8f, t = 0, t0 = dayT;
        while (t < T) { t += Time.deltaTime; float k = Mathf.Clamp01(t / T), e = k * k * (3f - 2f * k); dayT = Mathf.Lerp(t0, 1.30f, e); yield return null; }
        dayT = 0.30f; drivenToday = 0; tipLow = tipEmpty = false;
        double hBefore = js.health;
        Journey.Camp(js);
        solPop = 0.7f; RefreshJourneyHud();
        if (js.health < hBefore) { BannerQ("SUPPLIES RAN OUT", "The crew lost " + Fmt(hBefore - js.health) + " health last night", C(1f, 0.4f, 0.35f)); Flash(C(1f, 0.1f, 0.1f, 0.4f), 0.9f); Sfx("alarm"); }
        else if (storm) { BannerQ("DUST STORM NIGHT", "The panel recharged only " + Fmt(Journey.StormRecharge) + "%", C(1f, 0.68f, 0.35f)); Sfx("warn"); }
        else { BannerQ("SOL " + js.sol, Fmt(Journey.KmLeft(js)) + " km to the outpost. Oxygen for " + Fmt(js.o2 / Journey.O2PerSol) + " sols.", C(0.7f, 0.92f, 1f)); Sfx("good"); }
        yield return new WaitForSeconds(shots ? 0.5f : 1.6f);
        while (bannerQueue.Count > 0 || bannerT > 0.05f) yield return null;
        cineActive = false; Letterbox(false); campBusy = false;
        if (js.failed) { yield return JourneyFail(); yield break; }
        mode = Mode.Play;
        if (js.sol == Journey.StormSol) PipSay("A dust storm is here. Tonight the panel will recharge only " + Fmt(Journey.StormRecharge) + "%. Use today's battery well.", 2);
        AskQuestion("Morning check. PIP has a problem for you. Solve it to tune the solar panel: +15% battery.",
            right => { if (right) js.battery = Math.Min(100, js.battery + 15); return right ? "Panel tuned: +15% battery." : "No bonus this morning."; });
    }

    IEnumerator Arrive()
    {
        if (js.arrived) yield break;
        js.arrived = true; mode = Mode.Cine; Letterbox(true); cineActive = true; cineSpeed = 1.3f; roverSpeed = 0; driveTarget = null; debugStorm = -1f;
        Vector3 b = Ground(OutpostPos); cinePos = b + new Vector3(30f, 12f, -34f); cineLook = b + Vector3.up * 4f;
        BannerQ("OUTPOST REACHED", "Sol " + js.sol + ". " + js.pods + " pods opened, " + js.right + " of " + js.asked + " challenges solved.", C(0.55f, 1f, 0.72f));
        Sfx("win"); Fireworks(b); Flash(C(0.6f, 1f, 0.8f, 0.35f), 1f);
        yield return new WaitForSeconds(shots ? 1.0f : 3.4f);
        Fireworks(b);
        yield return new WaitForSeconds(shots ? 0.3f : 1.2f);
        // the 30-sol mission starts with what is left
        journey = false; SetJourneyHud(false);
        StartGame("mars", Journey.ToGameState(js, seed));
    }

    IEnumerator JourneyFail()
    {
        mode = Mode.Cine; Letterbox(true); cineActive = true; cineSpeed = 1.4f;
        cinePos = roverPos + new Vector3(14f, 6f, -12f); cineLook = roverPos + Vector3.up * 6f;
        BannerQ("MEDICAL EVACUATION", "Sol " + js.sol + ". A rescue hopper reaches the crew in time.", C(1f, 0.4f, 0.34f));
        Sfx("alarm"); Flash(C(1f, 0.05f, 0.05f, 0.45f), 1.2f); rocketOn = true;
        reliefShipJourney = new GameObject("Hopper"); reliefShipJourney.transform.SetParent(siteRoot, false); BuildShip(reliefShipJourney.transform);
        Vector3 land = Ground(roverPos + new Vector3(9f, 0, 6f)); float T = shots ? 1.2f : 4.2f, t = 0;
        while (t < T)
        {
            t += Time.deltaTime; float k = Mathf.Clamp01(t / T), e = 1f - (1f - k) * (1f - k);
            var p = land + Vector3.up * Mathf.Lerp(90f, 0f, e); reliefShipJourney.transform.position = p; cineLook = p + Vector3.up * 4f;
            for (int i = 0; i < 3; i++) Emit(psGlow, p + Vector3.up * 0.8f, Vector3.down * UnityEngine.Random.Range(8f, 16f), C(1f, 0.7f, 0.3f), UnityEngine.Random.Range(1f, 2.2f), 0.5f);
            yield return null;
        }
        rocketOn = false;
        yield return new WaitForSeconds(shots ? 0.3f : 1.2f);
        cineActive = false; Letterbox(false); mode = Mode.Modal;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Medical evacuation on sol " + js.sol + ", " + Fmt(Journey.KmLeft(js)) + " km from the outpost. The crew is safe.").AppendLine();
        sb.AppendLine("What happened:");
        sb.AppendLine("  Sol 1: the ship crashed 40 km from the outpost with oxygen for under 5 sols.");
        foreach (var l in js.log.Take(5)) sb.AppendLine("  " + l);
        sb.AppendLine().AppendLine("The trade-off: every detour and every stretch of soft sand costs battery, and every night to recharge costs a sol of air, water and food.");
        sb.AppendLine().AppendLine("Next time: " + (js.pods == 0 ? "open the oxygen and water pods near your route, and drive around the soft sand." : "drive around the soft sand, and solve the morning challenge for extra battery."));
        ShowModal("MISSION DEBRIEF", sb.ToString(), new List<Btn> {
            new Btn { label = "Try Hard mode again", act = () => StartHard() },
            new Btn { label = "Normal mode (Mars)", act = () => { journey = false; SetJourneyHud(false); StartGame("mars"); } } }, 16);
    }
    GameObject reliefShipJourney;
}
