public interface IDamageable
{
    bool TakeDamage(DamageType type);
    int Health { get; }
}
