using System.Globalization;
using System.IO;
using System.Windows.Forms;
using log4net.Appender;
using log4net.Core;

namespace log4net.Appender
{
    internal class TextBoxAppender : AppenderSkeleton
    {
        private delegate void UpdateTextDelegate(string text);

        private static TextBox textBox;

        public static TextBox TextBox
        {
            get { return textBox; }
            set { textBox = value; }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var stringWriter = new StringWriter(CultureInfo.CurrentCulture);
            Layout.Format(stringWriter, loggingEvent);
            UpdateText(stringWriter.ToString());
        }

        private static void UpdateText(string text)
        {
            if (textBox != null)
            {
                if (textBox.InvokeRequired)
                    textBox.Invoke((UpdateTextDelegate)UpdateText, new object[] { text });
                else
                    textBox.AppendText(text);
            }
        }
    }
}
