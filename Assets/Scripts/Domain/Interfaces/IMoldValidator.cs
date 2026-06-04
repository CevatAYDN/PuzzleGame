using PuzzleGame.Domain.Models;

namespace PuzzleGame.Domain.Interfaces
{
    public interface IMoldValidator
    {
        bool CanCast(MoldState source, MoldState target);
        bool IsComplete(MoldState Mold);
        bool ColorsMatch(DomainColor a, DomainColor b);
    }
}
