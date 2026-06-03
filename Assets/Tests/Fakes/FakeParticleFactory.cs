using UnityEngine;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeParticleFactory : IParticleFactory
    {
        public ParticleSystem CreateSplash()
        {
            var go = new GameObject("FakeSplashParticle");
            var ps = go.AddComponent<ParticleSystem>();
            return ps;
        }

        public ParticleSystem CreateBubble()
        {
            var go = new GameObject("FakeBubbleParticle");
            var ps = go.AddComponent<ParticleSystem>();
            return ps;
        }
    }
}
