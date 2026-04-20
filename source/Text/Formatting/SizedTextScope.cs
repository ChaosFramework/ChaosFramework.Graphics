namespace ChaosFramework.Graphics.Text.Formatting
{
    public class SizedTextScope : Scope
    {
        internal const char ESCAPE_CODE_SZ = 's';
        public static readonly string SIZE_CODE = ESCAPE_CODE.ToString() + ESCAPE_CODE_SZ;
        public static readonly string RESET_SIZE_CODE = SIZE_CODE + ESCAPE_CODE;

        public static string GetSizeCode(float size)
            => SIZE_CODE + size.ToString() + ESCAPE_CODE;

        protected override void Reset()
            => bldr.Append(RESET_SIZE_CODE);

        public SizedTextScope(System.Text.StringBuilder bldr, float size)
            : base(bldr.Append(GetSizeCode(size)))
        { }
    }
}
