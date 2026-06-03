using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// Abstraction for runtime particle system prefab creation.
    /// Implementation belongs in Infrastructure, keeping Application independent of concrete object creation.
    /// </summary>
    public interface IParticleFactory
    {
        ParticleSystem CreateSplash();
        ParticleSystem CreateBubble();
    }
}
