using System;
using Microsoft.VisualBasic;

namespace CopyUtility
{
    public static class HelpService
    {
        public static void ShowMessage(string msg)
        {
            string output = string.Format("| {0,10} |", msg);
            string line = Strings.StrDup(output.Length,'-');
            Console.WriteLine(line);
            Console.WriteLine(output);
            Console.WriteLine(line);
        }

        public static long RoundForMultiplyOfFour(long fileLength)
        {
            var fileSizeInKb = (long)Math.Ceiling(fileLength / 1024.0);
            if (fileSizeInKb % 4 != 0)
            {
                fileSizeInKb += 4 - fileSizeInKb % 4;
            }
            return fileSizeInKb;
        }
    }
}
