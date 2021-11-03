namespace Reventuous {
    public record ExpectedStreamVersion(long Value) {
        public static readonly ExpectedStreamVersion NoStream = new(-1);
        public static readonly ExpectedStreamVersion Any = new(-2);
    }

    public record StreamReadPosition(string Value) {
        public static StreamReadPosition Start = new("0");
        public static implicit operator string(StreamReadPosition position) => position.Value;
    }
}