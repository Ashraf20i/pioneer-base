// Pioneer Base 3D - PioneerGame.Actors.cs
// The player astronaut, the three other crew members, PIP the companion drone, and the camera.
using System.Collections.Generic;
using UnityEngine;
using PioneerBase;

public partial class PioneerGame
{
    class Astro
    {
        public Transform root, rig, legL, legR, armL, armR; public Vector3 pos, dest; public float yaw, phase, wait, walkT, speed = 2.2f;
        public bool moving, npc, working; public int target = -1;
    }
    Astro player; readonly List<Astro> crew = new List<Astro>();
    Light helmetLamp;
    Transform pip; Material pipEye; Light pipLight; Vector3 pipPos; Color pipEyeTarget = new Color(0.3f, 0.9f, 1f);

    // camera
    float camYaw = 0f, camPitch = 20f, camDist = 13f; Vector3 camPos = new Vector3(0, 30, -60), camLook = Vector3.zero;
    bool cineActive; Vector3 cinePos, cineLook; float cineSpeed = 2.2f, shakeAmt;

    Astro MakeAstro(Color accent, bool npc)
    {
        var a = new Astro { npc = npc };
        a.root = new GameObject(npc ? "Crew" : "Player").transform; a.root.SetParent(siteRoot, false);
        a.rig = Empty("Rig", a.root, Vector3.zero);
        var white = Lit(0.94f, 0.95f, 0.98f); var grey = Lit(0.5f, 0.54f, 0.62f); var acc = Lit(accent); var skin = Lit(1f, 0.8f, 0.66f);
        P(PrimitiveType.Capsule, a.rig, new Vector3(0, 1.08f, 0), new Vector3(0.66f, 0.44f, 0.5f), white);
        P(PrimitiveType.Cube, a.rig, new Vector3(0, 1.14f, -0.31f), new Vector3(0.56f, 0.66f, 0.28f), grey);
        P(PrimitiveType.Cube, a.rig, new Vector3(0, 0.82f, 0), new Vector3(0.66f, 0.1f, 0.5f), acc);
        P(PrimitiveType.Cube, a.rig, new Vector3(0, 1.2f, 0.25f), new Vector3(0.28f, 0.2f, 0.08f), grey);
        P(PrimitiveType.Sphere, a.rig, new Vector3(0.07f, 1.2f, 0.3f), Vector3.one * 0.06f, Unlit(C(0.3f, 1f, 0.8f)), null, false);
        // helmet: white shell behind, face in front, glass visor over it
        P(PrimitiveType.Sphere, a.rig, new Vector3(0, 1.76f, -0.05f), Vector3.one * 0.7f, white);
        P(PrimitiveType.Sphere, a.rig, new Vector3(0, 1.75f, 0.1f), new Vector3(0.5f, 0.5f, 0.46f), skin);
        P(PrimitiveType.Sphere, a.rig, new Vector3(0, 1.88f, 0.06f), new Vector3(0.5f, 0.3f, 0.46f), Lit(0.42f, 0.25f, 0.14f));
        foreach (float x in new[] { -0.1f, 0.1f })
        {
            P(PrimitiveType.Sphere, a.rig, new Vector3(x, 1.77f, 0.31f), Vector3.one * 0.1f, Unlit(Color.white), null, false);
            P(PrimitiveType.Sphere, a.rig, new Vector3(x, 1.77f, 0.345f), Vector3.one * 0.06f, Unlit(C(0.15f, 0.4f, 0.85f)), null, false);
        }
        P(PrimitiveType.Cube, a.rig, new Vector3(0, 1.64f, 0.325f), new Vector3(0.12f, 0.025f, 0.02f), Unlit(C(0.6f, 0.2f, 0.2f)), null, false);
        P(PrimitiveType.Sphere, a.rig, new Vector3(0, 1.75f, 0.14f), new Vector3(0.6f, 0.56f, 0.5f), Glass(C(0.6f, 0.85f, 1f, 0.2f)), null, false);
        foreach (float x in new[] { -0.36f, 0.36f }) P(PrimitiveType.Cylinder, a.rig, new Vector3(x, 1.76f, -0.02f), new Vector3(0.2f, 0.04f, 0.2f), acc, new Vector3(0, 0, 90));
        a.armL = Empty("ArmL", a.rig, new Vector3(-0.42f, 1.32f, 0)); a.armR = Empty("ArmR", a.rig, new Vector3(0.42f, 1.32f, 0));
        foreach (var arm in new[] { a.armL, a.armR })
        {
            P(PrimitiveType.Capsule, arm, new Vector3(0, -0.27f, 0), new Vector3(0.18f, 0.3f, 0.18f), white);
            P(PrimitiveType.Sphere, arm, new Vector3(0, -0.58f, 0), Vector3.one * 0.18f, acc);
        }
        a.legL = Empty("LegL", a.rig, new Vector3(-0.17f, 0.74f, 0)); a.legR = Empty("LegR", a.rig, new Vector3(0.17f, 0.74f, 0));
        foreach (var leg in new[] { a.legL, a.legR })
        {
            P(PrimitiveType.Capsule, leg, new Vector3(0, -0.34f, 0), new Vector3(0.24f, 0.36f, 0.24f), white);
            P(PrimitiveType.Cube, leg, new Vector3(0, -0.67f, 0.05f), new Vector3(0.26f, 0.14f, 0.38f), grey);
        }
        return a;
    }

