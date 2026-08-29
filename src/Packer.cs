using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

class Packer
{
    // packer.exe <inZip> <outDat> — AES-encrypt an existing zip into a
    // payload.dat. The zip itself is created by 7-Zip (multithreaded, much
    // faster than the old single-threaded .NET ZipArchive over ~36k files).
    // Format (unchanged): AES-256-CBC, Key=SHA256("DSH-Portable-2026"),
    // IV=SHA256("DeepSeekHarness-iv")[0..16], footer "DSHPAYLOAD01"+Int64 len.
    static int Main(string[] args)
    {
        if (args.Length < 2) { Console.WriteLine("usage: packer <inZip> <outDat>"); return 1; }
        string inZip = Path.GetFullPath(args[0]);
        string outDat = Path.GetFullPath(args[1]);

        byte[] key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("DSH-Portable-2026"));
        byte[] iv = new byte[16];
        Array.Copy(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("DeepSeekHarness-iv")), iv, 16);

        if (File.Exists(outDat)) File.Delete(outDat);
        using (FileStream srcFs = File.OpenRead(inZip))
        using (FileStream dstFs = File.Create(outDat))
        using (RijndaelManaged aes = new RijndaelManaged { Key = key, IV = iv, Mode = CipherMode.CBC, Padding = PaddingMode.PKCS7 })
        using (CryptoStream cs = new CryptoStream(dstFs, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            srcFs.CopyTo(cs);
        }
        // footer: magic + encrypted payload length (footer not yet written, so Length = encrypted size)
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
}
