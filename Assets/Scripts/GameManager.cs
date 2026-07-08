using System.Collections.Generic;
using UnityEngine;

// scene-д гараар юм тавиагүй, Play дарахад эндээс бүгд үүснэ
public static class Starter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Go()
    {
        if (Object.FindObjectOfType<GameManager>() == null)
            new GameObject("gm").AddComponent<GameManager>();
    }
}

public class GameManager : MonoBehaviour
{
    int lvl;
    List<Vector2Int>[] lines;   // зурсан зурааснууд
    Vector2Int[][] dots;        // өнгө болгоны 2 цэг
    int drawing = -1;           // одоо аль өнгийг зурж байгаа
    bool won;

    Camera cam;
    Transform board, lineRoot;
    Sprite sq, circ;

    Color[] colors = {
        new Color(0.92f, 0.26f, 0.21f),
        new Color(0.3f, 0.69f, 0.31f),
        new Color(0.26f, 0.52f, 0.96f),
        new Color(1f, 0.84f, 0f),
        new Color(1f, 0.55f, 0f),
    };

    // үе болгонд өнгө тус бүрийн 2 цэг: x1,y1,x2,y2 (y=0 нь доод мөр)
    static int[][][] levels = {
        new int[][]{ new int[]{0,4,1,0}, new int[]{1,4,4,0}, new int[]{1,3,2,0} },
        new int[][]{ new int[]{0,4,1,2}, new int[]{3,4,2,2}, new int[]{4,2,2,0} },
        new int[][]{ new int[]{0,4,1,0}, new int[]{2,4,1,2}, new int[]{3,2,4,0}, new int[]{1,1,2,0} },
        new int[][]{ new int[]{4,4,4,2}, new int[]{0,4,1,2}, new int[]{2,2,4,0}, new int[]{2,1,3,0} },
        new int[][]{ new int[]{0,4,1,2}, new int[]{2,4,4,3}, new int[]{2,2,4,1}, new int[]{0,1,2,0}, new int[]{3,1,4,0} },
    };

    void Awake()
    {
        Application.targetFrameRate = 60;

        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.transform.position = new Vector3(2.5f, 2.5f, -10);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.09f, 0.12f);

        sq = Sprite.Create(Texture2D.whiteTexture,
            new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);
        circ = MakeCircle();

        board = new GameObject("board").transform;
        lineRoot = new GameObject("lines").transform;