    void BuildActors()
    {
        player = MakeAstro(C(1f, 0.55f, 0.12f), false);
        player.pos = Ground(new Vector3(0, 0, -11f)); player.yaw = 0f;
        var hl = new GameObject("HelmetLamp"); hl.transform.SetParent(player.rig, false); hl.transform.localPosition = new Vector3(0, 1.95f, 0.2f); hl.transform.localEulerAngles = new Vector3(18, 0, 0);
        helmetLamp = hl.AddComponent<Light>(); helmetLamp.type = LightType.Spot; helmetLamp.range = 26f; helmetLamp.spotAngle = 62f; helmetLamp.color = C(0.9f, 0.95f, 1f); helmetLamp.shadows = LightShadows.None;
        Color[] cols = { C(0.3f, 0.75f, 1f), C(0.45f, 0.9f, 0.45f), C(0.95f, 0.4f, 0.6f) };
        Vector3[] starts = { new Vector3(-8, 0, -5), new Vector3(9, 0, -9), new Vector3(4, 0, -12) };
        for (int i = 0; i < 3; i++)
        {
            var c = MakeAstro(cols[i], true); c.pos = Ground(starts[i]); c.wait = 1f + i; c.yaw = 120f * i; crew.Add(c);
        }
        // PIP, the companion drone
        pip = new GameObject("PIP").transform; pip.SetParent(siteRoot, false);
        P(PrimitiveType.Sphere, pip, Vector3.zero, Vector3.one * 0.46f, Lit(0.95f, 0.95f, 0.98f));
        P(PrimitiveType.Cylinder, pip, Vector3.zero, new Vector3(0.6f, 0.03f, 0.6f), Lit(0.3f, 0.34f, 0.42f));
        pipEye = Unlit(C(0.3f, 0.9f, 1f));
        P(PrimitiveType.Sphere, pip, new Vector3(0, 0.02f, 0.17f), new Vector3(0.24f, 0.24f, 0.16f), pipEye, null, false);
        P(PrimitiveType.Sphere, pip, new Vector3(0, 0.02f, 0.235f), new Vector3(0.1f, 0.1f, 0.05f), Unlit(C(0.02f, 0.05f, 0.1f)), null, false);
        P(PrimitiveType.Cylinder, pip, new Vector3(0, 0.32f, 0), new Vector3(0.02f, 0.12f, 0.02f), Lit(0.3f, 0.34f, 0.42f));
        P(PrimitiveType.Sphere, pip, new Vector3(0, 0.45f, 0), Vector3.one * 0.07f, pipEye, null, false);
        var pl = new GameObject("PipLight"); pl.transform.SetParent(pip, false);
        pipLight = pl.AddComponent<Light>(); pipLight.type = LightType.Point; pipLight.range = 5f; pipLight.intensity = 0.8f; pipLight.shadows = LightShadows.None;
        pipPos = player.pos + new Vector3(1.2f, 2f, -0.5f);
        walkTarget = null; walkMod = null;
    }

    float HopHeight { get { return site == "moon" ? 0.32f : 0.12f; } }

