﻿using System;
using System.Collections.Generic;
using System.Text;
using GvWebCrawler;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.IO;

namespace xsxk
{
    class Program
    {
        static CookieContainer _cookies = new CookieContainer();

        static string _user = "";
        static string _pwd = "";
        static string[] _classes ;
        static string _rootUrl = "http://zfxk2.net.sxisa.com/";

        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("命令行调用方法，参数依次为 学号 密码 课号列表（逗号隔开）");
                Console.WriteLine("例如: xsxk.exe 1401260241 123456 1,5,6,7");
                Console.WriteLine("请输入学号：");
                _user = Console.ReadLine();
                Console.WriteLine("请输入教务管理系统密码：");
                _pwd = Console.ReadLine();
                Console.WriteLine("请输入选课课号，多个选课请按顺序用逗号隔开，如：1,5,6,7");
                _classes = Console.ReadLine().Split(',');
            }
            else
            {
                _user = args[0];
                _pwd = args[1];
                _classes = args[2].Split(',');
            }
            Console.WriteLine("###### 抢课开始！######");
            string sLogin = "";
            int n = 1;
            while (sLogin == "" || sLogin.IndexOf("系统繁忙") >= 0)
            {
                Thread.Sleep(1000);
                string _Post = "__VIEWSTATE=" + definition.LoginViewState + "&tbYHM=" + _user + "&tbPSW=" + _pwd + "&RadioButtonList1=%D1%A7%C9%FA&imgDL.x=74&imgDL.y=23"; 
                sLogin = GvCrawler.Post(_rootUrl + "default3.aspx", _Post, _cookies);
                if (sLogin.IndexOf("密码不正确") > 0)
                {
                    Console.WriteLine("######## 学号或密码错误！########");
                    Console.Read();
                    return;
                }

                Console.WriteLine("尝试登录第" + (n++) + "次");
                string stjkbcx = _rootUrl + "xsxk.aspx?xh=" + _user + "&lb=1";
                if ((sLogin != "" && sLogin.IndexOf("系统繁忙") < 0))
                {
                    Console.WriteLine("登录成功");
                    sLogin = GvCrawler.Get(stjkbcx, _cookies);

                    //while (sLogin.IndexOf("window.parent.location='';") < 0)
                    {
                        if ((sLogin != "" && sLogin.IndexOf("系统繁忙") < 0))
                        {
                            List<CLASS_INFO> lstClass = GetClassList(sLogin);
                            Console.WriteLine("获取选课列表");

                            _Post = "__VIEWSTATE=" + GetVIEWSTATE(sLogin) + "&__EVENTTARGET=btnXK&__EVENTARGUMENT=&HidXxmc=%C9%EE%DB%DA%D0%C5%CF%A2%D6%B0%D2%B5%BC%BC%CA%F5%D1%A7%D4%BA&HidLc=2&HidJkb=&HidJc=&HidXn=2017-2018&HidXq=1";

                            for (int i = 0; i < lstClass.Count; i++)
                            {
                                for (int j = 0; j < _classes.Length; j++)
                                {
                                    if (lstClass[i].sId == _classes[j])
                                    {
                                        _Post += "&" + lstClass[i].sCheck + "=no";
                                    }
                                }
                            }

                            while (sLogin.IndexOf("window.parent.location='';") < 0)
                            {
                                sLogin = GvCrawler.Post(_rootUrl + "xsxk.aspx?xh=" + _user + "&lb=1", _Post, _cookies);
                                if ((sLogin != "" && sLogin.IndexOf("系统繁忙") < 0))
                                {
                                    Console.WriteLine("#####选课成功#####");
                                    SaveClassInfo(lstClass);
                                    SaveToFile(sLogin, "result.html");
                                    Console.WriteLine(GetMessgae(sLogin));
                                    Console.Read();
                                }
                                else
                                {
                                    Console.WriteLine("提交失败");
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("获取选课列表失败");
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
            Console.Read();
        }

        /// <summary>
        /// 获取ViewState
        /// </summary>
        /// <param name="sHtml"></param>
        /// <returns></returns>
        static string GetVIEWSTATE(string sHtml)
        {
            string sRegex = "<input type=\"hidden\" name=\"__VIEWSTATE\" value=\"(.*?)\" />";
            MatchCollection Matches = Analtytic(sRegex, sHtml);
            if (Matches.Count <= 0) return "";
            return HttpUtility.UrlEncode(Matches[0].Groups[1].Value, Encoding.GetEncoding("GB2312"));
        }

        /// <summary>
        /// 获取提示
        /// </summary>
        /// <param name="sHtml"></param>
        /// <returns></returns>
        static string GetMessgae(string sHtml)
        {
            string sRegex = @"alert\('(.*?)'\)";

            MatchCollection Matches = Analtytic(sRegex, sHtml);
            if (Matches.Count <= 0) return "";

            string sContent = Matches[0].Groups[1].Value;

            return sContent.Replace(@"\r", "\n");
        }

        /// <summary>
        /// 获取课程列表
        /// </summary>
        /// <param name="sHtml"></param>
        /// <returns></returns>
        static List<CLASS_INFO> GetClassList(string sHtml)
        {
            string sRegex = @"<tr[^<]*?<td><a [^>]*?>([^<]*?)</a></td><td>[\d-]*?</td><td>([^<]*?)</td>.*?</td><td>(?:<a href=""[^""]*?"">([^<]*?)</a>)+</td><td> <input name=""(DataGrid1:_ctl\d*:zhj1)"" id=""DataGrid1__ctl\d*_zhj1"" type=""checkbox"" .*?</td><td>(\d*)</td><td>";

            MatchCollection Matches = Analtytic(sRegex, sHtml);
            List<CLASS_INFO> checks = new List<CLASS_INFO>();

            foreach (Match match in Matches)
            {
                CLASS_INFO item = new CLASS_INFO();
                item.sName = match.Groups[1].Value;
                item.sTime = match.Groups[2].Value;
                item.sTeacher = match.Groups[3].Value;
                item.sCheck = match.Groups[4].Value;
                item.sId = match.Groups[5].Value;
                checks.Add(item);
            }

            return checks;
        }
        
        /// <summary>
        /// 根据正则解析文本
        /// </summary>
        /// <param name="sRegEx"></param>
        /// <param name="sHtml"></param>
        /// <returns></returns>
        static public MatchCollection Analtytic(string sRegEx, string sHtml)
        {
            System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(sHtml, sRegEx, RegexOptions.IgnoreCase);
            //Console.WriteLine("Analtytic RegEx: " + sRegEx + " Count:" + matches.Count);
            return matches;
        }

        /// <summary>
        /// 保存课程列表
        /// </summary>
        /// <param name="lstInfo"></param>
        static void SaveClassInfo(List<CLASS_INFO> lstInfo)
        {
            StringBuilder sList = new StringBuilder();
            sList.AppendLine("名称,时间,老师,课号,请求");
            for (int i = 0; i < lstInfo.Count; i++)
            {
                sList.AppendLine(lstInfo[i].sName + ",'" + lstInfo[i].sTime + "'," + lstInfo[i].sTeacher + "," + lstInfo[i].sId + "," + lstInfo[i].sCheck);
            }
            SaveToFile(sList.ToString(), "class.csv");
        }

        static void SaveToFile(string sContent, string sFileName)
        {
            using (StreamWriter wr = new StreamWriter(sFileName, false, Encoding.Default))
            {
                wr.Write(sContent);
            }
        }
    }
}
