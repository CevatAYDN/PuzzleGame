using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Visual trail/ghost effect that follows the pour stream.
    /// Renders a fading line from source mouth position, creating
    /// a "memory" of recent stream positions.
    /// </summary>
    public interface IStreamTrailController
    {
        /// <summary>
        /// Call each frame during flow phase to update trail position.
        /// </summary>
        /// <param name="worldPosition">Current stream source mouth world position.</param>
        void UpdateTrail(Vector3 worldPosition);

        /// <summary>
        /// Begins the trail effect. Called when the flow phase starts.
        /// </summary>
        /// <param name="startPosition">Initial stream position.</param>
        /// <param name="color">Stream color for trail tint.</param>
        void BeginTrail(Vector3 startPosition, Color color);

        /// <summary>
        /// Ends the trail effect with a fade-out. Called when flow phase completes.
        /// </summary>
        void EndTrail();

        /// <summary>
        /// Destroys owned resources (LineRenderer material). Called when the
        /// owning cast animation state is released to prevent material leaks.
        /// </summary>
        void Cleanup();
    }
}
