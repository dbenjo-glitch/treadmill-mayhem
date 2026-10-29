using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TreadmillMayhem
{
    /// <summary>Fixed-column local top-10 table, used on the leaderboard screen and the results screen.</summary>
    public class LeaderboardTable
    {
        static readonly string[] Headers = { "#", "NAME", "SCORE", "TIME", "AI", "DATE" };
        static readonly float[] X = { -380f, -340f, 40f, 160f, 182f, 500f };
        static readonly float[] W = { 60f, 300f, 160f, 110f, 140f, 170f };
        static readonly TextAnchor[] Align = { TextAnchor.MiddleRight, TextAnchor.MiddleLeft, TextAnchor.MiddleRight, TextAnchor.MiddleRight, TextAnchor.MiddleLeft, TextAnchor.MiddleRight };

        readonly List<Text[]> rows = new List<Text[]>();
        readonly int cols;

        public LeaderboardTable(Transform parent, float top, bool showDate, float rowHeight = 60f)
        {
            cols = showDate ? 6 : 5;
            var header = Row(parent, top, 24);
            for (int c = 0; c < cols; c++) { header[c].text = Headers[c]; header[c].color = UIKit.Muted; }
            for (int i = 0; i < SaveSystem.MaxRuns; i++) rows.Add(Row(parent, top - 50f - i * rowHeight, 32));
        }

        Text[] Row(Transform parent, float y, int size)
        {
            var row = new Text[cols];
            for (int c = 0; c < cols; c++)
            {
                var t = UIKit.Text(parent, "", size, new Vector2(0.5f, 1f), new Vector2(X[c], y), Align[c], false, W[c]);
                var rt = (RectTransform)t.transform;
                rt.sizeDelta = new Vector2(W[c], size * 1.6f);
                rt.pivot = new Vector2(Align[c] == TextAnchor.MiddleLeft ? 0f : 1f, 0.5f);
                row[c] = t;
            }
            return row;
        }

        public void Fill(string highlightId)
        {
            var runs = SaveSystem.Data.runs;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (i >= runs.Count)
                {
                    foreach (var t in row) t.text = "";
                    if (i == 0 && runs.Count == 0) { row[1].text = "No runs yet - go play!"; row[1].color = UIKit.Muted; }
                    continue;
                }
                var r = runs[i];
                int secs = Mathf.FloorToInt(r.time);
                string mode = r.mode == "OBSTACLE" ? "OBST" : Trim(r.difficulty, 6);
                var vals = new[] { (i + 1) + ".", Trim(r.name, 12), r.score.ToString("N0"), (secs / 60) + ":" + (secs % 60).ToString("00"), mode, r.date };
                var color = r.id == highlightId ? UIKit.Gold : Color.white;
                for (int c = 0; c < row.Length; c++) { row[c].text = vals[c]; row[c].color = color; }
            }
        }

        static string Trim(string s, int n) => string.IsNullOrEmpty(s) ? "-" : s.Length <= n ? s : s.Substring(0, n);
    }
}
