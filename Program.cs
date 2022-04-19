namespace svFix
{
    class Program
    {
        static String documentPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        static String saveDirName = "GTA Vice City User Files";
        static String saveDirPath = Path.Combine(documentPath, saveDirName);
        public static String[] getAllFilePaths()
        {
            String[] allFilePaths = System.IO.Directory.GetFiles(saveDirPath + "\\", "*.sv", System.IO.SearchOption.TopDirectoryOnly);
            return allFilePaths;
        }
        public static String process(String fPath)
        {
            if (!File.Exists(fPath))
            {
                return "";
            }
            var allBytes = File.ReadAllBytes(fPath);
            // sv2bByBakaFT
            byte[] myBytes = {115,0,118,0,50,0,98,0,66,0,121,0,66,0,97,0,107,0,97,0,70,0,84,0,0,0};
            var byteIndex = 0;
            for(var i =4;byteIndex<myBytes.Length;++i){
                allBytes[i] = myBytes[byteIndex++];
            }
            String []splitedPath = fPath.Split('.');
            String nPath = Path.Combine(splitedPath[0]+".b");
            File.WriteAllBytes(nPath, allBytes);
            return nPath;
        }
        public static void FixCheckSum(string path)
        {
            uint checkSum = 0;
            using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length && reader.BaseStream.Position < 201824)
                {
                    checkSum += reader.ReadByte();
                }
            }
            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Open)))
            {
                writer.BaseStream.Seek(201824, SeekOrigin.Begin);
                writer.Write(checkSum);
            }
        }
        static void Main(string[] args)
        {
            Console.WriteLine("sv2b:转换罪吧汉化存档到原版\n如果对应存档槽位已经存在存档则会覆盖写入，注意备份同名存档！");
            Console.WriteLine("按下任意键进行转换");
            Console.ReadKey(true);
            foreach(string fPath in getAllFilePaths())
            {
                Console.WriteLine("In:" + fPath);
                String a = process(fPath);
                if( a!="")
                {
                    FixCheckSum(a);
                    Console.WriteLine("Out:"+a);
                }
            }
            Console.WriteLine("完成，按任意键退出");
            Console.ReadKey(true);
        }
    }
}
