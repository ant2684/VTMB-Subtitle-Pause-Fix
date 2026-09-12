using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace VtmbInstaller
{
    internal sealed class BinaryPatchChunk
    {
        public readonly int Offset;
        public readonly string BeforeHex;
        public readonly string AfterHex;

        public BinaryPatchChunk(int offset, string beforeHex, string afterHex)
        {
            Offset = offset;
            BeforeHex = beforeHex ?? "";
            AfterHex = afterHex ?? "";
        }
    }

    internal sealed class BinaryPatchRecipe
    {
        public readonly string Id;
        public readonly int OriginalSize;
        public readonly int PatchedSize;
        public readonly string OriginalSha256;
        public readonly string PatchedSha256;
        public readonly BinaryPatchChunk[] Chunks;

        public BinaryPatchRecipe(string id, int originalSize, int patchedSize, string originalSha256, string patchedSha256, BinaryPatchChunk[] chunks)
        {
            Id = id;
            OriginalSize = originalSize;
            PatchedSize = patchedSize;
            OriginalSha256 = originalSha256;
            PatchedSha256 = patchedSha256;
            Chunks = chunks ?? new BinaryPatchChunk[0];
        }
    }

    internal static class PatchEngine
    {
        public static void VerifyBinaryRecipe(string id)
        {
            BinaryPatchRecipe recipe = PatchData.Get(id);
            if (recipe == null) throw new InvalidDataException("Unknown patch recipe: " + id);
            if (recipe.OriginalSize <= 0 || recipe.PatchedSize <= 0 || recipe.Chunks.Length == 0)
                throw new InvalidDataException("Incomplete patch recipe: " + id);
            int lastEnd = 0;
            foreach (BinaryPatchChunk chunk in recipe.Chunks.OrderBy(x => x.Offset))
            {
                byte[] before = Hex(chunk.BeforeHex);
                byte[] after = Hex(chunk.AfterHex);
                if (chunk.Offset < lastEnd || chunk.Offset < 0 || chunk.Offset + before.Length > recipe.OriginalSize || chunk.Offset + after.Length > recipe.PatchedSize)
                    throw new InvalidDataException("Invalid patch range in recipe: " + id);
                if (before.Length != after.Length && !(before.Length == 0 && chunk.Offset >= recipe.OriginalSize))
                    throw new InvalidDataException("Unsupported variable-length patch range in recipe: " + id);
                lastEnd = chunk.Offset + Math.Max(before.Length, after.Length);
            }
        }

        public static bool IsBinaryRecipe(string id) { return PatchData.Get(id) != null; }

        public static bool IsModelRecipe(string id) { return false; }

        public static void VerifyModelRecipe(string id)
        {
            throw new InvalidDataException("Model recipes are not available in this package.");
        }

        public static byte[] BuildModel(string gameRoot, string id)
        {
            throw new InvalidDataException("Model recipes are not available in this package.");
        }

        public static byte[] ApplyBinary(string id, byte[] source)
        {
            BinaryPatchRecipe recipe = PatchData.Get(id);
            if (recipe == null) throw new InvalidDataException("Unknown patch recipe: " + id);
            return ApplyBinaryRecipe(recipe, source);
        }

        private static byte[] ApplyBinaryRecipe(BinaryPatchRecipe recipe, byte[] source)
        {
            string sourceHash = Sha256(source);
            if (Eq(sourceHash, recipe.PatchedSha256) && source.Length == recipe.PatchedSize) return (byte[])source.Clone();
            if (!Eq(sourceHash, recipe.OriginalSha256) || source.Length != recipe.OriginalSize)
                throw new InvalidDataException("The source file does not match the supported original for " + recipe.Id + ".");

            byte[] output = new byte[recipe.PatchedSize];
            Buffer.BlockCopy(source, 0, output, 0, Math.Min(source.Length, output.Length));
            foreach (BinaryPatchChunk chunk in recipe.Chunks)
            {
                byte[] before = Hex(chunk.BeforeHex);
                byte[] after = Hex(chunk.AfterHex);
                for (int i = 0; i < before.Length; i++)
                    if (source[chunk.Offset + i] != before[i])
                        throw new InvalidDataException("Patch precondition failed for " + recipe.Id + " at 0x" + (chunk.Offset + i).ToString("X") + ".");
                Buffer.BlockCopy(after, 0, output, chunk.Offset, after.Length);
            }
            if (!Eq(Sha256(output), recipe.PatchedSha256))
                throw new InvalidDataException("Patch verification failed for " + recipe.Id + ".");
            return output;
        }

        public static void SelfTest()
        {
            byte[] source = BuildSyntheticPe();
            byte[] expected = new byte[source.Length + 4];
            Buffer.BlockCopy(source, 0, expected, 0, source.Length);
            expected[0x100] = 0x11; expected[0x101] = 0x22; expected[0x102] = 0x33; expected[0x103] = 0x44;
            expected[0x200] = 0xDE; expected[0x201] = 0xAD; expected[0x202] = 0xBE; expected[0x203] = 0xEF;
            var recipe = new BinaryPatchRecipe("synthetic-pe", source.Length, expected.Length, Sha256(source), Sha256(expected), new[]
            {
                new BinaryPatchChunk(0x100, "00000000", "11223344"),
                new BinaryPatchChunk(0x200, "", "DEADBEEF")
            });
            byte[] actual = ApplyBinaryRecipe(recipe, source);
            if (!actual.SequenceEqual(expected) || actual[0] != (byte)'M' || actual[1] != (byte)'Z' ||
                actual[0x80] != (byte)'P' || actual[0x81] != (byte)'E')
                throw new InvalidDataException("Synthetic PE patch self-test failed.");
        }

        private static byte[] BuildSyntheticPe()
        {
            byte[] pe = new byte[0x200];
            pe[0] = (byte)'M'; pe[1] = (byte)'Z';
            pe[0x3C] = 0x80;
            pe[0x80] = (byte)'P'; pe[0x81] = (byte)'E';
            pe[0x84] = 0x4C; pe[0x85] = 0x01; pe[0x86] = 0x01;
            return pe;
        }

        private static byte[] Hex(string value)
        {
            if ((value.Length & 1) != 0) throw new InvalidDataException("Invalid hexadecimal patch data.");
            byte[] result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++) result[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            return result;
        }

        private static string Sha256(byte[] data)
        {
            using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
        }

        private static bool Eq(string left, string right) { return string.Equals(left, right, StringComparison.OrdinalIgnoreCase); }
    }
}