        SetLevel(0);
    }

    void LateUpdate()
    {
        // цонхны хэмжээ өөрчлөгдөхөд самбар багтаж харагдана
        cam.orthographicSize = Mathf.Max(4.2f, 3.1f / cam.aspect);
    }

    Sprite MakeCircle()
    {
        int s = 64;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var px = new Color32[s * s];
        float c = s / 2f - 0.5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(s / 2f - 1 - d + 0.5f);
                px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
    }

    void SetLevel(int n)
    {
        lvl = n;
        won = false;
        drawing = -1;

        var data = levels[n];
        dots = new Vector2Int[data.Length][];
        lines = new List<Vector2Int>[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            dots[i] = new Vector2Int[] {
                new Vector2Int(data[i][0], data[i][1]),
                new Vector2Int(data[i][2], data[i][3])
            };
            lines[i] = new List<Vector2Int>();
        }

        Wipe(board);
        for (int x = 0; x < 5; x++)
            for (int y = 0; y < 5; y++)
                Put(board, sq, Pos(new Vector2Int(x, y)), new Vector2(0.93f, 0.93f), new Color(0.16f, 0.17f, 0.22f), 0);
        for (int i = 0; i < dots.Length; i++)
        {
            Put(board, circ, Pos(dots[i][0]), new Vector2(0.62f, 0.62f), colors[i], 4);
            Put(board, circ, Pos(dots[i][1]), new Vector2(0.62f, 0.62f), colors[i], 4);
        }

        DrawLines();
    }

    void Update()
    {
        if (won) return;

        if (Input.GetMouseButtonDown(0)) PressDown();
        else if (Input.GetMouseButton(0) && drawing >= 0) Drag();

        if (Input.GetMouseButtonUp(0))
        {
            drawing = -1;
            CheckWin();
        }
    }

    void PressDown()
    {
        Vector2Int c;
        if (!GetCell(out c)) return;
        //Debug.Log(c);

        // цэг дээр нь дарвал хуучин зураасыг нь арилгаад шинээр эхэлнэ
        int d = DotAt(c);
        if (d >= 0)
        {
            lines[d].Clear();
            lines[d].Add(c);
            drawing = d;
            DrawLines();
            return;
        }

        // зураасны дундаас дарвал тэр хүртэл нь тайраад цааш нь зурна
        for (int i = 0; i < lines.Length; i++)
        {
            int k = lines[i].IndexOf(c);
            if (k >= 0)
            {
                lines[i].RemoveRange(k + 1, lines[i].Count - k - 1);
                drawing = i;
                DrawLines();
                return;
            }
        }
    }

    void Drag()
    {
        Vector2Int target;
        if (!GetCell(out target)) return;

        // хурдан чирэхэд нүд алгасчихдаг байсан тул нэг нэгээр нь ойртуулна
        for (int guard = 0; guard < 8 && drawing >= 0; guard++)
        {
            var line = lines[drawing];
            var last = line[line.Count - 1];
            if (last == target) break;

            int dx = target.x - last.x, dy = target.y - last.y;
            Vector2Int step;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0) step = new Vector2Int(dx > 0 ? 1 : -1, 0);
            else step = new Vector2Int(0, dy > 0 ? 1 : -1);

            if (!Step(last + step))
            {
                // болохгүй бол нөгөө чиглэлээр нь оролдоно
                Vector2Int alt;
                if (step.x != 0) alt = new Vector2Int(0, dy > 0 ? 1 : dy < 0 ? -1 : 0);
                else alt = new Vector2Int(dx > 0 ? 1 : dx < 0 ? -1 : 0, 0);
                if (alt == Vector2Int.zero || !Step(last + alt)) break;
            }
        }
    }

    // зурж байгаа зураасыг нэг нүдээр сунгах гэж оролдоно
    bool Step(Vector2Int c)
    {
        if (c.x < 0 || c.x > 4 || c.y < 0 || c.y > 4) return false;
        var line = lines[drawing];

        // ухарвал сүүлийн нүдийг арилгана
        if (line.Count >= 2 && c == line[line.Count - 2])
        {
            line.RemoveAt(line.Count - 1);
            DrawLines();
            return true;
        }

        // өөрийнхөө зураасан дээр буцаж ирвэл тэр хүртэл нь тайрна
        int self = line.IndexOf(c);
        if (self >= 0)
        {
            line.RemoveRange(self + 1, line.Count - self - 1);
            DrawLines();
            return true;
        }

        if (Linked(drawing)) return false; // холбочихсон бол цааш сунахгүй

        int d = DotAt(c);
        if (d >= 0 && d != drawing) return false; // өөр өнгийн цэг дээгүүр гарахгүй

        // өөр зураастай давхцвал тэрийг нь давхцсан газраас нь тайрна
        for (int i = 0; i < lines.Length; i++)
        {
            if (i == drawing) continue;
            int k = lines[i].IndexOf(c);
            if (k >= 0)
            {
                lines[i].RemoveRange(k, lines[i].Count - k);
                break;
            }
        }

        line.Add(c);

        if (d == drawing) // нөгөө цэгтээ хүрлээ
        {
            drawing = -1;
            CheckWin();
        }
        DrawLines();
        return true;
    }

    int DotAt(Vector2Int c)
    {
        for (int i = 0; i < dots.Length; i++)
            if (dots[i][0] == c || dots[i][1] == c) return i;
        return -1;
    }

    bool Linked(int i)
    {
        var l = lines[i];
        if (l.Count < 2) return false;
        var a = l[0];
        var b = l[l.Count - 1];
        return (a == dots[i][0] && b == dots[i][1]) || (a == dots[i][1] && b == dots[i][0]);
    }

    int LinkedCount()
    {
        int n = 0;
        for (int i = 0; i < lines.Length; i++) if (Linked(i)) n++;
        return n;
    }

    void CheckWin()
    {
        if (LinkedCount() == lines.Length)
        {
            won = true;
            drawing = -1;
        }
    }

    bool GetCell(out Vector2Int cell)
    {
        var w = cam.ScreenToWorldPoint(Input.mousePosition);
        cell = new Vector2Int(Mathf.FloorToInt(w.x), Mathf.FloorToInt(w.y));
        return cell.x >= 0 && cell.x < 5 && cell.y >= 0 && cell.y < 5;
    }

    Vector2 Pos(Vector2Int c) { return new Vector2(c.x + 0.5f, c.y + 0.5f); }

    void Wipe(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }

    void Put(Transform parent, Sprite sp, Vector2 pos, Vector2 size, Color col, int order)
    {
        var go = new GameObject("x");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
        go.transform.localScale = new Vector3(size.x, size.y, 1);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sp;
        r.color = col;
        r.sortingOrder = order;
    }

    void DrawLines()
    {
        Wipe(lineRoot);
        for (int i = 0; i < lines.Length; i++)
        {
            var col = colors[i];
            var l = lines[i];
            for (int k = 0; k < l.Count; k++)
            {
                // нүдний бүдэг дэвсгэр + үений дугуй
                Put(lineRoot, sq, Pos(l[k]), new Vector2(0.93f, 0.93f), new Color(col.r, col.g, col.b, 0.22f), 1);
                Put(lineRoot, circ, Pos(l[k]), new Vector2(0.32f, 0.32f), col, 3);
                if (k > 0)
                {
                    var mid = (Pos(l[k]) + Pos(l[k - 1])) / 2f;
                    if (l[k].y == l[k - 1].y)
                        Put(lineRoot, sq, mid, new Vector2(1f, 0.32f), col, 2);
                    else
                        Put(lineRoot, sq, mid, new Vector2(0.32f, 1f), col, 2);
                }
            }
        }
    }

    void OnGUI()
    {
        float s = Screen.height / 720f;

        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.skin.label.fontSize = (int)(30 * s);
        GUI.skin.label.normal.textColor = Color.white;
        GUI.Label(new Rect(0, 8 * s, Screen.width, 40 * s), "Үе " + (lvl + 1) + " / " + levels.Length);

        GUI.skin.label.fontSize = (int)(18 * s);
        GUI.skin.label.fontStyle = FontStyle.Normal;
        GUI.skin.label.normal.textColor = new Color(0.75f, 0.78f, 0.85f);
        int used = 0;
        for (int i = 0; i < lines.Length; i++) used += lines[i].Count;
        GUI.Label(new Rect(0, 48 * s, Screen.width, 28 * s),
            "Холбосон: " + LinkedCount() + "/" + lines.Length + "   Дүүргэлт: " + (used * 100 / 25) + "%");

        GUI.skin.button.fontSize = (int)(20 * s);
        if (GUI.Button(new Rect(Screen.width / 2f - 210 * s, Screen.height - 78 * s, 200 * s, 58 * s), "Цэвэрлэх (Clear)"))
            SetLevel(lvl); // restart

        if (won)
        {
            GUI.skin.label.fontSize = (int)(34 * s);
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.normal.textColor = new Color(0.55f, 0.95f, 0.55f);
            bool last = lvl == levels.Length - 1;
            GUI.Label(new Rect(0, 84 * s, Screen.width, 44 * s), last ? "Бүх үе дууслаа!" : "Үе дууслаа!");
            if (GUI.Button(new Rect(Screen.width / 2f + 10 * s, Screen.height - 78 * s, 200 * s, 58 * s),
                    last ? "Эхнээс (Replay)" : "Дараах (Next)"))
                SetLevel(last ? 0 : lvl + 1);
        }
    }
}
