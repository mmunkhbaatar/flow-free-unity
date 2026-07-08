using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Flow Free clone — бүх зүйлийг runtime дээр код-оор үүсгэдэг тул
/// scene дотор ямар ч объект шаардлагагүй. Play дархад л ажиллана.
/// </summary>
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (Object.FindObjectOfType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }
}

public class GameManager : MonoBehaviour
{
    const int N = 5; // 5x5 хүснэгт

    static readonly Color[] Palette =
    {
        new Color(0.92f, 0.26f, 0.21f), // улаан
        new Color(0.30f, 0.69f, 0.31f), // ногоон
        new Color(0.26f, 0.52f, 0.96f), // цэнхэр
        new Color(1.00f, 0.84f, 0.00f), // шар
        new Color(1.00f, 0.55f, 0.00f), // улбар шар
    };

    // Үе бүр: өнгө тус бүрийн хоёр төгсгөлийн цэг {x1, y1, x2, y2}.
    // (x=багана, y=мөр, y=0 нь доод мөр). Бүх үе бүрэн шийдэлтэй
    // (хүснэгтийг 100% дүүргэдэг шийдээс эхлэн зохиогдсон).
    static readonly int[][][] Levels =
    {
        new[] { new[]{0,4,1,0}, new[]{1,4,4,0}, new[]{1,3,2,0} },
        new[] { new[]{0,4,1,2}, new[]{3,4,2,2}, new[]{4,2,2,0} },
        new[] { new[]{0,4,1,0}, new[]{2,4,1,2}, new[]{3,2,4,0}, new[]{1,1,2,0} },
        new[] { new[]{4,4,4,2}, new[]{0,4,1,2}, new[]{2,2,4,0}, new[]{2,1,3,0} },
        new[] { new[]{0,4,1,2}, new[]{2,4,4,3}, new[]{2,2,4,1}, new[]{0,1,2,0}, new[]{3,1,4,0} },
    };

    int levelIndex;
    Vector2Int[][] endpoints;   // өнгө бүрийн 2 төгсгөлийн цэг
    List<Vector2Int>[] paths;   // өнгө бүрийн зурсан зам (үргэлж endpoint-ээс эхэлнэ)
    int drawingColor = -1;      // одоо зурж буй өнгө (-1 = зураагүй)
    bool levelComplete;

    Camera cam;
    Transform boardRoot, pathRoot;
    Sprite squareSprite, circleSprite;
    GUIStyle titleStyle, infoStyle, msgStyle, btnStyle;

    void Awake()
    {
        Application.targetFrameRate = 60;
        SetupCamera();
        squareSprite = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
        circleSprite = MakeCircleSprite();
        boardRoot = new GameObject("Board").transform;
        pathRoot = new GameObject("Paths").transform;
        LoadLevel(0);
    }

