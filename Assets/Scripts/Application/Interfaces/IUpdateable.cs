namespace BottleShaders.Application.Interfaces
{
    /// <summary>
    /// Contract for any class that needs to receive centralized frame-rate updates.
    /// Helps eliminate MonoBehaviour Update overhead.
    /// </summary>
    public interface IUpdateable
    {
        void OnUpdate(float deltaTime);
    }
}
