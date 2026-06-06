using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    public class FakeActiveMoldsProvider : IActiveMoldsProvider
    {
        public IMoldView[] Molds { get; set; } = System.Array.Empty<IMoldView>();
    }
}