    void AnimateAstro(Astro a, float dt, float speedScale)
    {
        if (a.moving) a.phase += dt * (site == "moon" ? 7.5f : 10f) * speedScale; else a.phase = Mathf.MoveTowards(a.phase % (Mathf.PI * 2f), 0f, dt * 12f);
        float sw = Mathf.Sin(a.phase), hop = a.moving ? Mathf.Abs(Mathf.Sin(a.phase)) * HopHeight : 0f;
        float swing = a.moving ? 34f : 0f;
        a.legL.localEulerAngles = new Vector3(sw * swing, 0, 0); a.legR.localEulerAngles = new Vector3(-sw * swing, 0, 0);
        if (a.working) { float w = Mathf.Sin(Time.time * 6f + a.pos.x) * 25f; a.armL.localEulerAngles = new Vector3(-60f + w, 0, 0); a.armR.localEulerAngles = new Vector3(-60f - w, 0, 0); }
        else { a.armL.localEulerAngles = new Vector3(-sw * swing * 0.8f, 0, 6f); a.armR.localEulerAngles = new Vector3(sw * swing * 0.8f, 0, -6f); }
        float idle = a.moving ? 0f : Mathf.Sin(Time.time * 2f + a.pos.z) * 0.012f;
        a.root.position = a.pos + Vector3.up * (hop + idle);
        a.root.rotation = Quaternion.Slerp(a.root.rotation, Quaternion.Euler(0, a.yaw, 0), dt * 10f);
        a.rig.localEulerAngles = new Vector3(a.moving ? 6f : 0f, 0, 0);
    }

    Vector3 Collide(Vector3 p, float pad)
    {
        foreach (var m in mods)
        {
            if (!m.solid || !m.root.gameObject.activeInHierarchy) continue;
            float dx = p.x - m.pos.x, dz = p.z - m.pos.z, d = Mathf.Sqrt(dx * dx + dz * dz), min = m.radius * 0.85f + pad;
            if (d < min && d > 0.001f) { p.x = m.pos.x + dx / d * min; p.z = m.pos.z + dz / d * min; }
        }
        foreach (var s in slots)
        {
            if (s.mod != null || s.state != 3) continue;
            float dx = p.x - s.pos.x, dz = p.z - s.pos.z, d = Mathf.Sqrt(dx * dx + dz * dz), min = Mathf.Min(s.radius, 1.2f) * 0.8f + pad;
            if (d < min && d > 0.001f) { p.x = s.pos.x + dx / d * min; p.z = s.pos.z + dz / d * min; }
        }
        float r = Mathf.Sqrt(p.x * p.x + p.z * p.z); if (r > 68f) { p.x *= 68f / r; p.z *= 68f / r; }
        p.y = H(p.x, p.z); return p;
    }
    void StepPlayer(Vector3 dir, float dt)
    {
        dir.y = 0; if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();
        player.pos = Collide(player.pos + dir * 5.2f * dt, 0.45f);
        player.yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; player.moving = true;
        puffT -= dt;
        if (puffT <= 0)
        {
            puffT = site == "moon" ? 0.42f : 0.32f; DustPuff(player.pos, 2);
            // boot prints stay on the ground
            printSide = -printSide;
            var q = Quaternion.Euler(0, player.yaw, 0);
            var fp = P(PrimitiveType.Cube, siteRoot, Ground(player.pos + q * new Vector3(0.17f * printSide, 0, 0)) + Vector3.up * 0.015f, new Vector3(0.2f, 0.02f, 0.38f),
                Lit(site == "mars" ? C(0.36f, 0.18f, 0.11f) : C(0.26f, 0.26f, 0.28f)), new Vector3(0, player.yaw, 0), false);
            prints.Enqueue(fp); if (prints.Count > 140) { var old = prints.Dequeue(); if (old != null) Destroy(old); }
        }
    }
    float puffT, printSide = 1f; readonly Queue<GameObject> prints = new Queue<GameObject>();

