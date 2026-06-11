using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame.Application.Configuration
{
    /// <summary>
    /// Serializable data for a custom/editor-created level.
    /// Saved as JSON to persistentDataPath.
    /// </summary>
    [Serializable]
    public class EditorLevelData
    {
        public string levelName = "Custom Level";
        public int moldCount = 5;
        public int emptyMoldCount = 2;
        public int colorCount = 4;
        public int maxLayersPerMold = 8;
        public int parMoves = 10;
        public int goodMoves = 15;
        public List<EditorMoldData> molds = new List<EditorMoldData>();
        public string createdAt;

        /// <summary>Convert to runtime LevelData fields for playtesting.</summary>
        public void ApplyTo(LevelData target)
        {
            if (target == null) return;
            target.MoldCount = moldCount;
            target.emptyMoldCount = emptyMoldCount;
            target.colorCount = colorCount;
            target.maxLayersPerMold = maxLayersPerMold;
            target.parMoves = parMoves;
            target.goodMoves = goodMoves;
            target.autoGenerate = molds.Count == 0;

            target.Molds.Clear();
            foreach (var em in molds)
            {
                var mold = new LevelMoldData { isEmpty = em.isEmpty };
                foreach (var layerColor in em.layerColors)
                {
                    mold.layers.Add(new LevelLayerData { color = layerColor, amount = 0.25f });
                }
                target.Molds.Add(mold);
            }
        }
    }

    [Serializable]
    public class EditorMoldData
    {
        public bool isEmpty;
        public List<Color> layerColors = new List<Color>();
    }
}