    void SetupCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.transform.position = new Vector3(N / 2f, N / 2f, -10f);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.09f, 0.12f);
    }

    void LateUpdate()
    {
        // Цонхны хэмжээ өөрчлөгдөхөд самбар багтаж харагдана
        cam.orthographicSize = Mathf.Max(4.2f, 3.1f / cam.aspect);
    }

    Sprite MakeCircleSprite()
    {
        const int S = 64;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        float c = S / 2f - 0.5f, r = S / 2f - 1f;
        var px = new Color32[S * S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                byte a = (byte)(Mathf.Clamp01(r - d + 0.5f) * 255f);
                px[y * S + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
    }

    // ---------------- Үе ачаалах ----------------

    void LoadLevel(int index)
    {
        levelIndex = index;
        levelComplete = false;
        drawingColor = -1;

        var data = Levels[index];
        endpoints = new Vector2Int[data.Length][];
        paths = new List<Vector2Int>[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            endpoints[i] = new[]
            {
                new Vector2Int(data[i][0], data[i][1]),
                new Vector2Int(data[i][2], data[i][3]),
            };
            paths[i] = new List<Vector2Int>();
        }

        Clear(boardRoot);
        for (int x = 0; x < N; x++)
            for (int y = 0; y < N; y++)
                Spawn(boardRoot, squareSprite, Center(new Vector2Int(x, y)),
                    new Vector2(0.93f, 0.93f), new Color(0.16f, 0.17f, 0.22f), 0);
        for (int i = 0; i < endpoints.Length; i++)
            foreach (var e in endpoints[i])
                Spawn(boardRoot, circleSprite, Center(e), new Vector2(0.62f, 0.62f), Palette[i], 4);

        Redraw();
    }

    // ---------------- Оролт ----------------

    void Update()
    {
        if (levelComplete) return;

        if (Input.GetMouseButtonDown(0)) BeginDraw();
        else if (Input.GetMouseButton(0) && drawingColor >= 0) ContinueDraw();

        if (Input.GetMouseButtonUp(0))
        {
            drawingColor = -1;
            CheckWin();
        }
    }

    void BeginDraw()
    {
        if (!CellAt(Input.mousePosition, out var c)) return;

        // Endpoint дээр дарвал: тухайн өнгийн хуучин зураас арилаад шинээр эхэлнэ
        int ep = EndpointColorAt(c);
        if (ep >= 0)
        {
            paths[ep].Clear();
            paths[ep].Add(c);
            drawingColor = ep;
            Redraw();
            return;
        }

        // Байгаа зураасын дундаас дарвал: тэр цэг хүртэл тайрч, үргэлжлүүлэн зурна
        for (int i = 0; i < paths.Length; i++)
        {
            int idx = paths[i].IndexOf(c);
            if (idx >= 0)
            {
                paths[i].RemoveRange(idx + 1, paths[i].Count - idx - 1);
                drawingColor = i;
                Redraw();
                return;
            }
        }
    }

    void ContinueDraw()
    {
        if (!CellAt(Input.mousePosition, out var target)) return;

        // Хулгана хурдан хөдөлсөн үед нэг нүдээр алхуулж ойртоно
        for (int guard = 0; guard < 8 && drawingColor >= 0; guard++)
        {
            var p = paths[drawingColor];
            var last = p[p.Count - 1];
            if (last == target) break;

            var d = target - last;
            Vector2Int step = (Mathf.Abs(d.x) >= Mathf.Abs(d.y) && d.x != 0)
                ? new Vector2Int(d.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, d.y > 0 ? 1 : -1);

            if (!TryStep(last + step))
            {
                var alt = step.x != 0
                    ? new Vector2Int(0, d.y > 0 ? 1 : d.y < 0 ? -1 : 0)
                    : new Vector2Int(d.x > 0 ? 1 : d.x < 0 ? -1 : 0, 0);
                if (alt == Vector2Int.zero || !TryStep(last + alt)) break;
            }
        }
    }

    /// <summary>Зурж буй замыг нэг нүдээр сунгах/засах. Амжилттай бол true.</summary>
    bool TryStep(Vector2Int c)
    {
        if (c.x < 0 || c.x >= N || c.y < 0 || c.y >= N) return false;
        var p = paths[drawingColor];

        // Буцаж алхвал (backtrack) сүүлийн нүдийг арилгана — засварлах боломж
        if (p.Count >= 2 && c == p[p.Count - 2])
        {
            p.RemoveAt(p.Count - 1);
            Redraw();
            return true;
        }

        // Өөрийн зам дээгүүрээ давхарвал тэр цэг хүртэл тайрна
        int self = p.IndexOf(c);
        if (self >= 0)
        {
            p.RemoveRange(self + 1, p.Count - self - 1);
            Redraw();
            return true;
        }

        if (IsConnected(drawingColor)) return false; // аль хэдийн холбогдсон

        // Өөр өнгийн endpoint дээгүүр гарч болохгүй
        int ep = EndpointColorAt(c);
        if (ep >= 0 && ep != drawingColor) return false;

        // Өөр өнгийн зураастай давхцвал тэр зураасыг давхцсан цэгээс нь тайрна
        for (int i = 0; i < paths.Length; i++)
        {
            if (i == drawingColor) continue;
            int idx = paths[i].IndexOf(c);
            if (idx >= 0)
            {
                paths[i].RemoveRange(idx, paths[i].Count - idx);
                break;
            }
        }

        p.Add(c);

        // Нөгөө endpoint-доо хүрвэл энэ өнгө холбогдлоо
        if (ep == drawingColor)
        {
            drawingColor = -1;
            CheckWin();
        }
        Redraw();
        return true;
    }

    // ---------------- Дүрэм / төлөв ----------------

    int EndpointColorAt(Vector2Int c)
    {
        for (int i = 0; i < endpoints.Length; i++)
            if (endpoints[i][0] == c || endpoints[i][1] == c) return i;
        return -1;
    }

    bool IsConnected(int color)
    {
        var p = paths[color];
        if (p.Count < 2) return false;
        var a = endpoints[color][0];
        var b = endpoints[color][1];
        var first = p[0];
        var last = p[p.Count - 1];
        return (first == a && last == b) || (first == b && last == a);
    }

    int ConnectedCount()
    {
        int n = 0;
        for (int i = 0; i < paths.Length; i++)
            if (IsConnected(i)) n++;
        return n;
    }

    int FilledCells()
    {
        int n = 0;
        foreach (var p in paths) n += p.Count;
        return n;
    }

    void CheckWin()
    {
        if (ConnectedCount() == paths.Length)
        {
            levelComplete = true;
            drawingColor = -1;
        }
    }

    // ---------------- Дүрслэл ----------------

    Vector2 Center(Vector2Int c) => new Vector2(c.x + 0.5f, c.y + 0.5f);

    bool CellAt(Vector3 screenPos, out Vector2Int cell)
    {
        var w = cam.ScreenToWorldPoint(screenPos);
        cell = new Vector2Int(Mathf.FloorToInt(w.x), Mathf.FloorToInt(w.y));
        return cell.x >= 0 && cell.x < N && cell.y >= 0 && cell.y < N;
    }

    void Clear(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    void Spawn(Transform parent, Sprite sprite, Vector2 pos, Vector2 scale, Color color, int order)
    {
        var go = new GameObject("gfx");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
    }

    void Redraw()
    {
        Clear(pathRoot);
        for (int i = 0; i < paths.Length; i++)
        {
            var col = Palette[i];
            var glow = new Color(col.r, col.g, col.b, 0.22f);
            var p = paths[i];
            for (int k = 0; k < p.Count; k++)
            {
                // нүдний бүдэг дэвсгэр
                Spawn(pathRoot, squareSprite, Center(p[k]), new Vector2(0.93f, 0.93f), glow, 1);
                // үений тод дугуй
                Spawn(pathRoot, circleSprite, Center(p[k]), new Vector2(0.32f, 0.32f), col, 3);
                // өмнөх нүдтэй холбосон зурвас
                if (k > 0)
                {
                    var mid = (Center(p[k]) + Center(p[k - 1])) / 2f;
                    bool horizontal = p[k].y == p[k - 1].y;
                    var scale = horizontal ? new Vector2(1f, 0.32f) : new Vector2(0.32f, 1f);
                    Spawn(pathRoot, squareSprite, mid, scale, col, 2);
                }
            }
        }
    }

    // ---------------- UI (IMGUI) ----------------

    void OnGUI()
    {
        float s = Screen.height / 720f;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            infoStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            msgStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            btnStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
        }
        titleStyle.fontSize = Mathf.RoundToInt(30 * s);
        infoStyle.fontSize = Mathf.RoundToInt(18 * s);
        msgStyle.fontSize = Mathf.RoundToInt(34 * s);
        btnStyle.fontSize = Mathf.RoundToInt(20 * s);
        titleStyle.normal.textColor = Color.white;
        infoStyle.normal.textColor = new Color(0.75f, 0.78f, 0.85f);
        msgStyle.normal.textColor = new Color(0.55f, 0.95f, 0.55f);

        GUI.Label(new Rect(0, 8 * s, Screen.width, 40 * s),
            $"Үе {levelIndex + 1} / {Levels.Length}", titleStyle);
        GUI.Label(new Rect(0, 48 * s, Screen.width, 28 * s),
            $"Холбосон: {ConnectedCount()}/{paths.Length}    Дүүргэлт: {FilledCells() * 100 / (N * N)}%",
            infoStyle);

        // Replay/Clear — үеийг шинээр эхлүүлэх
        if (GUI.Button(new Rect(Screen.width / 2f - 210 * s, Screen.height - 78 * s, 200 * s, 58 * s),
                "Цэвэрлэх (Clear)", btnStyle))
            LoadLevel(levelIndex);

        if (levelComplete)
        {
            bool last = levelIndex == Levels.Length - 1;
            GUI.Label(new Rect(0, 84 * s, Screen.width, 44 * s),
                last ? "Бүх үе дууслаа!" : "Үе дууслаа!", msgStyle);
            if (GUI.Button(new Rect(Screen.width / 2f + 10 * s, Screen.height - 78 * s, 200 * s, 58 * s),
                    last ? "Эхнээс (Replay)" : "Дараах (Next)", btnStyle))
                LoadLevel(last ? 0 : levelIndex + 1);
        }
    }
}
