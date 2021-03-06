using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.IO;
using Sinawler.Model;
using System.Data;
using System.Xml;
using Newtonsoft.Json;

namespace Sinawler
{
    public class RobotBase
    {
        protected APIInfo api;
        protected bool blnAsyncCancelled = false;     //指示爬虫线程是否被取消，来帮助中止爬虫循环
        protected string strLogFile = "";             //日志文件
        private string strLogMessage = "";          //日志内容
        protected bool blnSuspending = false;         //是否暂停，默认为“否”
        protected SinaMBCrawler crawler;              //爬虫对象。构造函数中初始化
        protected long lCurrentID = 0;               //当前爬取的用户或微博ID，随时抛出传递给另外的机器人，由各子类决定由其暴露的属性名
        protected BackgroundWorker bwAsync = null;
        protected int iMinSleep = 100;              //minimum ms for sleeping

        protected UserQueue queueUserForUserInfoRobot;            //用户信息机器人使用的用户队列引用
        protected UserQueue queueUserForUserRelationRobot;        //用户关系机器人使用的用户队列引用
        protected UserQueue queueUserForStatusRobot;          //微博机器人使用的用户队列引用
        protected UserQueue queueUserForUserTagRobot;             //用户标签机器人使用的用户队列引用
        protected StatusQueue queueStatus;                    //微博队列引用

        //构造函数，需要传入相应的新浪微博API和主界面
        public RobotBase(SysArgFor robotType)
        {
            crawler = new SinaMBCrawler(robotType);
            api = GlobalPool.GetAPI(robotType);
            switch (robotType)
            {
                case SysArgFor.USER_INFO:
                    if (iMinSleep < GlobalPool.MinSleepMsForUserInfo) iMinSleep = GlobalPool.MinSleepMsForUserInfo;
                    break;
                case SysArgFor.USER_TAG:
                    if (iMinSleep < GlobalPool.MinSleepMsForUserTag) iMinSleep = GlobalPool.MinSleepMsForUserTag;
                    break;
                case SysArgFor.STATUS:
                    if (iMinSleep < GlobalPool.MinSleepMsForStatus) iMinSleep = GlobalPool.MinSleepMsForStatus;
                    break;
                case SysArgFor.COMMENT:
                    if (iMinSleep < GlobalPool.MinSleepMsForComment) iMinSleep = GlobalPool.MinSleepMsForComment;
                    break;
                default:
                    if (iMinSleep < GlobalPool.MinSleepMsForUserRelation) iMinSleep = GlobalPool.MinSleepMsForUserRelation;
                    break;
            }
            AdjustRealFreq();
        }

        public bool AsyncCancelled
        {
            set
            {
                blnAsyncCancelled = value;
                crawler.StopCrawling = value;
            }
            get { return blnAsyncCancelled; }
        }

        public string LogFile
        {
            set { strLogFile = value; }
            get { return strLogFile; }
        }

        public string LogMessage
        {
            get { return strLogMessage; }
        }

        public bool Suspending
        {
            get { return blnSuspending; }
            set { blnSuspending = value; }
        }

        public BackgroundWorker AsyncWorker
        { set { bwAsync = value; } }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="strLog">日志内容</param>
        protected void Log(string strLog)
        {
            strLogMessage = DateTime.Now.ToString() + " " + strLog;
            StreamWriter swComment = File.AppendText(strLogFile);
            swComment.WriteLine(strLogMessage);
            swComment.Close();

            bwAsync.ReportProgress(0);
            Thread.Sleep(GlobalPool.SleepMsForThread);
        }

        //检查真实请求限制剩余次数，并根据情况调整访问频度并返回
        //2011-02-23 改为间隔下限为500ms
        //2011-05-24 改为间隔下限为1s
        //except user relation robot, others get and record the reset time only
        protected void AdjustRealFreq()
        {
            if (api == null) return;
            JsonRateLimit oJsonRateLimit = api.API.Account_Rate_Limit_Status();
            if (oJsonRateLimit == null) return;
            int iResetTimeInSeconds = oJsonRateLimit.reset_time_in_seconds;
            if (iResetTimeInSeconds < 0) iResetTimeInSeconds = 0;
            int iRemainingIPHits = oJsonRateLimit.remaining_ip_hits;
            if (iRemainingIPHits < 0) iRemainingIPHits = 0;
            int iRemainingUserHits = oJsonRateLimit.remaining_user_hits;
            if (iRemainingUserHits < 0) iRemainingUserHits = 0;

            if (api != null)
            {
                api.ResetTime = DateTime.Now;
                api.RemainingIPHits = iRemainingIPHits;
                api.RemainingUserHits = iRemainingUserHits;
                api.ResetTimeInSeconds = iResetTimeInSeconds;
            }
        }

        //从GlobalPool中检查请求限制剩余次数，并根据情况调整访问频度并返回
        //2011-02-23 改为间隔下限为500ms
        //2011-05-24 改为间隔下限为1s
        //except user relation robot, others get and record the reset time only
        protected void AdjustFreq()
        {
            if (api != null)
            {
                api.RemainingIPHits--;
                if (api.RemainingIPHits < 0) api.RemainingIPHits = 0;
                api.RemainingUserHits--;
                if (api.RemainingUserHits < 0) api.RemainingUserHits = 0;
                api.ResetTimeInSeconds = api.ResetTimeInSeconds - Convert.ToInt32((DateTime.Now - api.ResetTime).TotalSeconds);
                if (api.ResetTimeInSeconds < 0) api.ResetTimeInSeconds = 0;
                api.ResetTime = DateTime.Now;
                if (api.ResetTimeInSeconds % 100 == 0 || api.RemainingIPHits % 15 == 0 || api.RemainingUserHits % 10 == 0 || api.ResetTimeInSeconds <= 0 || api.RemainingIPHits < 0 || api.RemainingUserHits<0) AdjustRealFreq();
            }
        }

        //set the frequency to crawler
        protected void SetCrawlerFreq()
        {
            if (api != null)
            {
                int iSleep = api.ResetTimeInSeconds * 1000;
                if (iSleep < iMinSleep) iSleep = iMinSleep;
                int i=0, j=0;
                if (api.RemainingIPHits == 0 || api.RemainingUserHits == 0) iSleep = api.ResetTimeInSeconds * 1000;
                else
                {
                    if (api.RemainingIPHits > 0) i = Convert.ToInt32(api.ResetTimeInSeconds * 1000 / api.RemainingIPHits);
                    if (api.RemainingUserHits > 0) j = Convert.ToInt32(api.ResetTimeInSeconds * 1000 / api.RemainingUserHits);
                    iSleep = Math.Max(i, j);
                }
                if (iSleep < iMinSleep) iSleep = iMinSleep; //sleep at least 1s
                crawler.SleepTime = iSleep;
            }
        }

        public virtual void Initialize() { }
    }
}
