using System;

namespace PuzzleGame.Application.Interfaces
{
    public interface ICoinWallet
    {
        int Balance { get; }
        event Action<int> OnBalanceChanged;
        bool CanAfford(int amount);
        void Add(int amount, string reason);
        bool TrySpend(int amount, string reason);
    }
}
