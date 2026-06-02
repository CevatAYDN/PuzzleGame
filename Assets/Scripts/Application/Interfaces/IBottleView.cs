using PuzzleGame.Domain.Models;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// BottleController (MonoBehaviour) için Application-katmanı soyutlaması.
    /// Domain katmanının Unity'ye bağımlı olmamasını sağlar.
    /// Unit testlerde mock'lanabilir.
    /// </summary>
    public interface IBottleView
    {
        BottleState State { get; }
        bool IsEmpty { get; }
        bool IsCapped { get; }
        bool TryPourTo(IBottleView target);
        void SetSelectionHighlight(bool active);
        void AnimateCompletion();
        void UpdateVisualsFromState();
    }
}
