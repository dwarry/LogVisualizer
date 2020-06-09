using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace LogVisualizer.DialogServices
{
    public interface IDialogServices
    {
        /// <summary>
        /// Shows the Open File dialog and returns the path-name of the chosen file,
        /// or null if the dialog was cancelled. 
        /// </summary>
        /// <returns></returns>
        string ShowOpenFileDialog();

    }

    public class WpfDialogServices : IDialogServices
    {
        public string ShowOpenFileDialog()
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Select Log File",
                CheckFileExists = true,
                Multiselect = false,
                DereferenceLinks = true
            };

            dialog.Filter = "Delimited text log file|*.txt;*.log|Windows Event Log XML Export|*.xml";

            return dialog.ShowDialog().GetValueOrDefault()
                ? dialog.FileName
                : null;
        }
    }
}
