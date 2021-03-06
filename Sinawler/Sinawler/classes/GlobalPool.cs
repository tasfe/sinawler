﻿using System;
using System.Collections.Generic;
using System.Text;
//using Sina.Api;
using Open.Sina2SDK;
using Sinawler.Model;

namespace Sinawler
{
    public class APIInfo
    {
        //public SinaApiService API = new SinaApiService();
        public SinaSerive API = new SinaSerive();
        public int IPLimit=1000;
        public int RemainingIPHits=1000;
        public int RemainingUserHits=150;
        public DateTime ResetTime = DateTime.Now;
        public int ResetTimeInSeconds = 3600;
    }

    static public class GlobalPool
    {
        private static APIInfo ApiForUserRelation = new APIInfo();
        private static APIInfo ApiForUserInfo = new APIInfo();
        private static APIInfo ApiForUserTag = new APIInfo();
        private static APIInfo ApiForStatus = new APIInfo();
        private static APIInfo ApiForComment = new APIInfo();
        public static Object Lock = new Object();       //用于进程间同步的锁，注意一定要在队列初始化之前，因为队列要用它

        public static UserQueue UserQueueForUserInfoRobot = new UserQueue(QueueBufferFor.USER_INFO);  //用户信息机器人使用的用户队列
        public static UserQueue UserQueueForUserRelationRobot = new UserQueue(QueueBufferFor.USER_RELATION);  //用户关系机器人使用的用户队列
        public static UserQueue UserQueueForUserTagRobot = new UserQueue(QueueBufferFor.USER_TAG);  //用户标签机器人使用的用户队列
        public static UserQueue UserQueueForStatusRobot = new UserQueue(QueueBufferFor.STATUS);  //微博机器人使用的用户队列
        public static StatusQueue StatusQueue = new StatusQueue();  //微博队列

        public static bool UserInfoRobotEnabled = true;
        public static bool TagRobotEnabled = true;
        public static bool StatusRobotEnabled = true;
        public static bool CommentRobotEnabled = true;

        public static int MinSleepMsForUserRelation = 500;
        public static int MinSleepMsForUserInfo = 500;
        public static int MinSleepMsForUserTag = 500;
        public static int MinSleepMsForStatus = 500;
        public static int MinSleepMsForComment = 500;

        public static int SleepMsForThread = 1;

        public static long TimeStamp = Math.Abs(DateTime.Now.ToBinary());
        public static string UserRelationBufferTable = "queue_buffer_for_userRelation" + GlobalPool.TimeStamp.ToString();
        public static string UserInfoBufferTable = "queue_buffer_for_userInfo" + GlobalPool.TimeStamp.ToString();
        public static string UserTagBufferTable = "queue_buffer_for_tag" + GlobalPool.TimeStamp.ToString();
        public static string StatusBufferTable = "queue_buffer_for_status" + GlobalPool.TimeStamp.ToString();
        public static string CommentBufferTable = "queue_buffer_for_comment" + GlobalPool.TimeStamp.ToString();
        public static string SysArgsTable = "sys_args" + GlobalPool.TimeStamp.ToString();

        public static APIInfo GetAPI(SysArgFor apiType)
        {
            switch (apiType)
            {
                case SysArgFor.USER_RELATION:
                    ApiForUserRelation.API.Redirect_Uri = "http://weibo.com/sizheng";
                    return ApiForUserRelation;
                case SysArgFor.USER_INFO:
                    ApiForUserInfo.API.Redirect_Uri = "http://weibo.com/sizheng";
                    return ApiForUserInfo;
                case SysArgFor.USER_TAG:
                    ApiForUserTag.API.Redirect_Uri = "http://weibo.com/sizheng";
                    return ApiForUserTag;
                case SysArgFor.STATUS:
                    ApiForStatus.API.Redirect_Uri = "http://weibo.com/sizheng";
                    return ApiForStatus;
                case SysArgFor.COMMENT:
                    ApiForComment.API.Redirect_Uri = "http://weibo.com/sizheng";
                    return ApiForComment;
                default:
                    return null;
            }
        }
    }
}
