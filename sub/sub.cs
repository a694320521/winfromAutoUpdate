using System;
using System.Collections.Generic;
/*
 * 以下为使用SQL,XML IO引用 
 */
using System.Data.Sql;
using System.Data;
using System.Data.SqlClient;
using System.Xml;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;



using System.Data.OleDb;
using System.Data.ProviderBase;
/*
 * 以下定义为EXCEL引用
 */
using System.Reflection;

/*
 * 以下是自动发邮件的接口 
 */
using System.Net.Mail;
using System.Net.Mime;

/*
 * 以下为DES5加密算法引用
*/
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace iwin
{
    /// <summary>
    /// 数据库操作基础类(MS SQL)
    /// 在有事务的方法中,在捕获导常代码中,回滚事务,以避免出现未提交的事务而造成数据死锁.
    /// </summary>
    public class SqlData//读写数据库
    {
        //成员变量
        #region 
        private SqlConnection thisConnection=null;
        private SqlDataAdapter thisAdapter=null;
        private DataSet theDataSet = null;
        private SqlTransaction theTrant = null;
        private string _connect = null;
        private string err_msg;

        struct Tparam
        {
            public Tparam(string str1, string str2)
            {
                parames = str1;
                op = str2;
            }
            public string parames;
            public string op;
        }
        #endregion

        //属性
        /// <summary>
        /// 获得或设置SqlDataAdapter对象
        /// </summary>
        public SqlDataAdapter Adapter
        {
            get { return this.thisAdapter ;}
            set { thisAdapter = value; }
        }

        /// <summary>
        /// 返回最后的错误
        /// </summary>
        public string GetLastError
        {
            get { return err_msg; }
        }

        /// <summary>
        /// 获得或设置连接对象
        /// </summary>
        public SqlConnection connect
        {
            get { return thisConnection; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Contstr"></param>
        public SqlData(string Contstr)//带参数构造函数,并打开连接
        {
            try
            {
                _connect = Contstr;
                thisConnection = new SqlConnection(Contstr);
                thisConnection.Open();
            }
            catch (SqlException e)
            {
                err_msg = e.Message;
            }
        }

        /// <summary>
        /// 关闭连接,当对象使用完成时,或出现错误时调用.关闭当前连接.防止该连接有死锁或未提交的事务造成数据库瓶颈
        /// 该函数会回滚事务
        /// </summary>
        public void  SqlClose()
        {
            if (thisConnection.State == ConnectionState.Open)
            {
                thisConnection.Close();
            }
            RollBackTrant();  
        }

        /// <summary>
        /// 构造函数 关闭连接
        /// </summary>

        /// <summary>
        /// 读取指定SQL返回记录集,失败返回null
        /// </summary>
        /// <param name="SQL">查询SQL</param>
        /// <returns>记录集结果</returns>
        public DataSet ReadTable(string SQL)//读存指定SQL语句的数据返回DataSet对象
        {
            theDataSet = new DataSet();
            if (thisConnection.State != ConnectionState.Open)
            {
                err_msg = "connection error place check oebject";
            }
            else
            {
                try
                {
                    thisAdapter = new SqlDataAdapter();
                    thisAdapter.SelectCommand = new SqlCommand(SQL, thisConnection);
                    thisAdapter.Fill(theDataSet);
                }
                catch (SqlException es)
                {
                    err_msg = es.Message;
                    theDataSet = null;
                }
                catch (InvalidOperationException e)
                {
                    err_msg = e.Message;
                    theDataSet = null;
                }
            }
            return theDataSet;
        }

        /// <summary>
        /// 不再使用
        /// </summary>
        /// <param name="thisAdapter"></param>
        /// <param name="theDataSet"></param>
        /// <returns></returns>
        public static bool UpdateTable(SqlDataAdapter thisAdapter,DataSet theDataSet)
        {
            if (thisAdapter == null)
            {
                System.Windows.Forms.MessageBox.Show("未初始化SqlDataAdapter对象故无法更新");
                return false;
            }
            try
            {
                SqlCommandBuilder Builder = new SqlCommandBuilder(thisAdapter);
                thisAdapter.Update(theDataSet);
                
            }
            catch (SqlException er)
            {
                System.Windows.Forms.MessageBox.Show(er.Message);
                return false;
            }
            catch (InvalidOperationException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// 返回指定SQL读取到的记录集,失败返回空
        /// </summary>
        /// <param name="SQL">查询SQL语句</param>
        /// <returns>返回DataSet记录集</returns>
        public DataSet SeleteTable(string SQL)
        {
            theDataSet = new DataSet();
            try
            {
                thisAdapter = new SqlDataAdapter();
                if (theTrant != null)
                {
                    thisAdapter.SelectCommand = new SqlCommand(SQL, thisConnection, theTrant);
                }
                else
                {
                    thisAdapter.SelectCommand = new SqlCommand(SQL, thisConnection);
                }
                thisAdapter.Fill(theDataSet);
            }
            catch (SqlException e)
            {
                err_msg = e.Message;
                return null;
            }
            return theDataSet;
        }
        /*
         * 使用C#的自动更新对象SqlCommandBuilder是对DS进行一行一行执行(插入 删除 修改)只要当中有一行
         * 出现问题就会引异常.
         * 为了不使用户操作中断就需要用ReDataSet把成功操作的数据重新更新到DS中.同时该操作会刷新绑定的
         * DataGridView对象.会将下面未成功保存的数据全部清空.
         * ReDataSet就是为了解决这个
         */
        /// <summary>
        /// 重新填充DS对象.保存成功的数据.清除错误.出错行以下的修改将全部取消
        /// </summary>
        public void ReDataSet()
        {
            if (thisAdapter != null && theDataSet != null)
            {
                theDataSet.Clear();
                thisAdapter.Fill(theDataSet);
            }
        }

        /// <summary>
        /// 为该连接开启事务
        /// </summary>
        public void BeginTrant()
        {
            theTrant = thisConnection.BeginTransaction();
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void CommintTrant()
        {
            if (theTrant != null)
            {
                theTrant.Commit();
                theTrant = null;
            }
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void RollBackTrant()
        {
            if (theTrant != null)
            {
                theTrant.Rollback();
                theTrant = null;
            }
        }

        /// <summary>
        /// 动态更新DataGridView到数据库 与SelectTable(string SQL)配合使用
        /// 成功返回TRUE 失败返回FALSE
        /// </summary>
        /// <returns>成功返回TRUE 失败返回FALSE</returns>
        public bool Updatedata()
        {
            if (thisAdapter == null)
            {
                err_msg = "未初始化SqlDataAdapter对象故无法更新";
                return false;
            }
            try
            {
                SqlCommandBuilder Builder = new SqlCommandBuilder(thisAdapter);
                thisAdapter.Update(theDataSet);
            }
            catch (SqlException er)
            {
                err_msg = er.Message;
                return false;

            }
            catch (InvalidOperationException e)
            {
                err_msg = e.Message;
                return false;
            }
            return true;
        }

        /// <summary>
        /// 读存指定行的值,列的数据.无数据或超过行数返回NULL
        /// </summary>
        /// <param name="Sql">读数据的SQL语句</param>
        /// <param name="col">读存以1开始的行数</param>
        /// <param name="row">要读存的列名</param>
        /// <returns>返回读取到的值,出错返回null</returns>
        public string ReadRows(string Sql, int col,string row)
        {
            string val = null;
            SqlCommand cmd = null;
            if (theTrant != null)
            {
                cmd= new SqlCommand(Sql, thisConnection,theTrant);
            }
            else
            {
                 cmd= new SqlCommand(Sql, thisConnection);
            }

            try
            {
                SqlDataReader DataReader = cmd.ExecuteReader();
                for (int i = 0; i < col; i++)
                {
                    if (!DataReader.Read())
                    {
                        err_msg = "读取数据错误";
                        return null;
                    }
                }
                val=DataReader[row].ToString();
                DataReader.Close();
                return val;
            }
            catch (SqlException er)
            {
                err_msg = er.Message;
            }
            catch (ArgumentException ae)
            {
                err_msg = ae.Message;
            }
            catch (InvalidOperationException e)
            {
                err_msg = e.Message;
            }
            catch (IndexOutOfRangeException e)
            {
                err_msg = e.Message;
            }
            return null;
        }

        /// <summary>
        /// 取指定SQL获得的第一行数据,失败返回null
        /// </summary>
        /// <param name="SQL">SQL语句</param>
        /// <param name="item">要取几个数据</param>
        /// <returns>返回一个大小为item个的string数组</returns>
        public object[] ReadRows(string SQL,int item)
        {
            SqlCommand theCommand = null;
            SqlDataReader theReader = null;
            try
            {
                if (theTrant != null)
                {
                    theCommand = new SqlCommand(SQL, thisConnection, theTrant);
                }
                else
                {
                    theCommand = new SqlCommand(SQL, thisConnection);
                }
                theReader = theCommand.ExecuteReader();
                if (theReader.Read())
                {
                    object[] vals = new object[item];
                    for (int i = 0; i < item; i++)
                    {
                        vals[i] = theReader[i];
                    }
                    theReader.Close();
                    return vals;
                }
            }
            catch (SqlException e)
            {
                err_msg = e.Message;
            }
            catch (IndexOutOfRangeException e)
            {
                err_msg = e.Message;
            }
            return null;
 
        }

        /// <summary>
        /// 查询指定数据是否已存在数据库中.成功:返回行数.失败返回-1
        /// 参数SQL必须以select count(*)开头用来统计行数.
        /// </summary>
        /// <param name="sql">以select count(*) 开头的查询语句</param>
        /// <returns></returns>
        public int ReadCount(string sql)
        {
            if (!sql.Contains("count(*)") && !sql.Contains("COUNT(*)"))
            {
                err_msg = "SQL语句格式错误";
                return -1;
            }
            SqlCommand Command = null;
            SqlDataReader Reader = null;
            int Count = 0;
            try
            {
                if (this.theTrant == null)
                {
                    Command = new SqlCommand(sql,thisConnection);
                }
                else
                {
                    Command = new SqlCommand(sql,thisConnection,theTrant);
                }
                Reader = Command.ExecuteReader();
                if (Reader.Read())
                {
                    Count = Convert.ToInt32(Reader[0].ToString());
                }
                Reader.Close();
            }
            catch (OleDbException e)
            {
                err_msg = e.Message;
                return -1;
            }
            return Count;

        }
       
        /// <summary>
        /// 读一行数据.如果查询不到数据则返回null 已过时有GetRows取代
        /// </summary>
        /// <param name="SQL">传入要查询的语句</param>
        /// <param name="vals">用于接收的string数组</param>
        public void ReadRows(string SQL,ref string[] vals)
        {
            SqlCommand theCommand = null;
            SqlDataReader theReader=null;
            try
            {
                if (theTrant != null)
                {
                    theCommand = new SqlCommand(SQL, thisConnection, theTrant);
                }
                else
                {
                    theCommand = new SqlCommand(SQL, thisConnection);
                }
                theReader = theCommand.ExecuteReader();
                if (theReader.Read())
                {
                    for (int i = 0; i < vals.Length; i++)
                    {
                        vals[i] = theReader[i].ToString();
                    }
                }
                else
                {
                    vals = null;
                }
                theReader.Close();
            }
            catch (SqlException e)
            {
                err_msg = e.Message;
                vals = null;
            }
            catch (IndexOutOfRangeException e)
            {
                err_msg = e.Message;
                vals = null;
            }
        }

        /// <summary>
        /// 获得一行数量 返回一维数组(string[]).如果有多行则返回第一行
        /// </summary>
        /// <param name="SQL">查询SQL语句.却保只能查到一行数据,否则将返回第一行数据</param>
        /// <returns></returns>
        public string[] GetRows(string SQL)
        {
            SqlCommand theCommand = null;
            SqlDataReader theReader = null;
            string[] vals;
            try
            {
                if (theTrant != null)
                {
                    theCommand = new SqlCommand(SQL, thisConnection, theTrant);
                }
                else
                {
                    theCommand = new SqlCommand(SQL, thisConnection);
                }
                theReader = theCommand.ExecuteReader();  //读取数量

                int fct = theReader.FieldCount;   //当前列数
                vals = new string[fct];
                if (theReader.Read())
                {
                    for (int i = 0; i < vals.Length; i++)
                    {
                        vals[i] = theReader[i].ToString();
                    }
                }
                else
                {
                    vals = null;
                }
                theReader.Close();
            }
            catch (SqlException e)
            {
                err_msg = e.Message;
                vals = null;
            }
            catch (IndexOutOfRangeException e)
            {
                err_msg = e.Message;
                vals = null;
            }
            return vals;
        }

        /// <summary>
        /// 查询指定SQL的数据
        /// </summary>
        /// <param name="SQL">要查询的SQL语句</param>
        /// <returns>返回记录集</returns>
        public SqlDataReader ReadRows(string SQL)
        { 
            SqlCommand theCommand = null;
            SqlDataReader theReader=null;
            try
            {
                if (theTrant != null)
                {
                    theCommand = new SqlCommand(SQL, thisConnection, theTrant);
                }
                else
                {
                    theCommand = new SqlCommand(SQL, thisConnection);
                }
                theReader = theCommand.ExecuteReader();
                return theReader;
            }
            catch (SqlException e)
            {
                err_msg = e.Message;
                theReader.Close();
                
            }  
            return null;
        }

        /// <summary>
        /// 执行T-SQL语句 UPDATE DELETE INSERT
        /// 失败返回-1 成功返回受影响的行数
        /// </summary>
        /// <param name="sql">查执行的T-SQL语句,Insert Update Delete</param>
        /// <returns>失败返回-1 成功返回受影响的行数</returns>
        public int ExeCute(string sql)
        {
            if (thisConnection.State  != ConnectionState.Open)
            {
                err_msg = "数据库连接错误,无法初始化对象";
                return -1;
            }
            SqlCommand theCommand = null;
            if (theTrant != null)
            {
                theCommand = new SqlCommand(sql, thisConnection, theTrant);
            }
            else
            {
                theCommand=new SqlCommand(sql, thisConnection);
            }
            try
            {
                int retult = theCommand.ExecuteNonQuery();
                return retult;
            }
            catch (SqlException e)
            {
                err_msg = e.Message;
                return -1;
            }
        }

        /// <summary>
        /// 执行存储过程返回存储过程中的return值. output为存储过程中的output转出值.可为空
        /// </summary>
        /// <param name="procname">存储过程名</param>
        /// <param name="output">输出参数</param>
        /// <param name="paraValues">传入参数</param>
        /// <returns>返回受影响的行数,出错返回null</returns>
        public string ExecProc(string procname,ref List<string> output,params object[] paraValues)
        {
            SqlCommand cmd = null;
            string ret = null;
            try
            {
                if (theTrant != null)
                {
                    cmd = new SqlCommand(procname, thisConnection, theTrant);
                }
                else
                {
                    cmd = new SqlCommand(procname, thisConnection);
                }
                cmd.CommandType = CommandType.StoredProcedure;

                List<Tparam> param = GetParas(procname);
                SqlParameter pam = null;
                cmd.Parameters.Add("@RETURN_VALUE",SqlDbType.VarChar).Direction = ParameterDirection.ReturnValue;
                for (int i = 0; i < param.Count; i++)        //设置参数 
                {
                    pam= cmd.Parameters.AddWithValue(param[i].parames, paraValues[i]);
                    if (param[i].op == "1") //为输出参数
                    {
                        pam.Direction = ParameterDirection.Output;
                    }
                }
                cmd.ExecuteNonQuery(); //执行存储过程
                
                for (int i = 0; i < param.Count; i++)   //取输出参数
                {
                    if (param[i].op == "1") 
                    {
                        output.Add(cmd.Parameters[param[i].parames].Value.ToString()); 
                    }
                }
                ret = cmd.Parameters["@RETURN_VALUE"].Value.ToString();
            }
            catch (SqlException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return null;
            }
            catch (IndexOutOfRangeException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
            cmd.Clone();
            return ret;
        }

        /// <summary>
        /// 执行SQL存储过程
        /// </summary>
        /// <param name="procname">存储过程名</param>
        /// <param name="paraValues">传入的参数</param>
        /// <returns>返回受影响的行数,错误返回null</returns>
        public string ExecProc(string procname, params object[] paraValues)
        {
            SqlCommand cmd = null;
            string ret = null;
            try
            {
                if (theTrant != null)
                {
                    cmd = new SqlCommand(procname, thisConnection, theTrant);
                }
                else
                {
                    cmd = new SqlCommand(procname, thisConnection);
                }
                cmd.CommandType = CommandType.StoredProcedure;

                List<Tparam> param = GetParas(procname);
                SqlParameter pam = null;
                cmd.Parameters.Add("@RETURN_VALUE", SqlDbType.VarChar).Direction = ParameterDirection.ReturnValue;
                for (int i = 0; i < param.Count; i++)        //设置参数 
                {
                    pam = cmd.Parameters.AddWithValue(param[i].parames, paraValues[i]);
                }
                cmd.ExecuteNonQuery(); //执行存储过程
                ret = cmd.Parameters["@RETURN_VALUE"].Value.ToString();
            }
            catch (SqlException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return null;
            }
            catch (IndexOutOfRangeException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
            cmd.Clone();
            return ret;
        }

        /// <summary>
        /// 动态设置存储过程的参数.由ExecProc调用.后面的实体参数不可传入null
        /// </summary>
        /// <param name="procname">存储过程名</param>
        /// <returns>返回存储过程的参数列表</returns>
        private List<Tparam> GetParas(string procname)
        {

            List<Tparam> parm = new List<Tparam>();
            string SQL = string.Format("select name,isoutparam from syscolumns where id =(select id from sysobjects where name='{0}')", 
                procname);
            SqlCommand cmd = null;
            try
            {
                if (theTrant != null)   //对象是否已开启事务
                {
                    cmd = new SqlCommand(SQL, thisConnection, theTrant);
                }
                else
                {
                    cmd = new SqlCommand(SQL, thisConnection);
                }
                SqlDataReader DataReader = cmd.ExecuteReader();
                Tparam val;
                while (DataReader.Read())
                {
                  //  vals = vals + DataReader[0].ToString() + ",";
                    val.parames = DataReader[0].ToString();
                    val.op = DataReader[1].ToString();
                    parm.Add(val);   
                }
                DataReader.Close();
                cmd.Clone();
            }
            catch (SqlException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
            return parm;
        }

    }
    /// <summary>
    /// 读写Xml文件
    /// </summary>
    public class XmlRW
    {
        /// <summary>
        /// Xml文件决定地址
        /// </summary>
        private string XmlFile;

        /// <summary>
        /// //构造函数,确认xml文件是否存在
        /// </summary>
        /// <param name="_xmlfile">Xml文件名+地址</param>
        public XmlRW(string _xmlfile) 
        {
            XmlFile = _xmlfile;
        }

        /// <summary>
        ///读取指定的节点的值
        /// </summary>
        /// <param name="node">节点名</param>
        /// <returns>节点值</returns>
        public string XmlRead(string node) 
        {
            if (!File.Exists(XmlFile))
            {
                System.Windows.Forms.MessageBox.Show("配置文件不存在,请检查!");
            }
            else
            {
                XmlDocument read = new XmlDocument();
                read.Load(XmlFile);
                XmlNode root = read.SelectSingleNode("Servers");
                XmlNodeList nlist = root.ChildNodes;
                foreach (XmlNode final in nlist)
                {     
                    if (final.Name == node) 
                    {
                        return final.InnerText;                 
                    }  
                }   
            }
            return null;
        }

        /// <summary>
        /// 改写指定节点的值
        /// </summary>
        /// <param name="node">节点名</param>
        /// <param name="val">要更改的节点值</param>
        /// <returns>成功返回true,失败返回false</returns>
        public bool XmlWrite(string node, string val)
        {
            if (!File.Exists(XmlFile))
            {
                System.Windows.Forms.MessageBox.Show("配置文件不存在,请检查!");
                return false;
            }
            else
            {
                XmlDocument read = new XmlDocument();
                read.Load(XmlFile);
                XmlNode root = read.SelectSingleNode("Servers");
                XmlNodeList nlist = root.ChildNodes;
                foreach (XmlNode final in nlist)
                {
                    if (final.Name == node)
                    {
                        final.InnerText = val;
                        read.Save(XmlFile);
                        return true;
                    }
                }
                return false;
                
            }
        }

        /// <summary>
        /// 获得或设置文件地址(含文件名)
        /// </summary>
        public string FileName
        {
            get { return XmlFile; }
            set { XmlFile = value; }
        }
    }

    /// <summary>
    /// 实现邮件接口
    /// </summary>
    public class SendMail
    {
        private string _Smtp;           //stmp 服务器
  //      private string _Pop3;           //pop3 服务器
        private string _UserName;       //登陆用户名
        private string _PassWorld;      //登陆密码
    /*  private string Address;         //收件人地址 
        private string ccs;             //超送
        private string SubJect;         //主题
        private string TxtBoby;         //正文
        private string FileName;        //附件  */

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public SendMail()
        {
            InitMail();
        }

        /// <summary>
        /// 读存程序根目录下的config.xml获得默认设置
        /// </summary>
        private void InitMail()
        {
            XmlRW xml = new XmlRW(System.AppDomain.CurrentDomain.BaseDirectory + "\\config.xml");
            _Smtp = xml.XmlRead("smtp");
            _UserName = xml.XmlRead("username");
            _PassWorld = xml.XmlRead("password");
 
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="address">收件人</param>
        /// <param name="cc">抄送</param>
        /// <param name="SubJect">主题</param>
        /// <param name="txtBoay">正文</param>
        /// <param name="fileName">附件</param>
        /// <returns>成功返回1 返回非1请查错误代码</returns>
        public int Send(string address, string cc, string SubJect, string txtBoay, string fileName) //发送邮件
        {
            if (_UserName == null)
                return -11;      //发件人为空
            if (_Smtp == null)
                return -12;      //smtp服务器为空
            try
            {
                MailAddress from = new MailAddress("s_hhm@frae.com", "赵文涛"); //邮件的发件人
                MailMessage mail = new MailMessage();

                //设置邮件的标题
                mail.Subject = SubJect;

                //设置邮件的发件人
                //Pass:如果不想显示自己的邮箱地址，这里可以填符合mail格式的任意名称，真正发mail的用户不在这里设定，这个仅仅只做显示用
                mail.From = from;
                //设置邮件的收件人
                /*  这里这样写是因为可能发给多个联系人，每个地址用 ; 号隔开 
                 * 一般从地址簿中直接选择联系人的时候格式都会是 ：用户名1 < mail1 >; 用户名2 < mail 2>; 
                 * 因此就有了下面一段逻辑不太好的代码
                 * 如果永远都只需要发给一个收件人那么就简单了 mail.To.Add("收件人mail");
                 */
                //设置邮件的抄送收件人
                mail.To.Add(new MailAddress(address, ""));
                //这个就简单多了，如果不想快点下岗重要文件还是CC一份给领导比较好
                if (string.IsNullOrEmpty(cc))
                {
                    mail.CC.Add(new MailAddress(cc, ""));
                }//设置邮件的内容
                mail.Body = txtBoay;

                //设置邮件的格式
                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.IsBodyHtml = true;
                //设置邮件的发送级别
                mail.Priority = MailPriority.Normal;
                if (File.Exists(fileName))
                {
                    //构造一个附件对象
                    Attachment attach = new Attachment(fileName);
                    //得到文件的信息
                    /*  ContentDisposition disposition = attach.ContentDisposition;
                      disposition.CreationDate = System.IO.File.GetCreationTime(fileName);
                      disposition.ModificationDate = System.IO.File.GetLastWriteTime(fileName);
                      disposition.ReadDate = System.IO.File.GetLastAccessTime(fileName);*/
                    //向邮件添加附件
                    mail.Attachments.Add(attach);
                }
                else
                {
                    return -2;   //所设置的附件不存在
                }
                mail.DeliveryNotificationOptions = DeliveryNotificationOptions.OnSuccess;
                SmtpClient client = new SmtpClient();
                //设置用于 SMTP 事务的主机的名称，填IP地址也可以了
                client.Host = _Smtp;
                //设置用于 SMTP 事务的端口，默认的是 25
                //client.Port = 25;
                client.UseDefaultCredentials = false;
                //这里才是真正的邮箱登陆名和密码
                client.Credentials = new System.Net.NetworkCredential(_UserName,_PassWorld);
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Send(mail);
                this.SvaeLog(SubJect,address);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return 0;
            }
            return 1;

        }
       
        /// <summary>
        /// 写日志文件.发送成功后,记录收件人与正文件内容
        /// </summary>
        /// <param name="subject">正文</param>
        /// <param name="address">收件人</param>
        private void SvaeLog(string subject,string address)
        {
            string LogPath = System.Environment.CurrentDirectory + "\\log.txt";
            try
            {
                StreamWriter sw = File.AppendText(LogPath);
                DateTime dt = System.DateTime.Now;
                sw.WriteLine("{0}:{1}:{2} {3}-{4}-{5} 主题:{6} 收件人:{7}", dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second,subject,address);
                sw.Close();
            }
            catch (IOException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
        }
    }

    /// <summary>
    /// DES加密类
    /// </summary>
    public class DES
    {
        private Byte[] _IV = null;   //8位的钥匙  可为数字
        private Byte[] _KEY = null;  //8位加密Key 可为字母 

        /// <summary>
        /// 设置IV
        /// </summary>
        public Byte[] IV
        {
            get { return _IV; }
         //   set { _IV = value; }
        }

        /// <summary>
        /// 设置KEY
        /// </summary>
        public Byte[] KEY
        {
            get { return _KEY;}
          //  set { _KEY = value;}
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="key">加密KEY</param>
        /// <param name="iv">加密IV</param>
        public DES(string key, string iv)
        {
            _IV = System.Text.Encoding.UTF8.GetBytes(iv.Substring(0, 8));
            _KEY = System.Text.Encoding.UTF8.GetBytes(key.Substring(0, 8));
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        /// <param name="strText">被解密的字符串</param>
        /// <returns>返回原字符串</returns>
        public String Decrypt(String strText)
        {
        //   Byte[] byKey = { };
        //    Byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
            Byte[] inputByteArray = new byte[strText.Length];
            try
            {
         //       byKey = System.Text.Encoding.UTF8.GetBytes(sDecrKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                inputByteArray = Convert.FromBase64String(strText);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(_KEY, _IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                System.Text.Encoding encoding = System.Text.Encoding.UTF8;
                return encoding.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// 加密字符串
        /// </summary>
        /// <param name="strText">待加密的字符串</param>
        /// <returns>加密后的字符串</returns>
        public string Encrypt(string strText)
        {
      //      Byte[] byKey = { };
       //     Byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
            try
            {
       //         byKey = System.Text.Encoding.UTF8.GetBytes(strEncrKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                Byte[] inputByteArray = System.Text.Encoding.UTF8.GetBytes(strText);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(_KEY, _IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                return null;
            }
        }
    }

    /// <summary>
    /// EXCEL打印数据结构,用于一次性填充EXCEL数据,
    /// </summary>
    public class DataXls 
    {
        private int row;
        private int col;
        private string val;

        /// <summary>
        /// 获取或设置行坐标
        /// </summary>
        public int Row
        {
            get { return row; }
            set { row = value; }
        }

        /// <summary>
        /// 获取或设置列坐标
        /// </summary>
        public int Col
        {
            get { return col; }
            set { col = value; }
        }

        /// <summary>
        /// 获取或设置值
        /// </summary>
        public string Val
        {
            get { return val; }
            set { val = value; }
        }
    }

    /// <summary>
    /// combobox控件的数据类.为了实现,显示值与实际值不同而设定
    /// </summary>
    public class TextAndValue 
    {
        private string _RealValue = "";
        private string _DisplayText = "";

        /// <summary>
        /// 获得显示值
        /// </summary>
        public string DisplayText
        {
            get
            {
                return _DisplayText;
            }
        }

        /// <summary>
        /// 获得实际值
        /// </summary>
        public string RealValue
        {
            get
            {
                return _RealValue;
            }
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        /// <param name="ShowText">显示值</param>
        /// <param name="RealVal">实际值</param>
        public TextAndValue(string ShowText, string RealVal)
        {
            _DisplayText = ShowText;
            _RealValue = RealVal;
        }

        /// <summary>
        /// 重写的ToString方法
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _RealValue.ToString();
        }
    }

    /// <summary>
    /// 动态生成按钮到框架类时的铵钮信息
    /// </summary>
    public class Button_info
    {
        private string _Name=null;
        private string _Text = null;
        private EventHandler func = null;

        /// <summary>
        /// 初始化按钮信息
        /// </summary>
        /// <param name="name">按钮ID</param>
        /// <param name="text">按钮名称</param>
        /// <param name="fc">回调函数,按钮点击事件发生时的处理函数 函数原型 void func(object sender, EventArgs e)</param>
        public Button_info(string name, string text, EventHandler fc)
        {
            _Name = name;
            _Text = text;
            func = fc;
        }
        public string NAME
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
            }
        }
        public string TEXT
        {
            get
            {
                return _Text;
            }
            set
            {
                _Text = value;
            }
        }
        //获得或设置响应函数
        public EventHandler FUNC
        {
            get
            {
                return func;
            }
            set
            {
                func = value;
            }
        }

    }
    /// <summary>
    /// 保存一个记录集对象,用于实现,数据的上 下笔游动
    /// </summary>
    public class Cursor
    {
        private System.Data.DataTable dt = null;
        private int index = -1;
        /// <summary>
        /// 默认构造函数
        /// </summary>
        /// <param name="table">传入DataTable集</param>
        public Cursor(System.Data.DataTable table)
        {
            dt = table;
            index = 0;
        }

        /// <summary>
        /// 返回该对象保存的所有数据的集合,可以用来遍历获得所有值
        /// </summary>
        public System.Data.DataTable Table
        {
            get 
            {
                return dt;
            }
        }

        /// <summary>
        /// 返回总行数
        /// </summary>
        public int RowSum //总行数
        {
            get
            {
                return dt.Rows.Count;
            }
        }

        /// <summary>
        /// 返回当选索引
        /// </summary>
        public int Current_Index
        {
            get
            {
                return index;
            }
        }

        /// <summary>
        /// 判断记住集是否为空 为空返回真
        /// </summary>
        public bool IsNull
        {
            get 
            {
                if (dt.Rows.Count == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 下一笔,传入下标进行各列值的访问 默认传入0
        /// </summary>
        public System.Data.DataRow Next
        {
            get 
            {
                if (dt.Rows.Count == 0)
                    return null;
                index++;
                if (index >= dt.Rows.Count)  //最大笔检测,防止数组越界
                {
                    index--;
                }
                return dt.Rows[index]; 
            }
        }

        /// <summary>
        /// 上一笔游动 传入下标进行各列值的访问 默认传入0
        /// </summary>
        public System.Data.DataRow Past
        {
            get
            {
                if (dt.Rows.Count == 0)
                    return null;
                index--;
                if (index < 0)               //最小笔检测,防止数组越界
                {
                    index = 0;
                }
                return dt.Rows[index];
            }
        }

        /// <summary>
        /// 第一笔游动 传入下标进行各列值的访问 默认传入0
        /// </summary>
        public System.Data.DataRow Frist
        {
            get
            {
                if (dt.Rows.Count == 0)
                    return null;
                index = 0;
                return dt.Rows[index];
            }
        }

        /// <summary>
        /// 第末笔游动 传入下标进行各列值的访问 默认传入0
        /// </summary>
        public System.Data.DataRow Last
        {
            get
            {
                if (dt.Rows.Count == 0)
                    return null;
                index = dt.Rows.Count - 1;
                return dt.Rows[index];
            }
        }

        /// <summary>
        /// 删除当前行数据,并递减
        /// </summary>
        public void DeleteCurrent()
        {
            dt.Rows[index].Delete();
        }
    }

    /// <summary>
    /// 框架类事件枚举
    /// </summary>
    public enum FrameEvent
    {
        /// <summary>
        /// 录入前事件
        /// </summary>
        BeforeInsert = 0,
 
        /// <summary>
        /// 录入后事件
        /// </summary>
        AfterInsert = 1,
      
        /// <summary>
        /// 修改前事件。
        /// </summary>
        BeforeAlter = 2,

        /// <summary>
        /// 修改后事件
        /// </summary>
        AfterAlter = 3,

        /// <summary>
        /// 单身前事件。
        /// </summary>
        BeforeBody = 4,

        /// <summary>
        /// 单身后事件。
        /// </summary>
        AfterBody = 5,

        /// <summary>
        /// 查询前事件.
        /// </summary>
        BeforeQuery = 6,
 
        /// <summary>
        /// 查询后事件
        /// </summary>
        AfterQuery = 7,

        /// <summary>
        /// 导出EXCEL事件
        /// </summary>
        ExportEvent = 8,

        /// <summary>
        /// 打印事件
        /// </summary>
        PrintEvent = 9,
 
        /// <summary>
        /// 第一笔事件
        /// </summary>
        FristEvent = 10,

        /// <summary>
        /// 下一笔事件
        /// </summary>
        NextEvent = 11,

        /// <summary>
        /// 上一笔事件
        /// </summary>
        PastEvent = 12,
 
        /// <summary>
        /// 第末笔事件
        /// </summary>
        LastEvent = 13,

        /// <summary>
        /// 单身删除事件
        /// </summary>
        DeleteRowEvent=14,

        /// <summary>
        /// 删除事件
        /// </summary>
        DeleteEvent = 15,

        /// <summary>
        /// 自定义事件
        /// </summary>
        DefineEvent=16,


    }

    /// <summary>
    /// 保存datagridview控件的数据结构
    /// </summary>
    [Serializable]
    public class DataGridinfo
    {
        //要显示的列名
        private string _ColumnName=null;
        //列ID,对应的数据库字段
        private string _ColumnID = null;
        //是否显示
        private bool _ColumnVisible = true;
        //列宽度
        private int _ColumnWidth = 0;
        //列显示索引
        private int _DisplayIndex = 0;
        
        //属性
        /// <summary>
        /// 列名称
        /// </summary>
        public string ColumnName
        {
            get
            {
                return _ColumnName;
            }
            set
            {
                _ColumnName = value;
            }
        }
        /// <summary>
        /// 列ID,对应数据库字段
        /// </summary>
        public string ColumnID
        {
            get
            {
                return _ColumnID;
            }
            set
            {
                _ColumnID = value;
            }
        }
        /// <summary>
        /// 是否显示
        /// </summary>
        public bool ComlumnVisible
        {
            get
            {
                return _ColumnVisible;
            }
            set
            {
                _ColumnVisible = value;
            }
        }
        /// <summary>
        /// 宽度
        /// </summary>
        public int ColumnWidth
        {
            get
            {
                return _ColumnWidth;
            }
            set
            {
                _ColumnWidth = value;
            }
        }
        /// <summary>
        /// 显示索引
        /// </summary>
        public int DisplayIndex
        {
            get
            {
                return _DisplayIndex;
            }
            set
            {
                _DisplayIndex = value;
            }
        }
    }

    /// <summary>
    /// 以oledb的方式访问oracle数据库
    /// 客户机需安装oracle客户端(运行时库)
    /// </summary>
    public class OracleData
    {
        private string _connect = null;
        private OleDbConnection theConnect = null;
        private OleDbDataAdapter theAdapter = null;
        private DataSet ds = null;
        private OleDbTransaction theTranct = null;


        public OracleData(string connect)
        {
            _connect = connect;
            try
            {
                theConnect = new OleDbConnection(_connect);
                theConnect.Open();
            }
            catch (OleDbException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 读取指定SQL的数据,返回到Dataset对象中
        /// </summary>
        /// <param name="sql">用于读数据的SQL语句</param>
        /// <returns></returns>
        public DataSet ReadTable(string sql)
        {
            try
            {
                ds = new DataSet();
                theAdapter = new OleDbDataAdapter();
                if (theTranct != null) //已开事务
                {
                    theAdapter.SelectCommand = new OleDbCommand(sql, theConnect, theTranct);
                }
                else
                {
                    theAdapter.SelectCommand = new OleDbCommand(sql, theConnect);
                }
                theAdapter.Fill(ds);
            }
            catch (OleDbException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return null;
            }
            return ds;
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        public void BeginTrant()
        {
            try
            {
                theTranct = theConnect.BeginTransaction();
            }
            catch (InvalidOperationException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);

            }
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void CommintTrant()
        {
            theTranct.Commit();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void RollbackTrant()
        {
            theTranct.Rollback();
        }

        /// <summary>
        /// 动态更新ds中的数据到实体库失败返回-1 否则返回受影响的行数
        /// </summary>
        /// <returns></returns>
        public int Update()
        {
            int ret = 0;
            if (theAdapter == null || ds == null)
            {
                System.Windows.Forms.MessageBox.Show("对象未初始化或初始化错误,无法进行更新");
                return -1;
            }
            try
            {
                OleDbCommandBuilder Builder = new OleDbCommandBuilder(theAdapter);
                ret=theAdapter.Update(ds);
            }
            catch (OleDbException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return -1;
            }
            return ret;
        }

        /// <summary>
        /// 执行指定的SQL语句,update delete insert 失败返回-1 否则返回受影响的行数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public int ExeCute(string sql)
        {
            OleDbCommand theCommand = null;
            if (theTranct != null)
            {
                theCommand = new OleDbCommand(sql, theConnect, theTranct);
            }
            else
            {
                theCommand = new OleDbCommand(sql, theConnect);
            }
            try
            {
                int retult = theCommand.ExecuteNonQuery();
                return retult;
            }
            catch (SqlException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return -1;
            }
 
        }

        /// <summary>
        /// 读取指定SQL 成功返回数据集对象 失败返回空.
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public OleDbDataReader ReadData(string sql)
        {
            OleDbCommand Command = null;
            OleDbDataReader Reader = null;

            try
            {
                if (theTranct == null)
                {
                    Command = new OleDbCommand(sql, theConnect);
                }
                else
                {
                    Command = new OleDbCommand(sql, theConnect, theTranct);
                }
                Reader = Command.ExecuteReader();
            }
            catch (OleDbException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return null;
            }
            return Reader;
        }

        /// <summary>
        /// 读取指定SQL 成功返回总行数,失败返回-1 sql中必须以 
        /// select count(*) 开头 否则将返回不可预知的值
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public int ReadCount(string sql)
        {
            OleDbCommand Command = null;
            OleDbDataReader Reader = null;
            int Count = 0;
            try
            {
                if (theTranct == null)
                {
                    Command = new OleDbCommand(sql, theConnect);
                }
                else
                {
                    Command = new OleDbCommand(sql, theConnect, theTranct);
                }
                Reader = Command.ExecuteReader();
                if(Reader.Read())
                {
                    Count=Convert.ToInt32(Reader[0].ToString());
                }
                Reader.Close();
            }
            catch (OleDbException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return -1;
            }
            return Count;
 
        }

        /// <summary>
        /// 读取第一行数据 返回到string数组.
        /// 失败或为空返回null
        /// </summary>
        /// <param name="sql">用于读取数据的SQL</param>
        /// <param name="vals">要取那些列的值</param>
        /// <returns>string数组,包含所有数据</returns>
        public string[]  ReadRow(string sql,params string []vals)
        {
            OleDbCommand Command = null;
            OleDbDataReader Reader = null;
            string[] rets = vals;
            try
            {
                if (theTranct == null)
                {
                    Command = new OleDbCommand(sql, theConnect);
                }
                else
                {
                    Command = new OleDbCommand(sql, theConnect, theTranct);
                }
                Reader = Command.ExecuteReader();
                if (Reader.Read())
                {
                    int i = 0;
                    foreach(string it in vals)
                    {
                        rets[i] = Reader[it].ToString();
                        i++;
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("无资料或未知错误");
                    return null;
                }
                Reader.Close();
            }
            catch (OleDbException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
                return null;
            }
            catch (IndexOutOfRangeException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
            return rets;
        }

    }

    /// <summary>
    /// 序列化对象到文件,以及返序列化文件到对象
    /// </summary>
    public class Serialize
    {
        /* 序列化笔记
         * 反序列化其实就是把文件中的信息写入到数据流(内存中)内存指针指向0的位置.然后从该位置读取指定长度的内存数据出来
         * 同时移动指针.所以序列化可以一次写入多个不同类型的对象,但返序列化时一定得按序列化时的顺序进行.否则就会因读到的
         * 数据不完整或过多而无法还原数据.造成失败.
         * #如果要一次序列化与反序列化多个对象.可以将所以对象保存在一个List<object>中.然后序列化一个对象即可.
         */
        private string strFile = null;
        private string msg_err;

        public string GetLastError
        {
            get { return msg_err; }
        }

        /// <summary>
        /// 构构用来处理序列化对象的类,传入文件保存的位置
        /// </summary>
        /// <param name="path"></param>
        public Serialize(string path)
        {
            strFile = path;
        }

        /// <summary>
        /// 序列化一个对象到文件,传入可序列化的对象
        /// </summary>
        /// <param name="obj"></param>
        public bool Serializes(object obj)
        {
            FileStream fs=null;
            try
            {
                fs = new FileStream(strFile, FileMode.Create);
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, obj);
                fs.Flush();
            }
            catch (IOException io)
            {
                msg_err = io.Message;
                return false;
            }
            catch (System.Runtime.Serialization.SerializationException e)
            {
                msg_err = e.Message;
                return false;
            }
            finally
            {
                fs.Close();
            }
            return true;
        }

        /// <summary>
        /// 返序列化 返回一个object 需要人为去转换到实际对象,失败返回null.查看错误请调用GetLastError属性
        /// </summary>
        /// <returns></returns>
        public object Deserialize()
        {
            if (!File.Exists(strFile))
            {
                msg_err = "文件不存在,或拒绝访问,无法反序列化";
                return null;
            }
            FileStream fs = null;
            object obj = null;
            try
            {
                fs = new System.IO.FileStream(strFile, FileMode.Open);
                BinaryFormatter formatter = new BinaryFormatter();
                obj = formatter.Deserialize(fs);
            }
            catch (IOException io)
            {
                msg_err = io.Message;
                obj = null;
                throw;
            }
            catch (System.Runtime.Serialization.SerializationException e)
            {
                msg_err = e.Message;
                obj = null;
                throw;
            }
            finally
            {
                fs.Close();
            }
             return obj;
        }
    }

    /*
     * 实现文件的下载,重命名,删除
     */
    public class FileOption
    {
        private string _Directory;

        public FileOption(string Directory)
        {
            _Directory = Directory;
        }

        /// <summary>        
        /// 下载文件        
        /// </summary>        
        /// <param name="URL">下载文件地址包含文件名</param>       
        /// 
        /// <param name="Filename">保存的文件名,不包含路径</param>        
        /// <param name="Prog">用于显示的进度条</param>        
        /// 
        public void DownloadFile(string URL, string filename, System.Windows.Forms.ProgressBar prog)
        {
            filename = _Directory + "\\" + filename;
            try
            {
                System.Net.HttpWebRequest Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URL);
                System.Net.HttpWebResponse myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();
                long totalBytes = myrp.ContentLength;
                if (prog != null)
                {
                    prog.Maximum = (int)totalBytes;
                }
                System.IO.Stream st = myrp.GetResponseStream();
                System.IO.Stream so = new System.IO.FileStream(filename, System.IO.FileMode.Create);
                long totalDownloadedByte = 0;
                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, (int)by.Length);
                while (osize > 0)
                {
                    totalDownloadedByte = osize + totalDownloadedByte;
                    System.Windows.Forms.Application.DoEvents();
                    so.Write(by, 0, osize);
                    if (prog != null)
                    {
                        prog.Value = (int)totalDownloadedByte;
                    }
                    osize = st.Read(by, 0, (int)by.Length);
                }
                so.Close();
                st.Close();
            }
            catch (System.Exception)
            {
                throw;
            }
        }

        public void DeleteFile(string filename)
        {
            try
            {
                System.IO.File.Delete(_Directory + "\\" + filename);
            }
            catch (System.Exception)
            {
                throw;
            }
        }

    }

    /// <summary>
    /// 该类通过HttpWebRequest类访问http接口,实现的方法有;
    /// 1:获得数据
    /// 2:提交数据
    ///   2.1: AddFile(string name, string filePath, string fileName); 添加文件
    ///   2.2: AddValues(string name, string val); 添加上传的字段以及值
    ///   2.3: Send(); 提交请求,返回服务器执行结果;
    ///   2.4: 注,服务端如果要求接收文件时,客户端必须调用AddFile进行添加文件.
    ///           如果又没有文件要上传就将参数strPath,fileName设置为null进行调用.(这样做的目的是为了写入一个空的文件上传头)否则http服务器会报错(非法上传)   
    /// </summary>
    public class WebServers
    {
     ///   2.5: 提交数据的格式
    ///     --+---------------0x14164318184 \r\n                                 //文件1
    ///     Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" Content-Type: application/octet-stream\r\n\r\n"   
    ///     文件内容(通过MemoryStream写入IO流中)\r\n                             
    ///     --+---------------0x14164318184 \r\n                                 //文件2
    ///     Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" Content-Type: application/octet-stream\r\n\r\n"
    ///     文件内容(通过MemoryStream写入IO流中)\r\n
    ///     --+---------------0x14164318184 \r\n                                 //数据1
    ///     \r\nContent-Disposition: form-data; name= \"{0}\ \r\n\r\n{1}\\r\n;
    ///      --+---------------0x14164318184 \r\n                                //数据2  
    ///     \r\nContent-Disposition: form-data; name= \"{0}\ \r\n\r\n{1}\\r\n;
    ///     
    ///     --+---------------0x14164318184 \r\n                                 //结束   
        
        #region 数据字段
        private string  postURL = null;   //上传或获得数据的URL接口
        private MemoryStream memStream;   //数据缓存
        private static string boundary = "---------------" + DateTime.Now.Ticks.ToString("x");  //边界符
        private byte[] beginBoundary = Encoding.ASCII.GetBytes("--" + boundary + "\r\n");       //开始
        private byte[] endBoundary = Encoding.ASCII.GetBytes("--" + boundary + "--\r\n");       //结束
        private Dictionary<string, string> stringDict;                                          //Key-Value数据
        string err_msg;
        #endregion

        /// <summary>
        /// http访问web服务接口,传入要访问的URL
        /// </summary>
        /// <param name="url"></param>
        public WebServers(string url)
        {
            
            postURL = url;
            memStream = new MemoryStream();   //数据缓存
            stringDict = new Dictionary<string, string>();           //数据流 Key-Value

        }
        /// <summary>
        /// 获得最后的错误
        /// </summary>
        public string GetLastError
        {
            get { return err_msg; }
        }
        
        /// <summary>
        /// 添加文件
        /// </summary>
        /// <param name="name">字段名称</param>
        /// <param name="filePath">文件路径+文件名</param>
        /// <param name="fileName">文件名</param>
        public void AddFile(string name, string filePath, string fileName)
        {
            //文件格式
            const string filePartHeader =
                    "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" +
                     "Content-Type: application/octet-stream\r\n\r\n";

            memStream.Write(beginBoundary, 0, beginBoundary.Length); //写入头
            if (string.IsNullOrEmpty(filePath))   //如果路径为空,就只写入一个http 文件头
            {
                var fileHeader = string.Format(filePartHeader, name, fileName);
                var fileHeaderBytes = Encoding.UTF8.GetBytes(fileHeader);
                memStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);
                string rn = "\r\n";
                var ret = Encoding.UTF8.GetBytes(rn);
                memStream.Write(ret, 0, ret.Length);
                return;
            }
            try
            {
                if (!System.IO.File.Exists(filePath))
                {
                    System.Windows.Forms.MessageBox.Show("文件不存在");
                    return;
                }
                
                var fileHeader = string.Format(filePartHeader, name, fileName);
                var fileHeaderBytes = Encoding.UTF8.GetBytes(fileHeader);
                memStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);

                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[1024];
                int bytesRead; // =0  
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    memStream.Write(buffer, 0, bytesRead);
                }
                string rn = "\r\n";
                var ret = Encoding.UTF8.GetBytes(rn);
                memStream.Write(ret, 0, ret.Length);
                fileStream.Close();
            }
            catch (FileNotFoundException ioEx)
            {
                System.Windows.Forms.MessageBox.Show(ioEx.Message);

            }
            catch (IOException e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
        }

        /// <summary>
        /// 增加 Key-Value数据
        /// </summary>
        /// <param name="name">Key 字段名称</param>
        /// <param name="val">Value 字段值</param>
        public void AddValues(string name, string val)
        {
            stringDict.Add(name, val);
        }

        /// <summary>
        /// 不使用
        /// </summary>
        /// <returns></returns>
        public string Send()
        {
            // Key-Value数据  
            var stringKeyHeader = "\r\n--" + boundary +
                                   "\r\nContent-Disposition: form-data; name=\"{0}\"" +
                                   "\r\n\r\n{1}\r\n";
            foreach (byte[] formitembytes in from string key in stringDict.Keys
                                             select string.Format(stringKeyHeader, key, stringDict[key])
                                                 into formitem
                                                 select Encoding.UTF8.GetBytes(formitem))
            {
                memStream.Write(formitembytes, 0, formitembytes.Length);
            }

            // 写入最后的结束边界符  
            memStream.Write(endBoundary, 0, endBoundary.Length);
            //倒腾到tempBuffer?  
            memStream.Position = 0;
            var tempBuffer = new byte[memStream.Length];
            memStream.Read(tempBuffer, 0, tempBuffer.Length);
            memStream.Close();

            try
            {
                // 创建webRequest并设置属性  
                var webRequest = (HttpWebRequest)WebRequest.Create(postURL);
                webRequest.Method = "POST";
                webRequest.Timeout = 100000;
                webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                webRequest.ContentLength = tempBuffer.Length;

                var requestStream = webRequest.GetRequestStream();
                requestStream.Write(tempBuffer, 0, tempBuffer.Length);
                requestStream.Close();

                var httpWebResponse = (HttpWebResponse)webRequest.GetResponse();
                string responseContent;
                using (var httpStreamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding("utf-8")))
                {
                    responseContent = httpStreamReader.ReadToEnd();
                }

                httpWebResponse.Close();
                webRequest.Abort();
                return responseContent;
            }
            catch (WebException e)
            {
                err_msg = e.Message;
                return null;
            }
        }

        /// <summary>
        /// 返回请求的数据,可用于更新数据,写入数据,请求数据,上传文件 失败返回null
        /// </summary>
        /// <returns></returns>
        public string Request()
        {
            // Key-Value数据  
            var stringKeyHeader = "\r\n--" + boundary +
                                   "\r\nContent-Disposition: form-data; name=\"{0}\"" +
                                   "\r\n\r\n{1}\r\n";
            foreach (byte[] formitembytes in from string key in stringDict.Keys
                                             select string.Format(stringKeyHeader, key, stringDict[key])
                                                 into formitem
                                                 select Encoding.UTF8.GetBytes(formitem))
            {
                memStream.Write(formitembytes, 0, formitembytes.Length);
            }

            // 写入最后的结束边界符  
            memStream.Write(endBoundary, 0, endBoundary.Length);
            //倒腾到tempBuffer?  
            memStream.Position = 0;
            var tempBuffer = new byte[memStream.Length];
            memStream.Read(tempBuffer, 0, tempBuffer.Length);
            memStream.Close();
            try
            {
                // 创建webRequest并设置属性  
                var webRequest = (HttpWebRequest)WebRequest.Create(postURL);
                webRequest.Method = "POST";
                webRequest.Timeout = 100000;
                webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
                webRequest.ContentLength = tempBuffer.Length;

                var requestStream = webRequest.GetRequestStream();
                requestStream.Write(tempBuffer, 0, tempBuffer.Length);
                requestStream.Close();

                var httpWebResponse = (HttpWebResponse)webRequest.GetResponse();
                string responseContent;
                using (var httpStreamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.GetEncoding("utf-8")))
                {
                    responseContent = httpStreamReader.ReadToEnd();
                }

                httpWebResponse.Close();
                webRequest.Abort();
                return responseContent;
            }
            catch (WebException e)
            {
                err_msg = e.Message;
                return null;
            }
        }

        /// <summary>
        /// 从Webservers返回 datatable数据
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        public System.Data.DataTable Request_Table()
        {
            string data =  Request();
            System.Data.DataTable dt = new System.Data.DataTable();

            dt.Columns.Add();
            dt.Columns.Add();
            dt.Columns.Add();
            for (int i = 0; i < 10; i++)
            {
                dt.Rows.Add("abc",1,"sfsf");
            }
            return dt;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="filename">将下载的文件以该文件名保存在XML指定的地址中.包含后缀</param>
        /// <returns></returns>
        public bool DownloadFile(string filename)
        {
            try
            {
                System.Net.HttpWebRequest Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(postURL);
                System.Net.HttpWebResponse myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();

                System.IO.Stream st = myrp.GetResponseStream();   //服务器文件流
                System.IO.Stream so = new System.IO.FileStream(filename, System.IO.FileMode.Create); //本地文件流

                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, (int)by.Length);
                while (osize > 0)
                {
                    so.Write(by, 0, osize);
                    osize = st.Read(by, 0, (int)by.Length);
                }
                so.Close();
                st.Close();
            }
            catch (System.Exception e)
            {
                err_msg = e.Message;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// 通过该类与服务器进行通信.
    /// </summary>
    public class stock
    {
        private string err_msg;
        private string _ip;
        private int _port;
        private TcpClient Client;
        public stock(string ip,int port)
        {
            _ip = ip;
            _port = port;
            Client = new TcpClient();
            
        }
        /// <summary>
        /// 最后的错误
        /// </summary>
        public string GetLastError
        {
            get { return err_msg;}
        }
        /// <summary>
        /// 向服务器发送数据.数据不可为空,否则直接返回空,更多错误描述请调用GetLastError
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public string Request(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    err_msg = "传入数据为空,不可发送请求";
                    return null;
                }
                TcpClient client = new TcpClient(_ip, _port);
                Byte[] data = System.Text.Encoding.UTF8.GetBytes(message);

                NetworkStream stream = client.GetStream(); 
                stream.Write(data, 0, data.Length);
                
                data = new Byte[1024];
                String responseData = String.Empty;
                do
                {
                    stream.Read(data, 0, data.Length);
                    responseData += System.Text.Encoding.UTF8.GetString(data, 0, data.Length);
                    stream.Read(data, 0, data.Length);
                }
                while (stream.DataAvailable);
                stream.Close();
                client.Close();
                return responseData;
            }
            catch (ArgumentNullException e)
            {
                err_msg = e.Message;
            }
            catch (SocketException e)
            {
                err_msg = e.Message;
            }
            return null;

        }
 
    }

    /// <summary>
    /// 公共函数数
    /// </summary>
    public class Comfunc
    {
        /// <summary>
        /// 返回大值
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int max(int a, int b)
        {
            if (a > b)
                return a;
            return b;
        }

        /// <summary>
        /// 返回大值
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static decimal max(decimal a, decimal b)
        {
            if (a > b)
                return a;
            return b;
        }

        /// <summary>
        /// 返回小值
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int min(int a, int b)
        {
            if (a > b)
                return b;
            return a;
        }

        /// <summary>
        /// 返回小值
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static decimal min(decimal a, decimal b)
        {
            if (a > b)
                return b;
            return a;
        }

        /// <summary>
        /// 确定框 确认返回真
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool cl_confirm(string str)
        {
            if (System.Windows.Forms.DialogResult.Yes == System.Windows.Forms.MessageBox.Show(str, "confirm", System.Windows.Forms.MessageBoxButtons.YesNo))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 按日期+3位流水或得唯一灌包号
        /// </summary>
        /// <param name="_conn"></param>
        /// <returns></returns>
        public static string cl_get_gb(string _conn)
        {

            string ruselt = DateTime.Now.ToString("yyyyMMdd");
            SqlData db = new SqlData(_conn);
            string sql = string.Format("SELECT max(ctf10) ctf10 FROM ctf_file WHERE ctf10 LIKE '{0}%'",ruselt);
            string l_ctf10 = db.ReadRows(sql, 1, "ctf10");
            if (string.IsNullOrEmpty(l_ctf10))
            {
                ruselt += "001";
            }
            else
            {
                ruselt = (Convert.ToInt64(l_ctf10) + 1).ToString();
            }

            return ruselt;
        }

        public static bool cl_init_form(System.Windows.Forms.Form form, string _language, string _conn)
        {  
            InitControl(form, form.Name, _language,_conn);
            return true;
        }

        private static void ergodic_menu(ToolStripMenuItem item,string _language,string _conn,string proc) 
        {
            iwin.SqlData db2 = new SqlData(_conn);
            //先翻译这个菜单
            string l_name = item.Name;
            string sql = string.Format("SELECT lge04 FROM lge_file WHERE lge01 = '{0}' AND lge02 = '{1}' AND lge03 = '{2}'"
                   , proc, l_name, _language);
            string l_lge04 = db2.ReadRows(sql, 1, "lge04");
            if (l_lge04 != null)
            {
                item.Text = l_lge04;
            }
            else
            {
                sql = string.Format("INSERT INTO lge_file VALUES('{0}','{1}','{2}','{3}')", proc, l_name, _language, item.Text);
                db2.ExeCute(sql);
            }

            // 遍历下拉菜单
            foreach (ToolStripMenuItem item1 in item.DropDownItems)
            {
                ergodic_menu(item1,_language,_conn,proc);

            }
        }

        private static void ergodic_tree(TreeNode node, string _language, string proc, SqlData db)
        {
            //先翻译这个节点,再遍历这个节点下面的节点
            string l_name = node.Name;
            string sql = string.Format("SELECT lge04 FROM lge_file WHERE lge01 = '{0}' AND lge02 = '{1}' AND lge03 = '{2}'"
                   , proc, l_name, _language);
            string l_lge04 = db.ReadRows(sql, 1, "lge04");
            if (l_lge04 != null)
            {
                node.Text = l_lge04;
            }
            else
            {
                sql = string.Format("INSERT INTO lge_file VALUES('{0}','{1}','{2}','{3}')", proc, l_name, _language, node.Text);
                db.ExeCute(sql);
            }
            
            foreach (TreeNode node2 in node.Nodes)
            {
                ergodic_tree(node2, _language,proc, db);
            }
        }

        //遍历控件上包含的所有控件
        private static void InitControl(Control TheTrol, string proc, string _language,string _conn)
        {
            iwin.SqlData db2 = new SqlData(_conn);

            //窗口处理.因为窗口上会包含其它的字控件,所以要进入递归
            if (TheTrol is Form)
            {
                string l_name = TheTrol.Name;
                string sql = string.Format("SELECT lge04 FROM lge_file WHERE lge01 = '{0}' AND lge02 = '{1}' AND lge03 = '{2}'"
                       , proc, l_name, _language);
                string l_lge04 = db2.ReadRows(sql, 1, "lge04");
                if (l_lge04 != null)
                {
                    TheTrol.Text = l_lge04;
                }
                else
                {
                    sql = string.Format("INSERT INTO lge_file VALUES('{0}','{1}','{2}','{3}')", proc, l_name, _language, TheTrol.Text);
                    db2.ExeCute(sql);
                }
                ToolTip a = new ToolTip();
                a.SetToolTip(TheTrol, TheTrol.Name);
                //翻译完窗口标题,递归该窗口上的其它待翻译的控件
                foreach (Control trol in TheTrol.Controls)
                {
                    InitControl(trol, proc, _language, _conn);
                }
                return;
            }
            //菜单处理
            if (TheTrol is MenuStrip)
            {
                MenuStrip menu = (MenuStrip)TheTrol;
                foreach (ToolStripMenuItem item in menu.Items)
                {
                    ergodic_menu(item,_language,_conn,proc);
                }
                return;
            }
            //树遍历
            if (TheTrol is TreeView)
            {
                TreeView tree = (TreeView)TheTrol;
                foreach (TreeNode node in tree.Nodes)
                {
                    ergodic_tree(node, _language, proc, db2);
                    
                }
                return;
            }
            //工具栏处理
            if (TheTrol is ToolStrip)
            {
                //add process code  
                ToolStrip tool = (ToolStrip)TheTrol;
                for (int i = 0; i < tool.Items.Count; i++)
                {
                    if (tool.Items[i] is ToolStripSeparator)
                        continue;
                    //查找该字段的译文,如果找不到,就以默认值写入数据库
                    string l_name = tool.Items[i].Name;
                    string l_text = tool.Items[i].Text;
                    string sql = string.Format("SELECT lge04 FROM lge_file WHERE lge01 = '{0}' AND lge02 = '{1}' AND lge03 = '{2}'"
                        , proc, l_name, _language);
                    string l_lge04 = db2.ReadRows(sql, 1, "lge04");
                    if (l_lge04 != null)
                    {
                        
                        tool.Items[i].Text = l_lge04;
                    }
                    else
                    {
                        sql = string.Format("INSERT INTO lge_file VALUES('{0}','{1}','{2}','{3}')", proc, l_name, _language, l_text);
                        db2.ExeCute(sql);
                    }
                    tool.Items[i].ToolTipText = l_name;
                }
                return;
            }
            //laber   查找它的译文,如果找不到就在数据库中写入一笔,同时设置鼠标悬停时提示ID
            if (TheTrol is System.Windows.Forms.Label)
            {
                string l_name = TheTrol.Name;
                string sql = string.Format("SELECT lge04 FROM lge_file WHERE lge01 = '{0}' AND lge02 = '{1}' AND lge03 = '{2}'"
                       , proc, l_name, _language);
                string l_lge04 = db2.ReadRows(sql, 1, "lge04");
                if (l_lge04 != null)
                {
                    TheTrol.Text = l_lge04;
                }
                else
                {
                    sql = string.Format("INSERT INTO lge_file VALUES('{0}','{1}','{2}','{3}')", proc, l_name, _language, TheTrol.Text);
                    db2.ExeCute(sql);
                }
                ToolTip a = new ToolTip();
                a.SetToolTip(TheTrol, TheTrol.Name);
                return;
            }
            if (TheTrol is System.Windows.Forms.Button || TheTrol is System.Windows.Forms.CheckBox)
            {
                string l_name = TheTrol.Name;
                string sql = string.Format("SELECT lge04 FROM lge_file WHERE lge01 = '{0}' AND lge02 = '{1}' AND lge03 = '{2}'"
                       , proc, l_name, _language);
                string l_lge04 = db2.ReadRows(sql, 1, "lge04");
                if (l_lge04 != null)
                {
                    TheTrol.Text = l_lge04;
                }
                else
                {
                    sql = string.Format("INSERT INTO lge_file VALUES('{0}','{1}','{2}','{3}')", proc, l_name, _language, TheTrol.Text);
                    db2.ExeCute(sql);
                }
                ToolTip a = new ToolTip();
                a.SetToolTip(TheTrol, TheTrol.Name);
                return;
            }
           // if (TheTrol is ComboText.Dcombobox)
           // {
           ///*     string newitems = null;
           //     ComboText.Dcombobox Combox = (ComboText.Dcombobox)TheTrol;
           //     string items = Combox.Items;
           //     string l_name = Combox.Name;
           //     items = items.TrimEnd(';');
           //     string[] keys = items.Split(';'); //按;号分割出所有项
           //     for (int i = 0; i < keys.Count(); i++)
           //     {
           //         string[] vals = keys[i].Split(':');  //分割出单项的key与value
           //         l_name += "_" + vals[0];             //组合ID为 控件ID+_+单项Key
           //         string sql = string.Format("SELECT lge04 FROM lge_file WHERE lge01 = '{0}' AND lge02 = '{1}' AND lge03 = '{2}'"
           //            , proc, l_name, _language);
           //         string l_lge04 = db2.ReadRows(sql, 1, "lge04");
           //         if (l_lge04 != null)   //如果存在就重新为新项
           //         {
           //             TheTrol.Text = l_lge04;
           //             newitems = newitems + vals[0] + ":" + l_lge04 + ";";
           //         }
           //         else                  //否则插入数据库,显示默认项
           //         {
           //             newitems = newitems + vals[0] + ":" + vals[1] + ";";
           //             sql = string.Format("INSERT INTO lge_file VALUES('{0}','{1}','{2}','{3}')", proc, l_name, _language, TheTrol.Text);
           //             db2.ExeCute(sql);
           //         }
           //     }
           //     Combox.Items = newitems;*/
           //     return;
           // }
           // //datatext控件 buttontext不处理
           // if (TheTrol is datatext.DateText || TheTrol is Controls.ButtonText)
           //     return;

            if (TheTrol is System.Windows.Forms.TreeView)
            {
                //遍历所有节点,查找译文
                return;
            }

            //递归其它类型的控制 group  panl spl
            foreach (Control trol1 in TheTrol.Controls)
            {
                InitControl(trol1, proc,_language,_conn);
            }
        }

    }
 
}