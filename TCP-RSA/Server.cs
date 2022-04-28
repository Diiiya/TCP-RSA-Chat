using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Helper;

namespace TCP_RSA
{
    public class Server
    {
        private static void AliceGeneratesAndExportsKeys(RSA rsa)
        {
            // Alice Generates and Exports Keys
            var privateFilePath = Path.Combine(HelperMethods.GetProjectDirectory(), "AlicePrivateKey.txt");
            File.WriteAllBytes(privateFilePath, rsa.ExportRSAPrivateKey());
            var publicFilePath = Path.Combine(HelperMethods.GetSolutionDirectory().FullName, "AlicePublicKey.txt");
            File.WriteAllBytes(publicFilePath, rsa.ExportRSAPublicKey());
        }

        private static void AliceDecryptsBobMessage(RSA rsa, string bobMessage)
        {
            // Alice imports her Private Key to Decrypt
            var privateAliceKeyPath = Path.Combine(HelperMethods.GetProjectDirectory(), "AlicePrivateKey.txt");
            rsa.ImportRSAPrivateKey(File.ReadAllBytes(privateAliceKeyPath), out _);

            // Alice converts the string to bytes, decrypts and converts back to string
            byte[] encryptedBytes = Convert.FromBase64String(bobMessage);
            byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
            string decryptedMessage = Encoding.UTF8.GetString(decryptedBytes);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Bob: " + decryptedMessage);
            Console.WriteLine();
        }

        private static string AliceEncryptsMessageForBob(string aliceMessage)
        {
            // Import Bob Public Key to Encrypt Message
            var publicBobKeyPath = Path.Combine(HelperMethods.GetSolutionDirectory().FullName, "BobPublicKey.txt");
            RSA rsaB = RSA.Create();
            rsaB.ImportRSAPublicKey(File.ReadAllBytes(publicBobKeyPath), out _);

            // Convert Message to bytes, encrypt and then convert again to string
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(aliceMessage);
            byte[] encryptedBytesAliceMsg = rsaB.Encrypt(bytesToBeEncrypted, RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encryptedBytesAliceMsg);
        }

        public static void Main()
        {
            try
            {
                string aliceMessage = "";
                TcpListener tcpListener = new(6666);
                tcpListener.Start();
                Console.WriteLine("Server Started");
                Socket socket = tcpListener.AcceptSocket();
                Console.WriteLine("Client Bob is Connected");
                NetworkStream networkStream = new(socket);
                StreamWriter streamWriter = new(networkStream);
                StreamReader streamReader = new(networkStream);

                RSA rsa = RSA.Create();
                AliceGeneratesAndExportsKeys(rsa);

                while (aliceMessage != "bye")
                {
                    if (socket.Connected)
                    {
                        string bobMessage = streamReader.ReadLine();
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine("Bob: " + bobMessage);
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine("Alice decrypts the Message ...");

                        AliceDecryptsBobMessage(rsa, bobMessage);

                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.Write("Alice: ");
                        aliceMessage = Console.ReadLine();
                        Console.WriteLine();

                        // Send Encrypted Message
                        streamWriter.WriteLine(AliceEncryptsMessageForBob(aliceMessage));
                        streamWriter.Flush();
                    }
                }
                streamReader.Close();
                networkStream.Close();
                streamWriter.Close();
                socket.Close();
                Console.WriteLine("Exiting ..");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
