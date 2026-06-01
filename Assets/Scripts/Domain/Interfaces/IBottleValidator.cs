using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Interfaces
{
    public interface IBottleValidator
    {
        bool CanPour(BottleState source, BottleState target);
        bool IsComplete(BottleState bottle);
        bool ColorsMatch(DomainColor a, DomainColor b);
    }
}
