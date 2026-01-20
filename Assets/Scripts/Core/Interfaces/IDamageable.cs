public interface IDamageable
{
    // Returns true if the object was destroyed
    bool TakeDamage(DamageType type);
    int Health { get; }
}
