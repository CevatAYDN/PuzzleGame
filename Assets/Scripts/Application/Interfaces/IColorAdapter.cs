using PuzzleGame.Domain.Models;
using UnityEngine;

namespace PuzzleGame.Application.Interfaces
{
    /// <summary>
    /// DomainColor ↔ UnityEngine.Color dönüşüm adaptörü.
    /// Application katmanında soyutlanmıştır — Infrastructure implementasyonu sağlar.
    /// Dependency Inversion: Application kendi interface'ini tanımlar, Infrastructure uygular.
    /// </summary>
    public interface IColorAdapter
    {
        DomainColor FromUnity(Color color);
        Color ToUnity(DomainColor color);
    }
}
