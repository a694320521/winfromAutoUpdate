using System;
using System.Collections.Generic;
using System.Data;

namespace update
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUpdateBll
    {
        string conn { get; set; }
        Dictionary<string, string> LocalDi { get; set; }
        Action<int, int> ShowUpdateProgress { get; set; }
        Dictionary<string, string> upFileDi { get; set; }
        DataTable ver_dt { get; set; }

        string comparisonVerInfo(string LocalPath);
        void Decompress(string filePath, string outFileDirectory);
        void DecompressByPwd(string filePath, string outFileDirectory, string password);
        string downLoadFile(string URL, string savePath);
        string getLocal_VerInfo(string fileName, string filePath);
        string getVer_Info();
        bool isUpdate(string LocalPath, out string result);
        string saveAllFileZiptoTempPath(string SaveTempDirectoryPath, out string fileName);
        void ZipCompress(string fromFileDirectory, string outFilePath, string password = "");
        void SetDefaultConn(string conn);
    }
}