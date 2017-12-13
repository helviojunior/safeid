using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Data;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using CAS.Web;


namespace IAMWebServer
{
    /// <summary>
    /// Determines how strong a password is based on lots of different criteria. 0 is very weak. 100 is Very strong.
    /// </summary>
    public class PasswordStrength
    {
        private DataTable dtDetails;
        private string PreviousPassword = "";
        private Boolean hasLength;
        private Boolean hasUpperCase;
        private Boolean hasLowerCase;
        private Boolean hasDigit;
        private Boolean hasSymbol;

        public Boolean HasLength { get { return hasLength; } }
        public Boolean HasUpperCase { get { return hasUpperCase; } }
        public Boolean HasLowerCase { get { return hasLowerCase; } }
        public Boolean HasDigit { get { return hasDigit; } }
        public Boolean HasSymbol { get { return hasSymbol; } }


        public PasswordStrength() { }

        /// <summary>
        /// Set the password
        /// </summary>
        /// <param name="pwd"></param>
        public void SetPassword(string pwd)
        {
            if (PreviousPassword != pwd)
            {
                PreviousPassword = pwd;
                CheckPasswordWithDetails(PreviousPassword);
            }
        }

        /// <summary>
        /// Returns score for the password passed in SetPassword
        /// </summary>
        /// <returns></returns>
        public int GetPasswordScore()
        {
            if (dtDetails != null)
                return (int)dtDetails.Rows[0][5];
            else
                return 0;
        }

        /// <summary>
        /// Returns a textual description of the stregth of the password
        /// </summary>
        /// <returns></returns>
        public string GetPasswordStrength()
        {
            if (dtDetails != null)
                return (String)dtDetails.Rows[0][3];
            else
            {
                String unk = "";
                try
                {
                    unk = MessageResource.GetMessage("unknow");
                }
                catch { unk = ""; }
                return (unk == "" || unk == null ? "Unknown" : unk);
            }
        }

        /// <summary>
        /// Returns the details for the password score - Allows you to see why a password has the score it does.
        /// </summary>
        /// <returns></returns>
        public DataTable GetStrengthDetails()
        {
            return dtDetails;
        }

