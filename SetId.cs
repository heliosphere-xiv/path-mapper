namespace PathMapper;

public readonly struct SetId : IComparable<SetId> {
    public readonly ushort Value;

    public SetId(ushort value) => this.Value = value;

    public static implicit operator SetId(ushort id) => new(id);

    public static explicit operator ushort(SetId id) => id.Value;

    public override string ToString() => this.Value.ToString();

    public int CompareTo(SetId other) => this.Value.CompareTo(other.Value);
}
