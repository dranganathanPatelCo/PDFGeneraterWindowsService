using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Pechkin;
using log4net;
using System.Configuration;

namespace PDFGeneraterService
{
    public partial class PDFGeneraterService : ServiceBase
    {
        private Timer _timer;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public PDFGeneraterService()
        {

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            int TimerValue =Convert.ToInt32(ConfigurationManager.AppSettings.Get("TimerValue").ToString());

            _timer = new Timer(TimerValue * 60 * 1000); 
            _timer.Elapsed += new ElapsedEventHandler(OnTimerElapsed);
            _timer.AutoReset = true;
            _timer.Start();
        }

        protected override void OnStop()
        {
            _timer.Stop();
            _timer.Dispose();
        }
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            string DestinationFolderPath = ConfigurationManager.AppSettings.Get("DestinationFolderPath");
            using (DataTable tableName = new DataTable())
            {
                using (SqlConnection sqlcon = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConString"].ToString()))
                {
                    try
                    {
                        using (SqlCommand cmd = new SqlCommand("usp_GetLoanPayOffEmailPdf", sqlcon))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                da.Fill(tableName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error occured while retrieving the email list:{0}", ex);
                    }
                }
                if (tableName.Rows.Count > 1)
                {
                    try
                    {
                        foreach (DataRow row in tableName.Rows)
                        {
                            string LogoName = ConfigurationManager.AppSettings.Get("LogoName");
                            string ReplaceImagePath = ConfigurationManager.AppSettings.Get("ReplaceImagePath");
                            string RootPath = AppDomain.CurrentDomain.BaseDirectory.Replace(ReplaceImagePath, LogoName);
                            int MemberId = Convert.ToInt32(row["MemberId"].ToString());
                            int Id = Convert.ToInt32(row["Id"].ToString());
                            String Email = row["Email"].ToString();
                            string ToAddress = row["ToAddress"].ToString();
                            string HtmlPdf = Email.Replace("http://origins.patelco.org/staff_share/assets/images/Logo_Patelco.png", RootPath);
                            byte[] PdfBuffer = new SimplePechkin(new GlobalConfig()).Convert(HtmlPdf);
                            string FileName = MemberId + "_DemandLetter_" + DateTime.Now.ToString("MM_dd_yyyy") + ".pdf";
                            bool PDFGeneratedStatus;
                            PDFGeneratedStatus = ByteArrayToFile(DestinationFolderPath + FileName, PdfBuffer) ? true : false;

                            using (SqlConnection sqlConnectionCmdString = new SqlConnection(ConfigurationManager.ConnectionStrings["DBConString"].ToString()))
                            {
                                using (SqlCommand sqlRenameCommand = new SqlCommand("usp_UpdateLoanPayOffEmailPdf", sqlConnectionCmdString))
                                {
                                    sqlRenameCommand.CommandType = CommandType.StoredProcedure;
                                    sqlRenameCommand.Parameters.Add("@Id", SqlDbType.Int).Value = Id;
                                    sqlRenameCommand.Parameters.Add("@PDFGeneratedStatus", SqlDbType.Bit).Value = PDFGeneratedStatus;
                                    sqlConnectionCmdString.Open();
                                    sqlRenameCommand.ExecuteNonQuery();
                                    sqlConnectionCmdString.Close();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Error:", ex);
                    }
                }
            }
        }
        public static bool ByteArrayToFile(string fileName, byte[] byteArray)
        {
            try
            {
                using (FileStream _FileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    _FileStream.Write(byteArray, 0, byteArray.Length);
                    _FileStream.Close();
                    return true;
                }
            }
            catch (Exception _Exception)
            {
                log.Error("Exception caught in process while trying to save the  PDF: {0}", _Exception);
                return false;
            }

        }
    }
}
