using System;
using PuzzleGame.Application.Interfaces;

namespace PuzzleGame.Tests.Fakes
{
    /// <summary>
    /// Controllable fake for <see cref="ICoinWallet"/>.
    /// Lets tests preset balance, override CanAfford / TrySpend returns, and observe calls.
    /// </summary>
    public class FakeCoinWallet : ICoinWallet
    {
        public int Balance { get; private set; }

        /// <summary>
        /// If set to a non-negative value, <see cref="CanAfford"/> returns this &gt;= amount
        /// regardless of <see cref="Balance"/>. Useful for "insufficient coins" tests.
        /// </summary>
        public int CanAffordOverride { get; set; } = -1;

        /// <summary>
        /// If false, <see cref="TrySpend"/> rejects even when the wallet has enough balance.
        /// </summary>
        public bool TrySpendReturn { get; set; } = true;

        public int AddCallCount { get; private set; }
        public int TrySpendCallCount { get; private set; }
        public int LastAddAmount { get; private set; }
        public int LastSpendAmount { get; private set; }
        public string LastAddReason { get; private set; }
        public string LastSpendReason { get; private set; }

        public event Action<int> OnBalanceChanged;

        public FakeCoinWallet(int initialBalance = 100)
        {
            Balance = initialBalance;
        }

        public bool CanAfford(int amount)
        {
            if (CanAffordOverride >= 0) return CanAffordOverride >= amount;
            return Balance >= amount;
        }

        public void Add(int amount, string reason)
        {
            if (amount <= 0) return;
            AddCallCount++;
            LastAddAmount = amount;
            LastAddReason = reason;
            Balance += amount;
            OnBalanceChanged?.Invoke(Balance);
        }

        public bool TrySpend(int amount, string reason)
        {
            TrySpendCallCount++;
            LastSpendAmount = amount;
            LastSpendReason = reason;
            if (amount <= 0) return false;
            if (!TrySpendReturn) return false;
            if (!CanAfford(amount)) return false;
            Balance -= amount;
            OnBalanceChanged?.Invoke(Balance);
            return true;
        }

        public void RaiseBalanceChanged(int newBalance)
        {
            Balance = newBalance;
            OnBalanceChanged?.Invoke(newBalance);
        }
    }
}
