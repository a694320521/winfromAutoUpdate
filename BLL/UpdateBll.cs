using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace update
{
    
    public class UpdateBll : IUpdateBll
    {
        //ArchiveEncoding.Default = Encoding.GetEncoding("utf-8"); 压缩时带上这句，防止中文乱码
        private DataTable _ver_dt ;
        private Dictionary<string, string> _LocalDi=new Dictionary<string, string>();
        private Dictionary<string, string> _upFileDi=new Dictionary<string, string>() ;
        private Action<int,int> _ShowUpdateProgress;

        public Action<int,int> ShowUpdateProgress
        {
            get { return _ShowUpdateProgress; }
            set { _ShowUpdateProgress = value; }
        }

        /// <summary>
        /// 数据库里的版本DT
        /// </summary>
        public DataTable ver_dt
        {
            get { return _ver_dt; }
            set { _ver_dt = value; }
        }
        /// <summary>
        /// 本地版本文件信息
        /// </summary>
        public Dictionary<string, string> LocalDi
        {
            get { return _LocalDi; }
            set { _LocalDi = value; }
        }
        /// <summary>
        /// 要升级的文件信息
        /// </summary>
        public Dictionary<string, string> upFileDi
        {
            get { return _upFileDi; }
            set { _upFileDi = value; }
        }
        private string _conn;
        /// <summary>
        /// 连接字符串
        /// </summary>
        public string conn
        {
            get { return _conn; }
            set { _conn = value; }
        }

        public  String GetKey(String configPath, String key)
        {
            Configuration ConfigurationInstance = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
            {
                ExeConfigFilename = configPath
            }, ConfigurationUserLevel.None);


            if (ConfigurationInstance.AppSettings.Settings[key] != null)
                return ConfigurationInstance.AppSettings.Settings[key].Value;
            else

                return string.Empty;
        }
        public virtual string downLoadFile(string URL, string savePath)
        {
            string result = "S-OK";
            try
            {
                System.Net.HttpWebRequest httpRq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URL);
                httpRq.Timeout = 3000;

                httpRq.ContentType = "application/octet-stream";
                System.Net.HttpWebResponse httpRe = (System.Net.HttpWebResponse)httpRq.GetResponse();
                long totalBytes = httpRe.ContentLength;
                
                using (Stream st = httpRe.GetResponseStream())
                {
                    using (Stream so = new FileStream(savePath, FileMode.Create))
                    {
                        long totalDownloadedByte = 0;
                        byte[] by = new byte[1024];
                        int osize = st.Read(by, 0, (int)by.Length);
                        while (osize > 0)
                        {
                            
                            totalDownloadedByte = osize + totalDownloadedByte;
                            Application.DoEvents();
                            so.Write(by, 0, osize);
                            
                            ShowUpdateProgress((int)totalBytes, (int)totalDownloadedByte);//进度条
                            osize = st.Read(by, 0, (int)by.Length);
                        }
                    }

                }


            }
            catch (Exception ex)
            {
                result = ex.Message;
            }

            return result;
        }


        internal bool isUse(string filePath)
        {
            FileStream stream = null;
            try
            {
                stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch
            {

                return true;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();

                }
            }
        }

        public virtual bool isUpdate(string LocalPath,out string result)
        {
            //遍历
             
             result = getVer_Info();
            if (result != "S-OK")
            {

                return false;
            }
            result=comparisonVerInfo(LocalPath);
            if (result != "S-OK")
            {

                return false;
            }
            if(upFileDi.Count>0)
            return true;

            return false;

        }

        /// <summary>
        /// 比较版本信息确定是否更新
        /// </summary>
        /// <returns></returns>
        public virtual string comparisonVerInfo(string LocalPath)
        {
            string result = "S-OK";
            try
            {
                bool isupdate = false;
                
                foreach (DataRow dr in ver_dt.Rows)
                {
                    string fileName = dr["ver01"].ToString();
                    string filePath = LocalPath + @"\" + fileName;
                    result = getLocal_VerInfo(fileName, filePath);
                    if (result != "S-OK")
                    {
                        return result;
                    }
                }

                foreach (DataRow dr in ver_dt.Rows)
                {
                    if (LocalDi.ContainsKey(dr["ver01"].ToString()))
                    {
                        if (LocalDi[dr["ver01"].ToString()] == "ALL"|| LocalDi[dr["ver01"].ToString()]=="*")
                        {
                            upFileDi.Add(dr["ver01"].ToString(), dr["ver03"].ToString());
                        }
                        else
                        {
                            Version v1 = new Version(LocalDi[dr["ver01"].ToString()]);
                            Version v2 = new Version(dr["ver02"].ToString());
                            if (v2 != v1)//判断版本是否一直
                            {
                                isupdate = true;
                                upFileDi.Add(dr["ver01"].ToString(), dr["ver03"].ToString());

                            }
                        }
                    }
                }

                if (!isupdate)
                {
                    upFileDi.Clear();

                }
            }
            catch (Exception ex)
            {

                result = ex.Message;
            }
            

            return result;

        }

        public virtual string getVer_Info()
        {
            try
            {
                string sql = string.Format("select * from ver_file where ver01 not like '%.rar' or ver02 like '%.zip' ");
                 
                iwin.SqlData dba = new iwin.SqlData(conn);
                DataSet ds = dba.ReadTable(sql);
                ver_dt = ds.Tables[0];
                if (ver_dt.Rows.Count == 0)
                {
                    return "获取版本信息为空，请检查数据库是否有版本信息";
                }
                return "S-OK";
            }
            catch (Exception ex)
            {

                return $"获取版本信息失败！{ex.Message}";

            }

        }


        public virtual string getLocal_VerInfo(string fileName, string filePath)
        {
            try
            {
                //文件不存在，也得添加进去
                if (File.Exists(filePath))
                {
                    //if (util.isUse(filePath))
                    //{
                    //    return "文件被占用，请关闭系统重新登录";
                    //}
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Extension.ToLower() != ".dll"&& fileInfo.Extension.ToLower() != ".exe")
                    {
                        LocalDi.Add(fileName, "ALL");
                        return "S-OK";
                    }
                    System.Diagnostics.FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(filePath);
                    if (!LocalDi.ContainsKey(fileName))
                        LocalDi.Add(fileName, fileVersion.FileVersion);
                    return "S-OK";
                }
                LocalDi.Add(fileName, "*");
                return "S-OK";
            }
            catch (Exception ex)
            {

                return ex.Message;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SaveTempDirectoryPath"></param>
        /// <returns></returns>
        public virtual string saveAllFileZiptoTempPath( string SaveTempDirectoryPath,out string fileName)
        {
            string result = "S-OK";
            fileName = "";
            try
            {
                string sql = string.Format("select * from dbo.ver_file where ver01 like '%{0}' or ver02 like '%{1}' ", ".rar", ".zip");
                iwin.SqlData dba = new iwin.SqlData(conn);
                DataSet ds = dba.ReadTable(sql);
                if (ds==null||ds.Tables.Count == 0)
                {
                    return result="并未查询到总文件的信息，请检查服务端是否存放要下载的压缩文件";
                }
                DataTable dt = ds.Tables[0];
                 fileName = dt.Rows[0]["ver01"].ToString();
                string url = dt.Rows[0]["ver03"].ToString();
                if (string.IsNullOrWhiteSpace(url))
                    return "获取地址失败";
                string tempFile = SaveTempDirectoryPath +fileName;
                if (File.Exists(tempFile))
                    if (isUse(tempFile))
                    {
                        return result = "临时文件正在使用中，删除失败";
                    }
                    else
                    {
                        File.Delete(tempFile);
                    }
                result =downLoadFile(url, tempFile);
                if (result != "S-OK")
                {
                    return result;
                }

                return result;
            }
            catch (Exception ex)
            {

                return result=ex.Message;
            }
            
        }

        /// <summary>
        ///  (得再封装一层BLL，不是直接调用工具类)支持解压zip，rar
        /// </summary>
        /// <param name="filePath">待解压的文件路径</param>
        /// <param name="outFileDirectory">输出解压后文件的所在目录</param>
        /// <returns></returns>
        public virtual void Decompress(string filePath, string outFileDirectory)
        {
            try
            {
                var archive = ArchiveFactory.Open(filePath);

                foreach (var item in archive.Entries)
                {
                    if (!item.IsDirectory)
                    {
                        //Debug.WriteLine(entry.Key);
                        item.WriteToDirectory(outFileDirectory, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            
        }
        /// <summary>
        /// (得再封装一层BLL，不是直接调用工具类)支持解压zip，rar 带密码解压
        /// </summary>
        /// <param name="filePath">待解压的文件路径</param>
        /// <param name="outFileDirectory">输出解压后文件的所在目录</param>
        /// <param name="password">解压密码</param>
        public virtual void DecompressByPwd(string filePath, string outFileDirectory, string password)
        {
            try
            {
                using (var archive = ArchiveFactory.Open(filePath, new ReaderOptions { Password = password }))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            //Debug.WriteLine(entry.Key);
                            entry.WriteToDirectory(outFileDirectory, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                throw ex;
            }
            
        }


        // <summary>
        ///  (得再封装一层BLL，不是直接调用工具类)压缩（zip格式）
        /// </summary>
        /// <param name="fromFileDirectory">待压缩目录</param>
        /// <param name="outFilePath">压缩后文全件路径</param>
        public virtual void ZipCompress(string fromFileDirectory, string outFilePath,string password="")
        {
            //解决中文乱码问题
            SharpCompress.Common.ArchiveEncoding ArchiveEncoding = new SharpCompress.Common.ArchiveEncoding();
            ArchiveEncoding.Default = Encoding.GetEncoding("utf-8");
            SharpCompress.Writers.WriterOptions options = new SharpCompress.Writers.WriterOptions(CompressionType.Deflate);
            options.ArchiveEncoding = ArchiveEncoding;

            using (var archive = ZipArchive.Create())
            {
                archive.AddAllFromDirectory(fromFileDirectory);
                using (var zip = File.OpenWrite(outFilePath))
                    archive.SaveTo(zip, options);
            }
        }

        public virtual void SetDefaultConn(string Conn)
        {
            if (string.IsNullOrWhiteSpace(Conn))
                conn = GetKey("updateApp.config", "iwinSoure");
            else
                conn = Conn;
        }
    }
}