    void TickActors(float dt)
    {
        if (player == null) return;
        if (journey) { TickPip(roverPos, roverYaw, dt, 2.6f); if (helmetLamp != null) helmetLamp.intensity = 0; return; }
        AnimateAstro(player, dt, 1f); player.moving = false;
        bool hide = st != null && plan != null && plan.activity == "shelter" && mode != Mode.Title;
        float lapse = lapseActive ? 5f : 1f;
        foreach (var c in crew)
        {
            Vector3 to = c.dest - c.pos; to.y = 0; float d = to.magnitude;
            bool arrived = d <= 0.9f;
            if (hide || dayA < 0.2f) { c.dest = Ground(new Vector3(0, 0, -7.2f)); c.target = -2; }
            else if (c.target != 1 || (arrived && c.wait <= 0) || c.walkT > 25f)
            {
                var m = mods[Random.Range(0, mods.Count)];
                if (m.root.gameObject.activeInHierarchy) { c.dest = m.ipos + new Vector3(Random.Range(-1.2f, 1.2f), 0, Random.Range(-1.2f, 1.2f)); c.target = 1; c.wait = Random.Range(3f, 8f); c.walkT = 0; }
            }
            to = c.dest - c.pos; to.y = 0; d = to.magnitude;
            bool inside = c.target == -2 && d < 1.6f;
            c.root.gameObject.SetActive(!inside);
            if (d > 0.9f)
            {
                var step = to / d * c.speed * lapse * dt; c.pos = Collide(c.pos + step, 0.45f);
                c.yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg; c.moving = true; c.working = false; c.walkT += dt * lapse;
            }
            else { c.moving = false; c.working = c.target == 1; c.wait -= dt * lapse; }
            AnimateAstro(c, dt, lapse * 0.6f); c.moving = false;
        }
        if (helmetLamp != null) helmetLamp.intensity = 2.4f * (1f - dayA) + (stormK > 0.5f ? 1.2f : 0f);

        TickPip(player.pos, player.yaw, dt, 1.15f);
    }

    // PIP hovers beside the player (or the rover) and looks at the camera
    void TickPip(Vector3 anchor, float yaw, float dt, float side)
    {
        Vector3 want = anchor + Quaternion.Euler(0, yaw, 0) * new Vector3(side, side > 2f ? 3.3f : 2.05f, -0.2f) + Vector3.up * Mathf.Sin(Time.time * 2.4f) * 0.1f;
        pipPos = Vector3.Lerp(pipPos, want, 1f - Mathf.Exp(-dt * 3.5f));
        pip.position = pipPos;
        Vector3 look = (cam.transform.position - pipPos); look.y *= 0.3f;
        if (look.sqrMagnitude > 0.01f) pip.rotation = Quaternion.Slerp(pip.rotation, Quaternion.LookRotation(look), dt * 4f);
        pipEye.color = Color.Lerp(pipEye.color, pipEyeTarget * (0.85f + 0.15f * Mathf.Sin(Time.time * 5f)), dt * 5f);
        pipLight.color = pipEye.color;
    }

    void TickCamera(float dt)
    {
        Vector3 wantPos, wantLook; float speed = 9f;
        if (mode == Mode.Title && !cineActive)
        {
            float ang = Time.time * 5f * Mathf.Deg2Rad + 0.6f;
            wantLook = new Vector3(0, 3f, 0); wantPos = new Vector3(Mathf.Sin(ang) * 44f, 13f, -Mathf.Cos(ang) * 44f); speed = 3f;
        }
        else if (cineActive) { wantPos = cinePos; wantLook = cineLook; speed = cineSpeed; }
        else
        {
            wantLook = (journey ? roverPos : player.pos) + Vector3.up * (journey ? 2f : 1.5f);
            wantPos = wantLook + Quaternion.Euler(camPitch, camYaw, 0) * new Vector3(0, 0, -camDist);
        }
        float minY = H(wantPos.x, wantPos.z) + 0.7f; if (wantPos.y < minY) wantPos.y = minY;
        float k = 1f - Mathf.Exp(-dt * speed);
        camPos = Vector3.Lerp(camPos, wantPos, k); camLook = Vector3.Lerp(camLook, wantLook, k);
        Vector3 sh = shakeAmt > 0.001f ? Random.insideUnitSphere * shakeAmt : Vector3.zero; shakeAmt = Mathf.MoveTowards(shakeAmt, 0, dt * 1.2f);
        cam.transform.position = camPos + sh;
        cam.transform.rotation = Quaternion.LookRotation((camLook - camPos).normalized, Vector3.up);
    }
    void SnapCamera()
    {
        Vector3 look = player.pos + Vector3.up * 1.5f; camLook = look; camPos = look + Quaternion.Euler(camPitch, camYaw, 0) * new Vector3(0, 0, -camDist);
    }
    void Shake(float a) { shakeAmt = Mathf.Max(shakeAmt, a); }
}
