using BottleShaders.Domain.Models;

namespace BottleShaders.Domain.Interfaces
{
    public interface IBottleValidator
    {
        bool CanPour(BottleState source, BottleState target);
        bool IsComplete(BottleState bottle);
        bool ColorsMatch(DomainColor a, DomainColor b);
        bool UnityColorsMatch(UnityEngine.Color a, UnityEngine.Color b);
    }
}