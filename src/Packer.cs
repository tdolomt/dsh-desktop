using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

class Packer
{
    // packer.exe <sourceDir> <outDat>
    static int Main(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("usage: packer <sourceDir> <outDat>"); return 1; }
        string src = Path.GetFullPath(args[0]);
        string outDat = Path.GetFullPath(args[1]);

        string tmpZip = Path.Combine(Path.GetTempPath(), "dsh_payload_" + Guid.NewGuid().ToString("N") + ".zip");
        long count = 0;
        try
        {
            using (FileStream fs = File.Create(tmpZip))
            using (ZipArchive za = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                foreach (string file in Directory.EnumerateFiles(src, "*", SearchOption.AllDirectories))
                {
                    string rel = file.Substring(src.Length).TrimStart('\\', '/').Replace('\\', '/');
                    ZipArchiveEntry entry = za.CreateEntry(rel, CompressionLevel.Fastest);
                    using (Stream es = entry.Open())
                    using (Stream inS = File.OpenRead(file))
                        inS.CopyTo(es);
                    count++;
                }
            }
            Console.WriteLine("zip files=" + count);

            byte[] key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("DSH-Portable-2026"));
            byte[] iv = new byte[16];
            Array.Copy(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("DeepSeekHarness-iv")), iv, 16);

            if (File.Exists(outDat)) File.Delete(outDat);
            using (FileStream srcFs = File.OpenRead(tmpZip))
            using (FileStream dstFs = File.Create(outDat))
            using (RijndaelManaged aes = new RijndaelManaged { Key = key, IV = iv, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
            using (CryptoStream cs = new CryptoStream(dstFs, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                srcFs.CopyTo(cs);
            }
            // footer: magic + payload length (footer not yet written, so Length = encrypted size)
            using (FileStream dstFs = File.OpenWrite(outDat))
            {
                dstFs.Seek(0, SeekOrigin.End);
                byte[] footer = Encoding.ASCII.GetBytes("DSHPAYLOAD01");
                byte[] len = BitConverter.GetBytes(new FileInfo(outDat).Length);
                dstFs.Write(footer, 0, footer.Length);
                dstFs.Write(len, 0, 8);
            }
            Console.WriteLine("PACK_OK dat=" + new FileInfo(outDat).Length + " bytes");
            return 0;
        }
        finally { try { File.Delete(tmpZip); } catch { } }
    }
}
