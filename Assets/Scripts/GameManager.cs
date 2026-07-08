using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int level = 0;
    public float speed = 1f;

    List<Vector2Int>[] zuraas;  // zursan zuraasnuud
    Vector2Int[][] tseg;        // ungu bolgonii 2 tseg
    int zurjBgaa = -1;
    bool hojson = false;

    Camera cam;
    GameObject sambarObj;
    GameObject zuraasRoot;
    Sprite dorvoljin;
    Sprite dugui;

    Color[] ungunuud;

    // level bolgond ungu tus buriin 2 tseg. x1,y1,x2,y2 gesen daraalaltai
    // y=0 ni dood mur
    static int[][][] levels = new int[][][] {
        new int[][]{ new int[]{0,4,1,0}, new int[]{1,4,4,0}, new int[]{1,3,2,0} },
        new int[][]{ new int[]{0,4,1,2}, new int[]{3,4,2,2}, new int[]{4,2,2,0} },
        new int[][]{ new int[]{0,4,1,0}, new int[]{2,4,1,2}, new int[]{3,2,4,0}, new int[]{1,1,2,0} },
        new int[][]{ new int[]{4,4,4,2}, new int[]{0,4,1,2}, new int[]{2,2,4,0}, new int[]{2,1,3,0} },
        new int[][]{ new int[]{0,4,1,2}, new int[]{2,4,4,3}, new int[]{2,2,4,1}, new int[]{0,1,2,0}, new int[]{3,1,4,0} }
    };

    void Start()
    {
        cam = Camera.main;
        cam.orthographic = true;
        cam.transform.position = new Vector3(2.5f, 2.5f, -10);
        cam.backgroundColor = new Color(0.09f, 0.09f, 0.12f);

        ungunuud = new Color[5];
        ungunuud[0] = new Color(0.92f, 0.26f, 0.21f); // ulaan
        ungunuud[1] = new Color(0.3f, 0.69f, 0.31f);  // nogoon
        ungunuud[2] = new Color(0.26f, 0.52f, 0.96f); // tsenher
        ungunuud[3] = new Color(1f, 0.84f, 0f);       // shar
        ungunuud[4] = new Color(1f, 0.55f, 0f);       // ulbar shar

        dorvoljin = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f), Texture2D.whiteTexture.width);

        // dugui hiideg texture kod, internetees haij olson
        int s = 64;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        Color32[] px = new Color32[s * s];
        float c = s / 2f - 0.5f;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(s / 2f - 1 - d + 0.5f);
                px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        dugui = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);

        sambarObj = new GameObject("sambar");
        zuraasRoot = new GameObject("zuraasnuud");

        LevelEhluuleh(0);
    }

    void Update()
    {
        // delgetsnii hemjee uurchlugduhud sambar bagtaj haragdana
        float k = 3.1f / cam.aspect;
        if (k < 4.2f) k = 4.2f;
        cam.orthographicSize = k;

        if (hojson == true) return;

        if (Input.GetMouseButtonDown(0))
        {
            DaraltEhlel();
        }
        else if (Input.GetMouseButton(0))
        {
            if (zurjBgaa >= 0) Chirelt();
        }

        if (Input.GetMouseButtonUp(0))
        {
            zurjBgaa = -1;
            HojsonEsehShalgah();
        }
    }

    void DaraltEhlel()
    {
        Vector2Int nud;
        if (NudOloh(out nud) == false) return;
        //Debug.Log(nud);

        // tseg deer ni darval huuchin zuraasiig ni ustgaad shineer ehelne
        int t = TsegShalgah(nud);
        if (t >= 0)
        {
            zuraas[t].Clear();
            zuraas[t].Add(nud);
            zurjBgaa = t;
            ZuraasZurah();
            return;
        }

        // zuraasnii dundaas darval ter hurtel ni tairaad tsaash ni zurna
        for (int i = 0; i < zuraas.Length; i++)
        {
            int k = zuraas[i].IndexOf(nud);
            if (k >= 0)
            {
                zuraas[i].RemoveRange(k + 1, zuraas[i].Count - k - 1);
                zurjBgaa = i;
                ZuraasZurah();
                return;
            }
        }
    }

    void Chirelt()
    {
        Vector2Int zorilt;
        if (NudOloh(out zorilt) == false) return;

        // hurdan chirehed nud algasaad baisan bolohoor neg negeer ni oiruulj bgaa
        int hamgaalalt = 0;
        while (hamgaalalt < 8)
        {
            hamgaalalt++;
            if (zurjBgaa < 0) break;
            List<Vector2Int> l = zuraas[zurjBgaa];
            Vector2Int suuliin = l[l.Count - 1];
            if (suuliin == zorilt) break;

            int dx = zorilt.x - suuliin.x;
            int dy = zorilt.y - suuliin.y;
            Vector2Int alham = new Vector2Int(0, 0);
            if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0)
            {
                if (dx > 0) alham = new Vector2Int(1, 0);
                else alham = new Vector2Int(-1, 0);
            }
            else
            {
                if (dy > 0) alham = new Vector2Int(0, 1);
                else alham = new Vector2Int(0, -1);
            }

            if (Alham(suuliin + alham) == false)
            {
                // bolohgui bol nuguu chigleleer ni orolduno
                Vector2Int alham2 = new Vector2Int(0, 0);
                if (alham.x != 0)
                {
                    if (dy > 0) alham2 = new Vector2Int(0, 1);
                    if (dy < 0) alham2 = new Vector2Int(0, -1);
                }
                else
                {
                    if (dx > 0) alham2 = new Vector2Int(1, 0);
                    if (dx < 0) alham2 = new Vector2Int(-1, 0);
                }
                if (alham2 == new Vector2Int(0, 0)) break;
                if (Alham(suuliin + alham2) == false) break;
            }
        }
    }

    // zurj bgaa zuraasiig neg nudeer sungah gej oroldono
    bool Alham(Vector2Int nud)
    {
        if (nud.x < 0) return false;
        if (nud.x > 4) return false;
        if (nud.y < 0) return false;
        if (nud.y > 4) return false;

        List<Vector2Int> l = zuraas[zurjBgaa];

        // uhraad butsval suuliin nudiig ustgana
        if (l.Count >= 2)
        {
            if (nud == l[l.Count - 2])
            {
                l.RemoveAt(l.Count - 1);
                ZuraasZurah();
                return true;
            }
        }

        // uuriin zuraasan deeree davhtsval ter hurtel ni tairna
        int self = l.IndexOf(nud);
        if (self >= 0)
        {
            l.RemoveRange(self + 1, l.Count - self - 1);
            ZuraasZurah();
            return true;
        }

        if (Holbogdson(zurjBgaa) == true) return false; // holbochihson bol tsaash sunahgui

        int t = TsegShalgah(nud);
        if (t >= 0 && t != zurjBgaa) return false; // uur ungiin tseg deeguur garch bolohgui

        // uur zuraastai davhtsval teriig ni davhtssan gazraas ni tairna
        for (int i = 0; i < zuraas.Length; i++)
        {
            if (i == zurjBgaa) continue;
            int k = zuraas[i].IndexOf(nud);
            if (k >= 0)
            {
                zuraas[i].RemoveRange(k, zuraas[i].Count - k);
                break;
            }
        }

        l.Add(nud);

        if (t == zurjBgaa)
        {
            // nuguu tsegtee hurlee
            zurjBgaa = -1;
            HojsonEsehShalgah();
        }
        ZuraasZurah();
        return true;
    }

    int TsegShalgah(Vector2Int nud)
    {
        for (int i = 0; i < tseg.Length; i++)
        {
            if (tseg[i][0] == nud) return i;
            if (tseg[i][1] == nud) return i;
        }
        return -1;
    }

    bool Holbogdson(int i)
    {
        List<Vector2Int> l = zuraas[i];
        if (l.Count < 2) return false;
        Vector2Int ehnii = l[0];
        Vector2Int suuliin = l[l.Count - 1];
        if (ehnii == tseg[i][0] && suuliin == tseg[i][1]) return true;
        if (ehnii == tseg[i][1] && suuliin == tseg[i][0]) return true;
        return false;
    }

    int HolbogdsonToo()
    {
        int too = 0;
        for (int i = 0; i < zuraas.Length; i++)
        {
            if (Holbogdson(i) == true) too = too + 1;
        }
        return too;
    }

    void HojsonEsehShalgah()
    {
        if (HolbogdsonToo() == zuraas.Length)
        {
            hojson = true;
            zurjBgaa = -1;
            //Debug.Log("hojloo!!");
        }
    }

    bool NudOloh(out Vector2Int nud)
    {
        Vector3 pos = cam.ScreenToWorldPoint(Input.mousePosition);
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        nud = new Vector2Int(x, y);
        if (x < 0) return false;
        if (x > 4) return false;
        if (y < 0) return false;
        if (y > 4) return false;
        return true;
    }

    void LevelEhluuleh(int n)
    {
        level = n;
        hojson = false;
        zurjBgaa = -1;

        int[][] data = levels[n];
        tseg = new Vector2Int[data.Length][];
        zuraas = new List<Vector2Int>[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            tseg[i] = new Vector2Int[2];
            tseg[i][0] = new Vector2Int(data[i][0], data[i][1]);
            tseg[i][1] = new Vector2Int(data[i][2], data[i][3]);
            zuraas[i] = new List<Vector2Int>();
        }

        // sambariig dahin zurna
        for (int i = sambarObj.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(sambarObj.transform.GetChild(i).gameObject);
        }
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                Zurah(dorvoljin, x + 0.5f, y + 0.5f, 0.93f, 0.93f, new Color(0.16f, 0.17f, 0.22f), 0, sambarObj);
            }
        }
        for (int i = 0; i < tseg.Length; i++)
        {
            Zurah(dugui, tseg[i][0].x + 0.5f, tseg[i][0].y + 0.5f, 0.62f, 0.62f, ungunuud[i], 4, sambarObj);
            Zurah(dugui, tseg[i][1].x + 0.5f, tseg[i][1].y + 0.5f, 0.62f, 0.62f, ungunuud[i], 4, sambarObj);
        }

        ZuraasZurah();
    }

    void ZuraasZurah()
    {
        // huuchniig bugdiig ni ustgaad dahij zurna
        for (int i = zuraasRoot.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(zuraasRoot.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < zuraas.Length; i++)
        {
            Color ung = ungunuud[i];
            Color burheg = new Color(ung.r, ung.g, ung.b, 0.22f);
            for (int k = 0; k < zuraas[i].Count; k++)
            {
                Vector2Int nud = zuraas[i][k];
                Zurah(dorvoljin, nud.x + 0.5f, nud.y + 0.5f, 0.93f, 0.93f, burheg, 1, zuraasRoot);
                Zurah(dugui, nud.x + 0.5f, nud.y + 0.5f, 0.32f, 0.32f, ung, 3, zuraasRoot);
                if (k > 0)
                {
                    Vector2Int umnuh = zuraas[i][k - 1];
                    float mx = (nud.x + umnuh.x) / 2f + 0.5f;
                    float my = (nud.y + umnuh.y) / 2f + 0.5f;
                    if (nud.y == umnuh.y)
                    {
                        Zurah(dorvoljin, mx, my, 1f, 0.32f, ung, 2, zuraasRoot);
                    }
                    else
                    {
                        Zurah(dorvoljin, mx, my, 0.32f, 1f, ung, 2, zuraasRoot);
                    }
                }
            }
        }
    }

    void Zurah(Sprite sp, float x, float y, float w, float h, Color ung, int order, GameObject parent)
    {
        GameObject obj = new GameObject("obj");
        obj.transform.parent = parent.transform;
        obj.transform.position = new Vector3(x, y, 0);
        obj.transform.localScale = new Vector3(w, h, 1);
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.color = ung;
        sr.sortingOrder = order;
    }

    void OnGUI()
    {
        if (zuraas == null) return;

        float s = Screen.height / 720f;

        GUI.skin.label.alignment = TextAnchor.MiddleCenter;
        GUI.skin.label.fontStyle = FontStyle.Bold;
        GUI.skin.label.fontSize = (int)(30 * s);
        GUI.skin.label.normal.textColor = Color.white;
        GUI.Label(new Rect(0, 8 * s, Screen.width, 40 * s), "Үе " + (level + 1) + " / " + levels.Length);

        GUI.skin.label.fontSize = (int)(18 * s);
        GUI.skin.label.fontStyle = FontStyle.Normal;
        GUI.skin.label.normal.textColor = new Color(0.75f, 0.78f, 0.85f);
        int too = 0;
        for (int i = 0; i < zuraas.Length; i++) too = too + zuraas[i].Count;
        GUI.Label(new Rect(0, 48 * s, Screen.width, 28 * s), "Холбосон: " + HolbogdsonToo() + "/" + zuraas.Length + "   Дүүргэлт: " + (too * 100 / 25) + "%");

        GUI.skin.button.fontSize = (int)(20 * s);
        if (GUI.Button(new Rect(Screen.width / 2f - 210 * s, Screen.height - 78 * s, 200 * s, 58 * s), "Цэвэрлэх (Clear)"))
        {
            LevelEhluuleh(level);
        }

        if (hojson == true)
        {
            GUI.skin.label.fontSize = (int)(34 * s);
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.skin.label.normal.textColor = new Color(0.55f, 0.95f, 0.55f);
            if (level == levels.Length - 1)
            {
                GUI.Label(new Rect(0, 84 * s, Screen.width, 44 * s), "Бүх үе дууслаа!");
                if (GUI.Button(new Rect(Screen.width / 2f + 10 * s, Screen.height - 78 * s, 200 * s, 58 * s), "Эхнээс (Replay)"))
                {
                    LevelEhluuleh(0);
                }
            }
            else
            {
                GUI.Label(new Rect(0, 84 * s, Screen.width, 44 * s), "Үе дууслаа!");
                if (GUI.Button(new Rect(Screen.width / 2f + 10 * s, Screen.height - 78 * s, 200 * s, 58 * s), "Дараах (Next)"))
                {
                    LevelEhluuleh(level + 1);
                }
            }
        }
    }
}