        /// <summary>
        /// This is the method which checks the password and determines the score.
        /// </summary>
        /// <param name="pwd"></param>
        private void CheckPasswordWithDetails(string pwd)
        {
            // Init Vars
            int nScore = 0;
            string sComplexity = "";
            int iUpperCase = 0;
            int iLowerCase = 0;
            int iDigit = 0;
            int iSymbol = 0;
            int iRepeated = 1;
            Hashtable htRepeated = new Hashtable();
            int iMiddle = 0;
            int iMiddleEx = 1;
            int ConsecutiveMode = 0;
            int iConsecutiveUpper = 0;
            int iConsecutiveLower = 0;
            int iConsecutiveDigit = 0;
            int iLevel = 0;
            string sAlphas = "abcdefghijklmnopqrstuvwxyz";
            string sNumerics = "01234567890";
            int nSeqAlpha = 0;
            int nSeqChar = 0;
            int nSeqNumber = 0;

            // Create data table to store results
            CreateDetailsTable();
            DataRow drScore = AddDetailsRow(iLevel++, "Score", "", "", 0, 0);

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

                // Count repeated letters 
                if (Char.IsLetter(ch))
                {
                    if (htRepeated.Contains(Char.ToLower(ch))) iRepeated++;
                    else htRepeated.Add(Char.ToLower(ch), 0);

                    if (iMiddleEx > 1)
                        iMiddle = iMiddleEx - 1;
                }

                if (iUpperCase > 0 || iLowerCase > 0)
                {
                    if (Char.IsDigit(ch) || Char.IsSymbol(ch))
                        iMiddleEx++;
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

            // Calcuate score
            AddDetailsRow(iLevel++, "Additions", "", "", 0, 0);

            // Score += 4 * Password Length
            nScore = 4 * pwd.Length;
            AddDetailsRow(iLevel++, "Password Length", "Flat", "(n*4)", pwd.Length, pwd.Length * 4);

            // if we have uppercase letetrs Score +=(number of uppercase letters *2)
            if (iUpperCase > 0)
            {
                nScore += ((pwd.Length - iUpperCase) * 2);
                AddDetailsRow(iLevel++, "Uppercase Letters", "Cond/Incr", "+((len-n)*2)", iUpperCase, ((pwd.Length - iUpperCase) * 2));
            }
            else
                AddDetailsRow(iLevel++, "Uppercase Letters", "Cond/Incr", "+((len-n)*2)", iUpperCase, 0);

            // if we have lowercase letetrs Score +=(number of lowercase letters *2)
            if (iLowerCase > 0)
            {
                nScore += ((pwd.Length - iLowerCase) * 2);
                AddDetailsRow(iLevel++, "Lowercase Letters", "Cond/Incr", "+((len-n)*2)", iLowerCase, ((pwd.Length - iLowerCase) * 2));
            }
            else
                AddDetailsRow(iLevel++, "Lowercase Letters", "Cond/Incr", "+((len-n)*2)", iLowerCase, 0);


            // Score += (Number of digits *4)
            nScore += (iDigit * 4);
            AddDetailsRow(iLevel++, "Numbers", "Cond", "+(n*4)", iDigit, (iDigit * 4));

            // Score += (Number of Symbols * 6)
            nScore += (iSymbol * 6);
            AddDetailsRow(iLevel++, "Symbols", "Flat", "+(n*6)", iSymbol, (iSymbol * 6));

            // Score += (Number of digits or symbols in middle of password *2)
            nScore += (iMiddle * 2);
            AddDetailsRow(iLevel++, "Middle Numbers or Symbols", "Flat", "+(n*2)", iMiddle, (iMiddle * 2));

            //requirments
            int requirments = 0;
            if (pwd.Length >= 8) requirments++;     // Min password length
            if (iUpperCase > 0) requirments++;      // Uppercase letters
            if (iLowerCase > 0) requirments++;      // Lowercase letters
            if (iDigit > 0) requirments++;          // Digits
            if (iSymbol > 0) requirments++;         // Symbols

            if (pwd.Length >= 8) hasLength = true;     // Min password length
            if (iUpperCase > 0) hasUpperCase = true;      // Uppercase letters
            if (iLowerCase > 0) hasLowerCase = true;      // Lowercase letters
            if (iDigit > 0) hasDigit = true;          // Digits
            if (iSymbol > 0) hasSymbol = true;         // Symbols

            // If we have more than 3 requirments then
            if (requirments > 3)
            {
                // Score += (requirments *2) 
                nScore += (requirments * 2);
                AddDetailsRow(iLevel++, "Requirments", "Flat", "+(n*2)", requirments, (requirments * 2));
            }
            else
                AddDetailsRow(iLevel++, "Requirments", "Flat", "+(n*2)", requirments, 0);

            //
            // Deductions
            //
            AddDetailsRow(iLevel++, "Deductions", "", "", 0, 0);

            // If only letters then score -=  password length
            if (iDigit == 0 && iSymbol == 0)
            {
                nScore -= pwd.Length;
                AddDetailsRow(iLevel++, "Letters only", "Flat", "-n", pwd.Length, -pwd.Length);
            }
            else
                AddDetailsRow(iLevel++, "Letters only", "Flat", "-n", 0, 0);

            // If only digits then score -=  password length
            if (iDigit == pwd.Length)
            {
                nScore -= pwd.Length;
                AddDetailsRow(iLevel++, "Numbers only", "Flat", "-n", pwd.Length, -pwd.Length);
            }
            else
                AddDetailsRow(iLevel++, "Numbers only", "Flat", "-n", 0, 0);

            // If repeated letters used then score -= (iRepeated * (iRepeated - 1));
            if (iRepeated > 1)
            {
                nScore -= (iRepeated * (iRepeated - 1));
                AddDetailsRow(iLevel++, "Repeat Characters (Case Insensitive)", "Incr", "-(n(n-1))", iRepeated, -(iRepeated * (iRepeated - 1)));
            }

            // If Consecutive uppercase letters then score -= (iConsecutiveUpper * 2);
            nScore -= (iConsecutiveUpper * 2);
            AddDetailsRow(iLevel++, "Consecutive Uppercase Letters", "Flat", "-(n*2)", iConsecutiveUpper, -(iConsecutiveUpper * 2));

            // If Consecutive lowercase letters then score -= (iConsecutiveUpper * 2);
            nScore -= (iConsecutiveLower * 2);
            AddDetailsRow(iLevel++, "Consecutive Lowercase Letters", "Flat", "-(n*2)", iConsecutiveLower, -(iConsecutiveLower * 2));

            // If Consecutive digits used then score -= (iConsecutiveDigits* 2);
            nScore -= (iConsecutiveDigit * 2);
            AddDetailsRow(iLevel++, "Consecutive Numbers", "Flat", "-(n*2)", iConsecutiveDigit, -(iConsecutiveDigit * 2));

            // If password contains sequence of letters then score -= (nSeqAlpha * 3)
            nScore -= (nSeqAlpha * 3);
            AddDetailsRow(iLevel++, "Sequential Letters (3+)", "Flat", "-(n*3)", nSeqAlpha, -(nSeqAlpha * 3));

            // If password contains sequence of digits then score -= (nSeqNumber * 3)
            nScore -= (nSeqNumber * 3);
            AddDetailsRow(iLevel++, "Sequential Numbers (3+)", "Flat", "-(n*3)", nSeqNumber, -(nSeqNumber * 3));

            /* Determine complexity based on overall score */
            if (nScore > 100) { nScore = 100; } else if (nScore < 0) { nScore = 0; }

            try
            {
                String key = "";
                if (nScore >= 0 && nScore < 20) { key = "very_weak"; }
                else if (nScore >= 20 && nScore < 40) { key = "weak"; }
                else if (nScore >= 40 && nScore < 60) { key = "good"; }
                else if (nScore >= 60 && nScore < 80) { key = "strong"; }
                else if (nScore >= 80 && nScore <= 100) { key = "very_strong"; }

                sComplexity = MessageResource.GetMessage(key);
            }
            catch { sComplexity = ""; }

            if ((sComplexity == null) || (sComplexity == ""))
            {
                if (nScore >= 0 && nScore < 20) { sComplexity = "Very Weak"; }
                else if (nScore >= 20 && nScore < 40) { sComplexity = "Weak"; }
                else if (nScore >= 40 && nScore < 60) { sComplexity = "Good"; }
                else if (nScore >= 60 && nScore < 80) { sComplexity = "Strong"; }
                else if (nScore >= 80 && nScore <= 100) { sComplexity = "Very Strong"; }
            }

            // Store score and complexity in dataset
            drScore[5] = nScore;
            drScore[3] = sComplexity;
            dtDetails.AcceptChanges();
        }

        /// <summary>
        /// Create datatable for results
        /// </summary>
        private void CreateDetailsTable()
        {
            dtDetails = new DataTable("PasswordDetails");
            dtDetails.Columns.Add(new DataColumn("Level", typeof(Int32)));
            dtDetails.Columns.Add(new DataColumn("Description", typeof(String)));
            dtDetails.Columns.Add(new DataColumn("Type", typeof(String)));
            dtDetails.Columns.Add(new DataColumn("Rate", typeof(String)));
            dtDetails.Columns.Add(new DataColumn("Count", typeof(Int32)));
            dtDetails.Columns.Add(new DataColumn("Bonus", typeof(Int32)));
        }

        /// <summary>
        /// Helper method to add row into DataTable
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="Description"></param>
        /// <param name="Type"></param>
        /// <param name="Rate"></param>
        /// <param name="Count"></param>
        /// <param name="Bouns"></param>
        /// <returns></returns>
        private DataRow AddDetailsRow(int Id, string Description, string Type, string Rate, int Count, int Bouns)
        {
            DataRow dr = dtDetails.NewRow();
            dr[0] = Id;
            dr[1] = Description;
            dr[2] = Type;
            dr[3] = Rate;
            dr[4] = Count;
            dr[5] = Bouns;
            dtDetails.Rows.Add(dr);
            dtDetails.AcceptChanges();
            return dr;
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

        public Color GetStrengthColor()
        {
            #region Color Array
            Color[] Colors = { 
                                 Color.FromArgb(255,255,0,0),
                                 Color.FromArgb(255,255,1,0),                                                                 
                                Color.FromArgb(255,255,2,0),
                                Color.FromArgb(255,255,4,0),
                                Color.FromArgb(255,255,5,0),
                                Color.FromArgb(255,255,6,0),
                                Color.FromArgb(255,255,7,0),
                                Color.FromArgb(255,255,8,0),
                                Color.FromArgb(255,255,10,0),
                                Color.FromArgb(255,255,11,0),
                                Color.FromArgb(255,255,12,0),
                                Color.FromArgb(255,255,13,0),
                                Color.FromArgb(255,255,15,0),
                                Color.FromArgb(255,255,16,0),
                                Color.FromArgb(255,255,17,0),
                                Color.FromArgb(255,255,19,0),
                                Color.FromArgb(255,255,20,0),
                                Color.FromArgb(255,255,21,0),
                                Color.FromArgb(255,255,22,0),
                                Color.FromArgb(255,255,23,0),
                                Color.FromArgb(255,255,25,0),
                                Color.FromArgb(255,255,26,0),
                                Color.FromArgb(255,255,27,0),
                                Color.FromArgb(255,255,28,0),
                                Color.FromArgb(255,255,30,0),
                                Color.FromArgb(255,255,31,0),
                                Color.FromArgb(255,255,32,0),
                                Color.FromArgb(255,255,34,0),
                                Color.FromArgb(255,255,35,0),
                                Color.FromArgb(255,255,36,0),
                                Color.FromArgb(255,255,37,0),
                                Color.FromArgb(255,255,38,0),
                                Color.FromArgb(255,255,40,0),
                                Color.FromArgb(255,255,41,0),
                                Color.FromArgb(255,255,42,0),
                                Color.FromArgb(255,255,43,0),
                                Color.FromArgb(255,255,44,0),
                                Color.FromArgb(255,255,46,0),
                                Color.FromArgb(255,255,47,0),
                                Color.FromArgb(255,255,48,0),
                                Color.FromArgb(255,255,49,0),
                                Color.FromArgb(255,255,51,0),
                                Color.FromArgb(255,255,52,0),
                                Color.FromArgb(255,255,53,0),
                                Color.FromArgb(255,255,55,0),
                                Color.FromArgb(255,255,56,0),
                                Color.FromArgb(255,255,57,0),
                                Color.FromArgb(255,255,58,0),
                                Color.FromArgb(255,255,59,0),
                                Color.FromArgb(255,255,61,0),
                                Color.FromArgb(255,255,62,0),
                                Color.FromArgb(255,255,63,0),
                                Color.FromArgb(255,255,64,0),
                                Color.FromArgb(255,255,66,0),
                                Color.FromArgb(255,255,67,0),
                                Color.FromArgb(255,255,68,0),
                                Color.FromArgb(255,255,70,0),
                                Color.FromArgb(255,255,71,0),
                                Color.FromArgb(255,255,72,0),
                                Color.FromArgb(255,255,73,0),
                                Color.FromArgb(255,255,74,0),
                                Color.FromArgb(255,255,76,0),
                                Color.FromArgb(255,255,77,0),
                                Color.FromArgb(255,255,78,0),
                                Color.FromArgb(255,255,80,0),
                                Color.FromArgb(255,255,81,0),
                                Color.FromArgb(255,255,82,0),
                                Color.FromArgb(255,255,83,0),
                                Color.FromArgb(255,255,85,0),
                                Color.FromArgb(255,255,86,0),
                                Color.FromArgb(255,255,87,0),
                                Color.FromArgb(255,255,88,0),
                                Color.FromArgb(255,255,89,0),
                                Color.FromArgb(255,255,91,0),
                                Color.FromArgb(255,255,92,0),
                                Color.FromArgb(255,255,93,0),
                                Color.FromArgb(255,255,95,0),
                                Color.FromArgb(255,255,96,0),
                                Color.FromArgb(255,255,97,0),
                                Color.FromArgb(255,255,98,0),
                                Color.FromArgb(255,255,100,0),
                                Color.FromArgb(255,255,101,0),
                                Color.FromArgb(255,255,102,0),
                                Color.FromArgb(255,255,103,0),
                                Color.FromArgb(255,255,104,0),
                                Color.FromArgb(255,255,106,0),
                                Color.FromArgb(255,255,107,0),
                                Color.FromArgb(255,255,108,0),
                                Color.FromArgb(255,255,110,0),
                                Color.FromArgb(255,255,111,0),
                                Color.FromArgb(255,255,112,0),
                                Color.FromArgb(255,255,113,0),
                                Color.FromArgb(255,255,115,0),
                                Color.FromArgb(255,255,116,0),
                                Color.FromArgb(255,255,117,0),
                                Color.FromArgb(255,255,118,0),
                                Color.FromArgb(255,255,119,0),
                                Color.FromArgb(255,255,121,0),
                                Color.FromArgb(255,255,122,0),
                                Color.FromArgb(255,255,123,0),
                                Color.FromArgb(255,255,124,0),
                                Color.FromArgb(255,255,125,0),
                                Color.FromArgb(255,255,127,0),
                                Color.FromArgb(255,255,128,0),
                                Color.FromArgb(255,255,129,0),
                                Color.FromArgb(255,255,131,0),
                                Color.FromArgb(255,255,132,0),
                                Color.FromArgb(255,255,133,0),
                                Color.FromArgb(255,255,134,0),
                                Color.FromArgb(255,255,136,0),
                                Color.FromArgb(255,255,137,0),
                                Color.FromArgb(255,255,138,0),
                                Color.FromArgb(255,255,139,0),
                                Color.FromArgb(255,255,140,0),
                                Color.FromArgb(255,255,142,0),
                                Color.FromArgb(255,255,143,0),
                                Color.FromArgb(255,255,144,0),
                                Color.FromArgb(255,255,146,0),
                                Color.FromArgb(255,255,147,0),
                                Color.FromArgb(255,255,148,0),
                                Color.FromArgb(255,255,149,0),
                                Color.FromArgb(255,255,151,0),
                                Color.FromArgb(255,255,152,0),
                                Color.FromArgb(255,255,153,0),
                                Color.FromArgb(255,255,154,0),
                                Color.FromArgb(255,255,155,0),
                                Color.FromArgb(255,255,155,0),
                                Color.FromArgb(255,255,156,0),
                                Color.FromArgb(255,255,157,0),
                                Color.FromArgb(255,255,158,0),
                                Color.FromArgb(255,255,159,0),
                                Color.FromArgb(255,255,159,0),
                                Color.FromArgb(255,255,160,0),
                                Color.FromArgb(255,255,161,0),
                                Color.FromArgb(255,255,162,0),
                                Color.FromArgb(255,255,163,0),
                                Color.FromArgb(255,255,163,0),
                                Color.FromArgb(255,255,164,0),
                                Color.FromArgb(255,255,165,0),
                                Color.FromArgb(255,255,166,0),
                                Color.FromArgb(255,255,167,0),
                                Color.FromArgb(255,255,167,0),
                                Color.FromArgb(255,255,168,0),
                                Color.FromArgb(255,255,169,0),
                                Color.FromArgb(255,255,170,0),
                                Color.FromArgb(255,255,171,0),
                                Color.FromArgb(255,255,171,0),
                                Color.FromArgb(255,255,172,0),
                                Color.FromArgb(255,255,173,0),
                                Color.FromArgb(255,255,174,0),
                                Color.FromArgb(255,255,175,0),
                                Color.FromArgb(255,255,175,0),
                                Color.FromArgb(255,255,176,0),
                                Color.FromArgb(255,255,177,0),
                                Color.FromArgb(255,255,178,0),
                                Color.FromArgb(255,255,179,0),
                                Color.FromArgb(255,255,179,0),
                                Color.FromArgb(255,255,180,0),
                                Color.FromArgb(255,255,181,0),
                                Color.FromArgb(255,255,181,0),
                                Color.FromArgb(255,255,182,0),
                                Color.FromArgb(255,255,183,0),
                                Color.FromArgb(255,255,184,0),
                                Color.FromArgb(255,255,185,0),
                                Color.FromArgb(255,255,185,0),
                                Color.FromArgb(255,255,186,0),
                                Color.FromArgb(255,255,187,0),
                                Color.FromArgb(255,255,188,0),
                                Color.FromArgb(255,255,189,0),
                                Color.FromArgb(255,255,189,0),
                                Color.FromArgb(255,255,190,0),
                                Color.FromArgb(255,255,191,0),
                                Color.FromArgb(255,255,192,0),
                                Color.FromArgb(255,255,193,0),
                                Color.FromArgb(255,255,193,0),
                                Color.FromArgb(255,255,194,0),
                                Color.FromArgb(255,255,195,0),
                                Color.FromArgb(255,255,196,0),
                                Color.FromArgb(255,255,197,0),
                                Color.FromArgb(255,255,197,0),
                                Color.FromArgb(255,255,198,0),
                                Color.FromArgb(255,255,199,0),
                                Color.FromArgb(255,255,200,0),
                                Color.FromArgb(255,255,201,0),
                                Color.FromArgb(255,255,201,0),
                                Color.FromArgb(255,255,202,0),
                                Color.FromArgb(255,255,203,0),
                                Color.FromArgb(255,255,204,0),
                                Color.FromArgb(255,255,205,0),
                                Color.FromArgb(255,255,205,0),
                                Color.FromArgb(255,255,206,0),
                                Color.FromArgb(255,255,207,0),
                                Color.FromArgb(255,255,208,0),
                                Color.FromArgb(255,255,209,0),
                                Color.FromArgb(255,255,209,0),
                                Color.FromArgb(255,255,210,0),
                                Color.FromArgb(255,255,211,0),
                                Color.FromArgb(255,255,212,0),
                                Color.FromArgb(255,255,213,0),
                                Color.FromArgb(255,255,213,0),
                                Color.FromArgb(255,255,214,0),
                                Color.FromArgb(255,255,215,0),
                                Color.FromArgb(255,255,216,0),
                                Color.FromArgb(255,255,217,0),
                                Color.FromArgb(255,255,217,0),
                                Color.FromArgb(255,255,218,0),
                                Color.FromArgb(255,255,219,0),
                                Color.FromArgb(255,255,220,0),
                                Color.FromArgb(255,255,221,0),
                                Color.FromArgb(255,255,221,0),
                                Color.FromArgb(255,255,222,0),
                                Color.FromArgb(255,255,223,0),
                                Color.FromArgb(255,255,224,0),
                                Color.FromArgb(255,255,225,0),
                                Color.FromArgb(255,255,225,0),
                                Color.FromArgb(255,255,226,0),
                                Color.FromArgb(255,255,227,0),
                                Color.FromArgb(255,255,228,0),
                                Color.FromArgb(255,255,229,0),
                                Color.FromArgb(255,255,229,0),
                                Color.FromArgb(255,255,230,0),
                                Color.FromArgb(255,255,231,0),
                                Color.FromArgb(255,255,232,0),
                                Color.FromArgb(255,255,233,0),
                                Color.FromArgb(255,255,233,0),
                                Color.FromArgb(255,255,234,0),
                                Color.FromArgb(255,255,235,0),
                                Color.FromArgb(255,255,236,0),
                                Color.FromArgb(255,255,237,0),
                                Color.FromArgb(255,255,237,0),
                                Color.FromArgb(255,255,238,0),
                                Color.FromArgb(255,255,239,0),
                                Color.FromArgb(255,255,240,0),
                                Color.FromArgb(255,255,241,0),
                                Color.FromArgb(255,255,241,0),
                                Color.FromArgb(255,255,242,0),
                                Color.FromArgb(255,255,243,0),
                                Color.FromArgb(255,255,244,0),
                                Color.FromArgb(255,255,245,0),
                                Color.FromArgb(255,255,245,0),
                                Color.FromArgb(255,255,246,0),
                                Color.FromArgb(255,255,247,0),
                                Color.FromArgb(255,255,248,0),
                                Color.FromArgb(255,255,249,0),
                                Color.FromArgb(255,255,249,0),
                                Color.FromArgb(255,255,250,0),
                                Color.FromArgb(255,255,251,0),
                                Color.FromArgb(255,255,252,0),
                                Color.FromArgb(255,255,253,0),
                                Color.FromArgb(255,255,253,0),
                                Color.FromArgb(255,255,254,0),
                                Color.FromArgb(255,255,255,0),
                                Color.FromArgb(255,255,255,0),
                                Color.FromArgb(255,254,254,1),
                                Color.FromArgb(255,253,254,1),
                                Color.FromArgb(255,252,254,1),
                                Color.FromArgb(255,251,253,2),
                                Color.FromArgb(255,251,253,2),
                                Color.FromArgb(255,250,252,3),
                                Color.FromArgb(255,249,252,3),
                                Color.FromArgb(255,248,252,3),
                                Color.FromArgb(255,247,251,4),
                                Color.FromArgb(255,247,251,4),
                                Color.FromArgb(255,246,250,5),
                                Color.FromArgb(255,245,250,5),
                                Color.FromArgb(255,244,250,5),
                                Color.FromArgb(255,243,249,6),
                                Color.FromArgb(255,243,249,6),
                                Color.FromArgb(255,242,248,7),
                                Color.FromArgb(255,241,248,7),
                                Color.FromArgb(255,240,248,7),
                                Color.FromArgb(255,239,247,8),
                                Color.FromArgb(255,239,247,8),
                                Color.FromArgb(255,238,247,8),
                                Color.FromArgb(255,237,246,9),
                                Color.FromArgb(255,237,246,9),
                                Color.FromArgb(255,236,245,10),
                                Color.FromArgb(255,235,245,10),
                                Color.FromArgb(255,234,245,10),
                                Color.FromArgb(255,233,244,11),
                                Color.FromArgb(255,233,244,11),
                                Color.FromArgb(255,232,243,12),
                                Color.FromArgb(255,231,243,12),
                                Color.FromArgb(255,230,243,12),
                                Color.FromArgb(255,230,242,13),
                                Color.FromArgb(255,229,242,13),
                                Color.FromArgb(255,228,242,13),
                                Color.FromArgb(255,227,241,14),
                                Color.FromArgb(255,227,241,14),
                                Color.FromArgb(255,226,240,15),
                                Color.FromArgb(255,225,240,15),
                                Color.FromArgb(255,224,240,15),
                                Color.FromArgb(255,223,239,16),
                                Color.FromArgb(255,223,239,16),
                                Color.FromArgb(255,222,238,17),
                                Color.FromArgb(255,221,238,17),
                                Color.FromArgb(255,220,238,17),
                                Color.FromArgb(255,219,237,18),
                                Color.FromArgb(255,219,237,18),
                                Color.FromArgb(255,218,236,19),
                                Color.FromArgb(255,217,236,19),
                                Color.FromArgb(255,216,236,19),
                                Color.FromArgb(255,215,235,20),
                                Color.FromArgb(255,215,235,20),
                                Color.FromArgb(255,214,234,21),
                                Color.FromArgb(255,213,234,21),
                                Color.FromArgb(255,213,234,21),
                                Color.FromArgb(255,212,233,22),
                                Color.FromArgb(255,211,233,22),
                                Color.FromArgb(255,210,233,22),
                                Color.FromArgb(255,209,232,23),
                                Color.FromArgb(255,209,232,23),
                                Color.FromArgb(255,208,231,24),
                                Color.FromArgb(255,207,231,24),
                                Color.FromArgb(255,206,231,24),
                                Color.FromArgb(255,205,230,25),
                                Color.FromArgb(255,205,230,25),
                                Color.FromArgb(255,204,229,26),
                                Color.FromArgb(255,203,229,26),
                                Color.FromArgb(255,202,229,26),
                                Color.FromArgb(255,201,228,27),
                                Color.FromArgb(255,201,228,27),
                                Color.FromArgb(255,200,227,28),
                                Color.FromArgb(255,199,227,28),
                                Color.FromArgb(255,198,227,28),
                                Color.FromArgb(255,197,226,29),
                                Color.FromArgb(255,197,226,29),
                                Color.FromArgb(255,196,226,29),
                                Color.FromArgb(255,195,225,30),
                                Color.FromArgb(255,195,225,30),
                                Color.FromArgb(255,194,224,31),
                                Color.FromArgb(255,193,224,31),
                                Color.FromArgb(255,192,224,31),
                                Color.FromArgb(255,191,223,32),
                                Color.FromArgb(255,191,223,32),
                                Color.FromArgb(255,190,222,33),
                                Color.FromArgb(255,189,222,33),
                                Color.FromArgb(255,188,222,33),
                                Color.FromArgb(255,187,221,34),
                                Color.FromArgb(255,187,221,34),
                                Color.FromArgb(255,186,220,35),
                                Color.FromArgb(255,185,220,35),
                                Color.FromArgb(255,184,220,35),
                                Color.FromArgb(255,183,219,36),
                                Color.FromArgb(255,183,219,36),
                                Color.FromArgb(255,182,219,36),
                                Color.FromArgb(255,181,218,37),
                                Color.FromArgb(255,181,218,37),
                                Color.FromArgb(255,180,217,38),
                                Color.FromArgb(255,179,217,38),
                                Color.FromArgb(255,178,217,38),
                                Color.FromArgb(255,177,216,39),
                                Color.FromArgb(255,177,216,39),
                                Color.FromArgb(255,176,215,40),
                                Color.FromArgb(255,175,215,40),
                                Color.FromArgb(255,174,215,40),
                                Color.FromArgb(255,173,214,41),
                                Color.FromArgb(255,173,214,41),
                                Color.FromArgb(255,172,213,42),
                                Color.FromArgb(255,171,213,42),
                                Color.FromArgb(255,171,213,42),
                                Color.FromArgb(255,170,212,43),
                                Color.FromArgb(255,169,212,43),
                                Color.FromArgb(255,168,212,43),
                                Color.FromArgb(255,167,211,44),
                                Color.FromArgb(255,167,211,44),
                                Color.FromArgb(255,166,210,45),
                                Color.FromArgb(255,165,210,45),
                                Color.FromArgb(255,164,210,45),
                                Color.FromArgb(255,163,209,46),
                                Color.FromArgb(255,163,209,46),
                                Color.FromArgb(255,162,208,47),
                                Color.FromArgb(255,161,208,47),
                                Color.FromArgb(255,160,208,47),
                                Color.FromArgb(255,159,207,48),
                                Color.FromArgb(255,159,207,48),
                                Color.FromArgb(255,158,206,49),
                                Color.FromArgb(255,157,206,49),
                                Color.FromArgb(255,156,206,49),
                                Color.FromArgb(255,155,205,50),
                                Color.FromArgb(255,155,205,50),
                                Color.FromArgb(255,154,205,50),
                                Color.FromArgb(255,153,204,51),
                                Color.FromArgb(255,152,203,51),
                                Color.FromArgb(255,151,202,50),
                                Color.FromArgb(255,149,202,50),
                                Color.FromArgb(255,148,201,49),
                                Color.FromArgb(255,146,200,49),
                                Color.FromArgb(255,145,199,48),
                                Color.FromArgb(255,144,198,48),
                                Color.FromArgb(255,143,197,48),
                                Color.FromArgb(255,141,196,47),
                                Color.FromArgb(255,140,195,47),
                                Color.FromArgb(255,139,194,46),
                                Color.FromArgb(255,137,194,46),
                                Color.FromArgb(255,136,193,45),
                                Color.FromArgb(255,134,192,45),
                                Color.FromArgb(255,133,191,44),
                                Color.FromArgb(255,132,190,44),
                                Color.FromArgb(255,131,189,44),
                                Color.FromArgb(255,129,188,43),
                                Color.FromArgb(255,128,187,43),
                                Color.FromArgb(255,127,186,42),
                                Color.FromArgb(255,125,186,42),
                                Color.FromArgb(255,124,184,41),
                                Color.FromArgb(255,122,184,41),
                                Color.FromArgb(255,121,183,40),
                                Color.FromArgb(255,120,182,40),
                                Color.FromArgb(255,119,181,40),
                                Color.FromArgb(255,118,180,39),
                                Color.FromArgb(255,116,179,39),
                                Color.FromArgb(255,115,178,38),
                                Color.FromArgb(255,113,178,38),
                                Color.FromArgb(255,112,177,37),
                                Color.FromArgb(255,111,176,37),
                                Color.FromArgb(255,109,175,36),
                                Color.FromArgb(255,108,174,36),
                                Color.FromArgb(255,107,173,36),
                                Color.FromArgb(255,106,172,35),
                                Color.FromArgb(255,104,171,35),
                                Color.FromArgb(255,103,170,34),
                                Color.FromArgb(255,101,170,34),
                                Color.FromArgb(255,100,169,33),
                                Color.FromArgb(255,98,168,33),
                                Color.FromArgb(255,97,167,32),
                                Color.FromArgb(255,96,166,32),
                                Color.FromArgb(255,95,165,32),
                                Color.FromArgb(255,93,164,31),
                                Color.FromArgb(255,92,163,31),
                                Color.FromArgb(255,91,162,30),
                                Color.FromArgb(255,89,162,30),
                                Color.FromArgb(255,88,160,29),
                                Color.FromArgb(255,86,160,29),
                                Color.FromArgb(255,85,159,28),
                                Color.FromArgb(255,84,158,28),
                                Color.FromArgb(255,83,157,28),
                                Color.FromArgb(255,81,156,27),
                                Color.FromArgb(255,80,155,27),
                                Color.FromArgb(255,79,154,26),
                                Color.FromArgb(255,77,154,26),
                                Color.FromArgb(255,76,152,25),
                                Color.FromArgb(255,74,152,25),
                                Color.FromArgb(255,73,151,24),
                                Color.FromArgb(255,72,150,24),
                                Color.FromArgb(255,70,149,23),
                                Color.FromArgb(255,69,148,23),
                                Color.FromArgb(255,68,147,23),
                                Color.FromArgb(255,67,146,22),
                                Color.FromArgb(255,65,145,22),
                                Color.FromArgb(255,64,144,21),
                                Color.FromArgb(255,62,144,21),
                                Color.FromArgb(255,61,143,20),
                                Color.FromArgb(255,59,142,20),
                                Color.FromArgb(255,58,141,19),
                                Color.FromArgb(255,57,140,19),
                                Color.FromArgb(255,56,139,19),
                                Color.FromArgb(255,54,138,18),
                                Color.FromArgb(255,53,137,18),
                                Color.FromArgb(255,52,136,17),
                                Color.FromArgb(255,50,136,17),
                                Color.FromArgb(255,49,135,16),
                                Color.FromArgb(255,47,134,16),
                                Color.FromArgb(255,46,133,15),
                                Color.FromArgb(255,45,132,15),
                                Color.FromArgb(255,44,131,15),
                                Color.FromArgb(255,42,130,14),
                                Color.FromArgb(255,41,129,14),
                                Color.FromArgb(255,40,128,13),
                                Color.FromArgb(255,38,128,13),
                                Color.FromArgb(255,37,126,12),
                                Color.FromArgb(255,35,126,12),
                                Color.FromArgb(255,34,125,11),
                                Color.FromArgb(255,33,124,11),
                                Color.FromArgb(255,32,123,11),
                                Color.FromArgb(255,31,122,10),
                                Color.FromArgb(255,29,122,10),
                                Color.FromArgb(255,28,120,9),
                                Color.FromArgb(255,26,120,9),
                                Color.FromArgb(255,25,119,8),
                                Color.FromArgb(255,24,118,8),
                                Color.FromArgb(255,22,117,7),
                                Color.FromArgb(255,21,116,7),
                                Color.FromArgb(255,20,115,7),
                                Color.FromArgb(255,19,114,6),
                                Color.FromArgb(255,17,113,6),
                                Color.FromArgb(255,16,112,5),
                                Color.FromArgb(255,14,112,5),
                                Color.FromArgb(255,13,111,4),
                                Color.FromArgb(255,11,110,4),
                                Color.FromArgb(255,10,109,3),
                                Color.FromArgb(255,9,108,3),
                                Color.FromArgb(255,8,107,3),
                                Color.FromArgb(255,6,106,2),
                                Color.FromArgb(255,5,105,2),
                                Color.FromArgb(255,4,104,1),
                                Color.FromArgb(255,2,104,1),
                                Color.FromArgb(255,1,102,0),
                                Color.FromArgb(255,0,102,0),
                                Color.FromArgb(255,0,102,0),
                                Color.FromArgb(255,0,102,0)};
            #endregion

            int x1 = GetPasswordScore() * 4;
            int x2 = x1 + 99;
            if (x2 >= 500) x2 = 499;

            return Colors[x2];
        }
    }
}