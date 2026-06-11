using System.Collections.Generic;
using PuzzleGame.Application.Configuration;
using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Runtime level editor service for creating, editing, saving, and playtesting
    /// custom levels within the game (not Unity Editor).
    /// </summary>
    public interface ILevelEditorService
    {
        /// <summary>List of saved custom level names.</summary>
        IReadOnlyList<string> SavedLevelNames { get; }

        /// <summary>Currently loaded custom level data being edited.</summary>
        EditorLevelData CurrentEdit { get; }

        /// <summary>Whether a level is currently loaded for editing.</summary>
        bool HasActiveEdit { get; }

        /// <summary>Create a new blank level with default parameters.</summary>
        void CreateNewLevel(string name, int moldCount, int colorCount, int emptyMolds);

        /// <summary>Load a saved custom level for editing.</summary>
        bool LoadLevel(string levelName);

        /// <summary>Save the currently edited level.</summary>
        void SaveCurrentLevel();

        /// <summary>Delete a saved custom level.</summary>
        bool DeleteLevel(string levelName);

        /// <summary>Add a layer of a color to a specific mold in the current edit.</summary>
        void AddLayerToMold(int moldIndex, Color color);

        /// <summary>Remove the top layer from a mold in the current edit.</summary>
        void RemoveLayerFromMold(int moldIndex);

        /// <summary>Clear all layers from a mold.</summary>
        void ClearMold(int moldIndex);

        /// <summary>Apply the current edit to a LevelData for playtesting.</summary>
        void ApplyToLevelData(LevelData target);

        /// <summary>List all saved levels with metadata.</summary>
        List<EditorLevelData> ListSavedLevels();

        /// <summary>Refresh the saved levels list from disk.</summary>
        void RefreshSavedLevels();
    }
}
