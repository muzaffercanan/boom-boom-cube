public interface IFallable
{
    bool CanFall();
    void FallTo(int targetY, float duration);
}
