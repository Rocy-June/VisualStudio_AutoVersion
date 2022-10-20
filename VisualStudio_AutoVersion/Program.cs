using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VisualStudio_AutoVersion
{
    internal class Program
    {
        static int Main(string[] args)
        {
            /* 
             *  Program return code:
             *        0:    Normal exit.
             *        
             *       -1:    Startup arguments is not correct.
             *       -2:    Properties path is not exsit.
             *       -3:    AssemblyInfo.cs is not exsit.
             *       -4:    Could not find the version code in AssemblyInfo.cs 
             *                  and generate action has been canceled.
             *       
             *      -99:    Unknown error occurred.
             *      
             * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

            Properties.Resources.Culture = Thread.CurrentThread.CurrentCulture;

            if (args.Length != 1)
            {
                MessageBox.Show(
                    Properties.Resources.Initialize_ArgumentCountNotInRule,
                    Properties.Resources.MessageBox_Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return -1;
            }

            var propertiesPath = args[0];
            if (!Directory.Exists(propertiesPath))
            {
                MessageBox.Show(
                    Properties.Resources.Initialize_PropertiesPathNotExsit,
                    Properties.Resources.MessageBox_Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return -2;
            }

            var filePath = Path.Combine(propertiesPath, @"Properties\AssemblyInfo.cs");
            if (!File.Exists(filePath))
            {
                MessageBox.Show(
                    Properties.Resources.Initialize_AssemblyInfoFileNotExsit,
                    Properties.Resources.MessageBox_Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return -3;
            }

            try
            {
                string fileContent;
                using (var sr = new StreamReader(filePath))
                {
                    fileContent = sr.ReadToEnd();
                }

                var revision = 1;
                var date = DateTime.Now;
                var majorMinorVersion = $"{date:yy}.{date:Mdd}";

                var regex = new Regex(@"(?<=\[assembly:.+Assembly.*?\("")\d+\.\d+\.\d+\.\d+(?=""\)\])", RegexOptions.Multiline);
                var matches = regex.Matches(fileContent);
                if (matches.Count == 0)
                {
                    var writeNewFlag = MessageBox.Show(
                        Properties.Resources.RegexMatch_NoMatchWriteNew,
                        Properties.Resources.MessageBox_Tips,
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Information);
                    switch (writeNewFlag)
                    {
                        case MessageBoxResult.Cancel:
                            return -4;
                        case MessageBoxResult.Yes:
                            fileContent += $@"
[assembly: AssemblyVersion(""{{{{$$replacement$$}}}}"")]
[assembly: AssemblyFileVersion(""{{{{$$replacement$$}}}}"")]";
                            break;
                        case MessageBoxResult.No:
                            break;
                        default:
                            return -99;
                    }
                }
                else if (matches[0].Value.IndexOf(majorMinorVersion) >= 0)
                {
                    var nowVersionCode = matches[0].Value;
                    var nowRevisionStr = nowVersionCode.Split('.')[3];
                    var revisionConvertFlag = int.TryParse(nowRevisionStr, out revision);
                    revision = revisionConvertFlag
                        ? ++revision
                        : 1;
                }

                var fullVersion = $"{majorMinorVersion}.{date:Hmm}.{revision}";
                fileContent = regex.Replace(fileContent, fullVersion);
                fileContent = fileContent.Replace("{{$$replacement$$}}", fullVersion);
                fileContent = fileContent.TrimEnd();

                using (var sw = new StreamWriter(filePath, false))
                {
                    sw.WriteLine(fileContent);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{ex.Message}\r\n\r\n{ex.StackTrace}",
                    Properties.Resources.MessageBox_Error,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return -99;
            }

            return 0;
        }
    }
}
