using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Helper;

namespace TCP_RSA_Client
{
    class Client
    {
        private static void BobGeneratesAndExportsKeys(RSA rsa)
        {
            var privateFilePath = Path.Combine(HelperMethods.GetProjectDirectory(), "BobPrivateKey.txt");
            File.WriteAllBytes(privateFilePath, rsa.ExportRSAPrivateKey());
            var publicFilePath = Path.Combine(HelperMethods.GetSolutionDirectory().FullName, "BobPublicKey.txt");
            File.WriteAllBytes(publicFilePath, rsa.ExportRSAPublicKey());
        }

        private static string BobEncryptsMessageForAlice(RSA rsa, string bobMessage)
        {
            // Import Alice Public Key to Encrypt Message
            var publicAliceKeyPath = Path.Combine(HelperMethods.GetSolutionDirectory().FullName, "AlicePublicKey.txt");
            rsa.ImportRSAPublicKey(File.ReadAllBytes(publicAliceKeyPath), out _);

            // Convert Message to bytes, encrypt and then convert again to string
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(bobMessage);
            byte[] encryptedBytes = rsa.Encrypt(bytesToBeEncrypted, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encryptedBytes);
        }

        private static void BobDecryptsAliceMessage(string aliceMessage)
        {
            // Bob imports his Private Key to Decrypt
            var privateBobKeyPath = Path.Combine(HelperMethods.GetProjectDirectory(), "BobPrivateKey.txt");
            RSA rsaA = RSA.Create();
            rsaA.ImportRSAPrivateKey(File.ReadAllBytes(privateBobKeyPath), out _);

            // Bob converts the string to bytes, decrypts and converts back to string
            byte[] encryptedBytesAliceMsg = Convert.FromBase64String(aliceMessage);
            byte[] decryptedBytes = rsaA.Decrypt(encryptedBytesAliceMsg, RSAEncryptionPadding.OaepSHA256);
            string decryptedMessage = Encoding.UTF8.GetString(decryptedBytes);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Alice: " + decryptedMessage);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void Main(String[] args)
        {
            TcpClient client;
            try
            {
                client = new TcpClient("localhost", 6666);
                Console.WriteLine("Bob is Connected to Server!");
            }
            catch
            {
                Console.WriteLine("Failed to Connect!");
                return;
            }
            NetworkStream networkStream = client.GetStream();
            StreamReader streamReader = new(networkStream);
            StreamWriter streamWriter = new(networkStream);

            RSA rsa = RSA.Create();
            BobGeneratesAndExportsKeys(rsa);

            try
            {
                string bobMessage = "";
                while (bobMessage != "bye")
                {
                    Console.Write("Bob: ");
                    bobMessage = Console.ReadLine();

                    // Send Encrypted Message
                    streamWriter.WriteLine(BobEncryptsMessageForAlice(rsa, bobMessage));
                    streamWriter.Flush();

                    string aliceMessage = streamReader.ReadLine();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Alice: " + aliceMessage);

                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("Bob decrypts the Message ...");

                    BobDecryptsAliceMessage(aliceMessage);
                }
            }
            catch
            {
                Console.WriteLine("Exception reading from the server");
            }
            streamReader.Close();
            networkStream.Close();
            streamWriter.Close();
        }
    }
}
