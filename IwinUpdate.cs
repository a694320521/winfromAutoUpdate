using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;


namespace update
{
    public partial class IwinUpdate : Form
    {

        IUpdateBll bll = new UpdateBll();


        private Timer time;
        private Timer outTime;
        private int count;
        private int successFileCount;
        private int errorFileCount;
        private int FileCount;
        private string LocalPath;
        private string tempPath;
        bool isLogin = false;
        bool isshow = false;
        public IwinUpdate(bool islogin = false)
        {
            InitializeComponent();
            this.isLogin = islogin;
        }

        private void IwinUpdate_Load(object sender, EventArgs e)
        {
            LocalPath = Application.StartupPath;
            tempPath = System.IO.Path.GetTempPath();
            bll.SetDefaultConn(null);
            successFileCount = 0;
            errorFileCount = 0;
            if (isLogin)
            {
                string result;
                if (bll.isUpdate(LocalPath, out result))
                {
                    
                    bll.ShowUpdateProgress = this.showUpdateProgress;
                    FileCount = bll.upFileDi.Count;
                    onStart();

                }
                else
                {
                    if (result != "S-OK")
                    {
                        MessageBox.Show(string.Format("升级出现以下问题：{0}", result), "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.ExitThread();
                        Application.Exit();
                    }
                        
                    else
                    {
                        MessageBox.Show("此版本已是最新版，无需更新！！", "提示", MessageBoxButtons.OK, MessageBoxIcon.None);
                        isshow = false;
                        processStart();
                    }

                }
            }
            else
            {
                bll.ShowUpdateProgress = this.showUpdateProgress;
                onStart();

            }

        }
        #region 计时器一些事件与方法
        private void onStart()
        {
            isshow = true;
            count = 5;
            time = new Timer();
            time.Interval = 1000;
            time.Enabled = true;
            time.Tick += new EventHandler(time_event);
            time.Start();
        }

        

        private void onOut()
        {

            count = 3;
            outTime = new Timer();
            outTime.Interval = 1000;
            outTime.Enabled = true;
            outTime.Tick += new EventHandler(outTime_event);
            outTime.Start();
        }
        private void time_event(object sender, EventArgs e)
        {
            if (isLogin)
            {
                time_Action(() =>
                {
                    foreach (var item in bll.upFileDi)
                    {
                        lbComple.Text = string.Format("正在更新{0}文件...", item.Key);
                        //删除本地文件
                        string file = LocalPath + "\\" + item.Key;
                        if (File.Exists(file))
                            File.Delete(file);


                        string result = bll.downLoadFile(item.Value, file);
                        if (result != "S-OK")
                        {
                            errorFileCount++;
                            ListB01.Items.Add(string.Format("{0}文件更新失败！，请求服务器错误！{1}", item.Key, result));
                        }
                        else
                        {
                            successFileCount++;
                            lbComple.Text = string.Format("{0}文件更新成功", item.Key);
                            ListB01.Items.Add(string.Format("{0}文件更新成功", item.Key));
                        }

                    }
                });
                
            }
            else
            {
                time_Action(() =>
                {
                    string fileName;
                    lbComple.Text = string.Format("正在下载总文件...");
                    string result = bll.saveAllFileZiptoTempPath(tempPath,out fileName);

                    if (result != "S-OK")
                    {
                        MessageBox.Show(result, "升级提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Application.ExitThread();
                        Application.Exit();
                    }
                    else
                    {
                        try
                        {
                            ListB01.Items.Add(string.Format("总文件{0}下载成功", fileName));
                            label2.Text = string.Format(string.Format("总文件{0}下载成功，正在解压总文件......", fileName));
                            string tempFile = tempPath + fileName;
                            bll.Decompress(tempFile, LocalPath);
                        }
                        catch (Exception ex)
                        {

                            MessageBox.Show(result, "升级提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            
                            Application.ExitThread();
                            Application.Exit();
                        }
                       

                    }
                });
                
            }





        }

        

        private void time_Action(Action Action_Event)
        {
            label2.Text = "正在更新中。。。";
            count--;
            if (count > 0)
            {
                label2.Text = string.Format("系统将在{0}秒进行更新,请稍等！。。。。。", count);
            }
            else
            {
                time.Stop();
                try
                {
                    Action_Event();
                    label2.Text = string.Format("更新完成,共升级{0}个文件，成功{1}文件，失败{2}文件！！", FileCount.ToString(), successFileCount.ToString(), errorFileCount.ToString());
                    if (FileCount != successFileCount)
                    {
                        MessageBox.Show("系统更新部分文件出现问题！如遇到部分失败文件，请重新打开升级程序，重新更新！", "升级提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    onOut();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("获取文件失败!" + ex.Message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    Application.Exit();
                }
            }
        }
        private void outTime_event(object sender, EventArgs e)
        {
            count--;
            if (count > 0)
            {
                label2.Text = string.Format("系统将在{0}秒进行更新,请稍等！。。。。。", count);
            }
            else
            {
                time.Dispose();
                outTime.Stop();
                isshow = false;
                this.Close();
                outTime.Dispose();
                processStart();
               
            }
        }

        #endregion

        #region 进度条与启动程序方法
        private void showUpdateProgress(int totalBytes, int totalDownloadedByte)
        {
            
                this.progress.Maximum = totalBytes;
                this.progress.Value = totalDownloadedByte;
            
        }
      


        private void processStart()
        {
            Process.Start("iwin.exe");
            
            this.Dispose();
        }



        #endregion

        private void IwinUpdate_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isshow)
            {
                if (MessageBox.Show("是否要关闭升级程序？", "询问", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
                {
                    e.Cancel = true;

                }
            }
            else
            {
                e.Cancel = false;
            }


        }
    }
}
