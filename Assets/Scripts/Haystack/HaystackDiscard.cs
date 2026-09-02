namespace Needle.Haystack
{
    public static class HaystackDiscard
    {
        const byte SkipMask = (byte)(
            StrawFlags.Buried | StrawFlags.Grabbed | StrawFlags.Discarded | StrawFlags.IsNeedle);

        public static int MarkExposed(byte[] flags, int count, int wanted)
        {
            if (flags == null || count <= 0 || wanted <= 0)
                return 0;

            int n = count < flags.Length ? count : flags.Length;
            int marked = 0;
            for (int i = 0; i < n && marked < wanted; i++)
            {
                byte flag = flags[i];
                if ((flag & SkipMask) != 0)
                    continue;

                flags[i] = (byte)(flag | (byte)StrawFlags.Discarded);
                marked++;
            }

            return marked;
        }
    }
}
