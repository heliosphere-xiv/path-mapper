namespace PathMapper;

public readonly struct WeaponType : IEquatable<WeaponType> {
    public readonly ushort Value;

    public WeaponType(ushort value) => this.Value = value;

    public static implicit operator WeaponType(ushort id) => new(id);

    public static explicit operator ushort(WeaponType id) => id.Value;

    public override string ToString() => this.Value.ToString();

    public bool Equals(WeaponType other) => this.Value == other.Value;

    public override bool Equals(object? obj) => obj is WeaponType other && this.Equals(other);

    public override int GetHashCode() => this.Value.GetHashCode();

    public static bool operator ==(WeaponType lhs, WeaponType rhs) => lhs.Value == rhs.Value;

    public static bool operator !=(WeaponType lhs, WeaponType rhs) => lhs.Value != rhs.Value;
}
