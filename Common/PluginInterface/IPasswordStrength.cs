using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IAM.PluginInterface
{

    public class PasswordStrength
    {
        public Boolean HasLength { get; set; }
        public Boolean HasUpperCase { get; set; }
        public Boolean HasLowerCase { get; set; }
        public Boolean HasDigit { get; set; }
        public Boolean HasSymbol { get; set; }
        public Boolean HasNamePart { get; set; }
    }

    static class PasswordStrength2
    {

        public static Boolean Check(String fullName, String password)
        {

            IEnumerable<string> parts = fullName.ToLower().AllPartsOfLength(4)
                .Concat(fullName.ToLower().AllPartsOfLength(4))
                .Select(part => Regex.Escape(part));

            string regex = "(" + string.Join("|", parts) + ")";

            return !Regex.Match(password.ToLower(), regex, RegexOptions.IgnoreCase).Success;
        }


        private static IEnumerable<string> AllPartsOfLength(this string value, int length)
        {
            for (int startPos = 0; startPos <= value.Length - length; startPos++)
            {
                yield return value.Substring(startPos, length);
            }
            yield break;
        }

    }

    internal class IPasswordStrength
    {
        private PasswordStrength _result;

        public PasswordStrength Result { get { return _result; } }

        public IPasswordStrength(String fullName, String password)
        {
            _result = new PasswordStrength();

            CheckPassword(password);

            if (!String.IsNullOrEmpty(fullName))
                _result.HasNamePart = PasswordStrength2.Check(fullName, password);
        }
        

        /// <summary>
        /// This is the method which checks the password and determines the score.
        /// </summary>
        /// <param name="pwd"></param>
        private void CheckPassword(string pwd)
        {
            // Init Vars
            int iUpperCase = 0;
            int iLowerCase = 0;
            int iDigit = 0;
            int iSymbol = 0;
            int ConsecutiveMode = 0;
            int iConsecutiveUpper = 0;
            int iConsecutiveLower = 0;
            int iConsecutiveDigit = 0;
            string sAlphas = "abcdefghijklmnopqrstuvwxyz";
            string sNumerics = "01234567890";
            int nSeqAlpha = 0;
            int nSeqChar = 0;
            int nSeqNumber = 0;

            // Scan password
            foreach (char ch in pwd.ToCharArray())
            {
                // Count digits
                if (Char.IsDigit(ch))
                {
                    iDigit++;

                    if (ConsecutiveMode == 3)
                        iConsecutiveDigit++;
                    ConsecutiveMode = 3;
                }

                // Count uppercase characters
                if (Char.IsUpper(ch))
                {
                    iUpperCase++;
                    if (ConsecutiveMode == 1)
                        iConsecutiveUpper++;
                    ConsecutiveMode = 1;
                }

                // Count lowercase characters
                if (Char.IsLower(ch))
                {
                    iLowerCase++;
                    if (ConsecutiveMode == 2)
                        iConsecutiveLower++;
                    ConsecutiveMode = 2;
                }

                // Count symbols
                if (Char.IsSymbol(ch) || Char.IsPunctuation(ch))
                {
                    iSymbol++;
                    ConsecutiveMode = 0;
                }

            }

            // Check for sequential alpha string patterns (forward and reverse) 
            for (int s = 0; s < 23; s++)
            {
                string sFwd = sAlphas.Substring(s, 3);
                string sRev = strReverse(sFwd);
                if (pwd.ToLower().IndexOf(sFwd) != -1 || pwd.ToLower().IndexOf(sRev) != -1)
                {
                    nSeqAlpha++;
                    nSeqChar++;
                }
            }

            // Check for sequential numeric string patterns (forward and reverse)
            for (int s = 0; s < 8; s++)
            {
                string sFwd = sNumerics.Substring(s, 3);
                string sRev = strReverse(sFwd);
                if (pwd.ToLower().IndexOf(sFwd) != -1 || pwd.ToLower().IndexOf(sRev) != -1)
                {
                    nSeqNumber++;
                    nSeqChar++;
                }
            }


            if (pwd.Length >= 8) _result.HasLength = true;     // Min password length
            if (iUpperCase > 0) _result.HasUpperCase = true;      // Uppercase letters
            if (iLowerCase > 0) _result.HasLowerCase = true;      // Lowercase letters
            if (iDigit > 0) _result.HasDigit = true;          // Digits
            if (iSymbol > 0) _result.HasSymbol = true;         // Symbols

        }


        /// <summary>
        /// Helper string function to reverse string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private String strReverse(String str)
        {
            string newstring = "";
            for (int s = 0; s < str.Length; s++)
            {
                newstring = str[s] + newstring;
            }
            return newstring;
        }

    }
}
