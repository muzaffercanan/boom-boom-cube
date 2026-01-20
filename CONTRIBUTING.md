using System;

namespace Core.Interfaces
{
    public interface IFallable
    {
        bool CanFall();
        void FallTo(int targetY, float duration);
    }
}