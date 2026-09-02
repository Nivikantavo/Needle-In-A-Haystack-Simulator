using UnityEngine;

namespace Needle.Haystack
{
    public static class HaystackDiscard
    {
        const byte SkipMask = (byte)(
            StrawFlags.Buried | StrawFlags.Grabbed | StrawFlags.Discarded | StrawFlags.IsNeedle);

        public static int MarkExposed(byte[] flags, Matrix4x4[] matrices, int count, int wanted)
        {
            if (flags == null || matrices == null || count <= 0 || wanted <= 0)
                return 0;

            int n = count;
            if (n > flags.Length)
                n = flags.Length;
            if (n > matrices.Length)
                n = matrices.Length;

            int marked = 0;
            while (marked < wanted)
            {
                int best = FindHighestExposed(flags, matrices, n);
                if (best < 0)
                    break;

                flags[best] = (byte)(flags[best] | (byte)StrawFlags.Discarded);
                marked++;
            }

            return marked;
        }

        static int FindHighestExposed(byte[] flags, Matrix4x4[] matrices, int n)
        {
            int best = -1;
            float bestY = float.NegativeInfinity;
            for (int i = 0; i < n; i++)
            {
                if ((flags[i] & SkipMask) != 0)
                    continue;

                float y = matrices[i].GetPosition().y;
                if (best >= 0 && y <= bestY)
                    continue;

                best = i;
                bestY = y;
            }

            return best;
        }
    }
}
