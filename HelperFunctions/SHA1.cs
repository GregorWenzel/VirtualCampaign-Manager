using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Net;
using System.Xml;

namespace HelperFunctions
{
    public static class Encrypt
    {
        public static string GetSHA1Hash(string text)
        {
            SHA1 Sha1 = new SHA1CryptoServiceProvider();
            byte[] arrayData;
            byte[] arrayResult;
            string result = null;
            string temp = null;

            arrayData = Encoding.ASCII.GetBytes(text);
            arrayResult = Sha1.ComputeHash(arrayData);
            for (int i = 0; i < arrayResult.Length; i++)
            {
                temp = Convert.ToString(arrayResult[i], 16);
                if (temp.Length == 1)
                    temp = "0" + temp;
                result += temp;
            }
            return result;
        }
    }
}
